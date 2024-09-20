using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PlaylistChaser.Model;
using PlaylistChaser.Model.BuiltInIds;
using PlaylistChaser.Core.Sources;
using PlaylistChaser.Api.Database;
using Microsoft.AspNetCore.Http.HttpResults;

namespace PlaylistChaser.Api.Controllers.Sources
{
    public class SourcesController<T> : Controller where T : ISource
    {
        private readonly UserManager<User> userManager;
        private readonly IConfiguration configuration;
        private readonly AdminDBContext adminDBContext;
        private readonly ISource source;
        public SourcesController(T source, UserManager<User> userManager, IConfiguration configuration, AdminDBContext adminDBContext)
        {
            this.userManager = userManager;
            this.configuration = configuration;
            this.adminDBContext = adminDBContext;
            this.source = source;
        }


        [HttpPost]
        [Route("check-authenticated")]
        public bool CheckAuthenticated()
        {
            var userId = 1;
            var oAuth = adminDBContext.OAuth2Credential.SingleOrDefault(a => a.UserId == userId && a.Provider == source.SourceId.ToString());
            return oAuth != null;
        }

        [HttpGet]
        [Route("get-login-url")]
        public Uri GetLoginUrl()
        {
            var clientId = configuration["Spotify:ClientId"];
            var clientSecret = configuration["Spotify:ClientSecret"];
            var redirectUri = configuration["Spotify:RedirectUri"];

            return SpotifyApiHelper.GetLoginUri(clientId, redirectUri);

        }
    }
}