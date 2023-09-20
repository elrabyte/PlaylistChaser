using static PlaylistChaser.Web.Util.BuiltInIds;

namespace PlaylistChaser.Web.Models.SearchModel
{
    public class SongSearchModel
    {
        public string? SongName { get; set; }
        public string? ArtistName { get; set; }
        public Sources? Source { get; set; }

        public SongStates? SongState { get; set; }
    }
}