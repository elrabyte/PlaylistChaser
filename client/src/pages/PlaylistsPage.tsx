import { Alert, Anchor, AspectRatio, Button, Card, Center, Checkbox, Group, Image, Loader, Modal, SimpleGrid, Stack, Text, TextInput, Title } from "@mantine/core";
import { IconArrowsJoin, IconMusic, IconPlus, IconRefresh } from "@tabler/icons-react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { Link, useLocation, useNavigate } from "react-router";
import { api, type Playlist } from "../api";
import { useJobStarted } from "../components/JobsIndicator";
import { SourceBadge } from "../components/SourceBadge";
import { notifications } from "@mantine/notifications";

export function PlaylistsPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const jobStarted = useJobStarted();
  const playlists = useQuery({ queryKey: ["playlists"], queryFn: api.playlists });
  const sharedUrl = (location.state as { addUrl?: string } | null)?.addUrl;
  const [adding, setAdding] = useState(!!sharedUrl);
  const [url, setUrl] = useState(sharedUrl ?? "");
  const [combining, setCombining] = useState<number[] | null>(null);
  const [combinedName, setCombinedName] = useState("");

  const add = useMutation({
    mutationFn: () => api.addPlaylist(url.trim()),
    onSuccess: ({ id }) => { jobStarted(); navigate(`/playlists/${id}`); },
    meta: { silent: true },
  });
  const combine = useMutation({
    mutationFn: () => api.combine(combinedName.trim(), combining ?? []),
    onSuccess: ({ id }) => { jobStarted(); navigate(`/playlists/${id}`); },
  });
  const syncAll = useMutation({
    mutationFn: api.syncAll,
    onSuccess: () => { jobStarted(); notifications.show({ message: "Syncing all playlists in the background." }); },
  });

  const toggle = (id: number) => setCombining((ids) => (ids!.includes(id) ? ids!.filter((x) => x !== id) : [...ids!, id]));

  return (
    <Stack>
      <Group justify="space-between">
        <Title order={2}>Playlists</Title>
        <Group gap="xs">
          <Button leftSection={<IconPlus size={18} />} onClick={() => setAdding(true)}>Add</Button>
          <Button variant="default" leftSection={<IconArrowsJoin size={18} />} onClick={() => setCombining(combining ? null : [])}>
            {combining ? "Cancel" : "Combine"}
          </Button>
          <Button variant="default" leftSection={<IconRefresh size={18} />} onClick={() => syncAll.mutate()} loading={syncAll.isPending}>
            Sync all
          </Button>
        </Group>
      </Group>

      {combining && (
        <Card withBorder>
          <form onSubmit={(e) => { e.preventDefault(); combine.mutate(); }}>
            <Group align="end">
              <TextInput label={`Combine ${combining.length} selected playlists into`} placeholder="New playlist name"
                value={combinedName} onChange={(e) => setCombinedName(e.currentTarget.value)} style={{ flex: 1 }} required />
              <Button type="submit" disabled={combining.length < 2} loading={combine.isPending}>Create</Button>
            </Group>
          </form>
        </Card>
      )}

      {playlists.isPending && <Center p="xl"><Loader /></Center>}
      {playlists.data?.length === 0 && (
        <Alert title="No playlists yet">
          First connect YouTube and/or Spotify in <Anchor component={Link} to="/settings">Settings</Anchor>, then
          tap <b>Add</b> and paste a playlist link. On Android you can also share a playlist from the YouTube or Spotify app
          to PlaylistChaser once it's installed.
        </Alert>
      )}

      <SimpleGrid cols={{ base: 1, xs: 2, md: 3, xl: 4 }}>
        {playlists.data?.map((p) => (
          <PlaylistCard key={p.id} playlist={p}
            selectable={!!combining && p.kind === "simple"} selected={!!combining?.includes(p.id)}
            onClick={() => (combining ? p.kind === "simple" && toggle(p.id) : navigate(`/playlists/${p.id}`))} />
        ))}
      </SimpleGrid>

      <Modal opened={adding} onClose={() => setAdding(false)} title="Add a playlist">
        <form onSubmit={(e) => { e.preventDefault(); add.mutate(); }}>
          <Stack>
            <TextInput label="YouTube or Spotify playlist link" placeholder="https://open.spotify.com/playlist/…"
              value={url} onChange={(e) => setUrl(e.currentTarget.value)} required data-autofocus />
            {add.error && <Alert color="red">{add.error.message}</Alert>}
            <Button type="submit" loading={add.isPending}>Add and import songs</Button>
          </Stack>
        </form>
      </Modal>
    </Stack>
  );
}

function PlaylistCard({ playlist: p, selectable, selected, onClick }: { playlist: Playlist; selectable: boolean; selected: boolean; onClick: () => void }) {
  return (
    <Card withBorder padding="sm" onClick={onClick} style={{ cursor: "pointer", outline: selected ? "2px solid var(--mantine-color-blue-filled)" : undefined }}
      component="button" type="button" aria-pressed={selectable ? selected : undefined}>
      <Card.Section>
        <AspectRatio ratio={16 / 9}>
          {p.thumbnailUrl
            ? <Image src={p.thumbnailUrl} alt="" />
            : <Center bg="var(--mantine-color-default-hover)"><IconMusic size={48} opacity={0.4} /></Center>}
        </AspectRatio>
      </Card.Section>
      <Group justify="space-between" mt="sm" wrap="nowrap" align="start">
        <Stack gap={2} style={{ minWidth: 0, textAlign: "left" }}>
          <Text fw={600} truncate>{p.name}</Text>
          <Text size="sm" c="dimmed" truncate>
            {p.kind === "combined" ? "Combined" : p.creator} · {p.songCount} songs
          </Text>
        </Stack>
        {selectable && <Checkbox checked={selected} readOnly tabIndex={-1} aria-hidden />}
      </Group>
      <Group gap={4} mt="xs">
        {p.mainSource && <SourceBadge source={p.mainSource} variant="filled" />}
        {p.links.filter((l) => l.source !== p.mainSource).map((l) => <SourceBadge key={l.source} source={l.source} />)}
      </Group>
    </Card>
  );
}
