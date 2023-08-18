using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PlaylistChaser.Models;
using PlaylistChaser.Models.ViewModel;

namespace PlaylistChaser.Database
{
    public class PlaylistChaserDbContext : DbContext
    {
        #region 1:1 Views
        public DbSet<Playlist> Playlist { get; set; }
        public DbSet<Song> Song { get; set; }
        public DbSet<Thumbnail> Thumbnail{ get; set; }
        #endregion

        #region SP ViewModels
        private DbSet<PlaylistViewModel> PlaylistViewModel { get; set; }
        private DbSet<SongViewModel> SongViewModel { get; set; }
        #endregion

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {

            //string connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=playlistchaserdb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
            string connectionString = "Server=DESKTOP-AUKQ7J7\\SQLEXPRESS;Database=playlistchaserdb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
            optionsBuilder.UseSqlServer(connectionString);
        }

        public async Task<List<PlaylistViewModel>> GetPlaylists(int? playlistId = null)
        {
            var sql = "exec dbo.GetPlaylists @playlistId";
            return await PlaylistViewModel.FromSqlRaw(sql, new SqlParameter("playlistId", playlistId.HasValue ? playlistId.Value : DBNull.Value)).ToListAsync();
        }

		public async Task<List<SongViewModel>> GetSongs(int playlistId)
		{
			var sql = "exec dbo.GetSongs @playlistId";
			return await SongViewModel.FromSqlRaw(sql, new SqlParameter("playlistId", playlistId)).ToListAsync();
		}
	}
}