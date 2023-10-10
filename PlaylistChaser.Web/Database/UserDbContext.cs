using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PlaylistChaser.Web.Models;
using PlaylistChaser.Web.Models.ViewModel;

namespace PlaylistChaser.Web.Database
{
    public class UserDbContext : BaseDbContext
    {
        string dbUserName;
        string dbPassword;
        public UserDbContext(DbContextOptions<UserDbContext> options, IMemoryCache memoryCache, IConfiguration configuration, string dbUserName, string dbPassword) : base(options, memoryCache, configuration) {
            this.dbUserName = dbUserName;
            this.dbPassword = dbPassword;
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("VIEWPROG");
            base.OnModelCreating(modelBuilder);
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var baseConnectionString = configuration.GetConnectionString("BaseConnectionString");
            string connectionString = string.Format("{0}User Id={1};Password={2};", baseConnectionString, dbUserName, dbPassword);
            optionsBuilder.UseSqlServer(connectionString);
        }

        public List<Source> GetSources(List<Util.BuiltInIds.Sources> filteredSources = null)
        {
            var sources = GetCachedList(Source).OrderBy(i => i.Name).ToList();
            if (filteredSources != null && filteredSources.Any())
                sources = sources.Where(s => filteredSources.Contains((Util.BuiltInIds.Sources)s.Id)).ToList();
            return sources;
        }

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
        #endregion

        #region SP ViewModels
        private DbSet<PlaylistViewModel> PlaylistViewModel { get; set; }
        private DbSet<PlaylistSongViewModel> PlaylistSongViewModel { get; set; }
        private DbSet<SongViewModel> SongViewModel { get; set; }
        #endregion

        #region SP
        public async Task<List<PlaylistViewModel>> GetPlaylists(int? playlistId = null)
        {
            var sql = "exec viewprog.GetPlaylists @playlistId";
            return await PlaylistViewModel.FromSqlRaw(sql, new SqlParameter("playlistId", playlistId.HasValue ? playlistId.Value : DBNull.Value)).ToListAsync();
        }

        public async Task<List<PlaylistSongViewModel>> GetPlaylistSongs(int playlistId, int? limit = null)
        {
            var sql = "exec viewprog.GetPlaylistSongs @playlistId, @limit";
            var playlistSongs = await PlaylistSongViewModel.FromSqlRaw(sql, new SqlParameter("playlistId", playlistId),
                                                                            new SqlParameter("limit", limit.HasValue ? limit.Value : DBNull.Value)).ToListAsync();

            return playlistSongs;
        }

        public async Task<List<SongViewModel>> GetSongs()
        {
            var sql = "exec viewprog.GetSongs";
            var songs = await SongViewModel.FromSqlRaw(sql).ToListAsync();

            return songs;
        }


        public async Task<bool> MergeSongs(List<int> songIds, int? mainSongId = null)
        {
            var sql = "exec viewprog.MergeSongs @songIds, @mainSongId";
            var success = await Database.ExecuteSqlRawAsync(sql, GetParameterFromList("songIds", "List_int", "Id", songIds),
                                                                 GetParameter("mainSongId", mainSongId));

            return true;
        }

        #endregion
    }
}