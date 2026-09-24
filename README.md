<h1>PlaylistChaser</h1>

<p>A handy tool to manage your playlists on Spotify and YouTube.</p>
<p>Mirror your playlist so it exists on both Spotify and YouTube, and combine multiple playlists into a new one that keeps track of the playlists it was built from.</p>

## What's in this repo

- **PlaylistChaser.Web** — the ASP.NET Core backend. Serves the original web UI (works out of
  the box) and a JSON REST API used by the new React app.
- **PlaylistChaser.Client** — a new React web app (mobile-friendly, works in any phone/desktop
  browser) that talks to the REST API.
- **PlaylistChaser.DB** — the SQL Server database project (schema, views, stored procedures) for
  the original production deployment. Requires Visual Studio with SQL Server Data Tools (SSDT) to
  build/publish — it's not part of the Docker setup below.
- **PlaylistChaser.Test** — automated tests.

If you just want to run the whole thing locally without installing anything but Docker, skip to
[Run it with Docker](#run-it-with-docker).

## Run it with Docker (recommended)

Requires [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or another Docker
Compose-compatible setup).

```powershell
docker compose up --build
```

This starts three containers:
- the API on **http://localhost:5000**
- the web app on **http://localhost:8080**
- a PostgreSQL database (schema is created automatically on first start)

Open **http://localhost:8080** in a browser (including on your phone, if it's on the same
network as your computer, using your computer's IP instead of `localhost`). The very first
account you register becomes an administrator.

> Note: to actually sync playlists you'll need Spotify/YouTube API credentials — add them to
> `PlaylistChaser.Web/appsettings.Docker.json` (`Spotify:*` / `Youtube:*` sections) before
> starting the containers. Without them, sign-up/sign-in and browsing already-synced playlists
> still works.

## Running it without Docker

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) (for the React app)
- A database: either SQL Server (production setup, see below) or PostgreSQL (simpler for local
  dev, same as the Docker setup)

### Backend
```powershell
cd PlaylistChaser.Web
dotnet restore
dotnet build
dotnet run
```
You'll need an `appsettings.Development.json` (or `appsettings.Production.json`) file next to
`appsettings.json` with your connection string(s), a `Database:Provider` of `SqlServer` or
`Postgres`, a `Jwt:Key`, and your Spotify/YouTube API credentials — see
`appsettings.Docker.json` for the shape of this file (it's a working example for Postgres).
These per-environment files are intentionally left out of git since they hold secrets.

### Frontend
```powershell
cd PlaylistChaser.Client
npm install
npm run dev
```
Copy `.env.example` to `.env.local` and point `VITE_API_BASE_URL` at your running API
(`http://localhost:5000` by default).

### Database
- **SQL Server** (original production setup): open `PlaylistChaser.sln` in Visual Studio with
  SSDT installed, and build/publish `PlaylistChaser.DB` to your SQL Server instance. This is the
  only supported path for production today (per-user database logins, native row-level
  security — see `AGENTS.md` if you're curious how that works).
- **PostgreSQL** (simpler for local dev / what Docker uses): just point
  `ConnectionStrings:ServerConnectionString` at a Postgres database and set
  `Database:Provider` to `Postgres` — the app creates and seeds the schema itself on startup
  using EF Core migrations.

## Running the tests

```powershell
dotnet test PlaylistChaser.Test\PlaylistChaser.Test.csproj
```

Most tests pass without any setup — including `EfCorePlaylistDataStoreTests`, which runs the
Postgres/Docker-path database logic against a real (in-memory, no-install-required) SQLite
database, so you can confirm that part actually works even before trying Docker. `YoutubeApiTest`'s
"get playlist/song" tests call the real YouTube API and need a valid access token to pass —
they'll fail with an authentication error otherwise, which is expected without credentials
configured.

```powershell
cd PlaylistChaser.Client
npm run build   # type-checks and builds the app
npm run lint
```

## Where things stand

The React app currently covers the core flows: signing in, viewing your playlists, and viewing a
playlist's songs. Creating/syncing/combining playlists, linking Spotify/YouTube accounts, and
admin screens are still only available in the original web UI (both are running side by side, so
nothing was taken away — the plan is to keep porting features over to the new app). See
`AGENTS.md` for the full technical rundown if you're picking up this project's development.
