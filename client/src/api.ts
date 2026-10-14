// REST client for the PlaylistChaser server. Same origin, so the login cookie is sent automatically.

export type Source = "youtube" | "spotify";
export const SOURCES: Source[] = ["youtube", "spotify"];
export const SOURCE_LABEL: Record<Source, string> = { youtube: "YouTube", spotify: "Spotify" };
export const SOURCE_COLOR: Record<Source, string> = { youtube: "red", spotify: "green" };

export interface Me { userName: string; isAdmin: boolean }
export interface PlaylistLink { source: Source; url: string; isMine: boolean; lastPushedAt: string | null; pushed: number }
export interface Playlist {
  id: number;
  name: string;
  description: string | null;
  creator: string | null;
  thumbnailUrl: string | null;
  kind: "simple" | "combined";
  mainSource: Source | null;
  lastPulledAt: string | null;
  songCount: number;
  links: PlaylistLink[];
}
export type LinkStatus = "origin" | "found" | "notFound" | "manual";
export interface Song {
  id: number;
  name: string;
  artist: string;
  thumbnailUrl: string | null;
  links: { source: Source; status: LinkStatus; url: string | null }[];
}
export interface Job {
  id: number;
  kind: "pull" | "push" | "syncAll";
  playlistId: number | null;
  target: Source | null;
  status: "queued" | "running" | "done" | "failed";
  done: number;
  total: number;
  message: string | null;
  createdAt: string;
  finishedAt: string | null;
}
export interface SourceAccount { source: Source; configured: boolean; connected: boolean; needsReconnect: boolean; displayName: string | null }
export interface User { id: string; userName: string; isAdmin: boolean }

export class ApiError extends Error {
  constructor(public status: number, message: string) {
    super(message);
  }
}

async function call<T>(method: string, path: string, body?: unknown): Promise<T> {
  const res = await fetch(`/api${path}`, {
    method,
    headers: body === undefined ? undefined : { "Content-Type": "application/json" },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  if (!res.ok) {
    // server errors are RFC 7807 problem details: { title, detail }
    const problem = await res.json().catch(() => null);
    throw new ApiError(res.status, problem?.detail ?? problem?.title ?? res.statusText);
  }
  return res.status === 204 ? (undefined as T) : res.json();
}

export const api = {
  me: () => call<Me>("GET", "/auth/me"),
  login: (userName: string, password: string) => call<Me>("POST", "/auth/login", { userName, password }),
  logout: () => call<void>("POST", "/auth/logout"),
  changePassword: (currentPassword: string, newPassword: string) => call<void>("POST", "/auth/password", { currentPassword, newPassword }),

  users: () => call<User[]>("GET", "/users"),
  addUser: (userName: string, password: string, isAdmin: boolean) => call<{ id: string }>("POST", "/users", { userName, password, isAdmin }),
  deleteUser: (id: string) => call<void>("DELETE", `/users/${id}`),

  sources: () => call<SourceAccount[]>("GET", "/sources"),
  disconnect: (source: Source) => call<void>("DELETE", `/sources/${source}`),
  connectUrl: (source: Source) => `/api/sources/${source}/connect`,

  playlists: () => call<Playlist[]>("GET", "/playlists"),
  playlist: (id: number) => call<{ playlist: Playlist; members: number[] }>("GET", `/playlists/${id}`),
  songs: (id: number) => call<Song[]>("GET", `/playlists/${id}/songs`),
  addPlaylist: (url: string) => call<{ id: number }>("POST", "/playlists", { url }),
  combine: (name: string, playlistIds: number[]) => call<{ id: number }>("POST", "/playlists/combined", { name, playlistIds }),
  editPlaylist: (id: number, name: string, description: string | null) => call<void>("PATCH", `/playlists/${id}`, { name, description }),
  deletePlaylist: (id: number) => call<void>("DELETE", `/playlists/${id}`),
  pull: (id: number) => call<Job>("POST", `/playlists/${id}/pull`),
  push: (id: number, source: Source) => call<Job>("POST", `/playlists/${id}/push/${source}`),
  linkExisting: (id: number, url: string) => call<void>("POST", `/playlists/${id}/links`, { url }),
  unlink: (id: number, source: Source, deleteRemote: boolean) =>
    call<void>("DELETE", `/playlists/${id}/links/${source}?deleteRemote=${deleteRemote}`),

  setSongLink: (songId: number, source: Source, body: { url?: string; notFound?: boolean }) =>
    call<void>("PUT", `/songs/${songId}/links/${source}`, body),
  resetSongLink: (songId: number, source: Source) => call<void>("DELETE", `/songs/${songId}/links/${source}`),

  jobs: () => call<Job[]>("GET", "/jobs"),
  syncAll: () => call<Job>("POST", "/sync"),
};
