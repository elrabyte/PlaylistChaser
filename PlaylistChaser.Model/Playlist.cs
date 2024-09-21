using PlaylistChaser.Model.BuiltInIds;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlaylistChaser.Model
{
    public class Playlist
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string ChannelName { get; set; }
        public int ThumbnailId { get; set; }
        [ForeignKey(nameof(ThumbnailId))]
        public Thumbnail Thumbnail { get; set; }
        [Required]
        public PlaylistTypes PlaylistTypeId { get; set; }
        public string? Description { get; set; }
        [Required]
        public SourceId OriginSourceId { get; set; }
        [Required]
        public int UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public User User { get; set; }
    }
}