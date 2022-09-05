using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlaylistChaser.Models
{
    public class PlaylistModel
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int? Id { get; set; }
        //youtube
        [Required]
        public string Name { get; set; }
        public string? Description { get; set; }
        [Required]
        public string YoutubeUrl { get; set; }
        [Required]
        public string UploaderName { get; set; }

        //virtual public ICollection<SongModel> Songs { get; set; }
    }
}