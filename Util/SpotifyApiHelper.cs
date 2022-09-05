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
        internal SpotifyApiHelper(HttpContext context)
        {
            this.context = context;
            spotify = new SpotifyClient(getAccessToken().Result);
        }

        private async Task<string> getAccessToken()
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

        public async Task<SearchResponse> SearchSong(SearchRequest.Types type, string songName)
        {

            var searchRequest = new SearchRequest(type, songName);
            return await spotify.Search.Item(searchRequest);
        }
    }
}
