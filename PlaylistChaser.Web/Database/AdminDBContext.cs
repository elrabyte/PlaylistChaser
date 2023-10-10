using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PlaylistChaser.Web.Models;

namespace PlaylistChaser.Web.Database
{
    public class AdminDBContext : BaseDbContext
    {
        public AdminDBContext(DbContextOptions<AdminDBContext> options, IMemoryCache memoryCache, IConfiguration configuration) : base(options, memoryCache, configuration) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("dbo");
            base.OnModelCreating(modelBuilder);
        }        

        #region

        #endregion

        #region DbSet
        public DbSet<Playlist> Playlist { get; set; }
        public DbSet<PlaylistInfo> PlaylistInfo { get; set; }

        public DbSet<PlaylistSong> PlaylistSong { get; set; }
        public DbSet<PlaylistSongState> PlaylistSongState { get; set; }

        public DbSet<Song> Song { get; set; }
        public DbSet<SongInfo> SongInfo { get; set; }
        public DbSet<SongState> SongState { get; set; }

        public DbSet<Thumbnail> Thumbnail { get; set; }

        public DbSet<CombinedPlaylistEntry> CombinedPlaylistEntry { get; set; }

        public DbSet<OAuth2Credential> OAuth2Credential { get; set; }

        public DbSet<Source> Source { get; set; }

        public DbSet<User> AspNetUsers { get; set; }
        #endregion
    }
}