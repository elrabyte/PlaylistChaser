using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PlaylistChaser.Model;
using PlaylistChaser.Model.BuiltInIds;
using PlaylistChaser.Core.Sources;

namespace PlaylistChaser.Api.Database
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : Controller
    {
        private readonly UserManager<User> userManager;
        private readonly IConfigurationManager configurationManager;
        private readonly AdminDBContext adminDBContext;
        private readonly SpotifyApiHelper spotifyApiHelper;
        public AccountController(UserManager<User> userManager, IConfigurationManager configurationManager, AdminDBContext adminDBContext, SpotifyApiHelper spotifyApiHelper)
        {
            this.userManager = userManager;
            this.configurationManager = configurationManager;
            this.adminDBContext = adminDBContext;
            this.spotifyApiHelper = spotifyApiHelper;
        }

        [HttpPost]
        [Route("register-user")]
        public async Task<IActionResult> RegisterUser([FromBody] RegisterUserModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new User
                {
                    UserName = model.Email,
                    Email = model.Email,
                };

                // Insert the user with a password
                var result = await userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    return Ok(new { message = "User created successfully!" });
                }

                // Return validation errors
                return BadRequest(result.Errors);
            }

            return BadRequest(ModelState);
        }

        #region Spotify
        //public async Task<ActionResult> LoginToSpotify()
        //{
        //    try
        //    {
        //        var clientId = configurationManager.GetValue<string>("Spotify:ClientId");
        //        var clientSecret = configurationManager.GetValue<string>("Spotify:ClientSecret");
        //        var redirectUri = configurationManager.GetValue<string>("Spotify:RedirectUri");

        //        //var userId = getCurrentUserId();
        //        var userId = 1;
        //        if (userId == null)
        //            return new JsonResult(new { success = false, message = "Can't get userId" });

        //        //var oAuth = UserDbContext.OAuth2Credential.SingleOrDefault(a => a.UserId == userId && a.Provider == Sources.Spotify.ToString());
        //        var oAuth = adminDBContext.OAuth2Credential.SingleOrDefault(a => a.UserId == userId && a.Provider == Sources.Spotify.ToString());
        //        if (oAuth == null)
        //        {
        //            var url = SpotifyApiHelper.getLoginUri(clientId, redirectUri).ToString();
        //            return new JsonResult(new { success = true, url = url });
        //        }
        //        else if (oAuth.TokenExpiration < DateTime.Now) //refresh token
        //        {
        //            var newOAuth = await SpotifyApiHelper.GetOAuthCredential(clientId, clientSecret, oAuth.RefreshToken, userId);
        //            oAuth.AccessToken = newOAuth.AccessToken;
        //            oAuth.RefreshToken = newOAuth.RefreshToken;
        //            oAuth.TokenExpiration = newOAuth.TokenExpiration;

        //            adminDBContext.SaveChanges();
        //            //UserDbContext.SaveChanges();
        //        }

        //        return new JsonResult(new { success = true });
        //    }
        //    catch (Exception ex)
        //    {
        //        return new JsonResult(new { success = false, message = ex.Message });
        //    }

        //}
        //public async Task<ActionResult> AcceptSpotifyCode(string code)
        //{
        //    var clientId = configuration["Spotify:ClientId"];
        //    var clientSecret = configuration["Spotify:ClientSecret"];
        //    var redirectUri = configuration["Spotify:RedirectUri"];

        //    var userId = getCurrentUserId();
        //    if (userId == null)
        //        return new JsonResult(new { success = false, message = "Can't get userId" });

        //    var oAuth = await SpotifyApiHelper.GetOauthCredential(code, clientId, clientSecret, redirectUri, userId.Value);
        //    UserDbContext.OAuth2Credential.Add(oAuth);
        //    UserDbContext.SaveChanges();

        //    return RedirectToAction("Index", "Playlist");
        //}
        #endregion

    }
}