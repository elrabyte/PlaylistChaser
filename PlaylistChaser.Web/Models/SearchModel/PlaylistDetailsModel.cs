using PlaylistChaser.Web.Models.ViewModel;

namespace PlaylistChaser.Web.Models.SearchModel
{ 
    public class PlaylistDetailsModel
    {
        public bool AddSongStates { get; set; }
        public PlaylistViewModel Playlist { get; set; }
    }
}