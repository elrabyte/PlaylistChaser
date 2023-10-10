using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PlaylistChaser.Web.Models;
using System.Data;

namespace PlaylistChaser.Web.Database
{
    //public class BaseDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    public class BaseDbContext : DbContext
    {
        protected readonly IMemoryCache memoryCache;
        protected readonly IConfiguration configuration;
        public BaseDbContext(DbContextOptions options, IMemoryCache memoryCache, IConfiguration configuration) : base(options)
        {
            this.memoryCache = memoryCache;
            this.configuration = configuration;
        }
        
        #region DataHelper        

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

            // Configure Identity tables
            modelBuilder.Entity<User>().ToTable("AspNetUsers").HasKey(u => u.Id);
            modelBuilder.Entity<IdentityRole<int>>().ToTable("AspNetRoles").HasKey(r => r.Id);
            modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("AspNetUserClaims").HasKey(uc => uc.Id);
            modelBuilder.Entity<IdentityUserRole<int>>().ToTable("AspNetUserRoles").HasKey(ur => new { ur.UserId, ur.RoleId });
            modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("AspNetUserLogins").HasKey(ul => new { ul.LoginProvider, ul.ProviderKey });
            modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("AspNetRoleClaims").HasKey(rc => rc.Id);
            modelBuilder.Entity<IdentityUserToken<int>>().ToTable("AspNetUserTokens").HasKey(ut => new { ut.UserId, ut.LoginProvider, ut.Name });

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

        public static void GenerateDbCredentials(string userName, string password, out string dbUserName, out string dbPassword)
        {
            dbUserName = userName;
            dbUserName = dbUserName.Replace("@", "").Replace(".", "").Replace("-", "").Replace("+", "");
            dbPassword = password;
        }
        public async Task<bool> CreateDBUser(string dbUsername, string dbPasswordClear)
        {
            var sql = "exec sp_addlogin @loginname, @passwd";
            var result = await Database.ExecuteSqlRawAsync(sql, GetParameter("loginname", dbUsername),
                                                                GetParameter("passwd", dbPasswordClear));

            sql = "exec sp_adduser @loginname, @name_in_db";
            result = await Database.ExecuteSqlRawAsync(sql, GetParameter("loginname", dbUsername),
                                                            GetParameter("name_in_db", dbUsername));

            sql = "exec sp_addrolemember @rolename, @loginname";
            result = await Database.ExecuteSqlRawAsync(sql, GetParameter("rolename", "registered_user"),
                                                            GetParameter("loginname", dbUsername));

            return result != 0;
        }
    }
}