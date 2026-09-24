using PlaylistChaser.Web.Models.ViewModel;

namespace PlaylistChaser.Web.Database.Abstractions
{
    /// <summary>
    /// Default/production implementation of <see cref="IPlaylistDataStore"/>: delegates to the
    /// existing per-user <see cref="UserDbContext"/>, which calls the SQL-Server-only VIEWPROG
    /// stored procedures. Security (which rows a user can see) is enforced natively by SQL
    /// Server via the per-user database login the connection was opened with, so the
    /// <c>userId</c> parameters here are accepted for interface parity but are not needed to
    /// filter results - the database already scopes everything to the connected user.
    /// </summary>
    public class SqlServerViewProgDataStore : IPlaylistDataStore
    {
        private readonly UserDbContext userDbContext;

        public SqlServerViewProgDataStore(UserDbContext userDbContext)
        {
            this.userDbContext = userDbContext;
        }

        public Task<List<PlaylistViewModel>> GetPlaylistsAsync(int userId, int? playlistId = null)
            => userDbContext.GetPlaylists(playlistId);

        public Task<List<PlaylistSongViewModel>> GetPlaylistSongsAsync(int userId, int playlistId, int? limit = null)
            => userDbContext.GetPlaylistSongs(playlistId, limit);

        public Task<List<SongViewModel>> GetSongsAsync(int userId)
            => userDbContext.GetSongs();

        public Task<bool> MergeSongsAsync(int userId, List<int> songIds, int? mainSongId = null)
            => userDbContext.MergeSongs(songIds, mainSongId);
    }
}
