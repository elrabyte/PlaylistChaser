using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlaylistChaser.Web.Models.ViewModel
{
    public class SongViewModel
    {
        [Key]
        public int Id { get; set; }
        public string SongName { get; set; }
        public string? ArtistName { get; set; }
        public int? ThumbnailId { get; set; }

        [NotMapped]
        public List<SongAdditionalInfo>? SongInfos { get; set; }

    }
}