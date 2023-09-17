using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static PlaylistChaser.Web.Util.BuiltInIds;

namespace PlaylistChaser.Web.Models
{
    public class Playlist
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public string? Name { get; set; }
        [Required]
        public string? ChannelName { get; set; }
        public int? ThumbnailId { get; set; }
        [Required]
        public PLaylistTypes PlaylistTypeId { get; set; }
        public string? Description { get; set; }
        public Sources? MainSourceId { get; set; }
    }
}