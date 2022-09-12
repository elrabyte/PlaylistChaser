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
        public string? YoutubeUrl { get; set; }
        [Required]
        public string? Name { get; set; }
        [Required]
        public string? ChannelName { get; set; }
        public string? ImageBytes64 { get; set; }
        
        //spotify
        public string? SpotifyUrl { get; set; }
        public string? Description { get; set; }


        //virtual public ICollection<SongModel> Songs { get; set; }
        [NotMapped]
        public ICollection<SongModel> Songs { get; set; }
    }
}