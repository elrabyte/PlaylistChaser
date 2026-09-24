using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace PlaylistChaser.Web.Database.Abstractions
{
    /// <summary>
    /// Resolves the right <see cref="IPlaylistDataStore"/> implementation for the configured
    /// database provider (<c>Database:Provider</c> in appsettings: "SqlServer" (default) or
    /// "Postgres"), so callers (e.g. the REST API controllers) don't need to know which
    /// backend is active.
    /// </summary>
    public interface IPlaylistDataStoreFactory
    {
        Task<IPlaylistDataStore> CreateAsync(int userId);
    }

    public class PlaylistDataStoreFactory : IPlaylistDataStoreFactory
    {
        private readonly IConfiguration configuration;
        private readonly AdminDBContext dbAdmin;
        private readonly IMemoryCache memoryCache;

        public PlaylistDataStoreFactory(IConfiguration configuration, AdminDBContext dbAdmin, IMemoryCache memoryCache)
        {
            this.configuration = configuration;
            this.dbAdmin = dbAdmin;
            this.memoryCache = memoryCache;
        }

        public async Task<IPlaylistDataStore> CreateAsync(int userId)
        {
            var provider = configuration["Database:Provider"] ?? "SqlServer";

            if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                var user = await dbAdmin.AspNetUsers.SingleAsync(u => u.Id == userId);
                var userDbContext = new UserDbContext(new DbContextOptions<UserDbContext>(), memoryCache, configuration, user.DbUserName, user.DbPassword);
                return new SqlServerViewProgDataStore(userDbContext);
            }

            // Portable path: works for Postgres, SQLite, or SQL Server without per-user logins.
            return new EfCorePlaylistDataStore(dbAdmin);
        }
    }
}
