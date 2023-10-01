using PlaylistChaser.Web.Models;
using SpotifyAPI.Web;
using System.Text.RegularExpressions;
using static PlaylistChaser.Web.Util.BuiltInIds;

namespace PlaylistChaser.Web.Util.API
{
    internal class SpotifyApiHelper : ISource
    {
        private SpotifyClient spotify;
        static string[] scopes = { Scopes.PlaylistModifyPrivate, Scopes.PlaylistModifyPublic, Scopes.UserReadPrivate };

        public static string PlaylistUrlStart = "https://open.spotify.com/playlist/";

        internal SpotifyApiHelper(string accessToken)
        {
            if (accessToken == null)
            {
                throw new Exception("Not logged in yet");
            }

            spotify = new SpotifyClient(accessToken);
        }
        #region Playlist
        public PlaylistInfo GetPlaylistByUrl(string playlistUrl)
            => GetPlaylistById(GetPlaylistId(playlistUrl));
        public PlaylistInfo GetPlaylistById(string playlistId)
            => toPlaylistModel(spotify.Playlists.Get(playlistId).Result);

        public async Task<PlaylistInfo> CreatePlaylist(string playlistName, string? description, bool isPublic = true)
            => toPlaylistModel(await createPlaylist(playlistName, description, isPublic), true);
        private async Task<FullPlaylist> createPlaylist(string playlistName, string? description = null, bool isPublic = true)
        {
            var request = new PlaylistCreateRequest(playlistName);
            request.Public = isPublic;
            request.Description = description;
            var userId = (await spotify.UserProfile.Current()).Id;
            return await spotify.Playlists.Create(userId, request);
        }
        public async Task<bool> UpdatePlaylist(string spotifyPlaylistId, string? playlistName = null, string? playlistDescription = null, bool isPublic = true)
        {
            //update playlistdescription
            var request = new PlaylistChangeDetailsRequest();
            request.Name = playlistName;
            request.Description = playlistDescription;
            request.Public = isPublic;

            return await spotify.Playlists.ChangeDetails(spotifyPlaylistId, request);
        }

        public async Task<bool> DeletePlaylist(string plalyistId)
        {
            //only sets private for the moment. couldnt find api            
            var request = new PlaylistChangeDetailsRequest();
            request.Public = false;

            return await spotify.Playlists.ChangeDetails(plalyistId, request);
        }
        #endregion

        #region OAuth Credential
        static async internal Task<OAuth2Credential> GetOauthCredential(string code, string clientId, string clientSecret, string redirectUri, int userId)
        {
            var oAuth = await getToken(clientId, clientSecret, userId, code: code, redirectUri: redirectUri);
            return oAuth;
        }
        static async internal Task<OAuth2Credential> GetOAuthCredential(string clientId, string clientSecret, string refreshToken, int userId)
        {
            var oAuth = await getToken(clientId, clientSecret, userId, refreshToken: refreshToken);
            return oAuth;
        }
        private static async Task<OAuth2Credential> getToken(string clientId, string clientSecret, int userId, string? refreshToken = null, string? code = null, string? redirectUri = null)
        {
            IRefreshableToken response = null;
            DateTime? tokenExpiration = null;
            if (refreshToken != null)
            {
                var tokenResponse = await new OAuthClient().RequestToken(new AuthorizationCodeRefreshRequest(clientId, clientSecret, refreshToken));
                tokenExpiration = DateTime.Now.AddSeconds(tokenResponse.ExpiresIn);
                response = tokenResponse;
                response.RefreshToken = refreshToken;
            }
            else if (code != null && redirectUri != null)
            {
                var tokenResponse = await new OAuthClient().RequestToken(new AuthorizationCodeTokenRequest(clientId, clientSecret, code, new Uri(redirectUri)));
                tokenExpiration = DateTime.Now.AddSeconds(tokenResponse.ExpiresIn);
                response = tokenResponse;
            }

            var oAuth = new OAuth2Credential
            {
                Provider = Sources.Spotify.ToString(),
                AccessToken = response.AccessToken,
                RefreshToken = response.RefreshToken,
                TokenExpiration = tokenExpiration.Value,
                UserId = userId,
            };

            return oAuth;
        }

        public static Uri getLoginUri(string clientId, string redirectUri)
        {
            var loginRequest = new LoginRequest(new Uri(redirectUri), clientId, LoginRequest.ResponseType.Code)
            {
                Scope = scopes
            };
            return loginRequest.ToUri();
        }

        #endregion

        #region Playlistsongs
        public List<SongInfo> GetPlaylistSongs(string playlistId)
        => toSongModels(getPlaylistSongs(playlistId));

        private List<FullTrack> getPlaylistSongs(string playlistId)
        {
            var listRequest = spotify.Playlists.GetItems(playlistId).Result;

            var totalResults = listRequest.Total;
            var resultsShown = listRequest.Limit;

            var songs = listRequest.Items.Select(i => (FullTrack)i.Track).ToList();
            if (totalResults == 0)
                return songs;

            while (resultsShown <= totalResults)
            {
                listRequest = spotify.Playlists.GetItems(playlistId, new PlaylistGetItemsRequest { Offset = resultsShown }).Result;


                resultsShown += listRequest.Limit;
                songs.AddRange(listRequest.Items.Select(i => (FullTrack)i.Track).ToList());
            }
            return songs;
        }

        public async Task<bool> RemovePlaylistSong(string spotifyPlaylistId, string spotifySongId)
        {
            return await removePlaylistSongs(spotifyPlaylistId, new List<string> { spotifySongId });
        }
        private async Task<bool> removePlaylistSongs(string spotifyPlaylistId, List<string> spotifySongIds)
        {
            //can add max. 100 songs per request
            var rounds = Math.Ceiling(spotifySongIds.Count / 100d);
            for (int i = 0; i < rounds; i++)
                await spotify.Playlists.RemoveItems(spotifyPlaylistId, new PlaylistRemoveItemsRequest
                {
                    Tracks = spotifySongIds.Skip(i * 100)
                                           .Take(100)
                                           .Select(s => new PlaylistRemoveItemsRequest.Item { Uri = s })
                                           .ToList()
                });

            return true;
        }
        #endregion        

        #region Song
        public FoundSong FindSong(FindSong song)
        {
            try
            {
                var spotifySong = searchSongExact(SearchRequest.Types.Track, song.ArtistName, song.SongName).Result;
                if (spotifySong != null)
                {
                    var newSong = toSongModel(spotifySong);
                    newSong.SongId = song.SongId;
                    return new FoundSong(newSong, true);
                }
                else
                {
                    spotifySong = searchSong(SearchRequest.Types.Track, song.SongName).Result;
                    if (spotifySong != null)
                    {
                        var newSong = toSongModel(spotifySong);
                        newSong.SongId = song.SongId;
                        return new FoundSong(newSong, false);
                    }
                }
                return null;

            }
            catch (Exception ex)
            {
                return null;
            }
        }
        private async Task<FullTrack> searchSongExact(SearchRequest.Types type, string artistName, string songName)
        {
            var query = string.Format("artist:\"{0}\" track:\"{1}\"", artistName, songName);
            var searchRequest = new SearchRequest(type, query);

            var response = await spotify.Search.Item(searchRequest);
            if (response.Tracks.Items?.Count == 1)
                return response.Tracks.Items.Single();
            else
                return null;
        }
        private async Task<FullTrack> searchSong(SearchRequest.Types type, string songName)
        {
            var searchRequest = new SearchRequest(type, songName);

            var response = await spotify.Search.Item(searchRequest);
            if (response.Tracks.Items?.Count >= 1)
                return response.Tracks.Items.First();
            else
                return null;
        }
        public async Task<FullTrack> GetSong(string spotifySongId)
        {
            return await spotify.Tracks.Get(spotifySongId);
        }
        #endregion

        #region Thumbnail
        public async Task<SourceThumbnail> GetPlaylistThumbnail(string id)
        {
            var playlist = await spotify.Playlists.Get(id);
            var thumbnail = playlist.Images.OrderByDescending(i => i.Height).FirstOrDefault();
            if (thumbnail == null)
                return null;
            var fileContents = await Helper.GetImageByUrl(thumbnail.Url);

            return new SourceThumbnail(thumbnail.Url, fileContents);
        }

        public async Task<Dictionary<string, SourceThumbnail>> GetSongsThumbnailBySongIds(List<string> songIds)
        {
            //throw new NotImplementedException();
            var songThumbnails = new Dictionary<string, SourceThumbnail>();

            var songs = getSongs(songIds);

            foreach (var song in songs)
            {
                if (!songThumbnails.ContainsKey(song.Id))
                {
                    var url = song.Album.Images.OrderBy(i => i.Height).First().Url;
                    var fileContents = await Helper.GetImageByUrl(url);
                    var sourceThumbnail = new SourceThumbnail(url, fileContents);
                    songThumbnails.Add(song.Id, sourceThumbnail);
                }
            }
            return songThumbnails;
        }
        private List<FullTrack> getSongs(List<string> songIds)
        {
            const int requestLimit = 50;
            var songs = new List<FullTrack>();

            //split request 
            for (var i = 0; i <= songIds.Count; i += requestLimit)
            {
                var rangeCount = requestLimit;
                //if rangeCount exceeds maxResults, calc rest count
                if (i + requestLimit > songIds.Count)
                    rangeCount = songIds.Count - i;

                var curSongIds = songIds.GetRange(i, rangeCount);
                songs.AddRange(spotify.Tracks.GetSeveral(new TracksRequest(curSongIds)).Result.Tracks);
            }
            return songs;
        }
        #endregion

        #region Add songs to playlist
        /// <summary>
        ///can add max. 100 songs per request
        /// </summary>
        public bool AddSongsToPlaylistBatch(string playlistId, List<string> songIds)
        {
            try
            {
                var trackUris = songIds.Select(i => $"spotify:track:{i}").ToList();
                var response = spotify.Playlists.AddItems(playlistId, new PlaylistAddItemsRequest(trackUris)).Result;
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool AddSongToPlaylist(string playlistId, string songId)
        {
            try
            {
                var trackUri = new List<string> { $"spotify:track:{songId}" };
                var response = spotify.Playlists.AddItems(playlistId, new PlaylistAddItemsRequest(trackUri)).Result;

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        #endregion

        #region helper

        #region model 
        private PlaylistInfo toPlaylistModel(FullPlaylist spotifyPlaylist, bool isMine = false)
        {
            var info = new PlaylistInfo
            {
                Name = spotifyPlaylist.Name,
                CreatorName = spotifyPlaylist.Owner.DisplayName,
                Description = string.IsNullOrEmpty(spotifyPlaylist.Description) ? null : spotifyPlaylist.Description,
                PlaylistIdSource = spotifyPlaylist.Id,
                SourceId = Sources.Spotify,
                IsMine = isMine,
                Url = getPlaylistUrl(spotifyPlaylist.Id)
            };
            return info;
        }
        private List<SongInfo> toSongModels(List<FullTrack> songs)
            => songs.Select(s => toSongModel(s)).ToList();

        private SongInfo toSongModel(FullTrack song)
            => new SongInfo
            {
                SourceId = Sources.Spotify,
                SongIdSource = song.Id,
                Name = song.Name,
                ArtistName = song.Artists.First().Name,
                Url = getSongUrl(song.Id),
            };

        #endregion

        internal string GetPlaylistId(string url)
        {
            var pattern = @"playlist/(\w+)";
            Regex rg = new Regex(pattern);
            var match = rg.Match(url);
            return match.Groups[1].Value;
        }
        private string getPlaylistUrl(string playlistId)
            => PlaylistUrlStart + playlistId;

        private string getSongUrl(string songId)
            => "https://open.spotify.com/track/" + songId;


        #endregion
    }
}
