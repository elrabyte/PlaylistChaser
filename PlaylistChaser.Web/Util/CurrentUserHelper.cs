using Microsoft.AspNetCore.Identity;
using PlaylistChaser.Web.Models;

namespace PlaylistChaser.Web.Util
{
    internal class CurrentUserHelper
    {
        private User User;

        internal CurrentUserHelper(HttpContext httpContext, UserManager<User> userManager)
        {
            User = userManager.GetUserAsync(httpContext.User).Result;
        }

        public User GetCurrentUser()
        {
            return User;
        }

        public async static Task<User> GetCurrentUser(HttpContext httpContext, UserManager<User> userManager)
        {
            return await userManager.GetUserAsync(httpContext.User);
        }
    }
}
