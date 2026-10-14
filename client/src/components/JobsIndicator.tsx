import { ActionIcon, Badge, Drawer, Group, Indicator, Loader, Progress, Stack, Text } from "@mantine/core";
import { useDisclosure, useMediaQuery } from "@mantine/hooks";
import { IconListCheck } from "@tabler/icons-react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useRef } from "react";
import { api, SOURCE_LABEL, type Job, type Playlist } from "../api";
import { ago } from "../format";

const isActive = (j: Job) => j.status === "queued" || j.status === "running";
const STATUS_COLOR = { queued: "gray", running: "blue", done: "green", failed: "red" } as const;

/** Header button showing background jobs; polls quickly while one is running. */
export function JobsIndicator() {
  const queryClient = useQueryClient();
  const [opened, { open, close }] = useDisclosure(false);
  const isMobile = useMediaQuery("(max-width: 48em)");
  const jobs = useQuery({
    queryKey: ["jobs"],
    queryFn: api.jobs,
    refetchInterval: (q) => (q.state.data?.some(isActive) ? 2000 : 30000),
  });
  const playlists = queryClient.getQueryData<Playlist[]>(["playlists"]);
  const active = jobs.data?.filter(isActive).length ?? 0;

  // when a job finishes, the playlists it touched have changed
  const previous = useRef(active);
  useEffect(() => {
    if (active < previous.current) {
      for (const key of ["playlists", "playlist", "songs"]) queryClient.invalidateQueries({ queryKey: [key] });
    }
    previous.current = active;
  }, [active, queryClient]);

  const title = (j: Job) => {
    const name = playlists?.find((p) => p.id === j.playlistId)?.name ?? "playlist";
    if (j.kind === "pull") return `Update ${name}`;
    if (j.kind === "push") return `Copy ${name} to ${SOURCE_LABEL[j.target!]}`;
    return "Sync everything";
  };

  return (
    <>
      <Indicator label={active} size={16} disabled={active === 0}>
        <ActionIcon variant="subtle" size="lg" onClick={open} aria-label="Background jobs">
          {active > 0 ? <Loader size="sm" /> : <IconListCheck />}
        </ActionIcon>
      </Indicator>
      <Drawer opened={opened} onClose={close} title="Background jobs" position="right" size={isMobile ? "100%" : "md"}>
        <Stack>
          {jobs.data?.length === 0 && <Text c="dimmed">Nothing has run yet.</Text>}
          {jobs.data?.map((j) => (
            <Stack key={j.id} gap={4}>
              <Group justify="space-between" wrap="nowrap">
                <Text fw={500} truncate>{title(j)}</Text>
                <Badge color={STATUS_COLOR[j.status]} variant="light">{j.status}</Badge>
              </Group>
              {j.status === "running" && j.total > 0 && <Progress value={(100 * j.done) / j.total} />}
              {j.message && <Text size="sm" c={j.status === "failed" ? "red" : "dimmed"} style={{ whiteSpace: "pre-wrap" }}>{j.message}</Text>}
              <Text size="xs" c="dimmed">{j.status === "running" && j.total > 0 ? `${j.done} / ${j.total} · ` : ""}{ago(j.finishedAt ?? j.createdAt)}</Text>
            </Stack>
          ))}
        </Stack>
      </Drawer>
    </>
  );
}

/** Call after starting a job so the indicator starts polling right away. */
export function useJobStarted() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ["jobs"] });
}
