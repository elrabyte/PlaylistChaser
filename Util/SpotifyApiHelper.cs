using PlaylistChaser.Models;
using SpotifyAPI.Web;

namespace PlaylistChaser
{
    internal class SpotifyApiHelper
    {
        private SpotifyClient spotify;
        private const string spotifyAccessTokenKey = "spotifyAccessToken";
        private const string redirectUri = "https://localhost:7245/Playlist/loginToSpotify";
        private string spotifyClientId;
        private string spotifyClientSecret;

        internal SpotifyApiHelper(HttpContext context)
        {
            setSecrets();
            var accessToken = context.Session.GetString(spotifyAccessTokenKey);
            if (accessToken != null)
            {
                spotify = new SpotifyClient(accessToken);
                return;
            }

            var config = SpotifyClientConfig
                        .CreateDefault()
                        .WithAuthenticator(new ClientCredentialsAuthenticator(spotifyClientId, spotifyClientSecret));

            spotify = new SpotifyClient(config);
        }

        internal SpotifyApiHelper(HttpContext context, string code)
        {
            setSecrets();
            var accessToken = new OAuthClient().RequestToken(new AuthorizationCodeTokenRequest(spotifyClientId, spotifyClientSecret, code, new Uri(redirectUri))).Result.AccessToken;
            context.Session.SetString(spotifyAccessTokenKey, accessToken);

            spotify = new SpotifyClient(accessToken);
        }
        private void setSecrets()
        {
            spotifyClientId = Helper.ReadSecret("Spotify", "ClientId");
            spotifyClientSecret = Helper.ReadSecret("Spotify", "ClientSecret");
        }
        public static Uri getLoginUri()
        {
            var clientId = Helper.ReadSecret("Spotify", "ClientId");
            var loginRequest = new LoginRequest(new Uri(redirectUri), clientId, LoginRequest.ResponseType.Code)
            {
                Scope = new[] { Scopes.PlaylistModifyPrivate, Scopes.PlaylistModifyPublic, Scopes.UserReadPrivate }
            };
            return loginRequest.ToUri();
        }

        public async Task<SearchResponse> SearchSong(SearchRequest.Types type, string songName)
        {

            var searchRequest = new SearchRequest(type, songName);
            return await spotify.Search.Item(searchRequest);
        }
        public async Task<FullTrack> GetSong(string spotifySongId)
        {
            return await spotify.Tracks.Get(spotifySongId);
        }

        public async Task<FullPlaylist> CreatePlaylist(string playlistName)
        {
            return await createPlaylist(playlistName);
        }
        private async Task<FullPlaylist> createPlaylist(string playlistName, bool isPublic = true)
        {
            var request = new PlaylistCreateRequest(playlistName);
            request.Public = isPublic;
            var userId = (await spotify.UserProfile.Current()).Id;
            return await spotify.Playlists.Create(userId, request);
        }
        public async Task<bool> UpdatePlaylist(string spotifyPlaylistId, List<string> spotifySongIds, string playlistDescription = null)
        {
            //can add max. 100 songs per request
            var rounds = Math.Ceiling(spotifySongIds.Count / 100d);
            for (int i = 0; i < rounds; i++)
                await spotify.Playlists.AddItems(spotifyPlaylistId, new PlaylistAddItemsRequest(spotifySongIds.Skip(i * 100).Take(100).ToList()));

            //update playlistdescription
            var request = new PlaylistChangeDetailsRequest();
            request.Description = playlistDescription;

            return await spotify.Playlists.ChangeDetails(spotifyPlaylistId, request);
        }

        public async Task<bool> DeletePlaylist(PlaylistModel playlist)
        {
            //only sets private for the moment. couldnt find api            
            var request = new PlaylistChangeDetailsRequest();
            request.Public = false;

            return await spotify.Playlists.ChangeDetails(playlist.SpotifyUrl, request);
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

    }
}
