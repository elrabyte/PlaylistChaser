import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { api, type Playlist } from "../api";
import { useAuth } from "../AuthContext";

export function PlaylistsPage() {
  const { userName, logout } = useAuth();
  const [playlists, setPlaylists] = useState<Playlist[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api
      .getPlaylists()
      .then(setPlaylists)
      .catch(() => setError("Could not load playlists."));
  }, []);

  return (
    <div className="page">
      <header className="page__header">
        <h1>Playlists</h1>
        <div>
          <span className="muted">{userName}</span>
          <button className="link-button" onClick={logout}>
            Sign out
          </button>
        </div>
      </header>

      {error && <p className="error">{error}</p>}
      {!error && !playlists && <p className="muted">Loading...</p>}
      {playlists && playlists.length === 0 && <p className="muted">No playlists yet.</p>}

      <ul className="playlist-list">
        {playlists?.map((p) => (
          <li key={p.id}>
            <Link to={`/playlists/${p.id}`} className="playlist-card">
              <span className="playlist-card__name">{p.name}</span>
              <span className="playlist-card__meta">
                {p.playlistTypeName} - {p.songsTotal} songs
              </span>
            </Link>
          </li>
        ))}
      </ul>
    </div>
  );
}
