# AGENTS.md: map for agent/human sessions

## Layout

- **`server/PlaylistChaser`**: ASP.NET Core 10 minimal API in one project, with one folder per feature. It also serves the built SPA from `wwwroot` (copied in by the Dockerfile).
  - `Program.cs`: config checks, DI, Identity cookie auth, migrate on start, admin seed, routes.
  - `Data/AppDb.cs`: every entity and the EF model. `Data/Migrations` is Npgsql-only.
  - `Auth/`: login/logout/me/password, admin user management, and the env-var admin seed.
  - `Sources/`: providers by capability. `IPlaylistSource` (read playlists) and `IPlaylistDestination` (match songs, write playlists; extends source), the `Providers` list (name, colour, capabilities incl. Downloadable/Metadata flags that have no interface yet), `YoutubeSource`, `SpotifySource`, and `SourceAccounts` (OAuth connect/callback, token refresh, `ISourceProvider`; `GetDestination` checks the capability).
    - Adding a provider: a `Source` enum value, an entry in `Providers.All`, its class, a case in `SourceAccounts.Create`, and its login. The client builds its menus from `GET /api/sources`, so it needs no change except an optional brand icon in `SourceBadge.tsx`.
  - `Sync/SyncService.cs`: pull, push and sync-all. The core logic lives here.
  - `Jobs/Jobs.cs`: in-process `Channel` queue, a single worker, and `/api/jobs`.
  - `Tasks/Tasks.cs`: per-user scheduled tasks (cron via Cronos, server `TZ`), the minute-tick `Scheduler`, default tasks for new users, and `/api/tasks`.
  - `Playlists/PlaylistEndpoints.cs`: playlist and song-link endpoints.
- **`server/PlaylistChaser.Tests`**: xUnit + `WebApplicationFactory` (`TestApp`) with `FakeSources` in place of the real services.
  - Runs on SQLite by default, or on Postgres when `TEST_POSTGRES` is set (CI does both).
- **`client/`**: React 19 + Vite + TypeScript + Mantine 9 + TanStack Query + react-router 8 + `vite-plugin-pwa`.
  - `src/api.ts` is the only place that talks to the server.
- **`Dockerfile`** (one image: client build → server publish → aspnet runtime, non-root UID 1654).
- **`docker-compose.yml`** (app + Postgres): this exact file is attached to each GitHub release, with the image tag pinned by CI.
- **`.github/workflows/ci.yml`**: tests on every push/PR; a `v*` tag builds the image, smoke-tests compose, pushes to GHCR and creates the release.


## Commands

```bash
dotnet test server/PlaylistChaser.Tests
dotnet run --project server/PlaylistChaser --launch-profile dev   # http://localhost:5271, SQLite dev.db
npm --prefix client run dev                                       # http://localhost:5173, proxies /api
npm --prefix client run lint && npm --prefix client run build
```

New EF migration: run `dotnet tool restore` in `server/`, then in `server/PlaylistChaser`:
`POSTGRES_PASSWORD=x PUBLIC_URL=http://localhost dotnet ef migrations add <Name> -o Data/Migrations`.

## Decisions worth knowing before changing things

- **Security model:** every query goes through `Owned(userId)` (`Data/AppDb.cs`). A foreign id returns 404. Songs are per user on purpose (no shared catalog).
- **Auth:** Identity cookie with `SameSite=Strict`, JSON-only API, no CORS, no JWT.
  - The OAuth callback can't see that cookie (it's a cross-site redirect), so the `state` (DataProtector-protected, 15 minutes) carries the user id.
- **Secrets** come only from environment variables. YouTube/Spotify tokens are encrypted at rest with DataProtection, and the keys live in `DATA_DIR/keys`, the `/data` volume.
- **Config:**
  - `PUBLIC_URL` must be https except on localhost; OAuth redirects and PWA install require it.
  - `DB_CONNECTION` overrides the compose Postgres string. Starting it with `Data Source=` means SQLite, using `EnsureCreated` instead of migrations; that is for dev and tests only.
- **Push** = search unmatched songs, then add what the target playlist lacks **and was never pushed before** (the `PushedSong` table). This keeps tracks the user deleted by hand from coming back.
- **Quota:** a `QuotaExceededException` stops a push cleanly; the next run resumes. YouTube writes cost quota (insert = 50 units, 10k/day per Google project). Search goes through YoutubeExplode, which costs none.
- **Spotify** uses SpotifyAPI.Web's newer endpoints (`GetPlaylistItems`, `AddPlaylistItems`, `Create` on /me, `Library.RemoveItems`). The old ones were removed by Spotify.
- **Jobs:** `JobQueue` is in-process with a single worker. Running two replicas would need a shared queue (see the `shortcut:` comment). Jobs left running by a restart are marked failed at startup.
- **Not built on purpose:** song merging, remove-duplicates, Capacitor/native apps, arm64 images, mirroring removals, and importing data from the old SQL Server database.

## Not verifiable without the owner

Real YouTube/Spotify calls (OAuth, search quality, quota behaviour) can only be tested with real credentials on a host with HTTPS. The tests cover our logic with fakes only.
