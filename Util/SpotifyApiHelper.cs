using PlaylistChaser.Models;
using SpotifyAPI.Web;
using System.Web.Mvc;

namespace PlaylistChaser
{
    internal class SpotifyApiHelper
    {
        private SpotifyClient spotify;
        private const string spotifyAccessTokenKey = "spotifyAccessToken";
        private const string spotifyClientId = "5b7f5711cb3441e1b9956c2b5950f552";
        private const string spotifyClientSecret = "d03839b07802470cb6a2b218ea2389de";
        private const string redirectUri = "https://localhost:7245/Playlist/loginToSpotify";

        internal SpotifyApiHelper(HttpContext context)
        {
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
            var accessToken = new OAuthClient().RequestToken(new AuthorizationCodeTokenRequest(spotifyClientId, spotifyClientSecret, code, new Uri(redirectUri))).Result.AccessToken;
            context.Session.SetString(spotifyAccessTokenKey, accessToken);

            spotify = new SpotifyClient(accessToken);
        }


        public static Uri getLoginUri()
        {
            var loginRequest = new LoginRequest(new Uri(redirectUri), spotifyClientId, LoginRequest.ResponseType.Code)
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

    }
}
