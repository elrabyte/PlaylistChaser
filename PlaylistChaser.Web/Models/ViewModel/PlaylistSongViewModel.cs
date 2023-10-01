using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlaylistChaser.Web.Models.ViewModel
{
    public class PlaylistSongViewModel
    {
        [Key]
        public int PlaylistSongId { get; set; }
        public int SongId { get; set; }
        public string SongName { get; set; }
        public string? ArtistName { get; set; }
        public int? ThumbnailId { get; set; }

        [NotMapped]
        public List<PlaylistSongState>? PlaylistSongStates { get; set; }
        [NotMapped]
        public List<SongState>? SongStates { get; set; }

    }
}