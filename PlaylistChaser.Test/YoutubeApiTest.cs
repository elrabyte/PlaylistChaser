using PlaylistChaser.Web.Models;
using PlaylistChaser.Web.Util.API;

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
            var clientId = "";
            var clientSecret = "";
            youtubeApiHelper = new YoutubeApiHelper(clientId, clientSecret);
        }
        Playlist TestPlaylist = new Playlist
        {
            Id = 1,
        };
        PlaylistAdditionalInfo TestPlaylistInfoYT = new PlaylistAdditionalInfo
        {
            PlaylistId = 0,
            PlaylistIdSource = PLAYLIST_ID,
            SourceId = Web.Util.BuiltInIds.Sources.Youtube,
        };
        #endregion
        #region Test Results
        Playlist ExpectedPlaylist = new Playlist
        {
            Id = 1,
            Name = "Rick Astley Videos",
            ChannelName = "RickAstleyVEVO",
            Description = "Videos from the dapper British pop-soul singer.\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\nrick astley, never gonna give you up, together forever",
        };
        PlaylistAdditionalInfo ExpectedPlaylistInfoYT = new PlaylistAdditionalInfo
        {
            CreatorName = "RickAstleyVEVO",
            Description = "Videos from the dapper British pop-soul singer.\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\nrick astley, never gonna give you up, together forever",
            Id = null,
            IsMine = false,
            PlaylistId = 0,
            PlaylistIdSource = PLAYLIST_ID,
            Name = "Rick Astley Videos",
            SourceId = Web.Util.BuiltInIds.Sources.Youtube,
            Url = null,
        };
        #endregion
        [Fact]
        public void Init()
        {
            Assert.NotNull(youtubeApiHelper);
        }

        #region Get Stuff
        [Fact]
        public void GetPlaylistSongs()
        {
            var actualSongs = youtubeApiHelper.GetPlaylistSongs(TestPlaylistInfoYT.PlaylistIdSource);
            Assert.True(actualSongs.Any());
        }
        [Fact]
        public async void GetPlaylistThumbnailBase64()
        {
            var actualPlaylistThumbnail = await youtubeApiHelper.GetPlaylistThumbnail(TestPlaylistInfoYT.PlaylistIdSource);
            Assert.True(actualPlaylistThumbnail.Any());
        }
        [Fact]
        public async void GetSongsThumbnailBase64ByPlaylist()
        {
            var actualThumbnails = await youtubeApiHelper.GetSongsThumbnailByPlaylist(TestPlaylistInfoYT.PlaylistIdSource);
            Assert.True(actualThumbnails.Any());
        }
        #endregion
        #region Edit Stuff
        const string NEW_PLAYLIST_NAME = "Test - 7895";
        const string NEW_PLAYLIST_DESC = "TestDescritption - 79879";

        [Fact]
        public void GetPlaylist()
        {
            var testPLaylist = youtubeApiHelper.GetPlaylistById(PLAYLIST_ID);
            Assert.Equivalent(testPLaylist, ExpectedPlaylistInfoYT);

        }

        [Fact]
        public async void PlaylistLifeCycle()
        {
            string newPlaylistSourceId;

            //create test
            var newPlaylistInfo = youtubeApiHelper.CreatePlaylist(NEW_PLAYLIST_NAME, NEW_PLAYLIST_DESC).Result;
            newPlaylistSourceId = newPlaylistInfo.PlaylistIdSource;

            //delete test
            await youtubeApiHelper.DeletePlaylist(newPlaylistSourceId);
            try
            {
                youtubeApiHelper.GetPlaylistById(newPlaylistSourceId);
                Assert.Fail("Playlist wasn't deleted");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("quota"))
                    Assert.Fail("Quota exceeded");
            }
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