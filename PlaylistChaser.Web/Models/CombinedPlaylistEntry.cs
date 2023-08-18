using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlaylistChaser.Models
{
	public class CombinedPlaylistEntry
	{
		[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }
		public int CombinedPlaylistId { get; set; }
		public int PlaylistId { get; set; }
	}
}