using Microsoft.AspNetCore.Mvc;
using PlaylistChaser.Web.Util.API;

namespace PlaylistChaser.Web.Controllers
{
    public class LoginController : Controller
    {
        public ActionResult LoginToSpotify(string? code = null)
        {
            if (code == null)
                return Redirect(SpotifyApiHelper.getLoginUri().ToString());

            var spotifyHelper = new SpotifyApiHelper(HttpContext, code);
            return new JsonResult(new { success = true });
        }
    }
}