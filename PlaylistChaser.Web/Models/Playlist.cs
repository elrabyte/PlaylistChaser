using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static PlaylistChaser.Web.Util.BuiltInIds;

namespace PlaylistChaser.Web.Models
{
    public class Playlist
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int? Id { get; set; }

        //youtube
        [Required]
        public string? YoutubeUrl { get; set; }
        [Required]
        public string? YoutubeId { get; set; }
        [Required]
        public string? Name { get; set; }
        [Required]
        public string? ChannelName { get; set; }
        public int? ThumbnailId { get; set; }
        [Required]
        public PLaylistTypes PlaylistTypeId { get; set; }


        #region spotify
        public string? SpotifyUrl { get; set; }
        public string? Description { get; set; }
        #endregion
    }
}