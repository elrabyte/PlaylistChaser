using PlaylistChaser.Web.Models.ViewModel;

namespace PlaylistChaser.Web.Database.Abstractions
{
    /// <summary>
    /// Abstracts the "read model" queries that the legacy SQL Server database project
    /// (<c>PlaylistChaser.DB</c>) implements as VIEWPROG stored procedures backed by
    /// per-user database logins (native row-level security via <c>current_user</c>).
    ///
    /// This is the seam that makes the database backend swappable: implement this
    /// interface against a different provider/storage strategy and register it in
    /// Program.cs (see <c>Database:Provider</c> configuration) without touching
    /// controllers or other call sites.
    ///
    /// Two implementations are provided:
    /// - <see cref="SqlServerViewProgDataStore"/>: the original behavior, delegating to the
    ///   SQL-Server-only VIEWPROG stored procedures via a per-user <see cref="UserDbContext"/>.
    ///   Security is enforced natively by SQL Server (each app user maps to a real DB login).
    /// - <see cref="EfCorePlaylistDataStore"/>: a provider-agnostic implementation using plain
    ///   EF Core LINQ queries against a single shared connection (works with SQL Server,
    ///   PostgreSQL, SQLite, ...). Security is enforced at the application layer by explicitly
    ///   filtering on the supplied <c>userId</c> instead of relying on native DB principals.
    /// </summary>
    public interface IPlaylistDataStore
    {
        Task<List<PlaylistViewModel>> GetPlaylistsAsync(int userId, int? playlistId = null);
        Task<List<PlaylistSongViewModel>> GetPlaylistSongsAsync(int userId, int playlistId, int? limit = null);
        Task<List<SongViewModel>> GetSongsAsync(int userId);
        Task<bool> MergeSongsAsync(int userId, List<int> songIds, int? mainSongId = null);
    }
}
