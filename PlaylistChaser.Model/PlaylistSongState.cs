using Microsoft.EntityFrameworkCore;
using PlaylistChaser.Model.BuiltInIds;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlaylistChaser.Model
{
    [PrimaryKey(nameof(PlaylistSongId), nameof(SourceId))]
    public class PlaylistSongState
    {
        public int PlaylistSongId { get; set; }
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