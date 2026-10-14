import { useEffect } from "react";
import { Navigate, useNavigate, useSearchParams } from "react-router";

/** Target of the Android share sheet: picks the link out of what was shared and opens "Add playlist" with it. */
export function SharePage() {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  // apps put the link in "url" or somewhere inside "text"
  const shared = [params.get("url"), params.get("text"), params.get("title")].join(" ");
  const addUrl = shared.match(/https?:\/\/\S+/)?.[0];

  useEffect(() => {
    if (addUrl) navigate("/", { replace: true, state: { addUrl } });
  }, [addUrl, navigate]);

  return addUrl ? null : <Navigate to="/" replace />;
}
