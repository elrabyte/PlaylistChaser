using Microsoft.AspNetCore.Mvc;

namespace PlaylistChaser.Controllers
{
    public class LoginController : Controller
    {
        private readonly ILogger<LoginController> _logger;
        private LoginController db;

        public LoginController(ILogger<LoginController> logger)
        {
            _logger = logger;
        }

        public ActionResult LoginToSpotify(string code)
        {
            if (code == null)
                return Redirect(SpotifyApiHelper.getLoginUri().ToString());

            var spotifyHelper = new SpotifyApiHelper(HttpContext, code);
            return new JsonResult(new { success = true });
        }
    }
}