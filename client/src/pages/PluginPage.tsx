import { useParams } from "react-router";

/** A plugin shows its own settings page, served through PlaylistChaser so it uses the same login. */
export function PluginPage() {
  const { id } = useParams<{ id: string }>();
  return (
    <iframe
      key={id}
      title={`Plugin ${id}`}
      src={`/plugins/${id}/`}
      style={{ width: "100%", height: "calc(100dvh - 120px)", border: 0, borderRadius: 8 }}
    />
  );
}
