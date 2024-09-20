using System.ComponentModel.DataAnnotations;

namespace PlaylistChaser.Model
{
    public class AddPlaylistModel
    {
        [Required]
        public string PlaylistUrl { get; set; }

    }
}