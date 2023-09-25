using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PlaylistChaser.Web.Database;
using PlaylistChaser.Web.Util.API;
using static PlaylistChaser.Web.Util.BuiltInIds;

namespace PlaylistChaser.Web.Controllers
{
    public class LoginController : BaseController
    {
        public LoginController(IConfiguration configuration, PlaylistChaserDbContext db, IHubContext<ProgressHub> hubContext) 
            : base(configuration, db, hubContext) { }

        #region Spotify
        public async Task<ActionResult> LoginToSpotify()
        {
            try
            {
                var clientId = configuration["Spotify:ClientId"];
                var clientSecret = configuration["Spotify:ClientSecret"];
                var redirectUri = configuration["Spotify:RedirectUri"];
                var userId = 1;

                var oAuth = db.OAuth2Credential.SingleOrDefault(a => a.UserId == userId && a.Provider == Sources.Spotify.ToString());
                if (oAuth == null)
                {
                    var url = SpotifyApiHelper.getLoginUri(clientId, redirectUri).ToString();
                    return new JsonResult(new { success = true, url = url });
                }
                else if (oAuth.TokenExpiration < DateTime.Now) //refresh token
                {
                    var newOAuth = await SpotifyApiHelper.GetOAuthCredential(clientId, clientSecret, oAuth.RefreshToken, userId);
                    oAuth.AccessToken = newOAuth.AccessToken;
                    oAuth.RefreshToken = newOAuth.RefreshToken;
                    oAuth.TokenExpiration = newOAuth.TokenExpiration;

                    db.SaveChanges();
                }

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }

        }
        public async Task<ActionResult> AcceptSpotifyCode(string code)
        {
            var clientId = configuration["Spotify:ClientId"];
            var clientSecret = configuration["Spotify:ClientSecret"];
            var redirectUri = configuration["Spotify:RedirectUri"];
            var userId = 1;

            var oAuth = await SpotifyApiHelper.GetOauthCredential(code, clientId, clientSecret, redirectUri, userId);
            db.OAuth2Credential.Add(oAuth);
            db.SaveChanges();

            return RedirectToAction("Index", "Playlist");
        }
        #endregion

        #region Youtube
        public async Task<ActionResult> LoginToYoutube()
        {
            try
            {
                var clientId = configuration["Youtube:ClientId"];
                var clientSecret = configuration["Youtube:ClientSecret"];
                var redirectUri = configuration["Youtube:RedirectUri"];
                var userId = 1;

                var oAuth = db.OAuth2Credential.SingleOrDefault(a => a.UserId == userId && a.Provider == Sources.Youtube.ToString());
                if (oAuth != null && oAuth.TokenExpiration > DateTime.Now) //refresh token
                {
                    var newOAuth = await YoutubeApiHelper.GetOauthCredential(clientId, clientSecret, oAuth.RefreshToken, userId);
                    oAuth.AccessToken = newOAuth.AccessToken;
                    oAuth.RefreshToken = newOAuth.RefreshToken;
                    oAuth.TokenExpiration = newOAuth.TokenExpiration;

                    db.SaveChanges();
                }
                else
                {
                    var url = YoutubeApiHelper.getLoginUri(clientId, clientSecret, redirectUri).ToString();
                    return new JsonResult(new { success = true, url = url });
                }

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<ActionResult> AcceptYoutubeCode(string code)
        {
            var clientId = configuration["Youtube:ClientId"];
            var clientSecret = configuration["Youtube:ClientSecret"];
            var redirectUri = configuration["Youtube:RedirectUri"];
            var userId = 1;

            var oAuth = db.OAuth2Credential.SingleOrDefault(a => a.UserId == userId && a.Provider == Sources.Youtube.ToString());
            if (oAuth == null)
            {
                oAuth = new Models.OAuth2Credential();
                oAuth.UserId = userId;
                oAuth.Provider = Sources.Youtube.ToString();
                db.OAuth2Credential.Add(oAuth);
            }
            var newOAuth = await YoutubeApiHelper.GetOauthCredential(code, clientId, clientSecret, redirectUri, userId);

            oAuth.AccessToken = newOAuth.AccessToken;
            oAuth.RefreshToken = newOAuth.RefreshToken;
            oAuth.TokenExpiration = newOAuth.TokenExpiration;

            db.SaveChanges();

            return RedirectToAction("Index", "Playlist");
        }
        #endregion
    }
}