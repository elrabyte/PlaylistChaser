using Google.Apis.YouTube.v3.Data;
using PlaylistChaser.Models;

namespace PlaylistChaser.Test
{
	public class YoutubeApiTest
	{
		YoutubeApiHelper youtubeApiHelper;

		#region Test Inputs
		const string VIDEO_URL = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
		const string VIDEO_ID = "dQw4w9WgXcQ";
		const string PLAYLIST_URL = "https://www.youtube.com/watch?v=dQw4w9WgXcQ&list=PL2MI040U_GXq1L5JUxNOulWCyXn-7QyZK";
		const string PLAYLIST_ID = "PL2MI040U_GXq1L5JUxNOulWCyXn-7QyZK";

		public YoutubeApiTest()
		{
			youtubeApiHelper = new YoutubeApiHelper();
		}
		Models.Playlist TestPlaylist = new Models.Playlist
		{
			Id = 1,
			YoutubeUrl = PLAYLIST_URL,
			YoutubeId = PLAYLIST_ID,
			Songs = new List<Song>(),
		};
		#endregion
		#region Test Results
		Models.Playlist ExpectedPlaylist = new Models.Playlist
		{
			Id = 1,
			YoutubeUrl = PLAYLIST_URL,
			YoutubeId = PLAYLIST_ID,
			Name = "Rick Astley Videos",
			ChannelName = "RickAstleyVEVO",
			Description = "Videos from the dapper British pop-soul singer.\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\nrick astley, never gonna give you up, together forever",
			Songs = new List<Song>(),
		};
		#endregion
		[Fact]
		public void Init()
		{
			youtubeApiHelper = new YoutubeApiHelper();

			Assert.NotNull(youtubeApiHelper);
		}

		[Fact]
		public void SyncPlaylist()
		{
			var actualPlaylist = youtubeApiHelper.SyncPlaylist(TestPlaylist);

			Assert.Equivalent(ExpectedPlaylist, actualPlaylist);
		}

		#region Get Stuff
		[Fact]
		public void GetPlaylistSongs()
		{
			var actualSongs = youtubeApiHelper.GetPlaylistSongs(TestPlaylist);
			Assert.True(actualSongs.Any());
		}
		[Fact]
		public async void GetPlaylistThumbnailBase64()
		{
			var actualPlaylistThumbnail = await youtubeApiHelper.GetPlaylistThumbnailBase64(TestPlaylist.YoutubeId);
			Assert.True(!string.IsNullOrEmpty(actualPlaylistThumbnail));
		}
		[Fact]
		public async void GetSongsThumbnailBase64ByPlaylist()
		{
			var actualThumbnails = await youtubeApiHelper.GetSongsThumbnailBase64ByPlaylist(TestPlaylist.YoutubeId);
			Assert.True(actualThumbnails.Any());
		}
		#endregion
		#region Edit Stuff
		[Fact]
		public void CreatePlaylist()
		{
			throw new NotImplementedException();
		}
		[Fact]
		public void DeletePlaylist()
		{
			throw new NotImplementedException();
		}
		#endregion

		#region Helper
		[Fact]
		public void GetVideoIdFromUrl()
		{
			var actualId = youtubeApiHelper.GetVideoIdFromUrl(VIDEO_URL);
			Assert.Equal(VIDEO_ID, actualId);
		}
		[Fact]
		public void GetPlaylistIdFromUrl()
		{
			var actualId = youtubeApiHelper.GetPlaylistIdFromUrl(PLAYLIST_URL);
			Assert.Equal(PLAYLIST_ID, actualId);
		}

		#endregion
	}
}