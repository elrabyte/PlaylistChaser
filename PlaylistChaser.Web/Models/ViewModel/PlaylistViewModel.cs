using System.ComponentModel.DataAnnotations.Schema;
using static PlaylistChaser.Web.Util.BuiltInIds;

namespace PlaylistChaser.Web.Models.ViewModel
{
    public class PlaylistViewModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? ChannelName { get; set; }
        public int? ThumbnailId { get; set; }
        public PLaylistTypes PlaylistTypeId { get; set; }
        public string? Description { get; set; }
    
        public string PlaylistTypeName { get; set; }
        public int SongsTotal { get; set; }
        public Sources? MainSourceId { get; set; }
        [NotMapped]
        public List<PlaylistAdditionalInfo> Infos { get; set; }
    }
}