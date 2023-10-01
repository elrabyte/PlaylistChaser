using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlaylistChaser.Web.Models
{
    public class Song
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public string SongName { get; set; }
        [Required]
        public string ArtistName { get; set; }
        public int? ThumbnailId { get; set; }
    }
}