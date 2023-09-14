using PlaylistChaser.Web.Models.ViewModel;

namespace PlaylistChaser.Web.Models.SearchModel
{
    public class PlaylistIndexModel
    {
        public IEnumerable<PlaylistViewModel> Playlists { get; set; }
    }
}