using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlaylistChaser.Model
{
    public class PlaylistSong
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public int SongId { get; set; }
        [ForeignKey(nameof(SongId))]
        public Song Song { get; set; }
        [Required]
        public int PlaylistId { get; set; }
        [ForeignKey(nameof(PlaylistId))]
        public Playlist Playlist { get; set; }
    }
}