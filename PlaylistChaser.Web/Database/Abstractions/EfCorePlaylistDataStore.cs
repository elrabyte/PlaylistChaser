using Microsoft.EntityFrameworkCore;
using PlaylistChaser.Web.Models;
using PlaylistChaser.Web.Models.ViewModel;

namespace PlaylistChaser.Web.Database.Abstractions
{
    /// <summary>
    /// Provider-agnostic implementation of <see cref="IPlaylistDataStore"/> using plain EF Core
    /// LINQ queries against the shared <see cref="AdminDBContext"/> connection, re-implementing
    /// the same result shape as the SQL-Server-only VIEWPROG stored procedures
    /// (GetPlaylists.sql / GetPlaylistSongs.sql / GetSongs.sql / MergeSongs.sql).
    ///
    /// Use this when running against a database provider other than SQL Server (e.g. PostgreSQL
    /// in Docker), or when the per-user SQL Server login/native row-level-security model isn't
    /// available. Security is enforced here at the application layer (explicit
    /// <c>WHERE UserId = @userId</c> filtering) rather than via native database principals -
    /// functionally equivalent for the app's purposes, but without SQL Server's extra
    /// defense-in-depth layer. See AGENTS.md / README.md for the full tradeoff discussion.
    /// </summary>
    public class EfCorePlaylistDataStore : IPlaylistDataStore
    {
        private readonly AdminDBContext db;

        public EfCorePlaylistDataStore(AdminDBContext db)
        {
            this.db = db;
        }

        public async Task<List<PlaylistViewModel>> GetPlaylistsAsync(int userId, int? playlistId = null)
        {
            var query = db.Playlist.AsNoTracking().Where(p => p.UserId == userId);
            if (playlistId.HasValue)
                query = query.Where(p => p.Id == playlistId.Value);

            var playlistSongCounts = await db.PlaylistSong.AsNoTracking()
                .GroupBy(ps => ps.PlaylistId)
                .Select(g => new { PlaylistId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.PlaylistId, g => g.Count);

            var playlists = await query.ToListAsync();

            return playlists.Select(p => new PlaylistViewModel
            {
                Id = p.Id,
                Name = p.Name,
                ChannelName = p.ChannelName,
                Description = p.Description,
                ThumbnailId = p.ThumbnailId,
                PlaylistTypeId = p.PlaylistTypeId,
                PlaylistTypeName = p.PlaylistTypeId.ToString(),
                MainSourceId = p.MainSourceId,
                SongsTotal = playlistSongCounts.TryGetValue(p.Id, out var count) ? count : 0,
            }).ToList();
        }

        public async Task<List<PlaylistSongViewModel>> GetPlaylistSongsAsync(int userId, int playlistId, int? limit = null)
        {
            // mirror the native-RLS check the SQL Server VIEWPROG views perform: only return
            // songs for a playlist the requesting user actually owns.
            var ownsPlaylist = await db.Playlist.AsNoTracking().AnyAsync(p => p.Id == playlistId && p.UserId == userId);
            if (!ownsPlaylist)
                return new List<PlaylistSongViewModel>();

            var query = from ps in db.PlaylistSong.AsNoTracking()
                        join s in db.Song.AsNoTracking() on ps.SongId equals s.Id
                        where ps.PlaylistId == playlistId
                        orderby ps.Id descending
                        select new PlaylistSongViewModel
                        {
                            PlaylistSongId = ps.Id,
                            SongId = s.Id,
                            SongName = s.SongName,
                            ArtistName = s.ArtistName,
                            ThumbnailId = s.ThumbnailId,
                        };

            if (limit.HasValue)
                query = query.Take(limit.Value);

            return await query.ToListAsync();
        }

        public async Task<List<SongViewModel>> GetSongsAsync(int userId)
        {
            // the song catalog itself isn't per-user (matches viewprog.Song, which is unfiltered).
            return await db.Song.AsNoTracking()
                .Select(s => new SongViewModel
                {
                    Id = s.Id,
                    SongName = s.SongName,
                    ArtistName = s.ArtistName,
                    ThumbnailId = s.ThumbnailId,
                })
                .ToListAsync();
        }

        public async Task<bool> MergeSongsAsync(int userId, List<int> songIds, int? mainSongId = null)
        {
            if (songIds == null || !songIds.Any())
                return true;

            var resolvedMainSongId = mainSongId ?? songIds.Min();
            var otherSongIds = songIds.Where(id => id != resolvedMainSongId).ToList();

            var affectedPlaylistSongs = await db.PlaylistSong.Where(ps => songIds.Contains(ps.SongId)).ToListAsync();
            foreach (var playlistSong in affectedPlaylistSongs)
                playlistSong.SongId = resolvedMainSongId;

            if (otherSongIds.Any())
            {
                var otherStates = await db.SongState.Where(s => otherSongIds.Contains(s.SongId)).ToListAsync();
                db.SongState.RemoveRange(otherStates);

                var otherInfos = await db.SongInfo.Where(s => otherSongIds.Contains(s.SongId)).ToListAsync();
                db.SongInfo.RemoveRange(otherInfos);

                var otherSongs = await db.Song.Where(s => otherSongIds.Contains(s.Id)).ToListAsync();
                db.Song.RemoveRange(otherSongs);
            }

            await db.SaveChangesAsync();
            return true;
        }
    }
}
