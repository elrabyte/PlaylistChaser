const rtf = new Intl.RelativeTimeFormat(undefined, { numeric: "auto" });
const UNITS: [Intl.RelativeTimeFormatUnit, number][] = [["day", 86400], ["hour", 3600], ["minute", 60]];

/** "3 hours ago", "yesterday", or "never". */
export function ago(iso: string | null): string {
  if (!iso) return "never";
  const seconds = (new Date(iso).getTime() - Date.now()) / 1000;
  for (const [unit, size] of UNITS)
    if (Math.abs(seconds) >= size) return rtf.format(Math.round(seconds / size), unit);
  return "just now";
}
