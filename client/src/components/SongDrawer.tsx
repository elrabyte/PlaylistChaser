import { Anchor, Badge, Button, Divider, Drawer, Group, Stack, Text, TextInput } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { api, type Song, type Source } from "../api";
import { useProviders } from "../useProviders";
import { SourceIcon, STATUS } from "./SourceBadge";

/** Shows how a song maps to each service and lets the user correct it. */
export function SongDrawer({ song, playlistId, onClose }: { song: Song | null; playlistId: number; onClose: () => void }) {
  const isMobile = useMediaQuery("(max-width: 48em)");
  const providers = useProviders();
  return (
    <Drawer opened={!!song} onClose={onClose} position={isMobile ? "bottom" : "right"} size={isMobile ? "auto" : "md"} title={song?.name}>
      {song && (
        <Stack>
          <Text c="dimmed" size="sm">{song.artist}</Text>
          {providers.list.map(({ source: s }) => <SourceRow key={`${song.id}-${s}`} song={song} source={s} playlistId={playlistId} onDone={onClose} />)}
        </Stack>
      )}
    </Drawer>
  );
}

function SourceRow({ song, source, playlistId, onDone }: { song: Song; source: Source; playlistId: number; onDone: () => void }) {
  const queryClient = useQueryClient();
  const label = useProviders().label(source);
  const [url, setUrl] = useState("");
  const link = song.links.find((l) => l.source === source);
  const status = STATUS[link?.status ?? "missing"];
  const done = () => { queryClient.invalidateQueries({ queryKey: ["songs", playlistId] }); onDone(); };
  const set = useMutation({ mutationFn: (body: { url?: string; notFound?: boolean }) => api.setSongLink(song.id, source, body), onSuccess: done });
  const reset = useMutation({ mutationFn: () => api.resetSongLink(song.id, source), onSuccess: done });

  return (
    <>
      <Divider />
      <Group justify="space-between">
        <Group gap="xs"><SourceIcon source={source} /><Text fw={500}>{label}</Text></Group>
        <Badge color={status.color} variant="light">{status.label}</Badge>
      </Group>
      {link?.url && <Anchor href={link.url} target="_blank" rel="noreferrer" size="sm">Open on {label}</Anchor>}
      {link?.status !== "origin" && (
        <>
          <form onSubmit={(e) => { e.preventDefault(); set.mutate({ url: url.trim() }); }}>
            <Group align="end" wrap="nowrap">
              <TextInput label={`Wrong or missing? Paste the right ${label} link`} value={url}
                onChange={(e) => setUrl(e.currentTarget.value)} style={{ flex: 1 }} required />
              <Button type="submit" loading={set.isPending}>Save</Button>
            </Group>
          </form>
          <Group gap="xs">
            <Button variant="default" size="xs" onClick={() => set.mutate({ notFound: true })}>Not on {label}</Button>
            {link && <Button variant="default" size="xs" onClick={() => reset.mutate()} loading={reset.isPending}>Search again on next copy</Button>}
          </Group>
        </>
      )}
    </>
  );
}
