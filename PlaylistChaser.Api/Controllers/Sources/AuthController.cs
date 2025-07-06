using Microsoft.AspNetCore.Mvc;
using PlaylistChaser.Api.Database;
using PlaylistChaser.Core.Sources;

namespace PlaylistChaser.Api.Controllers.Sources
{
    public partial class AuthController : SourceBaseController<IAuth>
    {

        private readonly string clientId;
        private readonly string clientSecret;
        private readonly string redirectUrl;
        private readonly string frontEndUrl;
        public AuthController(AdminDBContext adminDBContext, IHttpContextAccessor httpContextAccessor, IConfiguration configuration) : base(adminDBContext, httpContextAccessor)
        {
            clientId = configuration[$"{sourceId.ToString()}:ClientId"];
            clientSecret = configuration[$"{sourceId.ToString()}:ClientSecret"];
            redirectUrl = configuration[$"{sourceId.ToString()}:RedirectUri"];
            frontEndUrl = configuration["FrontEndUrl"];
        }

        internal override IAuth GetApiHelper(Type type)
        {
            var accessToken = dbHelper.GetAccessToken(sourceId);
            return (IAuth)Activator.CreateInstance(type, new object[] { accessToken });
        }

        [HttpPost]
        [Route("check-has-accesstoken")]
        public bool CheckHasAccessToken()
        {
            var userId = 1;
            var accessToken = dbHelper.GetOauth(apiHelper.SourceId)?.AccessToken;
            return accessToken != null;
        }
        [HttpPost]
        [Route("check-accesstoken-expired")]
        public bool CheckAccesstokenExpired()
        {
            var userId = 1;
            var oAuth = dbHelper.GetOauth(apiHelper.SourceId);
            return oAuth.TokenExpiration < DateTime.UtcNow;
        }

        [HttpPost]
        [Route("refresh-accesstoken")]
        public async Task<ActionResult> RefreshAccesstoken()
        {
            try
            {
                var userId = 1;
                var oAuth = dbHelper.GetOauth(apiHelper.SourceId);
                if (oAuth != null && oAuth.TokenExpiration < DateTime.UtcNow)
                {
                    var newOAuth = await apiHelper.GetOAuthCredential(clientId, clientSecret, oAuth.RefreshToken, userId);
                    dbHelper.UpdateOAuthCredential(oAuth, newOAuth);

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
            return apiHelper.GetLoginUri(clientId, redirectUrl);
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

                var oAuth = dbHelper.GetOauth(apiHelper.SourceId);
                if (oAuth == null)
                {
                    var url = apiHelper.GetLoginUri(clientId, redirectUrl).ToString();
                    return new OkObjectResult(new { success = true, url = url });
                }
                else if (oAuth.TokenExpiration < DateTime.Now) //refresh token
                {
                    var newOAuth = await apiHelper.GetOAuthCredential(clientId, clientSecret, oAuth.RefreshToken, userId);
                    dbHelper.UpdateOAuthCredential(oAuth, newOAuth);
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

            var oAuth = dbHelper.GetOauth(sourceId);
            var newOAuth = await apiHelper.GetOAuthCredential(code, clientId, clientSecret, redirectUrl, userId);
            if (oAuth == null)
            {
                dbHelper.AddOAuthCredential(newOAuth);
            }
            else if (oAuth != null && CheckAccesstokenExpired())
            {
                dbHelper.UpdateOAuthCredential(oAuth, newOAuth);
            }



            return new RedirectResult(frontEndUrl);
        }
    }
}