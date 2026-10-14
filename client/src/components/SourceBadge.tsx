import { Badge } from "@mantine/core";
import { IconBrandSpotify, IconBrandYoutube } from "@tabler/icons-react";
import { SOURCE_COLOR, SOURCE_LABEL, type LinkStatus, type Source } from "../api";

export function SourceIcon({ source, size = 16 }: { source: Source; size?: number }) {
  return source === "youtube" ? <IconBrandYoutube size={size} /> : <IconBrandSpotify size={size} />;
}

export function SourceBadge({ source, variant = "light" }: { source: Source; variant?: string }) {
  return (
    <Badge color={SOURCE_COLOR[source]} variant={variant} leftSection={<SourceIcon source={source} size={12} />}>
      {SOURCE_LABEL[source]}
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
