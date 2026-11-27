using System.Diagnostics;
using Cronos;
using File = System.IO.File;
using TagLib;
using ByteVector = TagLib.ByteVector;
using Picture = TagLib.Picture;
using PictureType = TagLib.PictureType;

namespace Downloader;

public class Worker(IHttpClientFactory httpFactory, IConfiguration config, ILogger<Worker> log) : BackgroundService
{
    private readonly SemaphoreSlim runLock = new(1, 1);
    private string ConfigDir => config["CONFIG_DIR"] ?? "/config";
    private string AppUrl => config["APP_URL"] ?? "http://app:8080";
    private string Token => config["PLUGIN_TOKEN"] ?? "";

    /// <summary>Runs the download now, unless one is already running.</summary>
    public bool RunNow()
    {
        if (!runLock.Wait(0)) return false;
        _ = Task.Run(async () =>
        {
            try { await Run(); }
            finally { runLock.Release(); }
        });
        return true;
    }

    /// <summary>Checks the cron schedule once a minute; a run starts when a schedule time has passed since the last check.</summary>
    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        var lastCheck = DateTime.UtcNow;
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stop))
        {
            var now = DateTime.UtcNow;
            var settings = await Library.LoadAsync<Settings>(Library.SettingsPath(ConfigDir));
            if (settings is { Enabled: true } && CronExpression.TryParse(settings.Cron, out var cron)
                && cron.GetNextOccurrence(lastCheck, TimeZoneInfo.Utc) is { } due && due <= now)
                RunNow();
            lastCheck = now;
        }
    }

    private async Task Run()
    {
        var settings = await Library.LoadAsync<Settings>(Library.SettingsPath(ConfigDir));
        if (settings == null) return;
        var http = httpFactory.CreateClient();
        var app = new AppClient(http, AppUrl, Token, settings.UserId);
        var jobId = await app.StartJob("Download music", 0);
        try
        {
            var summary = await Download(settings, app, jobId, http);
            await app.Finish(jobId, true, summary);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Download run failed");
            await app.Log(jobId, "error", ex.Message);
            await app.Finish(jobId, false, ex.Message);
        }
    }

    private async Task<string> Download(Settings settings, AppClient app, int jobId, HttpClient http)
    {
        var folder = Path.GetFullPath(settings.Folder);
        Directory.CreateDirectory(folder);
        var state = await Library.LoadAsync<Dictionary<int, Stored>>(Library.StatePath(ConfigDir)) ?? [];

        // 1. what the chosen playlists want, with a match and tags for each song
        var chosen = (await app.Playlists()).Where(p => settings.PlaylistIds.Contains(p.Id)).ToList();
        var wanted = new Dictionary<int, Wanted>();
        var membership = new Dictionary<AppPlaylist, List<int>>();
        var skipped = 0;
        foreach (var playlist in chosen)
        {
            var ids = new List<int>();
            foreach (var song in await app.Songs(playlist.Id))
            {
                ids.Add(song.Id);
                if (wanted.ContainsKey(song.Id)) continue;
                var link = song.Links.FirstOrDefault(l => l.Source == settings.DownloadFrom);
                // the app searches each service once per song; a NotFound stays NotFound
                var youtubeId = link?.ExternalId ?? (link == null ? await app.Match(song.Id, settings.DownloadFrom) : null);
                if (youtubeId == null) { skipped++; continue; }
                AppMetadata? meta = null;
                var metaId = settings.MetadataFrom == "none" ? null : song.Links.FirstOrDefault(l => l.Source == settings.MetadataFrom)?.ExternalId;
                if (metaId != null) meta = await app.Metadata(settings.MetadataFrom, metaId);
                wanted[song.Id] = new Wanted(song.Id, youtubeId, meta?.Artist ?? song.Artist, meta?.Album ?? "", meta?.Title ?? song.Name,
                    meta?.TrackNumber, meta?.Year, meta?.CoverUrl);
            }
            membership[playlist] = ids;
        }
        if (skipped > 0) await app.Log(jobId, "warn", $"{skipped} songs have no {settings.DownloadFrom} match and were skipped.");

        // 2. download what is new or changed; delete what is no longer wanted
        var (download, remove) = Library.Diff(wanted.Values.ToList(), state);
        var total = download.Count + remove.Count;
        await app.Progress(jobId, 0, total);
        var done = 0;
        var failed = 0;
        foreach (var w in download)
        {
            try
            {
                await Fetch(w, folder, http);
                if (state.TryGetValue(w.SongId, out var old) && old.RelativePath != w.RelativePath)
                    File.Delete(Path.Combine(folder, old.RelativePath));
                state[w.SongId] = new Stored(w.YoutubeId, w.RelativePath);
                await app.Log(jobId, "info", $"Downloaded {w.Artist} - {w.Title}");
            }
            catch (Exception ex)
            {
                failed++;
                await app.Log(jobId, "warn", $"Could not download {w.Artist} - {w.Title}: {ex.Message}");
            }
            await app.Progress(jobId, ++done, total);
        }
        foreach (var id in remove)
        {
            if (state.TryGetValue(id, out var old)) File.Delete(Path.Combine(folder, old.RelativePath));
            state.Remove(id);
            await app.Log(jobId, "info", "Removed a song that is no longer in a chosen playlist");
            await app.Progress(jobId, ++done, total);
        }

        // 3. one playlist file per chosen playlist, listing the songs that are on disk
        var playlistDir = Path.Combine(folder, "Playlists");
        Directory.CreateDirectory(playlistDir);
        var written = new HashSet<string>();
        foreach (var (playlist, ids) in membership)
        {
            var name = Library.Safe(playlist.Name, $"playlist-{playlist.Id}") + ".m3u8";
            var paths = ids.Where(state.ContainsKey).Select(id => state[id].RelativePath).Distinct();
            await File.WriteAllTextAsync(Path.Combine(playlistDir, name), Library.PlaylistFile(paths));
            written.Add(name);
        }
        foreach (var stale in Directory.GetFiles(playlistDir, "*.m3u8").Where(f => !written.Contains(Path.GetFileName(f))))
            File.Delete(stale);

        await Library.SaveAsync(Library.StatePath(ConfigDir), state);
        return $"{download.Count - failed} downloaded, {failed} failed, {remove.Count} removed; {wanted.Count} songs in {chosen.Count} playlists.";
    }

    /// <summary>Downloads one song into its place in the library and writes its tags.</summary>
    private static async Task Fetch(Wanted w, string folder, HttpClient http)
    {
        var work = Path.Combine(Path.GetTempPath(), "dl-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try
        {
            var audio = await Ytdlp.Audio(w.YoutubeId, work);
            using (var file = TagLib.File.Create(audio))
            {
                file.Tag.Title = w.Title;
                file.Tag.Performers = [w.Artist];
                file.Tag.AlbumArtists = [w.Artist];
                file.Tag.Album = w.Album;
                if (w.Track is > 0) file.Tag.Track = (uint)w.Track.Value;
                if (w.Year is > 0) file.Tag.Year = (uint)w.Year.Value;
                if (w.CoverUrl != null)
                {
                    var cover = await http.GetByteArrayAsync(w.CoverUrl);
                    file.Tag.Pictures = [new Picture(new ByteVector(cover)) { Type = PictureType.FrontCover, MimeType = "image/jpeg" }];
                }
                file.Save();
            }
            var target = Path.Combine(folder, w.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Move(audio, target, overwrite: true);
        }
        finally
        {
            Directory.Delete(work, recursive: true);
        }
    }
}

public static class Ytdlp
{
    // shortcut: yt-dlp is pinned in the Dockerfile; YouTube changes break it every few weeks, so rebuild the image with a newer one
    public static async Task<string> Audio(string youtubeId, string workDir)
    {
        var psi = new ProcessStartInfo("yt-dlp") { RedirectStandardError = true, RedirectStandardOutput = true };
        foreach (var arg in new[]
        {
            "--no-playlist", "--no-warnings", "-f", "bestaudio[ext=m4a]/bestaudio", "-x", "--audio-format", "m4a",
            "-o", Path.Combine(workDir, "%(id)s.%(ext)s"), $"https://www.youtube.com/watch?v={youtubeId}",
        })
            psi.ArgumentList.Add(arg);
        using var process = Process.Start(psi)!;
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var audio = Path.Combine(workDir, youtubeId + ".m4a");
        if (process.ExitCode != 0 || !File.Exists(audio))
            throw new InvalidOperationException(stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "yt-dlp failed");
        return audio;
    }
}
