using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlaylistChaser.Model
{
    [Table("AspNetUsers")]
    public class User : IdentityUser<int>
    {
        public string? DbUserName { get; set; }
        public string? DbPassword { get; set; }
    }
}