using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util; //used for SystemClock.Default - only used in prod
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using PlaylistChaser.Web.Models;
using System.Text.RegularExpressions;
using static PlaylistChaser.Web.Util.BuiltInIds;
using Playlist = Google.Apis.YouTube.v3.Data.Playlist;

namespace PlaylistChaser.Web.Util.API
{
    internal class YoutubeApiHelper : ISource
    {
        private YouTubeService ytService;
        private string clientId;
        private string clientSecret;

        public OAuth2Credential oAuthCredential;

        string[] scopes = { "https://www.googleapis.com/auth/youtube" };

        internal YoutubeApiHelper(string clientId, string clientSecret, DatabaseDataStore dataStore)
        {
            this.clientId = clientId;
            this.clientSecret = clientSecret;
            var accessToken = getAccessToken(dataStore);

            setYoutubeService(accessToken);
        }
        internal YoutubeApiHelper(string accessToken)
        {
            setYoutubeService(accessToken);
        }
        private void setYoutubeService(string accessToken)
        {
            ytService = new YouTubeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = new AccessTokenInitializer(accessToken),
                ApplicationName = "PlaylistChaser", // Provide a meaningful name for your application
            });
        }
        private string getAccessToken(DatabaseDataStore dataStore, int userId = 1)
        {
            var userCredential = GoogleWebAuthorizationBroker.AuthorizeAsync(new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret },
                                                                             scopes,
                                                                             userId.ToString(),
                                                                             CancellationToken.None,
                                                                             dataStore
                                                                            ).Result;

#if DEBUG == false
            if (userCredential.Token.IsExpired(SystemClock.Default))
#endif
            var refreshed = userCredential.RefreshTokenAsync(CancellationToken.None);
            return userCredential.Token.AccessToken;
        }

        #region Get Stuff
        /// <summary>
        /// Gets List of songs in local Song-Model by youtube playlist id
        /// </summary>
        /// <param name="playlist">local playlist</param>
        /// <returns></returns>
        public List<SongAdditionalInfo> GetPlaylistSongs(string playlistId)
            => toSongInfoModels(getPlaylistSongs(playlistId));

        /// <summary>
        /// returns the playlist thumbnail as a base64 string
        /// </summary>
        /// <param name="id">local playlist </param>
        /// <returns></returns>
        public async Task<byte[]> GetPlaylistThumbnail(string id)
        {
            var ytPlaylist = getPlaylist(id);

            var thumbnail = ytPlaylist.Snippet.Thumbnails.Maxres
                            ?? ytPlaylist.Snippet.Thumbnails.High
                            ?? ytPlaylist.Snippet.Thumbnails.Medium
                            ?? ytPlaylist.Snippet.Thumbnails.Standard
                            ?? ytPlaylist.Snippet.Thumbnails.Default__;


            return thumbnail != null ? await Helper.GetImageByUrl(thumbnail.Url) : null;
        }

        /// <summary>
        /// returns the song thumbnail as a base64 string
        /// </summary>
        /// <param name="id">youtube song id</param>
        /// <returns></returns>
        internal async Task<byte[]> GetSongThumbnail(string id)
        {
            var listRequest = ytService.Videos.List("snippet");
            listRequest.Id = id;
            var resp = listRequest.Execute();
            var song = resp.Items.Single().Snippet;

            return await Helper.GetImageByUrl(song.Thumbnails.Standard.Url);
        }

        /// <summary>
        /// returns a list with a thumbnail for each song
        /// </summary>
        /// <param name="playlistId">youtube playlist id</param>
        /// <returns></returns>
        internal async Task<Dictionary<string, byte[]>> GetSongsThumbnailByPlaylist(string playlistId)
        {
            var ytSongs = getPlaylistSongs(playlistId);
            var songThumbnails = new Dictionary<string, byte[]>();

            foreach (var ytSong in ytSongs)
            {
                if (!songThumbnails.ContainsKey(ytSong.ResourceId.VideoId))
                    songThumbnails.Add(ytSong.ResourceId.VideoId, ytSong.Thumbnails.Default__ == null ? null : await Helper.GetImageByUrl(ytSong.Thumbnails.Default__.Url));
            }
            return songThumbnails;
        }
        public async Task<Dictionary<string, byte[]>> GetSongsThumbnailBySongIds(List<string> songIds)
        {
            var ytSongs = getSongs(songIds);
            var songThumbnails = new Dictionary<string, byte[]>();

            foreach (var ytSong in ytSongs)
            {
                if (!songThumbnails.ContainsKey(ytSong.Id))
                    songThumbnails.Add(ytSong.Id, ytSong.Snippet.Thumbnails.Default__ == null ? null : await Helper.GetImageByUrl(ytSong.Snippet.Thumbnails.Default__.Url));
            }
            return songThumbnails;
        }

        /// <summary>
        /// get youtube playlist by id
        /// </summary>
        /// <param name="id">youtube playlist id</param>
        /// <returns></returns>
        private Playlist getPlaylist(string id)
        {

            var listRequest = ytService.Playlists.List("snippet,status");
            listRequest.Id = id;
            var playlist = listRequest.Execute().Items.Single();

            return playlist;
        }

        private bool checkIfMyPlaylist(string playlistId)
        {
            var listRequest = ytService.Playlists.List("status");
            listRequest.Mine = true;
            var myPlaylists = listRequest.Execute().Items;
            return myPlaylists.Select(p => p.Id).Contains(playlistId);
        }

        /// <summary>
        /// Returns a list of songs from a playlist
        /// </summary>
        /// <param name="playlistId">youtube playlist id</param>
        /// <returns></returns>
        private List<PlaylistItemSnippet> getPlaylistSongs(string playlistId)
        {
            var listRequest = ytService.PlaylistItems.List("snippet");
            listRequest.MaxResults = 50;
            listRequest.PlaylistId = playlistId;
            var resp = listRequest.Execute();
            var resultsShown = resp.PageInfo.ResultsPerPage;
            var totalResults = resp.PageInfo.TotalResults;

            var songs = resp.Items.Select(i => i.Snippet).ToList();
            if (totalResults == 0)
                return songs;

            while (resultsShown <= totalResults)
            {
                listRequest.PageToken = resp.NextPageToken;
                resp = listRequest.Execute();
                resultsShown += resp.PageInfo.ResultsPerPage;
                songs.AddRange(resp.Items.Select(i => i.Snippet).ToList());
            }
            return songs;
        }

        private List<Video> getSongs(List<string> songIds)
        {
            var songs = new List<Video>();

            //seperate songIds into multiple requests
            const int maxResults = 50;
            for (var i = 0; i <= songIds.Count; i += maxResults)
            {
                var rangeCount = maxResults;
                //if rangeCount exceeds maxResults, calc rest count
                if (i + maxResults > songIds.Count)
                    rangeCount = songIds.Count - i;

                var separatedSongIds = songIds.GetRange(i, rangeCount);
                var listRequest = ytService.Videos.List("snippet");
                listRequest.MaxResults = maxResults;
                listRequest.Id = string.Join(',', separatedSongIds);
                var resp = listRequest.Execute();

                songs.AddRange(resp.Items);
            }
            return songs;
        }
        #endregion

        #region Edit Stuff
        /// <summary>   
        /// Creates the Playlist on Youtube
        /// </summary>
        /// <param name="playlistName">Name of the Playlist</param>
        /// <returns>returns the YT-Playlist in local Model</returns>
        public async Task<PlaylistAdditionalInfo> CreatePlaylist(string playlistName, string? description = null, bool isPublic = true)
        {
            // Create a new, private playlist in the authorized user's channel.
            var newPlaylist = new Playlist();
            newPlaylist.Snippet = new PlaylistSnippet();
            newPlaylist.Snippet.Title = playlistName;
            newPlaylist.Snippet.Description = description;
            newPlaylist.Status = new PlaylistStatus();
            newPlaylist.Status.PrivacyStatus = isPublic ? "public" : "private";
            newPlaylist = await ytService.Playlists.Insert(newPlaylist, "snippet,status").ExecuteAsync();
            return toPlaylistModel(newPlaylist);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="playlistId"></param>
        /// <param name="songIds"></param>
        /// <returns>song ids at source </returns>
        public List<string> AddSongsToPlaylist(string playlistId, List<string> songIds)
        {
            var uploadedSongs = new List<string>();
            if (songIds.Count == 0) return uploadedSongs;

            try
            {
                foreach (var songId in songIds)
                {
                    var playlistItem = new PlaylistItem();
                    playlistItem.Snippet = new PlaylistItemSnippet();
                    playlistItem.Snippet.PlaylistId = playlistId;
                    playlistItem.Snippet.ResourceId = new ResourceId();
                    playlistItem.Snippet.ResourceId.Kind = "youtube#video";
                    playlistItem.Snippet.ResourceId.VideoId = songId;

                    var response = ytService.PlaylistItems.Insert(playlistItem, "snippet").Execute();
                    uploadedSongs.Add(songId);
                }
                return uploadedSongs;
            }
            catch (Google.GoogleApiException ex)
            {
                if (ex.Message == "The service youtube has thrown an exception. HttpStatusCode is Forbidden. The request cannot be completed because you have exceeded your <a href=\"/youtube/v3/getting-started#quota\">quota</a>.")
                {
                    //exceeded daily? requests limit 
                    var a = 1;
                }

                return uploadedSongs;
            }
        }

        public async Task<bool> DeletePlaylist(string youtubePlaylistId)
        {
            try
            {
                await ytService.Playlists.Delete(youtubePlaylistId).ExecuteAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }


        public async Task<bool> UpdatePlaylist(string playlistId, string? playlistName = null, string? description = null, bool isPublic = true)
        {
            try
            {
                var playlist = getPlaylist(playlistId);
                playlist.Snippet.Title = playlistName;
                playlist.Snippet.Description = description;
                playlist.Status.PrivacyStatus = isPublic ? "public" : "private";
                await ytService.Playlists.Update(playlist, "snippet,status").ExecuteAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }
        #endregion

        #region Interface Implementations
        public PlaylistAdditionalInfo GetPlaylistById(string playlistId)
            => toPlaylistModel(getPlaylist(playlistId));
        public (List<(int Id, string IdAtSource)> Exact, List<(int Id, string IdAtSource)> NotExact) FindSongs(List<(int SongId, string ArtistName, string SongName)> songs)
        {
            var foundSongsExact = new List<(int Id, string SpotifyId)>();
            var foundSongs = new List<(int Id, string SpotifyId)>();
            try
            {
                foreach (var song in songs)
                {
                    var videoId = searchSongExact(song.ArtistName, song.SongName);
                    foundSongsExact.Add(new(song.SongId, videoId));
                }
                return (foundSongsExact, foundSongs);
            }
            catch (Exception)
            {
                return (foundSongsExact, foundSongs);
            }
        }
        #endregion

        #region model
        private PlaylistAdditionalInfo toPlaylistModel(Playlist ytPlaylist)
        {
            return new PlaylistAdditionalInfo
            {
                PlaylistIdSource = ytPlaylist.Id,
                Name = ytPlaylist.Snippet.Title,
                CreatorName = ytPlaylist.Snippet.ChannelTitle,
                Description = string.IsNullOrEmpty(ytPlaylist.Snippet.Description) ? null : ytPlaylist.Snippet.Description,
                SourceId = Sources.Youtube,
                IsMine = checkIfMyPlaylist(ytPlaylist.Id),
            };
        }

        private List<SongAdditionalInfo> toSongInfoModels(List<PlaylistItemSnippet> ytSongs)
        {
            var songs = new List<(Song Song, SongAdditionalInfo Info)>();

            return ytSongs.Select(ytSong => new SongAdditionalInfo
            {
                SongIdSource = ytSong.ResourceId.VideoId,
                Name = ytSong.Title,
                ArtistName = ytSong.VideoOwnerChannelTitle,
                SourceId = Sources.Youtube
            }).ToList();
        }
        #endregion

        #region helper
        internal string GetVideoIdFromUrl(string url)
        {
            var pattern = @"(?:youtube\.com\/(?:[^\/]+\/.+\/|(?:v|e(?:mbed)?)\/|.*[?&]v=)|youtu\.be\/)([^""&?\/\s]{11})";
            Regex rg = new Regex(pattern);
            var match = rg.Match(url);
            return match.Groups[1].Value;
        }
        internal string GetPlaylistIdFromUrl(string url)
        {
            var pattern = @"[?&]list=([^#\&\?]+)";
            Regex rg = new Regex(pattern);
            var match = rg.Match(url);
            return match.Groups[1].Value;
        }
        #endregion

        private string searchSongExact(string artistName, string songName)
        {
            // Search for the song using artist name and song name
            var searchListRequest = ytService.Search.List("snippet");
            searchListRequest.Q = $"{artistName} {songName}"; // Combine artist name and song name
            searchListRequest.Type = "youtube#video";
            searchListRequest.MaxResults = 1; // Number of results to retrieve

            return searchListRequest.Execute().Items.First().Id.VideoId; //first instead of single, because for some reason it sometimes returns more than 1 result
        }
    }
}
