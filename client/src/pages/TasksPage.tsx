import { ActionIcon, Button, Card, Center, Group, Loader, Menu, Select, Stack, Switch, Text, TextInput, Title } from "@mantine/core";
import { notifications } from "@mantine/notifications";
import { IconClockEdit, IconPlayerPlay, IconPlus, IconTrash } from "@tabler/icons-react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { api, TASK_LABEL, type ScheduledTask, type TaskInput, type TaskKind } from "../api";
import { useJobStarted } from "../components/JobsIndicator";
import { ago } from "../format";

const PRESETS = [
  { cron: "0 * * * *", label: "Every hour" },
  { cron: "0 */6 * * *", label: "Every 6 hours" },
  { cron: "0 3 * * *", label: "Every day at 03:00" },
  { cron: "0 3 * * 1", label: "Every Monday at 03:00" },
];
const KINDS = Object.entries(TASK_LABEL).map(([value, label]) => ({ value, label }));

export function TasksPage() {
  const queryClient = useQueryClient();
  const tasks = useQuery({ queryKey: ["tasks"], queryFn: api.tasks });
  const add = useMutation({
    mutationFn: () => api.addTask({ kind: "pullAll", cron: "0 */6 * * *", enabled: true }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["tasks"] }),
  });

  return (
    <Stack maw={720}>
      <Group justify="space-between">
        <Title order={2}>Tasks</Title>
        <Button leftSection={<IconPlus size={18} />} onClick={() => add.mutate()} loading={add.isPending}>Add task</Button>
      </Group>
      <Text c="dimmed" size="sm">
        Jobs that run on their own for all your playlists. The schedule is a cron expression
        (minute hour day month weekday) in the server's time zone; pick a preset with the clock button.
      </Text>
      {tasks.isPending && <Center p="xl"><Loader /></Center>}
      {tasks.data?.length === 0 && <Text c="dimmed">No tasks. Nothing runs automatically until you add one.</Text>}
      {tasks.data?.map((t) => <TaskCard key={t.id} task={t} />)}
    </Stack>
  );
}

function TaskCard({ task }: { task: ScheduledTask }) {
  const queryClient = useQueryClient();
  const jobStarted = useJobStarted();
  const [draft, setDraft] = useState<TaskInput>({ kind: task.kind, cron: task.cron, enabled: task.enabled });
  const dirty = draft.kind !== task.kind || draft.cron !== task.cron || draft.enabled !== task.enabled;
  const refresh = () => queryClient.invalidateQueries({ queryKey: ["tasks"] });

  const save = useMutation({ mutationFn: (input: TaskInput) => api.updateTask(task.id, input), onSuccess: refresh });
  const remove = useMutation({ mutationFn: () => api.deleteTask(task.id), onSuccess: refresh });
  const run = useMutation({
    mutationFn: () => api.runTask(task.id),
    onSuccess: () => { jobStarted(); refresh(); notifications.show({ message: `${TASK_LABEL[task.kind]} started.` }); },
  });

  return (
    <Card withBorder>
      <Stack gap="sm">
        <Group justify="space-between" wrap="nowrap">
          <Select data={KINDS} value={draft.kind} onChange={(v) => v && setDraft({ ...draft, kind: v as TaskKind })}
            allowDeselect={false} aria-label="What the task does" style={{ flex: 1 }} />
          {/* switching on/off saves right away; kind and schedule need Save */}
          <Switch label="On" checked={task.enabled} onChange={(e) => save.mutate({ kind: task.kind, cron: task.cron, enabled: e.currentTarget.checked })} />
        </Group>
        <Group align="end" wrap="nowrap">
          <TextInput label="Schedule (cron)" value={draft.cron} onChange={(e) => setDraft({ ...draft, cron: e.currentTarget.value })}
            styles={{ input: { fontFamily: "var(--mantine-font-family-monospace)" } }} style={{ flex: 1 }}
            rightSection={
              <Menu position="bottom-end">
                <Menu.Target><ActionIcon variant="subtle" aria-label="Schedule presets"><IconClockEdit size={18} /></ActionIcon></Menu.Target>
                <Menu.Dropdown>
                  {PRESETS.map((p) => (
                    <Menu.Item key={p.cron} onClick={() => setDraft({ ...draft, cron: p.cron })} rightSection={<Text size="xs" ff="monospace" c="dimmed">{p.cron}</Text>}>
                      {p.label}
                    </Menu.Item>
                  ))}
                </Menu.Dropdown>
              </Menu>
            } />
          {dirty && <Button onClick={() => save.mutate({ ...draft, enabled: task.enabled })} loading={save.isPending}>Save</Button>}
        </Group>
        <Group justify="space-between">
          <Text size="sm" c="dimmed">
            {task.enabled ? `Next run ${task.nextRunAt ? ago(task.nextRunAt) : "soon"}` : "Off"} · last run {ago(task.lastRunAt)}
          </Text>
          <Group gap="xs">
            <Button variant="default" size="xs" leftSection={<IconPlayerPlay size={14} />} onClick={() => run.mutate()} loading={run.isPending}>Run now</Button>
            <Button variant="subtle" color="red" size="xs" leftSection={<IconTrash size={14} />}
              onClick={() => window.confirm(`Delete "${TASK_LABEL[task.kind]}"?`) && remove.mutate()} loading={remove.isPending}>
              Delete
            </Button>
          </Group>
        </Group>
      </Stack>
    </Card>
  );
}
