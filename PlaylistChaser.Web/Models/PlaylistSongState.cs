using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static PlaylistChaser.Web.Util.BuiltInIds;

namespace PlaylistChaser.Web.Models
{
    public class PlaylistSongState
    {
        [Key, Column(Order = 1)]
        public int PlaylistSongId { get; set; }
        [Key, Column(Order = 2)]
        public Sources SourceId { get; set; }
        [Required]
        public PlaylistSongStates StateId { get; set; }
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