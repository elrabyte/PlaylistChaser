using System.ComponentModel.DataAnnotations;

namespace PlaylistChaser.Models.ViewModel
{
	public class SongViewModel
	{
		[Key]
		public int Id { get; set; }
		public string SongName { get; set; }
		public string? ArtistName { get; set; }
		public int? ThumbnailId{ get; set; }
		public string? ThumbnailBase64String { get; set; }
	}
}