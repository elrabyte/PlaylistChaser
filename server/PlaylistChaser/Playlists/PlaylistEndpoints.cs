using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PlaylistChaser.Data;
using PlaylistChaser.Jobs;
using PlaylistChaser.Sources;

namespace PlaylistChaser.Playlists;

public static class PlaylistEndpoints
{
    public record UrlRequest(string Url);
    public record CombineRequest(string Name, int[] PlaylistIds);
    public record EditRequest(string Name, string? Description);
    public record SongLinkRequest(string? Url, bool NotFound);

    public static void MapPlaylistEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/playlists", async (ClaimsPrincipal user, AppDb db) =>
            await Summaries(db, db.Playlists.Owned(user.Id()).OrderBy(p => p.Name)));

        api.MapGet("/playlists/{id:int}", async (int id, ClaimsPrincipal user, AppDb db) =>
        {
            var p = (await Summaries(db, db.Playlists.Owned(user.Id()).Where(p => p.Id == id))).SingleOrDefault();
            if (p == null) return Results.NotFound();
            var members = await db.CombinedMembers.Where(m => m.CombinedId == id).Select(m => m.MemberId).ToListAsync();
            return Results.Ok(new { playlist = p, members });
        });

        api.MapGet("/playlists/{id:int}/songs", async (int id, ClaimsPrincipal user, AppDb db) =>
            !await db.Playlists.Owned(user.Id()).AnyAsync(p => p.Id == id) ? Results.NotFound()
            : Results.Ok(await db.PlaylistSongs.Where(ps => ps.PlaylistId == id)
                .OrderByDescending(ps => ps.AddedAt).ThenBy(ps => ps.SongId)
                .Select(ps => new
                {
                    ps.Song.Id,
                    ps.Song.Name,
                    ps.Song.Artist,
                    ps.Song.ThumbnailUrl,
                    Links = ps.Song.Links.Select(l => new { l.Source, l.Status, l.Url }),
                })
                .ToListAsync()));

        api.MapPost("/playlists", (UrlRequest req, ClaimsPrincipal user, AppDb db, ISourceProvider sources, JobQueue queue, ILogger<Playlist> log) =>
            Guard(log, async () =>
            {
                if (!SourceUrls.TryParsePlaylist(req.Url, out var source, out var externalId))
                    return Results.Problem("Paste a YouTube or Spotify playlist link.", statusCode: 400);
                var userId = user.Id();
                var existing = await db.Playlists.Owned(userId)
                    .FirstOrDefaultAsync(p => p.MainSource == source && p.Links.Any(l => l.Source == source && l.ExternalId == externalId));
                if (existing == null)
                {
                    var remote = await (await sources.Get(userId, source)).GetPlaylist(externalId);
                    existing = new Playlist
                    {
                        UserId = userId,
                        Name = remote.Name,
                        Description = remote.Description,
                        Creator = remote.Creator,
                        ThumbnailUrl = remote.ThumbnailUrl,
                        Kind = PlaylistKind.Simple,
                        MainSource = source,
                        Links = [new PlaylistLink { Source = source, ExternalId = externalId, Url = SourceUrls.Playlist(source, externalId), IsMine = remote.IsMine }],
                    };
                    db.Playlists.Add(existing);
                    await db.SaveChangesAsync();
                }
                var job = await queue.Enqueue(db, userId, JobKind.Pull, existing.Id);
                return Results.Ok(new { id = existing.Id, jobId = job.Id });
            }));

        api.MapPost("/playlists/combined", async (CombineRequest req, ClaimsPrincipal user, AppDb db, JobQueue queue) =>
        {
            var userId = user.Id();
            var memberIds = req.PlaylistIds.Distinct().ToList();
            if (string.IsNullOrWhiteSpace(req.Name) || memberIds.Count < 2)
                return Results.Problem("Give it a name and pick at least two playlists.", statusCode: 400);
            var owned = await db.Playlists.Owned(userId).CountAsync(p => memberIds.Contains(p.Id) && p.Kind == PlaylistKind.Simple);
            if (owned != memberIds.Count) return Results.NotFound();

            var combined = new Playlist { UserId = userId, Name = req.Name.Trim(), Kind = PlaylistKind.Combined, Creator = user.Identity!.Name };
            db.Playlists.Add(combined);
            await db.SaveChangesAsync();
            db.CombinedMembers.AddRange(memberIds.Select(m => new CombinedMember { CombinedId = combined.Id, MemberId = m }));
            await db.SaveChangesAsync();
            var job = await queue.Enqueue(db, userId, JobKind.Pull, combined.Id);
            return Results.Ok(new { id = combined.Id, jobId = job.Id });
        });

        api.MapPatch("/playlists/{id:int}", async (int id, EditRequest req, ClaimsPrincipal user, AppDb db) =>
        {
            if (string.IsNullOrWhiteSpace(req.Name)) return Results.Problem("Name is required.", statusCode: 400);
            var p = await db.Playlists.Owned(user.Id()).SingleOrDefaultAsync(p => p.Id == id);
            if (p == null) return Results.NotFound();
            (p.Name, p.Description) = (req.Name.Trim(), req.Description);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        api.MapPost("/playlists/{id:int}/pull", async (int id, ClaimsPrincipal user, AppDb db, JobQueue queue) =>
            !await db.Playlists.Owned(user.Id()).AnyAsync(p => p.Id == id) ? Results.NotFound()
            : Results.Ok(await queue.Enqueue(db, user.Id(), JobKind.Pull, id)));

        api.MapPost("/playlists/{id:int}/push/{source}", async (int id, string source, ClaimsPrincipal user, AppDb db, JobQueue queue) =>
        {
            var p = await db.Playlists.Owned(user.Id()).SingleOrDefaultAsync(p => p.Id == id);
            if (p == null || !ApiHelpers.TryParseSource(source, out var target)) return Results.NotFound();
            if (p.MainSource == target) return Results.Problem($"{p.Name} comes from {target}; push it to another service.", statusCode: 400);
            return Results.Ok(await queue.Enqueue(db, user.Id(), JobKind.Push, id, target));
        });

        // Use an existing playlist of yours as the push target (e.g. one an earlier install created).
        api.MapPost("/playlists/{id:int}/links", (int id, UrlRequest req, ClaimsPrincipal user, AppDb db, ISourceProvider sources, ILogger<Playlist> log) =>
            Guard(log, async () =>
            {
                var p = await db.Playlists.Owned(user.Id()).Include(p => p.Links).SingleOrDefaultAsync(p => p.Id == id);
                if (p == null) return Results.NotFound();
                if (!SourceUrls.TryParsePlaylist(req.Url, out var source, out var externalId))
                    return Results.Problem("Paste a YouTube or Spotify playlist link.", statusCode: 400);
                if (source == p.MainSource || p.Links.Any(l => l.Source == source))
                    return Results.Problem($"{p.Name} already has a {source} playlist.", statusCode: 409);
                var remote = await (await sources.Get(user.Id(), source)).GetPlaylist(externalId);
                if (!remote.IsMine)
                    return Results.Problem($"That {source} playlist doesn't belong to your connected account.", statusCode: 400);
                p.Links.Add(new PlaylistLink { Source = source, ExternalId = externalId, Url = SourceUrls.Playlist(source, externalId), IsMine = true });
                await db.SaveChangesAsync();
                return Results.NoContent();
            }));

        // Unlink a push target; with deleteRemote=true also delete it on the service.
        api.MapDelete("/playlists/{id:int}/links/{source}", (int id, string source, bool? deleteRemote, ClaimsPrincipal user, AppDb db, ISourceProvider sources, ILogger<Playlist> log) =>
            Guard(log, async () =>
            {
                var p = await db.Playlists.Owned(user.Id()).Include(p => p.Links).SingleOrDefaultAsync(p => p.Id == id);
                if (p == null || !ApiHelpers.TryParseSource(source, out var s)) return Results.NotFound();
                var link = p.Links.SingleOrDefault(l => l.Source == s);
                if (link == null) return Results.NotFound();
                if (s == p.MainSource) return Results.Problem("That's where this playlist comes from; delete the playlist instead.", statusCode: 400);
                if (deleteRemote == true && link.IsMine)
                    await (await sources.GetDestination(user.Id(), s)).DeletePlaylist(link.ExternalId);
                p.Links.Remove(link);
                await db.PushedSongs.Where(x => x.PlaylistId == id && x.Source == s).ExecuteDeleteAsync();
                await db.SaveChangesAsync();
                return Results.NoContent();
            }));

        api.MapDelete("/playlists/{id:int}", async (int id, ClaimsPrincipal user, AppDb db) =>
        {
            var userId = user.Id();
            var p = await db.Playlists.Owned(userId).Include(p => p.Links).SingleOrDefaultAsync(p => p.Id == id);
            if (p == null) return Results.NotFound();
            if (p.Links.Any(l => l.IsMine && l.Source != p.MainSource))
                return Results.Problem("Delete or unlink its copies on other services first.", statusCode: 400);
            db.Playlists.Remove(p);
            await db.SaveChangesAsync();
            await db.Songs.Owned(userId).Where(s => !db.PlaylistSongs.Any(ps => ps.SongId == s.Id)).ExecuteDeleteAsync();
            return Results.NoContent();
        });

        // Fix how a song maps to a service: paste the right track URL, or mark it as not available there.
        api.MapPut("/songs/{id:int}/links/{source}", (int id, string source, SongLinkRequest req, ClaimsPrincipal user, AppDb db, ISourceProvider sources, ILogger<Playlist> log) =>
            Guard(log, async () =>
            {
                var song = await db.Songs.Owned(user.Id()).Include(s => s.Links).SingleOrDefaultAsync(s => s.Id == id);
                if (song == null || !ApiHelpers.TryParseSource(source, out var s)) return Results.NotFound();
                var link = song.Links.SingleOrDefault(l => l.Source == s);
                if (link?.Status == LinkStatus.Origin) return Results.Problem($"This song comes from {s}; its {s} link can't change.", statusCode: 400);

                string? externalId = null;
                if (!req.NotFound)
                {
                    externalId = SourceUrls.TryParseTrack(s, req.Url ?? "");
                    if (externalId == null) return Results.Problem($"Paste a {s} track link.", statusCode: 400);
                    if (await (await sources.GetDestination(user.Id(), s)).GetTrack(externalId) == null)
                        return Results.Problem($"That {s} track doesn't exist.", statusCode: 400);
                }
                if (link == null) song.Links.Add(link = new SongLink { Source = s });
                link.ExternalId = externalId;
                link.Url = externalId == null ? null : SourceUrls.Track(s, externalId);
                link.Status = req.NotFound ? LinkStatus.NotFound : LinkStatus.Manual;
                link.CheckedAt = DateTime.UtcNow;
                await ForgetPushed(db, id, s); // so the corrected track gets pushed
                await db.SaveChangesAsync();
                return Results.NoContent();
            }));

        // Forget the match so the next push searches again.
        api.MapDelete("/songs/{id:int}/links/{source}", async (int id, string source, ClaimsPrincipal user, AppDb db) =>
        {
            var song = await db.Songs.Owned(user.Id()).Include(s => s.Links).SingleOrDefaultAsync(s => s.Id == id);
            if (song == null || !ApiHelpers.TryParseSource(source, out var s)) return Results.NotFound();
            var link = song.Links.SingleOrDefault(l => l.Source == s);
            if (link == null) return Results.NoContent();
            if (link.Status == LinkStatus.Origin) return Results.Problem($"This song comes from {s}; its {s} link can't change.", statusCode: 400);
            song.Links.Remove(link);
            await ForgetPushed(db, id, s);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static async Task<List<object>> Summaries(AppDb db, IQueryable<Playlist> q)
    {
        var playlists = await q.Select(p => new { Playlist = p, SongCount = db.PlaylistSongs.Count(ps => ps.PlaylistId == p.Id) })
            .AsNoTracking().ToListAsync();
        var ids = playlists.Select(x => x.Playlist.Id).ToList();
        var links = (await db.PlaylistLinks.Where(l => ids.Contains(l.PlaylistId)).AsNoTracking().ToListAsync()).ToLookup(l => l.PlaylistId);
        var pushed = await db.PushedSongs.Where(x => ids.Contains(x.PlaylistId))
            .GroupBy(x => new { x.PlaylistId, x.Source })
            .Select(g => new { g.Key.PlaylistId, g.Key.Source, Count = g.Count() })
            .ToListAsync();
        return playlists.Select(x => (object)new
        {
            x.Playlist.Id,
            x.Playlist.Name,
            x.Playlist.Description,
            x.Playlist.Creator,
            x.Playlist.ThumbnailUrl,
            x.Playlist.Kind,
            x.Playlist.MainSource,
            x.Playlist.LastPulledAt,
            x.SongCount,
            Links = links[x.Playlist.Id].Select(l => new
            {
                l.Source,
                l.Url,
                l.IsMine,
                l.LastPushedAt,
                Pushed = pushed.FirstOrDefault(c => c.PlaylistId == l.PlaylistId && c.Source == l.Source)?.Count ?? 0,
            }),
        }).ToList();
    }

    private static Task ForgetPushed(AppDb db, int songId, Source source) =>
        db.PushedSongs.Where(x => x.SongId == songId && x.Source == source).ExecuteDeleteAsync();

    /// <summary>Turns errors from the music services (not connected, not found, quota) into a 400 with their message.</summary>
    internal static async Task<IResult> Guard(ILogger log, Func<Task<IResult>> action)
    {
        try { return await action(); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.LogWarning(ex, "Music service call failed");
            return Results.Problem(ex.Message, statusCode: 400);
        }
    }
}
