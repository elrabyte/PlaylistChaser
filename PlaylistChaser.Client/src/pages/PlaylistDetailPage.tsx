import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { api, type Playlist, type PlaylistSong } from "../api";

export function PlaylistDetailPage() {
  const { id } = useParams<{ id: string }>();
  const playlistId = Number(id);
  const [playlist, setPlaylist] = useState<Playlist | null>(null);
  const [songs, setSongs] = useState<PlaylistSong[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!playlistId) return;
    Promise.all([api.getPlaylist(playlistId), api.getPlaylistSongs(playlistId)])
      .then(([p, s]) => {
        setPlaylist(p);
        setSongs(s);
      })
      .catch(() => setError("Could not load this playlist."));
  }, [playlistId]);

  return (
    <div className="page">
      <header className="page__header">
        <Link to="/playlists" className="link-button">
          &larr; Back
        </Link>
      </header>

      {error && <p className="error">{error}</p>}
      {!error && !playlist && <p className="muted">Loading...</p>}

      {playlist && (
        <>
          <h1>{playlist.name}</h1>
          <p className="muted">{playlist.channelName}</p>
          {playlist.description && <p>{playlist.description}</p>}

          <ul className="song-list">
            {songs?.map((s) => (
              <li key={s.playlistSongId} className="song-list__item">
                <span className="song-list__name">{s.songName}</span>
                {s.artistName && <span className="song-list__artist">{s.artistName}</span>}
              </li>
            ))}
          </ul>
          {songs?.length === 0 && <p className="muted">No songs in this playlist yet.</p>}
        </>
      )}
    </div>
  );
}
