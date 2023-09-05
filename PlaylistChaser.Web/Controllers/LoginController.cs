using Microsoft.AspNetCore.Mvc;
using PlaylistChaser.Web.Util.API;

namespace PlaylistChaser.Web.Controllers
{
    public class LoginController : Controller
    {
        protected readonly IConfiguration configuration;
        public LoginController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public ActionResult LoginToSpotify(string? code = null)
        {
            var clientId = configuration["Spotify:ClientId"];
            var clientSecret = configuration["Spotify:ClientSecret"];
            var redirectUri = configuration["Spotify:RedirectUri"];

            if (code == null)
                return Redirect(SpotifyApiHelper.getLoginUri(clientId, redirectUri).ToString());

            new SpotifyApiHelper(HttpContext, code, clientId, clientSecret, redirectUri);
            return new JsonResult(new { success = true });
        }
    }
}