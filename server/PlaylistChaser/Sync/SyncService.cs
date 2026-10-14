using Microsoft.EntityFrameworkCore;
using PlaylistChaser.Data;
using PlaylistChaser.Sources;

namespace PlaylistChaser.Sync;

/// <summary>Pull = copy a playlist's tracks from its main service into the local DB (adds only).
/// Push = find each song on a target service and add what the target playlist is missing.</summary>
public class SyncService(AppDb db, ISourceProvider sources)
{
    public Task Run(Job job) => job.Kind switch
    {
        JobKind.Pull => Pull(job, job.PlaylistId!.Value),
        JobKind.Push => Push(job, job.PlaylistId!.Value, job.Target!.Value),
        _ => SyncAll(job),
    };

    public async Task Pull(Job job, int playlistId, bool pullMembers = true)
    {
        var p = await db.Playlists.Owned(job.UserId).Include(x => x.Links).SingleAsync(x => x.Id == playlistId);
        if (p.Kind == PlaylistKind.Combined)
        {
            var memberIds = await db.CombinedMembers.Where(m => m.CombinedId == p.Id).Select(m => m.MemberId).ToListAsync();
            if (pullMembers)
                foreach (var id in memberIds)
                    await Pull(job, id);
            var songIds = await db.PlaylistSongs.Where(ps => memberIds.Contains(ps.PlaylistId)).Select(ps => ps.SongId).Distinct().ToListAsync();
            await AddPlaylistSongs(p.Id, songIds);
        }
        else
        {
            var link = p.Links.Single(l => l.Source == p.MainSource);
            job.Message = $"Reading {p.Name} from {link.Source}";
            await db.SaveChangesAsync();
            var source = await sources.Get(job.UserId, link.Source);
            var tracks = (await source.GetTracks(link.ExternalId)).DistinctBy(t => t.Id).ToList();
            await AddPlaylistSongs(p.Id, await ImportSongs(job.UserId, link.Source, tracks));
        }
        p.LastPulledAt = DateTime.UtcNow;
        job.Message = null;
        await db.SaveChangesAsync();
    }

    public async Task Push(Job job, int playlistId, Source target)
    {
        var p = await db.Playlists.Owned(job.UserId).Include(x => x.Links).SingleAsync(x => x.Id == playlistId);
        if (p.MainSource == target)
            throw new InvalidOperationException($"{p.Name} comes from {target}; push it to another service.");
        var link = p.Links.SingleOrDefault(l => l.Source == target);
        if (link is { IsMine: false })
            throw new InvalidOperationException($"The {target} playlist linked to {p.Name} isn't yours, so it can't be changed.");
        var source = await sources.Get(job.UserId, target);

        var songs = await db.Songs.Include(s => s.Links).Where(s => db.PlaylistSongs.Any(ps => ps.PlaylistId == p.Id && ps.SongId == s.Id)).ToListAsync();

        // 1. find songs that have never been looked up on the target
        var unmatched = songs.Where(s => s.Links.All(l => l.Source != target)).ToList();
        (job.Done, job.Total, job.Message) = (0, unmatched.Count, $"Searching {target}");
        foreach (var song in unmatched)
        {
            var found = await source.Search(song.Name, song.Artist);
            song.Links.Add(new SongLink
            {
                Source = target,
                ExternalId = found?.Id,
                Url = found == null ? null : SourceUrls.Track(target, found.Id),
                Status = found == null ? LinkStatus.NotFound : LinkStatus.Found,
                CheckedAt = DateTime.UtcNow,
            });
            job.Done++;
            await db.SaveChangesAsync();
        }

        // 2. create the target playlist on first push
        var created = link == null;
        if (link == null)
        {
            var id = await source.CreatePlaylist(p.Name, "Created by PlaylistChaser");
            link = new PlaylistLink { PlaylistId = p.Id, Source = target, ExternalId = id, Url = SourceUrls.Playlist(target, id), IsMine = true };
            p.Links.Add(link);
            await db.SaveChangesAsync();
        }

        // 3. add what is missing and was never pushed before
        // a brand-new playlist is empty, and YouTube may not list it for a few seconds anyway
        var remote = created ? [] : (await source.GetTracks(link.ExternalId)).Select(t => t.Id).ToHashSet();
        var pushed = (await db.PushedSongs.Where(x => x.PlaylistId == p.Id && x.Source == target).Select(x => x.SongId).ToListAsync()).ToHashSet();
        var linked = songs
            .Select(s => (Song: s, Link: s.Links.Single(l => l.Source == target)))
            .Where(x => x.Link.ExternalId != null && x.Link.Status != LinkStatus.NotFound)
            .ToList();
        foreach (var x in linked.Where(x => remote.Contains(x.Link.ExternalId!) && pushed.Add(x.Song.Id)))
            db.PushedSongs.Add(new PushedSong { PlaylistId = p.Id, SongId = x.Song.Id, Source = target });
        var toAdd = linked.Where(x => !pushed.Contains(x.Song.Id)).DistinctBy(x => x.Link.ExternalId).ToList();

        (job.Done, job.Total, job.Message) = (0, toAdd.Count, $"Adding to {target}");
        await db.SaveChangesAsync();
        QuotaExceededException? quota = null;
        foreach (var chunk in toAdd.Chunk(source.MaxBatch))
        {
            try
            {
                await source.AddTracks(link.ExternalId, chunk.Select(x => x.Link.ExternalId!).ToList());
                db.PushedSongs.AddRange(chunk.Select(x => new PushedSong { PlaylistId = p.Id, SongId = x.Song.Id, Source = target }));
            }
            catch (QuotaExceededException ex) { quota = ex; break; }
            // a track the service refuses (removed, region-locked) shouldn't stop the rest
            catch (TrackRejectedException) when (chunk.Length == 1) { chunk[0].Link.Status = LinkStatus.NotFound; }
            job.Done += chunk.Length;
            await db.SaveChangesAsync();
        }

        link.LastPushedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        if (quota != null) throw quota;
        await source.UpdatePlaylist(link.ExternalId, p.Name, await Describe(p, target));
        job.Message = null;
    }

    public async Task SyncAll(Job job)
    {
        var playlists = await db.Playlists.Owned(job.UserId).Include(p => p.Links).OrderBy(p => p.Kind).ToListAsync();
        var errors = new List<string>();
        var blocked = new HashSet<Source>();
        foreach (var p in playlists)
        {
            try { await Pull(job, p.Id, pullMembers: false); } // members are simple playlists, pulled earlier in this loop
            catch (Exception ex) { errors.Add($"{p.Name}: {ex.Message}"); continue; }

            foreach (var target in p.Links.Where(l => l.IsMine && l.Source != p.MainSource).Select(l => l.Source).ToList())
            {
                if (blocked.Contains(target)) continue;
                try { await Push(job, p.Id, target); }
                catch (QuotaExceededException ex) { blocked.Add(target); errors.Add(ex.Message); }
                catch (Exception ex) { errors.Add($"{p.Name} → {target}: {ex.Message}"); }
            }
        }
        job.Message = errors.Count == 0 ? null : string.Join("\n", errors.Distinct());
    }

    private async Task<List<int>> ImportSongs(string userId, Source source, List<RemoteTrack> tracks)
    {
        var ids = tracks.Select(t => t.Id).ToList();
        var known = (await db.SongLinks
                .Where(l => l.Source == source && l.Song.UserId == userId && ids.Contains(l.ExternalId!))
                .Select(l => new { l.ExternalId, l.SongId })
                .ToListAsync())
            .DistinctBy(l => l.ExternalId)
            .ToDictionary(l => l.ExternalId!, l => l.SongId);

        var created = tracks.Where(t => !known.ContainsKey(t.Id)).ToDictionary(t => t.Id, t => new Song
        {
            UserId = userId,
            Name = t.Name,
            Artist = t.Artist,
            ThumbnailUrl = t.ThumbnailUrl,
            Links = [new SongLink { Source = source, ExternalId = t.Id, Url = SourceUrls.Track(source, t.Id), Status = LinkStatus.Origin, CheckedAt = DateTime.UtcNow }],
        });
        db.Songs.AddRange(created.Values);
        await db.SaveChangesAsync();
        return tracks.Select(t => known.TryGetValue(t.Id, out var id) ? id : created[t.Id].Id).ToList();
    }

    private async Task AddPlaylistSongs(int playlistId, List<int> songIds)
    {
        var existing = (await db.PlaylistSongs.Where(ps => ps.PlaylistId == playlistId).Select(ps => ps.SongId).ToListAsync()).ToHashSet();
        db.PlaylistSongs.AddRange(songIds.Distinct().Where(existing.Add)
            .Select(id => new PlaylistSong { PlaylistId = playlistId, SongId = id, AddedAt = DateTime.UtcNow }));
        await db.SaveChangesAsync();
    }

    private async Task<string> Describe(Playlist p, Source target)
    {
        var total = await db.PlaylistSongs.CountAsync(ps => ps.PlaylistId == p.Id);
        var uploaded = await db.PushedSongs.CountAsync(x => x.PlaylistId == p.Id && x.Source == target);
        var counts = $"{uploaded} / {total} - {DateTime.UtcNow:yyyy-MM-dd}";
        if (p.Kind == PlaylistKind.Simple)
            return $"Copied from {p.MainSource}: \n{p.Name} by {p.Creator}. \n{counts}";
        var members = await db.CombinedMembers.Where(m => m.CombinedId == p.Id)
            .Join(db.Playlists, m => m.MemberId, x => x.Id, (m, x) => x.Creator + " - " + x.Name).ToListAsync();
        return "A combined playlist consisting of:\n" + string.Join("\n", members) + "\n" + counts;
    }
}
