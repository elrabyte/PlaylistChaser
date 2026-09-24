using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using PlaylistChaser.Web.Database;
using PlaylistChaser.Web.Database.Abstractions;
using PlaylistChaser.Web.Models;
using static PlaylistChaser.Web.Util.BuiltInIds;

namespace PlaylistChaser.Test
{
    /// <summary>
    /// Exercises the provider-agnostic "swap the database backend" path
    /// (Database/Abstractions/EfCorePlaylistDataStore) end-to-end against a real SQL engine
    /// (SQLite, in-memory) rather than just asserting it compiles. This is the same code path
    /// used when Database:Provider is set to "Postgres" (e.g. in the docker-compose setup) -
    /// SQLite is used here purely so the test doesn't require installing/running a real
    /// Postgres or SQL Server instance.
    /// </summary>
    public class EfCorePlaylistDataStoreTests : IDisposable
    {
        private readonly SqliteConnection connection;
        private readonly AdminDBContext db;
        private readonly EfCorePlaylistDataStore store;

        public EfCorePlaylistDataStoreTests()
        {
            // an open in-memory SQLite connection is kept alive for the lifetime of the context;
            // closing it (Dispose) tears the in-memory database down.
            connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<AdminDBContext>()
                .UseSqlite(connection)
                .Options;
            var configuration = new ConfigurationBuilder().Build();
            db = new AdminDBContext(options, new MemoryCache(new MemoryCacheOptions()), configuration);
            db.Database.EnsureCreated();

            store = new EfCorePlaylistDataStore(db);
        }

        public void Dispose()
        {
            db.Dispose();
            connection.Dispose();
        }

        private void SeedPlaylist(int playlistId, int userId, string name, params (int songId, string songName)[] songs)
        {
            db.Playlist.Add(new Playlist
            {
                Id = playlistId,
                Name = name,
                ChannelName = "Test Channel",
                PlaylistTypeId = PLaylistTypes.Simple,
                UserId = userId,
            });

            foreach (var (songId, songName) in songs)
            {
                if (!db.Song.Any(s => s.Id == songId))
                    db.Song.Add(new Song { Id = songId, SongName = songName, ArtistName = "Test Artist" });

                db.PlaylistSong.Add(new PlaylistSong { PlaylistId = playlistId, SongId = songId });
            }

            db.SaveChanges();
        }

        [Fact]
        public async Task GetPlaylistsAsync_OnlyReturnsPlaylistsOwnedByTheRequestingUser()
        {
            SeedPlaylist(1, userId: 10, "User 10's playlist", (100, "Song A"), (101, "Song B"));
            SeedPlaylist(2, userId: 20, "User 20's playlist", (200, "Song C"));

            var user10Playlists = await store.GetPlaylistsAsync(userId: 10);
            var user20Playlists = await store.GetPlaylistsAsync(userId: 20);

            var user10Playlist = Assert.Single(user10Playlists);
            Assert.Equal("User 10's playlist", user10Playlist.Name);
            Assert.Equal(2, user10Playlist.SongsTotal);
            Assert.Equal("Simple", user10Playlist.PlaylistTypeName);

            var user20Playlist = Assert.Single(user20Playlists);
            Assert.Equal("User 20's playlist", user20Playlist.Name);
            Assert.Equal(1, user20Playlist.SongsTotal);
        }

        [Fact]
        public async Task GetPlaylistSongsAsync_ReturnsEmpty_WhenPlaylistIsNotOwnedByTheRequestingUser()
        {
            SeedPlaylist(1, userId: 10, "User 10's playlist", (100, "Song A"));

            // user 20 trying to read user 10's playlist songs - this is the application-level
            // security check that replaces SQL Server's native per-user-login row-level security.
            var songsForWrongUser = await store.GetPlaylistSongsAsync(userId: 20, playlistId: 1);
            var songsForOwner = await store.GetPlaylistSongsAsync(userId: 10, playlistId: 1);

            Assert.Empty(songsForWrongUser);
            Assert.Single(songsForOwner);
        }

        [Fact]
        public async Task GetSongsAsync_ReturnsTheWholeCatalog_RegardlessOfUser()
        {
            SeedPlaylist(1, userId: 10, "Playlist", (100, "Song A"), (101, "Song B"));

            var songs = await store.GetSongsAsync(userId: 999); // arbitrary user, catalog is global

            Assert.Equal(2, songs.Count);
        }

        [Fact]
        public async Task MergeSongsAsync_RepointsPlaylistSongsAndRemovesDuplicates()
        {
            SeedPlaylist(1, userId: 10, "Playlist", (100, "Song A (dup 1)"), (101, "Song A (dup 2)"), (102, "Unrelated"));

            var success = await store.MergeSongsAsync(userId: 10, songIds: new List<int> { 100, 101 }, mainSongId: 100);

            Assert.True(success);
            Assert.Equal(3, db.PlaylistSong.Count()); // playlist links stay, just repointed
            Assert.All(db.PlaylistSong.Where(ps => ps.SongId != 102), ps => Assert.Equal(100, ps.SongId));
            Assert.Null(db.Song.SingleOrDefault(s => s.Id == 101)); // duplicate removed
            Assert.NotNull(db.Song.SingleOrDefault(s => s.Id == 100)); // main song kept
            Assert.NotNull(db.Song.SingleOrDefault(s => s.Id == 102)); // unrelated song untouched
        }
    }
}
