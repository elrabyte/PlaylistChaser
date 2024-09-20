using Microsoft.EntityFrameworkCore;
using PlaylistChaser.Model.BuiltInIds;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlaylistChaser.Model
{
    [PrimaryKey(nameof(SongId), nameof(SourceId))]
    public class SongState
    {
        public int SongId { get; set; }
        [ForeignKey(nameof(SongId))]
        public Song Song { get; set; }
        public SourceId SourceId { get; set; }

        [Required]
        public SongStates StateId { get; set; }
        [Required]
        public DateTime LastChecked { get; set; }
    }
}