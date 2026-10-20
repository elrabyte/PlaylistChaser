import { Badge } from "@mantine/core";
import { IconBrandSpotify, IconBrandYoutube, IconMusic } from "@tabler/icons-react";
import { type LinkStatus, type Source } from "../api";
import { useProviders } from "../useProviders";

// brand icons for the providers we know; any other provider gets a generic note
const ICONS: Record<string, typeof IconMusic> = { youtube: IconBrandYoutube, spotify: IconBrandSpotify };

export function SourceIcon({ source, size = 16 }: { source: Source; size?: number }) {
  const Icon = ICONS[source] ?? IconMusic;
  return <Icon size={size} />;
}

export function SourceBadge({ source, variant = "light" }: { source: Source; variant?: string }) {
  const providers = useProviders();
  return (
    <Badge color={providers.color(source)} variant={variant} leftSection={<SourceIcon source={source} size={12} />}>
      {providers.label(source)}
    </Badge>
  );
}

export const STATUS: Record<LinkStatus | "missing", { color: string; label: string }> = {
  origin: { color: "green", label: "From here" },
  found: { color: "green", label: "Found" },
  manual: { color: "blue", label: "Set by you" },
  notFound: { color: "red", label: "Not found" },
  missing: { color: "gray", label: "Not searched yet" },
};
