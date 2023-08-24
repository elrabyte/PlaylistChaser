using System.ComponentModel.DataAnnotations;

namespace PlaylistChaser.Web.Models.ViewModel
{
	public class SongViewModel
	{
		[Key]
		public int Id { get; set; }
		public string SongName { get; set; }
		public string? ArtistName { get; set; }
		public int? ThumbnailId{ get; set; }
		public string? YoutubeId { get; set; }
	}
}