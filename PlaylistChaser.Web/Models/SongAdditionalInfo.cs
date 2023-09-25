using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static PlaylistChaser.Web.Util.BuiltInIds;

namespace PlaylistChaser.Web.Models
{
    public class SongAdditionalInfo
    {
        [Key, Column(Order = 1)]
        public int SongId { get; set; }
        [Key, Column(Order = 2)]
        public Sources SourceId { get; set; }
        
        public string? SongIdSource { get; set; }
        public string? Name { get; set; }
        public string? ArtistName { get; set; }
        public string? Url{ get; set; }


        [Required]
        public SongStates StateId { get; set; }
        [Required]
        public DateTime LastChecked { get; set; }

        [NotMapped]
        public string IconHtml
        {
            get
            {
                switch (SourceId)
                {
                    case Sources.Youtube:
                        return "<i class=\"bi bi-youtube\"></i>";
                    case Sources.Spotify:
                        return "<i class=\"bi bi-spotify\"></i>";
                    default:
                        return null;
                }
            }
        }
    }
}