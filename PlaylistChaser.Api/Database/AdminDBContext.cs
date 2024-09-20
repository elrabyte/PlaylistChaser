using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PlaylistChaser.Model;

namespace PlaylistChaser.Api.Database
{
    public class AdminDBContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public AdminDBContext(DbContextOptions<AdminDBContext> options) : base(options) { }


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

        public DbSet<User> User { get; set; }

    }
}