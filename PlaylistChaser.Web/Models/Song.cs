using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlaylistChaser.Web.Models
{
    public class Song
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        //youtube
        [Required]
        public string? YoutubeSongName { get; set; }
        [Required]
        public string? YoutubeId { get; set; }
        //spotify
        public string? SpotifyId { get; set; }
        public string? SongName { get; set; }
        public string? ArtistName { get; set; }
        public int? ThumbnailId { get; set; }
    }
}