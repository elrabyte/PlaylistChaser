using Microsoft.AspNetCore.Mvc;
using PlaylistChaser.Model;
using SpotifyAPI.Web;
using System.Text.RegularExpressions;
using SourceId = PlaylistChaser.Model.BuiltInIds.SourceId;
namespace PlaylistChaser.Core.Sources
{
    public class SpotifyApiHelper : ISource
    {
        private SpotifyClient spotify;
        static string[] scopes = { Scopes.PlaylistModifyPrivate, Scopes.PlaylistModifyPublic, Scopes.UserReadPrivate };

        public static string PlaylistUrlStart = "https://open.spotify.com/playlist/";

        public SourceId SourceId => SourceId.Spotify;

        public SpotifyApiHelper(string accessToken)
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
        public async Task<ActionResult> UpdatePlaylist(string spotifyPlaylistId, string? playlistName = null, string? playlistDescription = null, bool isPublic = true)
        {
            //update playlistdescription
            var request = new PlaylistChangeDetailsRequest();
            request.Name = playlistName;
            request.Description = playlistDescription;
            request.Public = isPublic;

            var success = await spotify.Playlists.ChangeDetails(spotifyPlaylistId, request);
            if (success)
                return new OkResult();
            else
                return new BadRequestObjectResult(new { message = "Update failed" });
        }

        public async Task<ActionResult> DeletePlaylist(string plalyistId)
        {
            //only sets private for the moment. couldnt find api            
            var request = new PlaylistChangeDetailsRequest();
            request.Public = false;

            var success = await spotify.Playlists.ChangeDetails(plalyistId, request);
            if (success)
                return new OkResult();
            else
                return new BadRequestObjectResult(new { message = "Delete failed" });
        }
        #endregion

        #region OAuth Credential
        static async public Task<OAuth2Credential> GetOauthCredential(string code, string clientId, string clientSecret, string redirectUri, int userId)
        {
            var oAuth = await getToken(clientId, clientSecret, userId, code: code, redirectUri: redirectUri);
            return oAuth;
        }
        static async public Task<OAuth2Credential> GetOAuthCredential(string clientId, string clientSecret, string refreshToken, int userId)
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
                tokenExpiration = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
                response = tokenResponse;
                response.RefreshToken = refreshToken;
            }
            else if (code != null && redirectUri != null)
            {
                var tokenResponse = await new OAuthClient().RequestToken(new AuthorizationCodeTokenRequest(clientId, clientSecret, code, new Uri(redirectUri)));
                tokenExpiration = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
                response = tokenResponse;
            }

            var oAuth = new OAuth2Credential
            {
                Provider = SourceId.Spotify.ToString(),
                AccessToken = response.AccessToken,
                RefreshToken = response.RefreshToken,
                TokenExpiration = tokenExpiration.Value,
                UserId = userId,
            };

            return oAuth;
        }

        public static Uri GetLoginUri(string clientId, string redirectUri)
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

        public async Task<ActionResult> RemovePlaylistSong(string spotifyPlaylistId, string spotifySongId)
        {
            return await removePlaylistSongs(spotifyPlaylistId, new List<string> { spotifySongId });
        }
        private async Task<ActionResult> removePlaylistSongs(string spotifyPlaylistId, List<string> spotifySongIds)
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

            return new OkResult();
        }
        #endregion


        public async Task<FullTrack> GetSong(string spotifySongId)
        {
            return await spotify.Tracks.Get(spotifySongId);
        }

        //#region Thumbnail
        //public async Task<SourceThumbnail> GetPlaylistThumbnail(string id)
        //{
        //    var playlist = await spotify.Playlists.Get(id);
        //    var thumbnail = playlist.Images.OrderByDescending(i => i.Height).FirstOrDefault();
        //    if (thumbnail == null)
        //        return null;
        //    var fileContents = await Helper.GetImageByUrl(thumbnail.Url);

        //    return new SourceThumbnail(thumbnail.Url, fileContents);
        //}

        //public async Task<Dictionary<string, SourceThumbnail>> GetSongsThumbnailBySongIds(List<string> songIds)
        //{
        //    //throw new NotImplementedException();
        //    var songThumbnails = new Dictionary<string, SourceThumbnail>();

        //    var songs = getSongs(songIds);

        //    foreach (var song in songs)
        //    {
        //        if (!songThumbnails.ContainsKey(song.Id))
        //        {
        //            var url = song.Album.Images.OrderBy(i => i.Height).First().Url;
        //            var fileContents = await Helper.GetImageByUrl(url);
        //            var sourceThumbnail = new SourceThumbnail(url, fileContents);
        //            songThumbnails.Add(song.Id, sourceThumbnail);
        //        }
        //    }
        //    return songThumbnails;
        //}
        //#endregion
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

        #region Add songs to playlist
        /// <summary>
        ///can add max. 100 songs per request
        /// </summary>
        public ActionResult AddSongsToPlaylistBatch(string playlistId, List<string> songIds)
        {
            try
            {
                var trackUris = songIds.Select(i => $"spotify:track:{i}").ToList();
                var response = spotify.Playlists.AddItems(playlistId, new PlaylistAddItemsRequest(trackUris)).Result;
                return new OkResult();
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new { message = ex.Message });
            }
        }

        public ActionResult AddSongToPlaylist(string playlistId, string songId)
        {
            try
            {
                var trackUri = new List<string> { $"spotify:track:{songId}" };
                var response = spotify.Playlists.AddItems(playlistId, new PlaylistAddItemsRequest(trackUri)).Result;

                return new OkResult();
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new { message = ex.Message });
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
                SourceId = SourceId.Spotify,
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
                SourceId = SourceId.Spotify,
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
