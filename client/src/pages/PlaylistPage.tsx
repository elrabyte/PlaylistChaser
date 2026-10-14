import { ActionIcon, Alert, Anchor, Avatar, Badge, Button, Card, Center, Group, Image, Loader, Menu, Modal, Stack, Text, TextInput, Textarea, Title, UnstyledButton } from "@mantine/core";
import { IconArrowLeft, IconDots, IconDownload, IconExternalLink, IconLink, IconMusic, IconPencil, IconSearch, IconTrash, IconUpload } from "@tabler/icons-react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { notifications } from "@mantine/notifications";
import { useState } from "react";
import { Link, useNavigate, useParams } from "react-router";
import { api, SOURCE_LABEL, SOURCES, type Playlist, type Song, type Source } from "../api";
import { useJobStarted } from "../components/JobsIndicator";
import { SongDrawer } from "../components/SongDrawer";
import { SourceBadge, SourceIcon, STATUS } from "../components/SourceBadge";
import { ago } from "../format";

export function PlaylistPage() {
  const id = Number(useParams().id);
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const jobStarted = useJobStarted();
  const detail = useQuery({ queryKey: ["playlist", id], queryFn: () => api.playlist(id) });
  const songs = useQuery({ queryKey: ["songs", id], queryFn: () => api.songs(id) });
  const accounts = useQuery({ queryKey: ["sources"], queryFn: api.sources });
  const allPlaylists = useQuery({ queryKey: ["playlists"], queryFn: api.playlists });
  const [filter, setFilter] = useState("");
  const [openSong, setOpenSong] = useState<Song | null>(null);
  const [editing, setEditing] = useState(false);
  const [linking, setLinking] = useState(false);

  const refresh = () => {
    queryClient.invalidateQueries({ queryKey: ["playlist", id] });
    queryClient.invalidateQueries({ queryKey: ["playlists"] });
  };
  const started = (message: string) => () => { jobStarted(); notifications.show({ message }); };
  const pull = useMutation({ mutationFn: () => api.pull(id), onSuccess: started("Updating songs in the background.") });
  const push = useMutation({
    mutationFn: (s: Source) => api.push(id, s),
    onSuccess: (_, s) => started(`Copying to ${SOURCE_LABEL[s]} in the background.`)(),
  });
  const unlink = useMutation({
    mutationFn: ({ source, deleteRemote }: { source: Source; deleteRemote: boolean }) => api.unlink(id, source, deleteRemote),
    onSuccess: refresh,
  });
  const remove = useMutation({
    mutationFn: () => api.deletePlaylist(id),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ["playlists"] }); navigate("/"); },
  });

  if (detail.isPending) return <Center p="xl"><Loader /></Center>;
  if (detail.isError) return <Alert color="red">{detail.error.message}</Alert>;
  const p = detail.data.playlist;
  const connected = (s: Source) => accounts.data?.find((a) => a.source === s)?.connected && !accounts.data?.find((a) => a.source === s)?.needsReconnect;
  const pushTargets = SOURCES.filter((s) => s !== p.mainSource);
  const members = allPlaylists.data?.filter((x) => detail.data.members.includes(x.id)) ?? [];
  const q = filter.toLowerCase();
  const visibleSongs = songs.data?.filter((s) => !q || s.name.toLowerCase().includes(q) || s.artist.toLowerCase().includes(q));

  return (
    <Stack>
      <Anchor component={Link} to="/" size="sm"><Group gap={4}><IconArrowLeft size={16} /> Playlists</Group></Anchor>

      <Group align="start" wrap="nowrap">
        {p.thumbnailUrl && <Image src={p.thumbnailUrl} alt="" w={120} h={90} radius="md" visibleFrom="xs" />}
        <Stack gap={4} style={{ minWidth: 0, flex: 1 }}>
          <Title order={2} style={{ overflowWrap: "anywhere" }}>{p.name}</Title>
          <Text c="dimmed" size="sm">
            {p.kind === "combined" ? "Combined playlist" : `by ${p.creator ?? "unknown"}`} · {p.songCount} songs · updated {ago(p.lastPulledAt)}
          </Text>
        </Stack>
        <Menu position="bottom-end">
          <Menu.Target><ActionIcon variant="default" size="lg" aria-label="More actions"><IconDots /></ActionIcon></Menu.Target>
          <Menu.Dropdown>
            <Menu.Item leftSection={<IconPencil size={16} />} onClick={() => setEditing(true)}>Rename</Menu.Item>
            <Menu.Item leftSection={<IconLink size={16} />} onClick={() => setLinking(true)}>Use an existing playlist as copy…</Menu.Item>
            <Menu.Item color="red" leftSection={<IconTrash size={16} />}
              onClick={() => window.confirm(`Delete "${p.name}" from PlaylistChaser? Nothing is deleted on YouTube or Spotify.`) && remove.mutate()}>
              Delete
            </Menu.Item>
          </Menu.Dropdown>
        </Menu>
      </Group>

      <Group gap="xs">
        <Button leftSection={<IconDownload size={18} />} onClick={() => pull.mutate()} loading={pull.isPending}>Update songs</Button>
        {pushTargets.map((s) => (
          <Button key={s} variant="default" leftSection={<IconUpload size={18} />} disabled={!connected(s)}
            onClick={() => push.mutate(s)} loading={push.isPending && push.variables === s}
            title={connected(s) ? undefined : `Connect ${SOURCE_LABEL[s]} in Settings first`}>
            Copy to {SOURCE_LABEL[s]}
          </Button>
        ))}
      </Group>

      <Card withBorder>
        <Stack gap="sm">
          {p.links.length === 0 && <Text c="dimmed" size="sm">Not on any service yet. Use "Copy to …" to create it there.</Text>}
          {p.links.map((l) => (
            <Group key={l.source} justify="space-between" wrap="nowrap">
              <Group gap="sm" wrap="nowrap" style={{ minWidth: 0 }}>
                <SourceBadge source={l.source} variant={l.source === p.mainSource ? "filled" : "light"} />
                <Text size="sm" c="dimmed" truncate>
                  {l.source === p.mainSource ? "Original" : `${l.pushed} / ${p.songCount} copied · ${ago(l.lastPushedAt)}`}
                </Text>
              </Group>
              <Group gap={4} wrap="nowrap">
                <ActionIcon variant="subtle" component="a" href={l.url} target="_blank" rel="noreferrer" aria-label={`Open on ${SOURCE_LABEL[l.source]}`}>
                  <IconExternalLink size={18} />
                </ActionIcon>
                {l.source !== p.mainSource && (
                  <Menu position="bottom-end">
                    <Menu.Target><ActionIcon variant="subtle" aria-label="Copy actions"><IconDots size={18} /></ActionIcon></Menu.Target>
                    <Menu.Dropdown>
                      <Menu.Item onClick={() => unlink.mutate({ source: l.source, deleteRemote: false })}>Stop syncing (keep it on {SOURCE_LABEL[l.source]})</Menu.Item>
                      {l.isMine && (
                        <Menu.Item color="red" onClick={() => window.confirm(`Delete this playlist on ${SOURCE_LABEL[l.source]}?`) && unlink.mutate({ source: l.source, deleteRemote: true })}>
                          Delete on {SOURCE_LABEL[l.source]}
                        </Menu.Item>
                      )}
                    </Menu.Dropdown>
                  </Menu>
                )}
              </Group>
            </Group>
          ))}
          {members.length > 0 && (
            <Text size="sm">Made from: {members.map((m, i) => <span key={m.id}>{i > 0 && ", "}<Anchor component={Link} to={`/playlists/${m.id}`}>{m.name}</Anchor></span>)}</Text>
          )}
        </Stack>
      </Card>

      <TextInput leftSection={<IconSearch size={16} />} placeholder="Filter songs" value={filter} onChange={(e) => setFilter(e.currentTarget.value)} />
      {songs.isPending && <Center><Loader /></Center>}
      {songs.data?.length === 0 && <Text c="dimmed">No songs yet. They appear after "Update songs" finishes.</Text>}
      <Stack gap={0}>
        {visibleSongs?.map((s) => (
          <UnstyledButton key={s.id} onClick={() => setOpenSong(s)} py={8} px={4} style={{ borderBottom: "1px solid var(--mantine-color-default-border)" }}>
            <Group wrap="nowrap">
              <Avatar src={s.thumbnailUrl} radius="sm"><IconMusic size={18} /></Avatar>
              <Stack gap={0} style={{ minWidth: 0, flex: 1 }}>
                <Text size="sm" fw={500} truncate>{s.name}</Text>
                <Text size="xs" c="dimmed" truncate>{s.artist}</Text>
              </Stack>
              <Group gap={4} wrap="nowrap">
                {SOURCES.map((src) => {
                  const status = STATUS[s.links.find((l) => l.source === src)?.status ?? "missing"];
                  return (
                    <Badge key={src} size="lg" circle color={status.color} variant="light" title={`${SOURCE_LABEL[src]}: ${status.label}`}>
                      <SourceIcon source={src} size={14} />
                    </Badge>
                  );
                })}
              </Group>
            </Group>
          </UnstyledButton>
        ))}
      </Stack>

      <SongDrawer song={openSong} playlistId={id} onClose={() => setOpenSong(null)} />
      <EditModal playlist={p} opened={editing} onClose={() => setEditing(false)} onSaved={refresh} />
      <LinkModal playlistId={id} opened={linking} onClose={() => setLinking(false)} onSaved={refresh} />
    </Stack>
  );
}

function EditModal({ playlist, opened, onClose, onSaved }: { playlist: Playlist; opened: boolean; onClose: () => void; onSaved: () => void }) {
  const [name, setName] = useState(playlist.name);
  const [description, setDescription] = useState(playlist.description ?? "");
  const save = useMutation({
    mutationFn: () => api.editPlaylist(playlist.id, name.trim(), description || null),
    onSuccess: () => { onSaved(); onClose(); },
  });
  return (
    <Modal opened={opened} onClose={onClose} title="Rename playlist">
      <form onSubmit={(e) => { e.preventDefault(); save.mutate(); }}>
        <Stack>
          <TextInput label="Name" description="Copies get this name on their next sync." value={name} onChange={(e) => setName(e.currentTarget.value)} required />
          <Textarea label="Notes" value={description} onChange={(e) => setDescription(e.currentTarget.value)} autosize minRows={2} />
          <Button type="submit" loading={save.isPending}>Save</Button>
        </Stack>
      </form>
    </Modal>
  );
}

function LinkModal({ playlistId, opened, onClose, onSaved }: { playlistId: number; opened: boolean; onClose: () => void; onSaved: () => void }) {
  const [url, setUrl] = useState("");
  const link = useMutation({
    mutationFn: () => api.linkExisting(playlistId, url.trim()),
    onSuccess: () => { onSaved(); onClose(); },
    meta: { silent: true },
  });
  return (
    <Modal opened={opened} onClose={onClose} title="Use an existing playlist as copy">
      <form onSubmit={(e) => { e.preventDefault(); link.mutate(); }}>
        <Stack>
          <Text size="sm">Already have a copy of this playlist (for example from an older PlaylistChaser)? Link it, and syncing adds only what's missing instead of creating another one.</Text>
          <TextInput label="Link to your YouTube or Spotify playlist" value={url} onChange={(e) => setUrl(e.currentTarget.value)} required data-autofocus />
          {link.error && <Alert color="red">{link.error.message}</Alert>}
          <Button type="submit" loading={link.isPending}>Link</Button>
        </Stack>
      </form>
    </Modal>
  );
}
