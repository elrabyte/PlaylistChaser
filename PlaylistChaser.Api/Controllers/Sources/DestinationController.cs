using Microsoft.AspNetCore.Mvc;
using PlaylistChaser.Api.Database;
using PlaylistChaser.Core.Sources;
using PlaylistChaser.Model.BuiltInIds;

namespace PlaylistChaser.Api.Controllers.Sources
{
    public class DestinationController<T> : Controller where T : IDestination, IAuth
    {
        private readonly DbHelper dbHelper;
        private readonly T apiHelper;

        public DestinationController(SourceId sourceId, AdminDBContext adminDBContext)
        {
            this.dbHelper = new DbHelper(adminDBContext);

            var oauth = adminDBContext.OAuth2Credential.SingleOrDefault(oau => oau.UserId == 1 && oau.Provider == sourceId.ToString());
            var accessToken = oauth?.AccessToken;

            this.apiHelper = (T)Activator.CreateInstance(typeof(T), new object[] { accessToken });
        }
    }
}