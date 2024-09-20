using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlaylistChaser.Model
{
	public class CombinedPlaylistEntry
	{
		[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }
		public int CombinedPlaylistId { get; set; }
        [ForeignKey(nameof(CombinedPlaylistId))]
        public Playlist CombinedPlaylist { get; set; }
        public int PlaylistId { get; set; }
        [ForeignKey(nameof(PlaylistId))]
        public Playlist Playlist { get; set; }
    }
}