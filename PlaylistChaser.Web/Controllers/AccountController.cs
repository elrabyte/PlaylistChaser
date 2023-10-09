using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using PlaylistChaser.Web.Database;
using PlaylistChaser.Web.Models;
using PlaylistChaser.Web.Models.ViewModel;
using PlaylistChaser.Web.Util.API;
using System.Security.Claims;
using static PlaylistChaser.Web.Util.BuiltInIds;

namespace PlaylistChaser.Web.Controllers
{
    public class AccountController : BaseController
    {
        public AccountController(IConfiguration configuration,
                                 PlaylistChaserDbContext db,
                                 IHubContext<ProgressHub> hubContext,
                                 IMemoryCache memoryCache,
                                 SignInManager<User> signInManager,
                                 UserManager<User> userManager)
            : base(configuration, db, hubContext, memoryCache)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
        }

        SignInManager<User> signInManager;
        UserManager<User> userManager;

        #region Spotify
        public async Task<ActionResult> LoginToSpotify()
        {
            try
            {
                var clientId = configuration["Spotify:ClientId"];
                var clientSecret = configuration["Spotify:ClientSecret"];
                var redirectUri = configuration["Spotify:RedirectUri"];



                if (!int.TryParse(HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value, out var userId))
                    return new JsonResult(new { success = false, message = "Can't get userId" });

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

            if (!int.TryParse(HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value, out var userId))
                return new JsonResult(new { success = false, message = "Can't get userId" });

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

                if (!int.TryParse(HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value, out var userId))
                    return new JsonResult(new { success = false, message = "Can't get userId" });

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

            if (!int.TryParse(HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value, out var userId))
                return new JsonResult(new { success = false, message = "Can't get userId" });

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

        #region User
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Create a new ApplicationUser with the data from the registration form
                var user = new User
                {
                    UserName = model.UserName
                };

                // Attempt to create the user in the Identity system
                var result = await userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // If user creation is successful, sign in the user (optional)
                    await signInManager.SignInAsync(user, isPersistent: false);

                    // Redirect to a success page or another appropriate action
                    return RedirectToAction("RegisterConfirmation", "Account");
                }

                // If user creation fails, add errors to ModelState and redisplay the form
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If model state is invalid, redisplay the registration form with validation errors
            return View(model);
        }


        [HttpGet]
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            model.ReturnUrl ??= Url.Action("Index", "Playlist");

            if (ModelState.IsValid)
            {
                var result = await signInManager.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    // User successfully logged in
                    return Redirect(model.ReturnUrl);
                }
                if (result.IsLockedOut)
                {
                    // Handle account lockout (if configured)
                    return Lockout();
                }
                else
                {
                    // Login failed, display error message
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }

            // If model state is invalid, redisplay the login form with validation errors
            return View(model);
        }

        private IActionResult Lockout()
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();

            // Redirect to the home page or another appropriate page after logout
            return RedirectToAction("Login", "Account");
        }
        #endregion
    }
}