using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PlaylistChaser.Model;
using PlaylistChaser.Model.BuiltInIds;
using PlaylistChaser.Core.Sources;
using PlaylistChaser.Api.Database;

namespace PlaylistChaser.Api.Controllers.Sources
{
    [ApiController]
    [Route("api/[controller]")]
    public class SpotifyController : SourcesController<SpotifyApiHelper>
    {
        private readonly IConfiguration configuration;
        private readonly AdminDBContext adminDBContext;
        public SpotifyController(UserManager<User> userManager, IConfiguration configuration, AdminDBContext adminDBContext) : base(new SpotifyApiHelper(""), userManager, configuration, adminDBContext)
        {
            this.configuration = configuration;
            this.adminDBContext = adminDBContext;
        }

        [HttpPost]
        [Route("LoginToSpotify")]
        public async Task<ActionResult> LoginToSpotify()
        {
            try
            {
                var clientId = configuration["Spotify:ClientId"];
                var clientSecret = configuration["Spotify:ClientSecret"];
                var redirectUri = configuration["Spotify:RedirectUri"];

                var userId = 1;
                if (userId == null)
                    return new JsonResult(new { success = false, message = "Can't get userId" });

                var oAuth = adminDBContext.OAuth2Credential.SingleOrDefault(a => a.UserId == userId && a.Provider == SourceId.Spotify.ToString());
                if (oAuth == null)
                {
                    var url = SpotifyApiHelper.GetLoginUri(clientId, redirectUri).ToString();
                    return new OkObjectResult(new { success = true, url = url });
                }
                else if (oAuth.TokenExpiration < DateTime.Now) //refresh token
                {
                    var newOAuth = await SpotifyApiHelper.GetOAuthCredential(clientId, clientSecret, oAuth.RefreshToken, userId);
                    oAuth.AccessToken = newOAuth.AccessToken;
                    oAuth.RefreshToken = newOAuth.RefreshToken;
                    oAuth.TokenExpiration = newOAuth.TokenExpiration;

                    adminDBContext.SaveChanges();
                }

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }

        }
        [HttpGet]
        [Route("AcceptSpotifyCode")]
        public async Task<ActionResult> AcceptSpotifyCode(string code)
        {
            var clientId = configuration["Spotify:ClientId"];
            var clientSecret = configuration["Spotify:ClientSecret"];
            var redirectUri = configuration["Spotify:RedirectUri"];
            var frontEndUrl = configuration["FrontEndUrl"];

            var userId = 1;
            if (userId == null)
                return new JsonResult(new { success = false, message = "Can't get userId" });

            var isAlreadyAuthenticated = base.CheckAuthenticated();
            if (isAlreadyAuthenticated) return new RedirectResult(frontEndUrl);

            var oAuth = await SpotifyApiHelper.GetOauthCredential(code, clientId, clientSecret, redirectUri, userId);
            adminDBContext.OAuth2Credential.Add(oAuth);
            adminDBContext.SaveChanges();

            return new RedirectResult(frontEndUrl);
        }
    }
}