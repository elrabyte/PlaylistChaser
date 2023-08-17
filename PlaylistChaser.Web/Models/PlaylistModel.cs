using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static PlaylistChaser.Web.Util.BuiltInIds;

namespace PlaylistChaser.Models
{
    [Table("Playlists")]
    public class PlaylistModel
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int? Id { get; set; }
        
        //youtube
        [Required]
        public string? YoutubeUrl { get; set; }
		[Required]
		public string? YoutubeId { get; set; }
		[Required]
        public string? Name { get; set; }
        [Required]
        public string? ChannelName { get; set; }
        public string? ImageBytes64 { get; set; }
        [Required]
        public PLaylistTypes PlaylistTypeId { get; set; }


        #region spotify
        public string? SpotifyUrl { get; set; }
        public string? Description { get; set; }
        #endregion

        //virtual public ICollection<SongModel> Songs { get; set; }
        [NotMapped]
        public ICollection<SongModel> Songs { get; set; }
    }
}