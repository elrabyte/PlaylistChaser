using Microsoft.AspNetCore.Mvc;
using PlaylistChaser.Api.Database;
using PlaylistChaser.Core.Sources;
using PlaylistChaser.Model.BuiltInIds;

namespace PlaylistChaser.Api.Controllers.Sources
{
    public class AuthController<T>  where T : IAuth
    {
        private readonly DbHelper dbHelper;
        private readonly string clientId;
        private readonly string clientSecret;
        private readonly string redirectUrl;
        private readonly string frontEndUrl;
        private readonly T authSource;

        public AuthController(SourceId sourceId, IConfiguration configuration, AdminDBContext adminDBContext)
        {

            dbHelper = new DbHelper(adminDBContext);

            clientId = configuration[$"{sourceId.ToString()}:ClientId"];
            clientSecret = configuration[$"{sourceId.ToString()}:ClientSecret"];
            redirectUrl = configuration[$"{sourceId.ToString()}:RedirectUri"];
            frontEndUrl = configuration["FrontEndUrl"];

            var accessToken = dbHelper.GetAccessToken(sourceId);

            this.authSource = (T)Activator.CreateInstance(typeof(T), new object[] { accessToken });
        }


        [HttpPost]
        [Route("check-has-accesstoken")]
        public bool CheckHasAccessToken()
        {
            var userId = 1;
            var accessToken = dbHelper.GetOauth(authSource.SourceId)?.AccessToken;
            return accessToken != null;
        }
        [HttpPost]
        [Route("check-accesstoken-expired")]
        public bool CheckAccesstokenExpired()
        {
            var userId = 1;
            var oAuth = dbHelper.GetOauth(authSource.SourceId);
            return oAuth.TokenExpiration < DateTime.UtcNow;
        }

        [HttpPost]
        [Route("refresh-accesstoken")]
        public async Task<ActionResult> RefreshAccesstoken()
        {
            try
            {
                var userId = 1;
                var oAuth = dbHelper.GetOauth(authSource.SourceId);
                if (oAuth != null && oAuth.TokenExpiration < DateTime.UtcNow)
                {
                    var newOAuth = await authSource.GetOAuthCredential(clientId, clientSecret, oAuth.RefreshToken, userId);
                    dbHelper.UpdateOAuthCredential(newOAuth);

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
            return authSource.GetLoginUri(clientId, redirectUrl);
        }

        [HttpPost]
        [Route(nameof(Login))]
        public async Task<ActionResult> Login()
        {
            try
            {
                var userId = 1;
                if (userId == null)
                    return new JsonResult(new { success = false, message = "Can't get userId" });

                var oAuth = dbHelper.GetOauth(authSource.SourceId);
                if (oAuth == null)
                {
                    var url = authSource.GetLoginUri(clientId, redirectUrl).ToString();
                    return new OkObjectResult(new { success = true, url = url });
                }
                else if (oAuth.TokenExpiration < DateTime.Now) //refresh token
                {
                    var newOAuth = await authSource.GetOAuthCredential(clientId, clientSecret, oAuth.RefreshToken, userId);
                    dbHelper.UpdateOAuthCredential(newOAuth);
                }

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Route(nameof(AcceptCode))]
        public async Task<ActionResult> AcceptCode(string code)
        {
            var userId = 1;
            if (userId == null)
                return new JsonResult(new { success = false, message = "Can't get userId" });

            if (!CheckHasAccessToken() || (CheckHasAccessToken() && CheckAccesstokenExpired()))
            {
                var oAuth = await authSource.GetOAuthCredential(code, clientId, clientSecret, redirectUrl, userId);
                dbHelper.AddOAuthCredential(oAuth);
            }

            return new RedirectResult(frontEndUrl);
        }
    }
}