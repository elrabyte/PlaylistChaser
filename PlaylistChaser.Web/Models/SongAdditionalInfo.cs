using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static PlaylistChaser.Web.Util.BuiltInIds;

namespace PlaylistChaser.Web.Models
{
    public class SongAdditionalInfo
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public int SongId { get; set; }
        [Required]
        public Sources SourceId { get; set; }
        [Required]
        public string SongIdSource { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string ArtistName { get; set; }
        public string? Url{ get; set; }

    }
}