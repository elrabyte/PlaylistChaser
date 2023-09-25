using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static PlaylistChaser.Web.Util.BuiltInIds;

namespace PlaylistChaser.Web.Models
{
    public class PlaylistAdditionalInfo
    {
        [Key, Column(Order = 1)]
        public int PlaylistId { get; set; }
        [Key, Column(Order = 2)]
        public Sources SourceId { get; set; }
        [Required]
        public string PlaylistIdSource { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string CreatorName { get; set; }
        [Required]
        public bool IsMine { get; set; }
        public string? Description { get; set; }
        public string? Url{ get; set; }

    }
}