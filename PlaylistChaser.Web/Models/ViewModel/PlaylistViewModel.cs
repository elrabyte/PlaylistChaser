using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static PlaylistChaser.Web.Util.BuiltInIds;

namespace PlaylistChaser.Web.Models.ViewModel
{
    public class PlaylistViewModel
    {
        [Key]
        public int PlaylistId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string AuthorName { get; set; }
        public PLaylistTypes PlaylistTypeId { get; set; }
        public string PlaylistTypeName { get; set; }
        public int? ThumbnailId { get; set; }


        [NotMapped]
        public List<PlaylistSongViewModel>? Songs { get; set; }
    }
}