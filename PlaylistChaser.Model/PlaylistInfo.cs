using Microsoft.EntityFrameworkCore;
using PlaylistChaser.Model.BuiltInIds;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlaylistChaser.Model
{
    [PrimaryKey(nameof(PlaylistId), nameof(SourceId))]
    public class PlaylistInfo
    {
        public int PlaylistId { get; set; }
        [ForeignKey(nameof(PlaylistId))]
        public Playlist Playlist { get; set; }
        public SourceId SourceId { get; set; }
        [Required]
        public string PlaylistIdSource { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string CreatorName { get; set; }
        [Required]
        public bool IsMine { get; set; }
        public string? Description { get; set; }
        [Required]
        public string Url { get; set; }
        [Required]
        public DateTime LastSynced { get; set; }

    }
}