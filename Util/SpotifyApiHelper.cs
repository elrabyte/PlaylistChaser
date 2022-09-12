using PlaylistChaser.Models;
using SpotifyAPI.Web;

namespace PlaylistChaser
{
    internal class SpotifyApiHelper
    {
        private HttpContext context;
        private SpotifyClient spotify;
        private const string spotifyAccessTokenKey = "spotifyAccessToken";
        private const string spotifyClientId = "5b7f5711cb3441e1b9956c2b5950f552";
        private const string spotifyClientSecret = "d03839b07802470cb6a2b218ea2389de";
        private const string userId = "31eanddqkyvals63bu4teybq76ci";
        internal SpotifyApiHelper(HttpContext context)
        {
            this.context = context;
            var config = SpotifyClientConfig
                        .CreateDefault()
                        .WithAuthenticator(new ClientCredentialsAuthenticator(spotifyClientId, spotifyClientSecret));


            spotify = new SpotifyClient(config);



            //var loginRequest = new LoginRequest(new Uri("https://localhost:7245/"), spotifyClientId, LoginRequest.ResponseType.Code)
            //{
            //    Scope = new[] { Scopes.PlaylistReadPrivate, Scopes.PlaylistReadCollaborative }
            //};
            //var uri = loginRequest.ToUri();

            //currentUserId = spotify.UserProfile.Current().Result.Id;
        }

        private async Task<string> getAccessToken()
        {
            try
            {
                var accessToken = context.Session.GetString(spotifyAccessTokenKey);
                if (accessToken != null)
                {
                    var request = new AuthorizationCodeRefreshRequest(spotifyClientId, spotifyClientSecret, accessToken);
                    accessToken = (await new OAuthClient().RequestToken(request)).AccessToken;
                }
                else
                {
                    var request = new ClientCredentialsRequest(spotifyClientId, spotifyClientSecret);
                    accessToken = (await new OAuthClient().RequestToken(request)).AccessToken;

                }
                context.Session.SetString(spotifyAccessTokenKey, accessToken);
                return accessToken;
            }
            catch (Exception)
            {
                var request = new ClientCredentialsRequest(spotifyClientId, spotifyClientSecret);
                var accessToken = (await new OAuthClient().RequestToken(request)).AccessToken;
                context.Session.SetString(spotifyAccessTokenKey, accessToken);
                return accessToken;
            }
        }

        public async Task<SearchResponse> SearchSong(SearchRequest.Types type, string songName)
        {

            var searchRequest = new SearchRequest(type, songName);
            return await spotify.Search.Item(searchRequest);
        }

        public async Task<FullPlaylist> CreatePlaylist(PlaylistModel playlist)
        {
            try
            {
                var request = new PlaylistCreateRequest(playlist.Name);
                request.Public = false;
                request.Description = string.Format("i'm a bot. This playlist is a copy of this youtube playlist: \"{0}\". " +
                                                    "\n Last updated on {1}. " +
                                                    "\n Found {2}/{3} Songs", playlist.YoutubeUrl, DateTime.Now, playlist.Songs.Where(s => s.FoundOnSpotify.Value).Count(), playlist.Songs.Count());
                return await spotify.Playlists.Create(userId, request);
            }
            catch (Exception)
            {
                return null;
            }
        }

    }
}
