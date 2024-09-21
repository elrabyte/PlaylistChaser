using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PlaylistChaser.Model;
using PlaylistChaser.Model.BuiltInIds;
using PlaylistChaser.Core.Sources;
using PlaylistChaser.Api.Database;

namespace PlaylistChaser.Api.Controllers.Sources
{
    public class SourcesController<T> : Controller where T : ISource
    {
        private readonly AdminDBContext adminDBContext;
        private readonly string clientId;
        private readonly string clientSecret;
        private readonly string redirectUrl;
        private readonly T source;

        public SourcesController(SourceId sourceId, IConfiguration configuration, AdminDBContext adminDBContext)
        {

            this.adminDBContext = adminDBContext;

            clientId = configuration[$"{sourceId.ToString()}:ClientId"];
            clientSecret = configuration[$"{sourceId.ToString()}:ClientSecret"];
            redirectUrl = configuration[$"{sourceId.ToString()}:RedirectUri"];

            var oauth = adminDBContext.OAuth2Credential.SingleOrDefault(oau => oau.UserId == 1 && oau.Provider == sourceId.ToString());
            var accessToken = oauth?.AccessToken;

            this.source = (T)Activator.CreateInstance(typeof(T), new object[] { accessToken });
        }
        [HttpPost]
        [Route("check-has-accesstoken")]
        public bool CheckHasAccessToken()
        {
            var userId = 1;
            var oAuth = adminDBContext.OAuth2Credential.SingleOrDefault(a => a.UserId == userId && a.Provider == source.SourceId.ToString());
            return oAuth != null;
        }
        [HttpPost]
        [Route("check-accesstoken-expired")]
        public bool CheckAccesstokenExpired()
        {
            var userId = 1;
            var oAuth = adminDBContext.OAuth2Credential.SingleOrDefault(a => a.UserId == userId && a.Provider == source.SourceId.ToString());
            return oAuth.TokenExpiration < DateTime.UtcNow;
        }

        [HttpPost]
        [Route("refresh-accesstoken")]
        public async Task<ActionResult> RefreshAccesstoken()
        {
            try
            {
                var userId = 1;
                var oAuth = adminDBContext.OAuth2Credential.SingleOrDefault(a => a.UserId == userId && a.Provider == source.SourceId.ToString());
                if (oAuth != null && oAuth.TokenExpiration < DateTime.UtcNow)
                {
                    var newOAuth = await SpotifyApiHelper.GetOAuthCredential(clientId, clientSecret, oAuth.RefreshToken, userId);
                    oAuth.AccessToken = newOAuth.AccessToken;
                    oAuth.RefreshToken = newOAuth.RefreshToken;
                    oAuth.TokenExpiration = newOAuth.TokenExpiration;

                    adminDBContext.SaveChanges();

                    return new OkResult();
                }

                return new BadRequestObjectResult(new { message = "Token already expired" });
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new { message = ex.Message });
            }
        }

        [HttpGet]
        [Route("get-login-url")]
        public Uri GetLoginUrl()
        {
            return SpotifyApiHelper.GetLoginUri(clientId, redirectUrl);
        }

        [HttpGet]
        [Route("get-playlist/{url}")]
        public PlaylistInfo GetPlaylist(string url)
        {
            var playlistInfo = source.GetPlaylistById(url);
            return playlistInfo;
        }
        [HttpPost]
        [Route("validate-playlist-url")]
        public bool ValidatePlaylistUrl(string url)
        {
            bool valid = source.ValidatePlaylistUrl(url);
            return valid;
        }
        [HttpPost]
        [Route("add-playlist")]
        public async Task<IActionResult> AddPlaylist(string url)
        {
            try
            {
                var playlistInfo = source.GetPlaylistByUrl(url);
                var playlistThumbnail = await source.GetPlaylistThumbnail(playlistInfo.PlaylistIdSource);
                adminDBContext.Thumbnail.Add(playlistThumbnail);
                var playlist = new Playlist
                {
                    Name = playlistInfo.Name,
                    ChannelName = playlistInfo.CreatorName,
                    PlaylistTypeId = PlaylistTypes.Simple,
                    Description = playlistInfo.Description,
                    Thumbnail = playlistThumbnail,
                    MainSourceId = source.SourceId,
                    UserId = 1
                };
                adminDBContext.Playlist.Add(playlist);
                playlistInfo.Playlist = playlist;
                adminDBContext.PlaylistInfo.Add(playlistInfo);


                adminDBContext.SaveChanges();
                return Ok();
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new { message = ex.Message });

            }
        }
    }
}