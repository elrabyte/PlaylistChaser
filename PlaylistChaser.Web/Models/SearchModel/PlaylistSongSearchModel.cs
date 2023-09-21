using static PlaylistChaser.Web.Util.BuiltInIds;

namespace PlaylistChaser.Web.Models.SearchModel
{
    public class PlaylistSongSearchModel : SongSearchModel
    {
        public int PlaylistId { get; set; }
        public PlaylistSongStates? PlaylistSongState { get; set; }
    }
}