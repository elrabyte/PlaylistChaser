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

            //string connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=playlistchaserdb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
            string connectionString = "Server=DESKTOP-AUKQ7J7\\SQLEXPRESS;Database=playlistchaserdb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
            optionsBuilder.UseSqlServer(connectionString);
        }


    }
}