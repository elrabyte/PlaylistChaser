using System.ComponentModel.DataAnnotations;

namespace PlaylistChaser.Model
{
    public class RegisterUserModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }

}