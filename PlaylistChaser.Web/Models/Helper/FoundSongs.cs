namespace PlaylistChaser.Web.Models
{
    public class FoundSong
    {
        public SongInfo NewSongInfo { get; set; }
        public bool ExactMatch { get; set; }
        public FoundSong(SongInfo newSongInfo, bool exactMatch)
        {
            NewSongInfo = newSongInfo;
            ExactMatch = exactMatch;
        }
    }
}