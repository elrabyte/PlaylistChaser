using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace PlaylistChaser.Web.Database
{
    /// <summary>
    /// Design-time factory used by `dotnet ef migrations add` to create the PostgreSQL-flavored
    /// migrations under Migrations/Postgres (see Database:Provider in appsettings / AGENTS.md).
    /// Not used at runtime - Program.cs configures AdminDBContext directly there.
    /// </summary>
    public class AdminDBContextPostgresFactory : IDesignTimeDbContextFactory<AdminDBContext>
    {
        public AdminDBContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:ServerConnectionString"] = "Host=localhost;Database=playlistchaser;Username=playlistchaser;Password=playlistchaser;",
                })
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<AdminDBContext>();
            optionsBuilder.UseNpgsql(configuration.GetConnectionString("ServerConnectionString"),
                npgsql => npgsql.MigrationsAssembly("PlaylistChaser.Web"));

            return new AdminDBContext(optionsBuilder.Options, new MemoryCache(new MemoryCacheOptions()), configuration);
        }
    }
}
