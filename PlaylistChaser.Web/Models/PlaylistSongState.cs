using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static PlaylistChaser.Web.Util.BuiltInIds;

namespace PlaylistChaser.Web.Models
{
    public class PlaylistSongState
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public int PlaylistSongId { get; set; }
        [Required]
        public Sources SourceId { get; set; }
        [Required]
        public States StateId { get; set; }
        [Required]
        public DateTime LastChecked { get; set; }
    }
}