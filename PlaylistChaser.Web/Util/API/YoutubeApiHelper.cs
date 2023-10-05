using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using PlaylistChaser.Web.Models;
using System.Text.RegularExpressions;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;
using YoutubeExplode.Search;
using static PlaylistChaser.Web.Util.BuiltInIds;
using Playlist = Google.Apis.YouTube.v3.Data.Playlist;

namespace PlaylistChaser.Web.Util.API
{
    internal class YoutubeApiHelper : ISource
    {
        private YouTubeService ytService;
        private YoutubeClient ytServiceReadOnly;
        static string[] scopes = { YouTubeService.Scope.Youtube };

        public static string PlaylistUrlStart = "https://www.youtube.com/playlist?list=";

        internal YoutubeApiHelper(string accessToken)
        {
            ytService = new YouTubeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = new AccessTokenInitializer(accessToken),
                ApplicationName = "PlaylistChaser"
            });
            ytServiceReadOnly = new YoutubeClient();
        }

        #region Playlist
        public PlaylistInfo GetPlaylistByUrl(string playlistUrl)
            => GetPlaylistById(GetPlaylistIdFromUrl(playlistUrl));
        public PlaylistInfo GetPlaylistById(string playlistId)
            => toPlaylistModel(getPlaylist(playlistId));

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
        private YoutubeExplode.Playlists.Playlist getPlaylistReadonly(string id)
        {
            var listRequest = ytServiceReadOnly.Playlists.GetAsync(id).Result;

            return listRequest;
        }

        private bool checkIfMyPlaylist(string playlistId)
        {
            var listRequest = ytService.Playlists.List("status");
            listRequest.Mine = true;
            var myPlaylists = listRequest.Execute().Items;
            return myPlaylists.Select(p => p.Id).Contains(playlistId);
        }

        /// <summary>   
        /// Creates the Playlist on Youtube
        /// </summary>
        /// <param name="playlistName">Name of the Playlist</param>
        /// <returns>returns the YT-Playlist in local Model</returns>
        public async Task<PlaylistInfo> CreatePlaylist(string playlistName, string? description = null, bool isPublic = true)
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

        public async Task<ReturnModel> DeletePlaylist(string youtubePlaylistId)
        {
            try
            {
                await ytService.Playlists.Delete(youtubePlaylistId).ExecuteAsync();

                return new ReturnModel();
            }
            catch (Exception ex)
            {
                return new ReturnModel(ex.Message);
            }

        }

        public async Task<ReturnModel> UpdatePlaylist(string playlistId, string? playlistName = null, string? description = null, bool isPublic = true)
        {
            try
            {
                var playlist = getPlaylist(playlistId);
                playlist.Snippet.Title = playlistName;
                playlist.Snippet.Description = description;
                playlist.Status.PrivacyStatus = isPublic ? "public" : "private";
                await ytService.Playlists.Update(playlist, "snippet,status").ExecuteAsync();
                return new ReturnModel();
            }
            catch (Exception ex)
            {
                return new ReturnModel(ex.Message);
            }

        }
        #endregion

        #region OAuth Credential

        /// <summary>
        /// Refresh Existing Token
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <param name="refreshToken"></param>
        /// <returns></returns>
        static async internal Task<OAuth2Credential> GetOauthCredential(string clientId, string clientSecret, string refreshToken, int userId)
        {
            var oAuth = await getToken(clientId, clientSecret, userId, refreshToken: refreshToken);
            return oAuth;
        }

        /// <summary>
        /// Create new Token
        /// </summary>
        /// <param name="code"></param>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <param name="redirectUri"></param>
        /// <returns></returns>
        static async internal Task<OAuth2Credential> GetOauthCredential(string code, string clientId, string clientSecret, string redirectUri, int userId)
        {
            var oAuth = await getToken(clientId, clientSecret, userId, code: code, redirectUri: redirectUri);
            return oAuth;
        }
        private static async Task<OAuth2Credential> getToken(string clientId, string clientSecret, int userId, string? refreshToken = null, string? code = null, string? redirectUri = null)
        {
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                },
                Scopes = scopes,
                DataStore = new NullDataStore()
            });

            TokenResponse credential = null;
            if (refreshToken != null)
                credential = await flow.RefreshTokenAsync(userId.ToString(), refreshToken, CancellationToken.None);
            else if (code != null && redirectUri != null)
                credential = await flow.ExchangeCodeForTokenAsync(userId.ToString(), code, redirectUri, CancellationToken.None);

            var oAuth = new OAuth2Credential
            {
                Provider = Sources.Youtube.ToString(),
                AccessToken = credential.AccessToken,
                RefreshToken = credential.RefreshToken,
                TokenExpiration = DateTime.Now.AddSeconds((double)credential.ExpiresInSeconds),
                UserId = userId
            };

            return oAuth;
        }

        public static Uri getLoginUri(string clientId, string clientSecret, string redirectUri)
        {
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                },
                Scopes = scopes,
                DataStore = new NullDataStore()
            });

            var loginRequest = flow.CreateAuthorizationCodeRequest(redirectUri);
            return loginRequest.Build();
        }
        #endregion

        #region Playlistsongs
        /// <summary>
        /// Gets List of songs in local Song-Model by youtube playlist id
        /// </summary>
        /// <param name="playlist">local playlist</param>
        /// <returns></returns>
        public List<SongInfo> GetPlaylistSongs(string playlistId)
            => toSongModels(getPlaylistSongs(playlistId));
        public List<SongInfo> GetPlaylistSongsReadOnly(string playlistId)
            => toSongModels(getPlaylistSongsReadOnly(playlistId).Result);

        /// <summary>
        /// Returns a list of songs from a playlist
        /// </summary>
        /// <param name="playlistId">youtube playlist id</param>
        /// <returns></returns>
        private List<PlaylistItemSnippet> getPlaylistSongs(string playlistId)
            => getPlaylistItems(playlistId).Select(i => i.Snippet).ToList();

        private List<PlaylistItem> getPlaylistItems(string playlistId)
        {
            var listRequest = ytService.PlaylistItems.List("snippet");
            listRequest.MaxResults = 50;
            listRequest.PlaylistId = playlistId;
            var resp = listRequest.Execute();
            var resultsShown = resp.PageInfo.ResultsPerPage;
            var totalResults = resp.PageInfo.TotalResults;

            var playlistItems = resp.Items.ToList();
            if (totalResults == 0)
                return playlistItems;

            while (resultsShown <= totalResults)
            {
                listRequest.PageToken = resp.NextPageToken;
                resp = listRequest.Execute();
                resultsShown += resp.PageInfo.ResultsPerPage;
                playlistItems.AddRange(resp.Items.ToList());
            }
            return playlistItems;

        }
        private async Task<IReadOnlyList<PlaylistVideo>> getPlaylistSongsReadOnly(string playlistId)
        {
            var searchResults = await ytServiceReadOnly.Playlists.GetVideosAsync(playlistId);

            return searchResults;
        }



        public bool RemoveDuplicatesFromPlaylist(string playlistId)
        {
            var playlistItems = getPlaylistItems(playlistId);

            var duplicateSongIds = playlistItems.GroupBy(i => i.Snippet.ResourceId.VideoId).Where(x => x.Count() > 1).Select(x => x.Key).ToList();
            var duplicatePlaylistItems = playlistItems.Where(i => duplicateSongIds.Contains(i.Snippet.ResourceId.VideoId));
            var playlistItemIds = duplicatePlaylistItems.DistinctBy(i => i.Snippet.ResourceId.VideoId).Select(i => i.Id).ToList();

            foreach (var playlistItemId in playlistItemIds)
            {
                removeSongFromPlaylist(playlistItemId);
            }
            return true;
        }

        private bool removeSongFromPlaylist(string playlistItemId)
        {
            ytService.PlaylistItems.Delete(playlistItemId).Execute();
            return true;
        }

        #endregion

        #region Song
        public FoundSong FindSong(FindSong song)
        {
            try
            {
                var video = searchSongExact(song.ArtistName, song.SongName).Result;
                var newSong = toSongModel(video);
                newSong.SongId = song.SongId;
                return new FoundSong(newSong, true);
            }
            catch (Exception)
            {
                return null;
            }
        }
        //private string searchSongExact(string artistName, string songName)
        //{
        //    // Search for the song using artist name and song name
        //    var searchListRequest = ytService.Search.List("snippet");
        //    searchListRequest.Q = $"{artistName} {songName}"; // Combine artist name and song name
        //    searchListRequest.Type = "youtube#video";
        //    searchListRequest.MaxResults = 1; // Number of results to retrieve

        //    return searchListRequest.Execute().Items.First().Id.VideoId; //first instead of single, because for some reason it sometimes returns more than 1 result
        //}
        private async Task<VideoSearchResult> searchSongExact(string artistName, string songName)
        {
            // Search for the song using artist name and song name
            var query = $"{artistName} {songName}"; // Combine artist name and song name
            var searchListRequest = await ytServiceReadOnly.Search.GetVideosAsync(query);
            return searchListRequest.FirstOrDefault();
        }
        #endregion

        #region Thumbnail
        /// <summary>
        /// returns the playlist thumbnail as a base64 string
        /// </summary>
        /// <param name="id">local playlist </param>
        /// <returns></returns>
        public async Task<SourceThumbnail> GetPlaylistThumbnail(string id)
        {
            var ytPlaylist = getPlaylist(id);

            var thumbnail = ytPlaylist.Snippet.Thumbnails.Maxres
                            ?? ytPlaylist.Snippet.Thumbnails.High
                            ?? ytPlaylist.Snippet.Thumbnails.Medium
                            ?? ytPlaylist.Snippet.Thumbnails.Standard
                            ?? ytPlaylist.Snippet.Thumbnails.Default__;

            if (thumbnail == null)
                return null;

            var fileContents = await Helper.GetImageByUrl(thumbnail.Url);

            return new SourceThumbnail(thumbnail.Url, fileContents);
        }
        public async Task<SourceThumbnail> GetPlaylistThumbnailReadOnly(string id)
        {
            var ytPlaylist = getPlaylistReadonly(id);

            var thumbnail = ytPlaylist.Thumbnails.OrderByDescending(t => t.Resolution.Area).First();

            if (thumbnail == null)
                return null;

            var fileContents = await Helper.GetImageByUrl(thumbnail.Url);

            return new SourceThumbnail(thumbnail.Url, fileContents);
        }

        /// <summary>
        /// returns a list with a thumbnail for each song
        /// </summary>
        /// <param name="playlistId">youtube playlist id</param>
        /// <returns></returns>
        internal async Task<Dictionary<string, SourceThumbnail>> GetSongsThumbnailByPlaylist(string playlistId)
        {
            var ytSongs = await getPlaylistSongsReadOnly(playlistId);
            var songThumbnails = new Dictionary<string, SourceThumbnail>();

            foreach (var ytSong in ytSongs)
            {
                if (!songThumbnails.ContainsKey(ytSong.Id))
                {
                    if (ytSong.Thumbnails.Any())
                    {
                        var url = ytSong.Thumbnails.OrderBy(t => t.Resolution).First().Url;
                        var fileContents = await Helper.GetImageByUrl(url);
                        var sourceThumbnail = new SourceThumbnail(url, fileContents);
                        songThumbnails.Add(ytSong.Id, sourceThumbnail);
                    }
                    else
                        songThumbnails.Add(ytSong.Id, null);
                }
            }
            return songThumbnails;
        }
        public async Task<Dictionary<string, SourceThumbnail>> GetSongsThumbnailBySongIds(List<string> songIds)
        {
            var ytSongs = await getSongsReadOnly(songIds);
            var songThumbnails = new Dictionary<string, SourceThumbnail>();

            foreach (var ytSong in ytSongs)
            {
                if (!songThumbnails.ContainsKey(ytSong.Id))
                {
                    if (ytSong.Thumbnails.Any())
                    {
                        var url = ytSong.Thumbnails.OrderBy(t => t.Resolution).First().Url;
                        var fileContents = await Helper.GetImageByUrl(url);
                        var sourceThumbnail = new SourceThumbnail(url, fileContents);
                        songThumbnails.Add(ytSong.Id, sourceThumbnail);
                    }
                    else
                        songThumbnails.Add(ytSong.Id, null);
                }
            }
            return songThumbnails;
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
        private async Task<List<YoutubeExplode.Videos.Video>> getSongsReadOnly(List<string> songIds)
        {
            var songs = new List<YoutubeExplode.Videos.Video>();
            foreach (var songId in songIds)
            {
                songs.Add(await ytServiceReadOnly.Videos.GetAsync(songId));
            }
            return songs;
        }
        #endregion

        #region Add songs to playlist
        public ReturnModel AddSongToPlaylist(string playlistId, string songId)
        {
            try
            {
                var playlistItem = new PlaylistItem();
                playlistItem.Snippet = new PlaylistItemSnippet();
                playlistItem.Snippet.PlaylistId = playlistId;
                playlistItem.Snippet.ResourceId = new ResourceId();
                playlistItem.Snippet.ResourceId.Kind = "youtube#video";
                playlistItem.Snippet.ResourceId.VideoId = songId;

                var request = ytService.PlaylistItems.Insert(playlistItem, "snippet");
                var response = request.Execute();
                return new ReturnModel();
            }
            catch (Exception ex)
            {
                return new ReturnModel(ex.Message);
            }
        }
        #endregion

        #region Helper

        #region Model
        private PlaylistInfo toPlaylistModel(Playlist ytPlaylist)
        {
            return new PlaylistInfo
            {
                SourceId = Sources.Youtube,
                PlaylistIdSource = ytPlaylist.Id,
                Url = getPlaylistUrl(ytPlaylist.Id),
                Name = ytPlaylist.Snippet.Title,
                CreatorName = ytPlaylist.Snippet.ChannelTitle,
                Description = string.IsNullOrEmpty(ytPlaylist.Snippet.Description) ? null : ytPlaylist.Snippet.Description,
                IsMine = checkIfMyPlaylist(ytPlaylist.Id)
            };
        }
        private List<SongInfo> toSongModels(List<PlaylistItemSnippet> ytSongs)
        {
            return ytSongs.Select(ytSong => toSongModel(ytSong)).ToList();
        }
        private List<SongInfo> toSongModels(IReadOnlyList<PlaylistVideo> ytSongs)
        {
            return ytSongs.Select(ytSong => toSongModel(ytSong)).ToList();
        }
        private SongInfo toSongModel(VideoSearchResult ytSong)
        {
            return new SongInfo
            {
                SourceId = Sources.Youtube,
                SongIdSource = ytSong.Id,
                Name = ytSong.Title,
                ArtistName = ytSong.Author.ChannelTitle ?? "NotAvailable",
                Url = getVideoUrl(ytSong.Id),
            };
        }
        private SongInfo toSongModel(PlaylistItemSnippet ytSong)
        {
            return new SongInfo
            {
                SourceId = Sources.Youtube,
                SongIdSource = ytSong.ResourceId.VideoId,
                Name = ytSong.Title,
                ArtistName = ytSong.VideoOwnerChannelTitle ?? "NotAvailable",
                Url = getVideoUrl(ytSong.ResourceId.VideoId)
            };
        }

        private SongInfo toSongModel(PlaylistVideo ytSong)
        {
            return new SongInfo
            {
                SourceId = Sources.Youtube,
                SongIdSource = ytSong.Id,
                Name = ytSong.Title,
                ArtistName = ytSong.Author.Title ?? "NotAvailable",
                Url = getVideoUrl(ytSong.Id),
            };
        }
        #endregion

        private string getPlaylistUrl(string playlistId)
        => PlaylistUrlStart + playlistId;

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

        private string getVideoUrl(string videoId)
            => $"https://www.youtube.com/watch?v={videoId}";
        #endregion        
    }
}
