# AGENTS.md — map for future Copilot / agent sessions

This file is for whoever (human or agent) picks this repo up next. It explains where things
live, what's real vs. legacy, and the tradeoffs baked into the current architecture.

## TL;DR

- **PlaylistChaser.Web** — the ASP.NET Core app. Contains BOTH the original server-rendered
  Razor/MVC UI (cookie auth) AND a newer JSON REST API (`Controllers/Api`, JWT auth) added to
  support the React SPA and future clients (mobile). Both are live at the same time.
- **PlaylistChaser.Client** — new React + TypeScript SPA (Vite), talks to the REST API,
  mobile-responsive, structured to be wrapped later with Capacitor for an installable app.
- **PlaylistChaser.DB** — legacy SQL Server Database Project (SSDT `.sqlproj`). Requires
  Visual Studio/SSDT tooling; **cannot** be built with `dotnet build` or in this repo's CI/Docker
  path. Still the source of truth for the production SQL Server deployment's stored procedures
  and per-user-login security model (see "Database backends" below).
- **PlaylistChaser.Test** — xUnit tests. `YoutubeApiTest` is a live-API integration suite, not
  unit tests — most of it fails without a real YouTube OAuth token/quota. That's expected.
- **docker-compose.yml** — `docker compose up --build` runs api + client + Postgres. This is the
  quickest way to see the whole stack running (not verified against a live Docker daemon in the
  environment this was authored in — sanity-check the Dockerfiles/compose file if something
  doesn't come up cleanly).

## Build / test commands

```powershell
dotnet build PlaylistChaser.Web\PlaylistChaser.Web.csproj      # builds fine
dotnet test  PlaylistChaser.Test\PlaylistChaser.Test.csproj    # builds fine; some tests need live YouTube creds
# PlaylistChaser.DB\PlaylistChaser.DB.sqlproj does NOT build via `dotnet build` (needs SSDT/VS)
```

Both `.csproj` projects target **net8.0** (bumped from net6.0/net7.0 — this environment only has
the .NET 8/9 runtimes installed; net6/net7 are EOL anyway). EF Core / Identity / SignalR / JWT
packages were bumped to the matching 8.0.x line.

React client:
```powershell
cd PlaylistChaser.Client
npm install
npm run dev      # Vite dev server, http://localhost:5173
npm run build    # tsc -b && vite build -> dist/
npm run lint     # oxlint
```

## What was fixed (build-error pass)

1. **`compilerconfig.json`/SCSS build step (`BuildWebCompiler2022`) failed in headless
   environments** (tries to install a compiler at build time, no network/tooling for it here).
   Disabled by default via `<RunWebCompiler>false</RunWebCompiler>` in the `.csproj` — the
   compiled `wwwroot/css`/`wwwroot/js` output is already committed, so this is safe. Re-enable
   locally in an IDE with working WebCompiler tooling if you need to recompile SCSS.
2. **Target frameworks net6.0/net7.0 have no matching runtime installed here** → retargeted both
   `PlaylistChaser.Web` and `PlaylistChaser.Test` to **net8.0**, and bumped
   `Microsoft.EntityFrameworkCore*`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`,
   `Microsoft.AspNetCore.SignalR.Common`, and `Microsoft.VisualStudio.Web.CodeGeneration.Design`
   to their 8.0.x releases to match (the old CodeGeneration.Design 6.0.16 also pulled in a Roslyn
   version that crashed `dotnet-ef` design-time builds — fixed by the same bump).
3. **Real bug**: `YoutubeApiHelper.GetSongsThumbnailByPlaylist`/`GetSongsThumbnailBySongIds`
   sorted thumbnails with `OrderBy(t => t.Resolution)` — `Resolution` isn't `IComparable`, so
   this threw `"At least one object must implement IComparable"` at runtime. Fixed to
   `OrderBy(t => t.Resolution.Area)`, matching the working pattern already used elsewhere in the
   same file. Caught by `PlaylistChaser.Test.YoutubeApiTest.GetSongsThumbnailBase64ByPlaylist`.
4. `PlaylistChaser.DB.sqlproj` still can't build outside Visual Studio/SSDT — not something fixable
   from the CLI; the Postgres/EF-migrations path (see below) is the workaround for
   Docker/local dev, not a replacement for it in production.

## Database backends — how the "swap the DB" ask was actually implemented

This app's original design is **deeply SQL-Server-specific**, beyond just "uses SQL Server":

- Each app user is provisioned as an actual **SQL Server login** (`BaseDbContext.CreateDBUser`,
  called from registration) with `sp_addlogin`/`sp_adduser`/`sp_addrolemember`.
- Row-level security is enforced **natively by SQL Server**: the `VIEWPROG` schema's views
  (`viewprog.Playlist`, etc.) filter with `WHERE UserId = viewprog.getCurrentUserId()`, where
  `getCurrentUserId()` resolves via `current_user` — i.e. *which SQL login you're connected as*.
  `UserDbContext` opens a distinct connection per request using each user's own DB credentials.
- Reads go through stored procedures (`VIEWPROG.GetPlaylists`, `GetPlaylistSongs`, `GetSongs`,
  `MergeSongs`) defined in the `PlaylistChaser.DB` SSDT project, not plain EF Core LINQ.

Reimplementing 100% of that (native per-user DB principals + T-SQL procs) against Postgres in one
pass would be high-risk for a real, already-deployed app, so instead of a forced rewrite, the
seam was made explicit and swappable:

- **`Database/Abstractions/IPlaylistDataStore`** abstracts the 4 read-model operations
  (`GetPlaylistsAsync`, `GetPlaylistSongsAsync`, `GetSongsAsync`, `MergeSongsAsync`).
  - `SqlServerViewProgDataStore` — the original behavior, unchanged, delegating to
    `UserDbContext`'s stored-procedure calls. Security is native (SQL Server logins).
  - `EfCorePlaylistDataStore` — a provider-agnostic reimplementation using plain EF Core LINQ
    against the shared `AdminDBContext` connection (works with Postgres, SQLite, or SQL Server
    without per-user logins). Security is enforced **at the application layer** instead
    (explicit `WHERE UserId = @userId`, and an ownership check before returning a playlist's
    songs) — functionally equivalent for the app, but without SQL Server's extra
    defense-in-depth layer. This tradeoff is intentional; call it out if you touch this code.
  - `Database/Abstractions/PlaylistDataStoreFactory` picks the implementation based on
    `Database:Provider` in config (`"SqlServer"` default / `"Postgres"`).
- `Program.cs` configures `AdminDBContext` with `UseSqlServer` or `UseNpgsql` based on the same
  `Database:Provider` setting, and Hangfire storage similarly switches between SQL Server storage
  and an in-memory store (`Hangfire.MemoryStorage` — jobs don't survive a restart on the
  Postgres/Docker path; acceptable for local/dev use, not a production HA story).
- EF Core migrations for the Postgres path live under `PlaylistChaser.Web/Migrations/Postgres`
  (generated via `AdminDBContextPostgresFactory`, a design-time-only `IDesignTimeDbContextFactory`).
  On startup, if `Database:Provider != SqlServer`, `Program.cs` calls `db.Database.Migrate()` and
  seeds the small set of built-in lookup rows the SQL Server deployment normally gets from
  `PlaylistChaser.DB\NewDbScripts\InBuiltValues` (`AspNetRoles.Administrator`,
  `Source.Youtube/Spotify`). Note: `PlaylistType`/`State` don't need seed rows — they're plain
  enums (`BuiltInIds.PLaylistTypes`/`SongStates`/`PlaylistSongStates`) with no corresponding
  `DbSet` in `AdminDBContext`, so there's no FK-backed lookup table for them in the EF model.
- If you add a new query to `UserDbContext`'s VIEWPROG surface, add it to `IPlaylistDataStore`
  and both implementations, not just one — otherwise the Postgres/Docker path silently loses
  functionality.

## Music-service backends (Spotify/YouTube) — the other "swap the backend" seam

`Util/ISource` (now `public`, was previously `internal`) is the interface both
`YoutubeApiHelper` and `SpotifyApiHelper` implement. `Util/API/ISourceFactory` /
`SourceFactory` is the new DI-registered factory (`Sources` enum → `ISource`) — add a new
streaming backend by implementing `ISource` and adding a case there; no controller changes
needed. Existing MVC controllers (`PlaylistController`, `SongController`) still construct
`YoutubeApiHelper`/`SpotifyApiHelper` directly (unchanged, to avoid touching working code); the
new `Controllers/Api` controllers are wired through `IPlaylistDataStoreFactory` instead, since
they don't need the per-source OAuth juggling those two MVC controllers already do.

## REST API (`Controllers/Api`)

- JWT bearer auth, separate scheme from the MVC cookie auth (both registered in `Program.cs`;
  `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]` on
  `ApiControllerBase`). Config under `Jwt:*` in appsettings (Key/Issuer/Audience/ExpiryMinutes).
- `POST /api/auth/register` — the **first** account in a fresh install can self-register and
  becomes Administrator (bootstrap, needed for Docker/fresh Postgres installs with zero users).
  After that, registration mirrors the existing MVC behavior (`AccountController.Register` is
  `[AuthorizeRole(Roles.Administrator)]`-gated) — the caller must already be an authenticated
  Administrator.
- `POST /api/auth/login`, `GET /api/playlists`, `GET /api/playlists/{id}`,
  `GET /api/playlists/{id}/songs`, `GET /api/songs`, `POST /api/songs/merge`.
- CORS policy `"SpaClient"` is origin-allowlisted via `Cors:AllowedOrigins` in config (empty by
  default = no cross-origin access) — set it for whichever origins your SPA/dev server runs on.

## React client (`PlaylistChaser.Client`)

- Vite + React 19 + TypeScript + `react-router-dom`. No CSS framework lock-in (plain CSS,
  mobile-first, 44px min tap targets under 480px) and no native-only APIs, so it can be wrapped
  later with Capacitor (or similar) for an installable app without restructuring.
- `src/api.ts` — REST client, JWT stored in `localStorage`, base URL from
  `VITE_API_BASE_URL` (empty string in Docker = same-origin, proxied by nginx; see
  `nginx.conf`/`Dockerfile`).
- Only the core flows are implemented so far: login/register-free login, playlist list,
  playlist detail + songs. **Not yet ported**: registration UI, song search/add, merge-songs UI,
  playlist create/sync/combine actions, Spotify/YouTube OAuth linking, admin screens. All of
  that still only exists in the Razor views — check there for the reference behavior before
  porting a flow.

## Docker

`docker-compose.yml` at the repo root: `api` (ASP.NET Core, Postgres-backed),
`client` (React SPA behind nginx, proxies `/api/*` to `api`), `db` (Postgres 16). Config for the
`api` service comes from `PlaylistChaser.Web/appsettings.Docker.json` (checked into git, dev-only
secrets) rather than compose `environment:` overrides — see the comment in `docker-compose.yml`
for why (the way `Program.cs` loads `appsettings.{ASPNETCORE_ENVIRONMENT}.json` gives it higher
precedence than plain environment variables). **This was authored without a Docker daemon
available in-session** — build/run it once before relying on it, and check `PlaylistChaser.Web/Dockerfile`,
`PlaylistChaser.Client/Dockerfile` and `nginx.conf` if something doesn't come up.

## Things intentionally left alone

- The existing Razor/MVC UI, cookie auth, and SQL-Server-only production configuration are
  untouched. Nothing here should change existing production behavior when `Database:Provider`
  is left at its default (`SqlServer`).
- `PlaylistChaser.DB` (the SSDT project) wasn't converted to the modern SDK-style
  `Microsoft.Build.Sql` format — that's a bigger, separate, riskier change than what was asked
  for here, and the EF-migrations path already covers the "swap the DB" ask for new deployments.
