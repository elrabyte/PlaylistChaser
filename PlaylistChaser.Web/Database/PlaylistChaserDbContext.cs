using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PlaylistChaser.Web.Models;
using PlaylistChaser.Web.Models.ViewModel;

namespace PlaylistChaser.Web.Database
{
    public class PlaylistChaserDbContext : DbContext
    {
        public PlaylistChaserDbContext(DbContextOptions<PlaylistChaserDbContext> options) : base(options) { }

        #region 1:1 Views
        public DbSet<Playlist> Playlist { get; set; }
        public IQueryable<Playlist> PlaylistReadOnly
            => Playlist.AsNoTracking();
        public DbSet<PlaylistAdditionalInfo> PlaylistAdditionalInfo { get; set; }
        public IQueryable<PlaylistAdditionalInfo> PlaylistAdditionalInfoReadOnly
            => PlaylistAdditionalInfo.AsNoTracking();
        public DbSet<PlaylistSong> PlaylistSong { get; set; }
        public IQueryable<PlaylistSong> PlaylistSongReadOnly
            => PlaylistSong.AsNoTracking();
        public DbSet<PlaylistSongState> PlaylistSongState { get; set; }
        public IQueryable<PlaylistSongState> PlaylistSongStateReadOnly
            => PlaylistSongState.AsNoTracking();

        public DbSet<Song> Song { get; set; }
        public IQueryable<Song> SongReadOnly
            => Song.AsNoTracking();
        public DbSet<SongAdditionalInfo> SongAdditionalInfo { get; set; }
        public IQueryable<SongAdditionalInfo> SongAdditionalInfoReadOnly
            => SongAdditionalInfo.AsNoTracking();

        public DbSet<Thumbnail> Thumbnail { get; set; }
        public IQueryable<Thumbnail> ThumbnailReadOnly
            => Thumbnail.AsNoTracking();
        public DbSet<CombinedPlaylistEntry> CombinedPlaylistEntry { get; set; }
        public IQueryable<CombinedPlaylistEntry> CombinedPlaylistEntryReadOnly
            => CombinedPlaylistEntry.AsNoTracking();
        public DbSet<OAuth2Credential> OAuth2Credential { get; set; }
        public IQueryable<OAuth2Credential> OAuth2CredentialReadOnly
            => OAuth2Credential.AsNoTracking();
        public DbSet<Source> Source { get; set; }
        public IQueryable<Source> SourceReadOnly
            => Source.AsNoTracking();
        #endregion

        #region SP ViewModels
        private DbSet<PlaylistViewModel> PlaylistViewModel { get; set; }
        private DbSet<PlaylistSongViewModel> PlaylistSongViewModel { get; set; }
        private DbSet<SongViewModel> SongViewModel { get; set; }
        #endregion

        #region SPs
        public async Task<List<PlaylistViewModel>> GetPlaylists(int? playlistId = null)
        {
            var sql = "exec dbo.GetPlaylists @playlistId";
            return await PlaylistViewModel.FromSqlRaw(sql, new SqlParameter("playlistId", playlistId.HasValue ? playlistId.Value : DBNull.Value)).ToListAsync();
        }

        public async Task<List<PlaylistSongViewModel>> GetPlaylistSongs(int playlistId, int? limit = null)
        {
            var sql = "exec dbo.GetPlaylistSongs @playlistId, @limit";
            var playlistSongs = await PlaylistSongViewModel.FromSqlRaw(sql, new SqlParameter("playlistId", playlistId),
                                                                            new SqlParameter("limit", limit.HasValue ? limit.Value : DBNull.Value)).ToListAsync();

            return playlistSongs;
        }

        public async Task<List<SongViewModel>> GetSongs()
        {
            var sql = "exec dbo.GetSongs";
            var songs = await SongViewModel.FromSqlRaw(sql).ToListAsync();

            return songs;
        }

        #endregion

        public List<Source> GetSources()
            => SourceReadOnly.OrderBy(i => i.Name).ToList();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OAuth2Credential>()
                .HasKey(m => new { m.UserId, m.Provider });
            modelBuilder.Entity<SongAdditionalInfo>()
                .HasKey(m => new { m.SongId, m.SourceId });
            modelBuilder.Entity<PlaylistSongState>()
                .HasKey(m => new { m.PlaylistSongId, m.SourceId });
            modelBuilder.Entity<PlaylistAdditionalInfo>()
                .HasKey(m => new { m.PlaylistId, m.SourceId });
        }
    }
}