using Microsoft.AspNetCore.Identity;

namespace PlaylistChaser.Web.Models
{
    public class User : IdentityUser<int>
    {
        public string? DbUserName { get; set; }
        public string? DbPassword { get; set; }
    }
}