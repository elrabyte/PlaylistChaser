import { Alert, Anchor, Loader, Select, Text } from "@mantine/core";
import { useQueries } from "@tanstack/react-query";
import { Link } from "react-router";
import { api } from "../api";
import { useProviders } from "../useProviders";

/** Lists the playlists of the connected accounts (private ones and liked songs too); the value is the playlist's link. */
export function AccountPlaylistPicker({ value, onChange }: { value: string; onChange: (url: string) => void }) {
  const providers = useProviders();
  const connected = providers.list.map((p) => p.source).filter((s) => providers.can(s, "source") && providers.usable(s));
  const lists = useQueries({
    queries: connected.map((source) => ({ queryKey: ["myPlaylists", source], queryFn: () => api.myPlaylists(source), staleTime: 60_000 })),
  });

  if (providers.isPending) return <Loader size="sm" />;
  if (connected.length === 0)
    return <Text size="sm">Connect YouTube or Spotify in <Anchor component={Link} to="/settings">Settings</Anchor> to pick from your playlists.</Text>;

  const data = connected.map((source, i) => ({
    group: providers.label(source),
    items: (lists[i].data ?? []).map((p) => ({ value: p.url, label: p.isMine || !p.creator ? p.name : `${p.name} (${p.creator})` })),
  }));

  return (
    <>
      <Select label="Your playlists" placeholder={lists.some((l) => l.isPending) ? "Loading…" : "Search your playlists"}
        data={data} value={value || null} onChange={(v) => onChange(v ?? "")} searchable nothingFoundMessage="No match"
        rightSection={lists.some((l) => l.isPending) ? <Loader size="xs" /> : undefined} data-autofocus />
      {lists.map((l, i) => l.error && (
        <Alert key={connected[i]} color="orange" title={providers.label(connected[i])}>{l.error.message}</Alert>
      ))}
    </>
  );
}
