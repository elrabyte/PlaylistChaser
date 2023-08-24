using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static PlaylistChaser.Web.Util.BuiltInIds;

namespace PlaylistChaser.Web.Models
{
    public class SongState
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public int SongId { get; set; }
        [Required]
        public Sources SourceId { get; set; }
        [Required]
        public SongStates StateId { get; set; }
        [Required]
        public DateTime LastChecked { get; set; }
    }
}