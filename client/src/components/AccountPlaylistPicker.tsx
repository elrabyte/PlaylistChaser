import { Alert, Anchor, Loader, Select, Text } from "@mantine/core";
import { useQueries, useQuery } from "@tanstack/react-query";
import { Link } from "react-router";
import { api, SOURCE_LABEL } from "../api";

/** Lists the playlists of the connected accounts (private ones and liked songs too); the value is the playlist's link. */
export function AccountPlaylistPicker({ value, onChange }: { value: string; onChange: (url: string) => void }) {
  const accounts = useQuery({ queryKey: ["sources"], queryFn: api.sources });
  const connected = accounts.data?.filter((a) => a.connected && !a.needsReconnect).map((a) => a.source) ?? [];
  const lists = useQueries({
    queries: connected.map((source) => ({ queryKey: ["myPlaylists", source], queryFn: () => api.myPlaylists(source), staleTime: 60_000 })),
  });

  if (accounts.isPending) return <Loader size="sm" />;
  if (connected.length === 0)
    return <Text size="sm">Connect YouTube or Spotify in <Anchor component={Link} to="/settings">Settings</Anchor> to pick from your playlists.</Text>;

  const data = connected.map((source, i) => ({
    group: SOURCE_LABEL[source],
    items: (lists[i].data ?? []).map((p) => ({ value: p.url, label: p.isMine || !p.creator ? p.name : `${p.name} (${p.creator})` })),
  }));

  return (
    <>
      <Select label="Your playlists" placeholder={lists.some((l) => l.isPending) ? "Loading…" : "Search your playlists"}
        data={data} value={value || null} onChange={(v) => onChange(v ?? "")} searchable nothingFoundMessage="No match"
        rightSection={lists.some((l) => l.isPending) ? <Loader size="xs" /> : undefined} data-autofocus />
      {lists.map((l, i) => l.error && (
        <Alert key={connected[i]} color="orange" title={SOURCE_LABEL[connected[i]]}>{l.error.message}</Alert>
      ))}
    </>
  );
}
