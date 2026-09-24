// Thin REST client for the PlaylistChaser API (see PlaylistChaser.Web/Controllers/Api).
// The JWT is kept in localStorage; every request attaches it as a Bearer token.

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:8080";
const TOKEN_STORAGE_KEY = "playlistchaser.token";

export interface AuthResponse {
  token: string;
  userName: string;
  userId: number;
}

export interface Playlist {
  id: number;
  name: string;
  channelName: string;
  thumbnailId: number | null;
  playlistTypeId: number;
  playlistTypeName: string;
  description: string | null;
  mainSourceId: number | null;
  songsTotal: number;
}

export interface PlaylistSong {
  playlistSongId: number;
  songId: number;
  songName: string;
  artistName: string | null;
  thumbnailId: number | null;
}

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_STORAGE_KEY);
}

export function setToken(token: string | null) {
  if (token) localStorage.setItem(TOKEN_STORAGE_KEY, token);
  else localStorage.removeItem(TOKEN_STORAGE_KEY);
}

class ApiError extends Error {
  status: number;
  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = getToken();
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options.headers,
    },
  });

  if (!response.ok) {
    const text = await response.text().catch(() => "");
    throw new ApiError(response.status, text || response.statusText);
  }

  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

export const api = {
  register: (email: string, password: string) =>
    request<AuthResponse>("/api/auth/register", { method: "POST", body: JSON.stringify({ email, password }) }),

  login: (email: string, password: string) =>
    request<AuthResponse>("/api/auth/login", { method: "POST", body: JSON.stringify({ email, password }) }),

  getPlaylists: () => request<Playlist[]>("/api/playlists"),

  getPlaylist: (id: number) => request<Playlist>(`/api/playlists/${id}`),

  getPlaylistSongs: (id: number, limit?: number) =>
    request<PlaylistSong[]>(`/api/playlists/${id}/songs${limit ? `?limit=${limit}` : ""}`),
};

export { ApiError };
