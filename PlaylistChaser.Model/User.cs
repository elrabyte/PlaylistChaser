using Microsoft.AspNetCore.Identity;

namespace PlaylistChaser.Model
{
    public class User : IdentityUser<int>
    {
        public string? DbUserName { get; set; }
        public string? DbPassword { get; set; }
    }
}