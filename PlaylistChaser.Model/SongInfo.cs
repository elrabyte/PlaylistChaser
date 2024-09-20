using Microsoft.EntityFrameworkCore;
using PlaylistChaser.Model.BuiltInIds;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlaylistChaser.Model
{
    [PrimaryKey(nameof(SongId), nameof(SourceId))]
    public class SongInfo
    {
        public int SongId { get; set; }
        [ForeignKey(nameof(SongId))]
        public Song Song { get; set; }
        public Sources SourceId { get; set; }

        [Required]
        public string SongIdSource { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string ArtistName { get; set; }
        [Required]
        public string Url { get; set; }

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