using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PlaylistChaser.Web.Models;
using PlaylistChaser.Web.Models.ViewModel;
using System.Data;

namespace PlaylistChaser.Web.Database
{
    public class PlaylistChaserDbContext : DbContext
    {
        protected readonly IMemoryCache memoryCache;
        public PlaylistChaserDbContext(DbContextOptions<PlaylistChaserDbContext> options, IMemoryCache memoryCache) : base(options)
        {
            this.memoryCache = memoryCache;
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


        public async Task<bool> MergeSongs(List<int> songIds, int? mainSongId = null)
        {
            var sql = "exec dbo.MergeSongs @songIds, @mainSongId";
            var success = await Database.ExecuteSqlRawAsync(sql, GetParameterFromList("songIds", "List_int", "Id", songIds),
                                                                 GetParameter("mainSongId", mainSongId));

            return true;
        }

        #endregion

        #region DataHelper
        public List<Source> GetSources(List<Util.BuiltInIds.Sources> filteredSources = null)
        {
            var sources = GetCachedList(Source).OrderBy(i => i.Name).ToList();
            if (filteredSources != null && filteredSources.Any())
                sources = sources.Where(s => filteredSources.Contains((Util.BuiltInIds.Sources)s.Id)).ToList();
            return sources;
        }

        public IQueryable<TEntity> GetReadOnlyQuery<TEntity>(DbSet<TEntity> dbSet) where TEntity : class
            => dbSet.AsNoTracking();
        public List<TEntity> GetCachedList<TEntity>(DbSet<TEntity> dbSet) where TEntity : class
        {
            var tableName = dbSet.EntityType.GetTableName();

            // Attempt to retrieve the data from cache
            var cachedData = memoryCache.Get(tableName) as List<TEntity>;

            if (cachedData == null)
            {
                // If data is not found in cache, retrieve it from the database
                cachedData = dbSet.AsNoTracking().ToList();

                if (cachedData.Any())
                {
                    memoryCache.Set(tableName, cachedData);
                }
            }

            return cachedData;
        }
        #endregion

        #region Override
        public override int SaveChanges()
        {
            // Get all the entries that have been modified
            var modifiedEntries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Modified
                            || e.State == EntityState.Added
                            || e.State == EntityState.Deleted)
                .ToList();
            if (!modifiedEntries.Any())
                return base.SaveChanges();

            var modifiedMetadatas = modifiedEntries.GroupBy(e => e.Metadata).Select(e => e.Key).ToList();
            foreach (var metaData in modifiedMetadatas)
            {
                //remove from cache
                memoryCache.Remove(metaData.GetTableName());
            }

            // Call the base SaveChanges to save the changes to the database
            return base.SaveChanges();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureCompositeKey<OAuth2Credential>(modelBuilder);
            ConfigureCompositeKey<SongInfo>(modelBuilder);
            ConfigureCompositeKey<SongState>(modelBuilder);
            ConfigureCompositeKey<PlaylistSongState>(modelBuilder);
            ConfigureCompositeKey<PlaylistInfo>(modelBuilder);
        }

        private void ConfigureCompositeKey<TEntity>(ModelBuilder modelBuilder)
            where TEntity : class
        {
            var entityType = modelBuilder.Model.FindEntityType(typeof(TEntity));
            if (entityType != null)
            {
                var primaryKeyPropertyNames = entityType.FindPrimaryKey().Properties.Select(p => p.Name).ToArray();
                var keyProperties = typeof(TEntity)
                    .GetProperties()
                    .Where(p => primaryKeyPropertyNames.Contains(p.Name))
                    .Select(p => p.Name)
                    .ToArray();

                modelBuilder.Entity<TEntity>().HasKey(keyProperties);
            }
        }
        #endregion

        #region SqlParameter
        public static SqlParameter GetParameter<T>(string name, T value)
        {
            var type = typeof(T);
            bool nullable = Nullable.GetUnderlyingType(type) != null;

            var dbType = SqlDbType.NVarChar;

            if (type.IsAssignableFrom(typeof(int)))
            {
                dbType = SqlDbType.Int;
            }

            return new SqlParameter(name, ((object)value) ?? DBNull.Value);
        }
        public static SqlParameter GetParameterFromList<T>(string name, string typeName, string listFieldName, List<T> valuesList)
        {
            var dt = new DataTable();
            dt.Columns.Add(listFieldName);

            if (valuesList != null)
            {
                foreach (var val in valuesList)
                    dt.Rows.Add(val);
            }

            var valParam = new SqlParameter(name, SqlDbType.Structured) { Value = dt, TypeName = typeName };
            return valParam;
        }
        #endregion
    }
}