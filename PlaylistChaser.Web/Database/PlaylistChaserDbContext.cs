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
        public DbSet<PlaylistAdditionalInfo> PlaylistAdditionalInfo { get; set; }
        public DbSet<PlaylistSong> PlaylistSong { get; set; }
        public DbSet<PlaylistSongState> PlaylistSongState { get; set; }

        public DbSet<Song> Song { get; set; }
        public DbSet<SongAdditionalInfo> SongAdditionalInfo { get; set; }
        public DbSet<SongState> SongState { get; set; }
        public DbSet<Thumbnail> Thumbnail { get; set; }
        public DbSet<CombinedPlaylistEntry> CombinedPlaylistEntry { get; set; }
        public DbSet<OAuth2Credential> OAuth2Credential { get; set; }
        #endregion

        #region SP ViewModels
        private DbSet<PlaylistViewModel> PlaylistViewModel { get; set; }
        private DbSet<PlaylistSongViewModel> SongViewModel { get; set; }
        #endregion

        #region SPs
        public async Task<List<PlaylistViewModel>> GetPlaylists(int? playlistId = null)
        {
            var sql = "exec dbo.GetPlaylists @playlistId";
            return await PlaylistViewModel.FromSqlRaw(sql, new SqlParameter("playlistId", playlistId.HasValue ? playlistId.Value : DBNull.Value)).ToListAsync();
        }

        public async Task<List<PlaylistSongViewModel>> GetSongs(int playlistId, bool addStates = true)
        {
            var sql = "exec dbo.GetPlaylistSongs @playlistId";
            var playlistSongs = await SongViewModel.FromSqlRaw(sql, new SqlParameter("playlistId", playlistId)).ToListAsync();

            if (addStates)
            {
                playlistSongs.ForEach(ps => ps.PlaylistSongStates = PlaylistSongState.Where(pss => pss.PlaylistSongId == ps.PlaylistSongId).ToList());
                playlistSongs.ForEach(ps => ps.SongStates = SongState.Where(ss => ss.SongId == ps.SongId).ToList());
            }

            return playlistSongs;

        }
        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OAuth2Credential>()
                .HasKey(oauth => new { oauth.UserId, oauth.Provider });

            // Other entity configurations...
        }
    }
}