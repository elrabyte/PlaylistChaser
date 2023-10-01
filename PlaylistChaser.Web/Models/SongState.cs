using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static PlaylistChaser.Web.Util.BuiltInIds;

namespace PlaylistChaser.Web.Models
{
    public class SongState
    {
        [Key, Column(Order = 1)]
        public int SongId { get; set; }
        [Key, Column(Order = 2)]
        public Sources SourceId { get; set; }

        [Required]
        public SongStates StateId { get; set; }
        [Required]
        public DateTime LastChecked { get; set; }
    }
}