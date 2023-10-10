using Hangfire.Dashboard;
using static PlaylistChaser.Web.Util.BuiltInIds;

namespace ats.Desk.Web.Util
{
    public class HangfireAuthorization : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            try
            {
                var httpContext = context.GetHttpContext();
                return httpContext.User.IsInRole(Roles.Administrator.ToString());
            }
            catch
            {
                return false;
            }
        }
    }
}