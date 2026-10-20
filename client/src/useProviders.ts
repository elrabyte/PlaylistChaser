import { useQuery } from "@tanstack/react-query";
import { useMemo } from "react";
import { api, type Capability, type Source } from "./api";

/** The providers the server offers, with display helpers. Adding a provider on the server makes it appear everywhere. */
export function useProviders() {
  const { data, isPending } = useQuery({ queryKey: ["sources"], queryFn: api.sources, staleTime: 60_000 });
  return useMemo(() => {
    const list = data ?? [];
    const info = (s: Source) => list.find((p) => p.source === s);
    return {
      list,
      isPending,
      label: (s: Source) => info(s)?.name ?? s,
      color: (s: Source) => info(s)?.color ?? "gray",
      can: (s: Source, capability: Capability) => !!info(s)?.capabilities.includes(capability),
      usable: (s: Source) => !!info(s)?.connected && !info(s)?.needsReconnect,
    };
  }, [data, isPending]);
}
