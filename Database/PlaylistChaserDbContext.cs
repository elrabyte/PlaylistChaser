using Microsoft.EntityFrameworkCore;
using PlaylistChaser.Models;

namespace PlaylistChaser.Database
{
    public class PlaylistChaserDbContext : DbContext
    {
        #region 1:1 Views
        public DbSet<PlaylistModel> Playlist { get; set; }
        public DbSet<SongModel> Song { get; set; }
        #endregion


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = Helper.ReadSecret("DB", "ConnectionString");
            optionsBuilder.UseMySQL("server=localhost;database=playlistchaserdb;user=root;password=1324");
        }


    }
}