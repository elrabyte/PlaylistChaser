<h1>PlaylistChaser</h1>

<p>Keep your playlists in sync between YouTube and Spotify.</p>

- **Copy** a playlist from one service to the other. Songs are found automatically, and you can fix any wrong matches.
- **Combine** several playlists into one and copy that anywhere.
- **Sync daily** without you doing anything: new songs get added, and songs you removed from a copy stay removed.
- **Use it on phone or desktop.** It installs to your home screen like an app. On Android you can share a playlist from the YouTube or Spotify app straight into it.
- **Multiple users**, each with their own playlists and accounts.

Repository: **https://github.com/elrabyte/PlaylistChaser**

## Install (Docker / TrueNAS SCALE)

1. Copy this compose file. It is the same as `docker-compose.yml` in the repo, and each [release](https://github.com/elrabyte/PlaylistChaser/releases/latest) also attaches it with the image pinned to that version:

```yaml
# PlaylistChaser: run with `docker compose up -d`, or paste into TrueNAS SCALE
# (Apps > Discover Apps > Custom App > Install via YAML).
#
# Before starting, replace every CHANGE_ME below. PUBLIC_URL must be the https address
# you open the app at: YouTube/Spotify logins and installing the app on a phone only work
# over https. See the README for HTTPS, Google/Spotify setup, backups and upgrades.

x-db-password: &db-password CHANGE_ME  # Postgres password, shared by both services

services:
  app:
    image: ghcr.io/elrabyte/playlistchaser:latest
    restart: unless-stopped
    depends_on:
      db:
        condition: service_healthy
    ports:
      - "8080:8080"
    environment:
      PUBLIC_URL: https://CHANGE_ME.example.com
      ADMIN_USERNAME: admin
      ADMIN_PASSWORD: CHANGE_ME     # 10+ characters; only used to create the first admin
      POSTGRES_PASSWORD: *db-password
      YOUTUBE_CLIENT_ID: ""         # optional; from Google Cloud (README: YouTube)
      YOUTUBE_CLIENT_SECRET: ""
      SPOTIFY_CLIENT_ID: ""         # optional; from the Spotify developer dashboard (README: Spotify)
      SPOTIFY_CLIENT_SECRET: ""
    volumes:
      # encryption keys for logins and stored YouTube/Spotify tokens; back it up with the database.
      # Using a host path instead? Give it to UID 1654 (the container's user).
      - app-data:/data

  db:
    image: postgres:17-alpine
    restart: unless-stopped
    environment:
      POSTGRES_DB: playlistchaser
      POSTGRES_USER: playlistchaser
      POSTGRES_PASSWORD: *db-password
    volumes:
      - db-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U playlistchaser -d playlistchaser"]
      interval: 5s
      timeout: 5s
      retries: 10

volumes:
  app-data:
  db-data:
```

2. Replace every `CHANGE_ME` in it:
   - `PUBLIC_URL`: the **https** address you'll open the app at (see [HTTPS](#https)).
   - `ADMIN_USERNAME` / `ADMIN_PASSWORD`: your first login. The password needs 10+ characters. These are only used when the database is empty.
   - The Postgres password at the top. It's shared by both services, and Postgres only reads it the first time it creates its database. If you change it later, the app can't connect (`password authentication failed`). Either put the old password back, or, if you have no data yet, delete the app's `db-data` volume and start again.
   - `YOUTUBE_*` / `SPOTIFY_*`: see [YouTube](#youtube) and [Spotify](#spotify). You can leave either one empty.
3. Start it:
   - **Plain Docker:** `docker compose up -d`.
   - **TrueNAS SCALE:** Apps → Discover Apps → Custom App → *Install via YAML*, then paste the file. TrueNAS doesn't read `.env` files, which is why the values are written straight into the file.
4. Open `PUBLIC_URL`, log in, go to **Settings** and connect YouTube and/or Spotify.

The app listens on port 8080. Images are published as `ghcr.io/elrabyte/playlistchaser`, for linux/amd64 only.

### HTTPS

You need it. Google and Spotify only redirect logins back to `https://` addresses (plain `http` works only for `localhost`), and phones only install web apps from https sites. The app refuses to start when `PUBLIC_URL` isn't https.

Put a reverse proxy with a certificate in front of port 8080. Any of these work:

- the reverse proxy / ingress of your NAS (e.g. Nginx Proxy Manager or Caddy as a TrueNAS app) with a domain you own
- [Tailscale Serve](https://tailscale.com/kb/1312/serve), which gives a private `https://<machine>.<tailnet>.ts.net` address
- a [Cloudflare Tunnel](https://developers.cloudflare.com/cloudflare-one/connections/connect-networks/), which needs no open ports

The proxy must pass `X-Forwarded-Proto`; all of the above do.

### YouTube

1. In the [Google Cloud console](https://console.cloud.google.com/), create a project and enable **YouTube Data API v3**.
2. Under *OAuth consent screen*, pick user type "External" and add the scope `.../auth/youtube`.
3. Under *Credentials*, create an **OAuth client ID** of type "Web application". Its authorized redirect URI is `https://<your PUBLIC_URL host>/api/sources/youtube/callback`.
4. Put the client ID and secret into `YOUTUBE_CLIENT_ID` / `YOUTUBE_CLIENT_SECRET`.
5. **Publish the app** (consent screen → *Publish app*). While it is in "Testing", Google expires the login after 7 days and the daily sync stops; Settings then shows **Reconnect**. A published but unverified app works fine for you and your users. Google just shows an "unverified app" warning once while connecting.

**Quota:** Google gives each project 10,000 units a day. Adding one song to a YouTube playlist costs 50, so all users together can add about **200 songs per day** to YouTube. When the quota runs out, the copy stops and continues on the next daily sync. Searching YouTube uses no quota.

### Spotify

1. In the [Spotify developer dashboard](https://developer.spotify.com/dashboard), create an app with the *Web API*.
2. Its redirect URI is `https://<your PUBLIC_URL host>/api/sources/spotify/callback`.
3. Put the client ID and secret into `SPOTIFY_CLIENT_ID` / `SPOTIFY_CLIENT_SECRET`.
4. Apps in Spotify's development mode only work for users you add under *User Management* in the dashboard, so add every PlaylistChaser user's Spotify account there.

### Backup, restore, upgrade

- **Back up** both volumes:
  - **`db-data`** (or `docker compose exec db pg_dump -U playlistchaser playlistchaser > backup.sql`)
  - **`app-data`**, which holds the keys that encrypt logins and the stored YouTube/Spotify tokens. Lose it and everyone has to log in and reconnect, but no playlists are lost.
- **Upgrade:** change the image tag (or download the new release's compose file) and restart. Database changes apply automatically on start. Back up before a major version, because they can't be rolled back.
- **Host-path volume instead of a named one?** The container runs as UID 1654, so give that directory to that user.

## How syncing works

- **Update songs** (pull) reads a playlist from where it came from and adds new songs. Songs removed there stay in PlaylistChaser.
- **Copy to …** (push) creates the playlist on the other service the first time.
  - Each song is searched there once. YouTube is searched by "artist title"; Spotify first by exact artist and title, then by title alone.
  - Only songs the copy doesn't have yet are added. A song you deleted from the copy is never added again.
- Tap a song to see how it was matched. Paste the right link, mark it "not on Spotify/YouTube", or let it be searched again on the next copy.
- **Daily sync** runs once a day per user: every playlist is updated, then copied to every service it was copied to before.
- **Coming from an older PlaylistChaser?** Add your playlists again, then use *⋯ → Use an existing playlist as copy* to link the copies the old version made, so no duplicates are created.

## Develop

Requires the .NET 10 SDK and Node 22+. No Docker or Postgres is needed locally, because dev and tests use SQLite.

```bash
dotnet run --project server/PlaylistChaser --launch-profile dev
```

```bash
npm --prefix client install
```

```bash
npm --prefix client run dev
```

Open http://localhost:5173 and log in as `admin` / `dev-password-1` (from `server/PlaylistChaser/Properties/launchSettings.json`). Vite forwards `/api` to the server on port 5271.

```bash
dotnet test server/PlaylistChaser.Tests
```

- **Tests** run against SQLite with fake YouTube/Spotify services. CI runs them a second time against Postgres with the real migrations.
- **Database changes:** edit `server/PlaylistChaser/Data/AppDb.cs`, then from `server/PlaylistChaser` run `POSTGRES_PASSWORD=x PUBLIC_URL=http://localhost dotnet ef migrations add <Name> -o Data/Migrations`. `dotnet tool restore` installs `dotnet-ef`.
- **Release:** push a tag like `v1.2.0`. CI then tests, builds the image, smoke-tests `docker-compose.yml` with it, pushes it to GHCR and creates the GitHub release with the compose file attached. A tag with a hyphen (`v1.2.0-rc1`) becomes a pre-release and doesn't move `:latest`. After the very first release, make the GHCR package public (package settings → visibility) so TrueNAS can pull it without logging in.
