
using Microsoft.AspNetCore.Authorization;
using PlaylistChaser.Model.BuiltInIds;
namespace PlaylistChaser.Api.Util
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class AuthorizeRoleAttribute : AuthorizeAttribute
    {
        public AuthorizeRoleAttribute(Roles role)
        {
            Roles = role.ToString();
        }
    }
}
