import { Alert, Badge, Button, Card, Center, Code, Collapse, Group, Loader, SegmentedControl, Stack, Text, Title, UnstyledButton } from "@mantine/core";
import { IconAlertTriangle, IconChevronDown, IconChevronRight, IconRefresh } from "@tabler/icons-react";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { api, TASK_LABEL, type Job, type JobLogLine, type Me, type ServerLogLevel, type StatusInfo } from "../api";
import { useJobTitle } from "../components/JobsIndicator";
import { SourceBadge } from "../components/SourceBadge";
import { ago } from "../format";

const RESULT_COLOR = { queued: "gray", running: "blue", done: "green", failed: "red" } as const;
const LINE_COLOR = { info: "dimmed", warn: "orange", error: "red" } as const;

export function StatusPage({ me }: { me: Me }) {
  // the Tasks page and the jobs drawer share the ["jobs"] query, so this also refreshes while a job runs
  const status = useQuery({ queryKey: ["status"], queryFn: api.status, refetchInterval: 15_000 });
  const jobs = useQuery({ queryKey: ["jobs"], queryFn: api.jobs, refetchInterval: (q) => (q.state.data?.some((j) => j.status === "running") ? 2000 : 30_000) });

  return (
    <Stack maw={820}>
      <Title order={2}>Status</Title>
      {status.isPending && <Center p="xl"><Loader /></Center>}

      {status.data?.limit && (
        <Alert color="orange" icon={<IconAlertTriangle size={18} />} title="Daily limit reached">
          {status.data.limit.message} It resets around {new Date(status.data.limit.resetsAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}.
          The next scheduled copy continues on its own.
        </Alert>
      )}

      {status.data && <Schedule status={status.data} />}
      {status.data && <Playlists status={status.data} />}
      <History jobs={jobs.data ?? []} pending={jobs.isPending} />
      {me.isAdmin && <ServerLog />}
    </Stack>
  );
}

function Schedule({ status }: { status: StatusInfo }) {
  return (
    <Card withBorder>
      <Stack gap="sm">
        <Title order={4}>Scheduled tasks</Title>
        {status.tasks.length === 0 && <Text c="dimmed" size="sm">Nothing is scheduled. Add tasks on the Tasks page.</Text>}
        {status.tasks.map((t) => (
          <Group key={t.id} justify="space-between" wrap="nowrap" align="start">
            <Stack gap={0} style={{ minWidth: 0 }}>
              <Text fw={500}>{TASK_LABEL[t.kind]}</Text>
              <Text size="sm" c="dimmed">
                {t.enabled ? `next ${t.nextRunAt ? ago(t.nextRunAt) : "soon"}` : "off"} · last run {ago(t.lastRunAt)}
              </Text>
              {t.lastResult?.status === "failed" && t.lastResult.message && <Text size="sm" c="red">{t.lastResult.message}</Text>}
            </Stack>
            {t.lastResult
              ? <Badge color={RESULT_COLOR[t.lastResult.status]} variant="light" style={{ flexShrink: 0 }}>{t.lastResult.status}</Badge>
              : <Badge color="gray" variant="light" style={{ flexShrink: 0 }}>never</Badge>}
          </Group>
        ))}
      </Stack>
    </Card>
  );
}

function Playlists({ status }: { status: StatusInfo }) {
  return (
    <Card withBorder>
      <Stack gap="sm">
        <Title order={4}>Playlists</Title>
        {status.playlists.length === 0 && <Text c="dimmed" size="sm">No playlists yet.</Text>}
        {status.playlists.map((p) => (
          <Stack key={p.id} gap={4}>
            <Group justify="space-between" wrap="nowrap">
              <Text fw={500} truncate>{p.name}</Text>
              <Text size="sm" c="dimmed">{p.songCount} songs</Text>
            </Group>
            <Group gap="xs" wrap="wrap">
              {p.mainSource && (
                <Group gap={6}><SourceBadge source={p.mainSource} variant="filled" /><Text size="sm" c="dimmed">updated {ago(p.lastPulledAt)}</Text></Group>
              )}
              {p.links.filter((l) => l.source !== p.mainSource).map((l) => (
                <Group key={l.source} gap={6}>
                  <SourceBadge source={l.source} />
                  <Text size="sm" c="dimmed">{l.pushed} / {p.songCount} copied · {ago(l.lastPushedAt)}</Text>
                </Group>
              ))}
            </Group>
          </Stack>
        ))}
      </Stack>
    </Card>
  );
}

function History({ jobs, pending }: { jobs: Job[]; pending: boolean }) {
  return (
    <Card withBorder>
      <Stack gap="xs">
        <Title order={4}>History</Title>
        {pending && <Loader size="sm" />}
        {!pending && jobs.length === 0 && <Text c="dimmed" size="sm">Nothing has run yet.</Text>}
        {jobs.map((j) => <JobRow key={j.id} job={j} />)}
      </Stack>
    </Card>
  );
}

function JobRow({ job }: { job: Job }) {
  const title = useJobTitle();
  const [open, setOpen] = useState(false);
  const logs = useQuery({ queryKey: ["jobLogs", job.id], queryFn: () => api.jobLogs(job.id), enabled: open, refetchInterval: job.status === "running" ? 2000 : false });

  return (
    <Stack gap={4}>
      <UnstyledButton onClick={() => setOpen(!open)} aria-expanded={open}>
        <Group justify="space-between" wrap="nowrap">
          <Group gap={6} wrap="nowrap" style={{ minWidth: 0 }}>
            {open ? <IconChevronDown size={16} /> : <IconChevronRight size={16} />}
            <Text truncate>{title(job)}</Text>
          </Group>
          <Group gap="xs" wrap="nowrap">
            <Text size="xs" c="dimmed">{ago(job.finishedAt ?? job.createdAt)}</Text>
            <Badge color={RESULT_COLOR[job.status]} variant="light" style={{ flexShrink: 0 }}>{job.status}</Badge>
          </Group>
        </Group>
      </UnstyledButton>
      <Collapse expanded={open}>
        <Stack gap={2} pl={22} pb="xs">
          {logs.isPending && <Loader size="xs" />}
          {logs.data?.length === 0 && <Text size="sm" c="dimmed">No details were recorded.</Text>}
          {logs.data?.map((l) => <LogLine key={l.id} line={l} />)}
        </Stack>
      </Collapse>
    </Stack>
  );
}

function LogLine({ line }: { line: JobLogLine }) {
  return (
    <Text size="sm" c={LINE_COLOR[line.level]}>
      <Text span size="xs" c="dimmed">{new Date(line.at).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit", second: "2-digit" })} </Text>
      {line.message}
    </Text>
  );
}

function ServerLog() {
  const [level, setLevel] = useState<ServerLogLevel>("warning");
  const logs = useQuery({ queryKey: ["serverLogs", level], queryFn: () => api.serverLogs(level) });

  return (
    <Card withBorder>
      <Stack gap="sm">
        <Group justify="space-between">
          <Title order={4}>Server log</Title>
          <Button variant="default" size="xs" leftSection={<IconRefresh size={14} />} onClick={() => logs.refetch()} loading={logs.isFetching}>Reload</Button>
        </Group>
        <Text size="sm" c="dimmed">The most recent 1000 lines since the server started. Only admins see this.</Text>
        <SegmentedControl size="xs" value={level} onChange={(v) => setLevel(v as ServerLogLevel)}
          data={[{ value: "error", label: "Errors" }, { value: "warning", label: "Warnings" }, { value: "information", label: "Everything" }]} />
        {logs.data?.length === 0 && <Text size="sm" c="dimmed">Nothing at this level.</Text>}
        <Stack gap={4} mah={360} style={{ overflow: "auto" }}>
          {logs.data?.map((l, i) => (
            <Text key={i} size="xs" ff="monospace" c={l.level === "information" ? "dimmed" : l.level === "warning" ? "orange" : "red"} style={{ overflowWrap: "anywhere" }}>
              <Code>{new Date(l.at).toLocaleTimeString()}</Code> {l.message}
            </Text>
          ))}
        </Stack>
      </Stack>
    </Card>
  );
}
