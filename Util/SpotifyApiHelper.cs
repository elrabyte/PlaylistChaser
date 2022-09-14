using PlaylistChaser.Models;
using SpotifyAPI.Web;
using System.Web.Mvc;

namespace PlaylistChaser
{
    internal class SpotifyApiHelper
    {
        private HttpContext context;
        private SpotifyClient spotify;
        private const string spotifyAccessTokenKey = "spotifyAccessToken";
        private const string spotifyClientId = "5b7f5711cb3441e1b9956c2b5950f552";
        private const string spotifyClientSecret = "d03839b07802470cb6a2b218ea2389de";
        private const string redirectUri = "https://localhost:7245/Playlist/loginToSpotify";

        internal SpotifyApiHelper(HttpContext context)
        {
            this.context = context;
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
            this.context = context;
            var response = new OAuthClient().RequestToken(new AuthorizationCodeTokenRequest(spotifyClientId, spotifyClientSecret, code, new Uri(redirectUri))).Result;
            var accessToken = response.AccessToken;
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

        public async Task<FullPlaylist> CreatePlaylist(PlaylistModel playlist)
        {
            try
            {
                return await createPlaylist(playlist.Name);
            }
            catch (Exception)
            {
                return null;
            }
        }
        private async Task<FullPlaylist> createPlaylist(string playlistName, bool isPublic = true)
        {
            try
            {
                var request = new PlaylistCreateRequest(playlistName);
                request.Public = isPublic;
                var userId = (await spotify.UserProfile.Current()).Id;
                return await spotify.Playlists.Create(userId, request);
            }
            catch (Exception)
            {
                return null;
            }
        }
        public async Task<bool> UpdatePlaylist(PlaylistModel playlist)
        {
            try
            {
                var playlistDescription = string.Format("Last updated on {1} - Found {2}/{3} Songs - This playlist is a copy of this youtube playlist: \"{0}\". ", playlist.YoutubeUrl, DateTime.Now, playlist.Songs.Where(s => s.FoundOnSpotify.Value).Count(), playlist.Songs.Count());


                return await updatePlaylist(playlist.SpotifyUrl, playlist.Songs.Where(s => s.FoundOnSpotify.Value && !s.AddedToSpotify.Value).Select(s => s.SpotifyId).ToList(), playlistDescription);
            }
            catch (Exception)
            {
                return false;
            }
        }
        private async Task<bool> updatePlaylist(string spotifyPlaylistId, List<string> spotifySongIds, string playlistDescription = "")
        {
            try
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
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DeletePlaylist(PlaylistModel playlist)
        {
            //only sets private for the moment. couldnt find api
            try
            {
                //update playlistdescription
                var request = new PlaylistChangeDetailsRequest();
                request.Public = false;

                return await spotify.Playlists.ChangeDetails(playlist.SpotifyUrl, request);
            }
            catch (Exception)
            {
                return false;
            }
        }

    }
}
