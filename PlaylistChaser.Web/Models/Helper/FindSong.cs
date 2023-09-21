namespace PlaylistChaser.Web.Models
{
    public class FindSong
    {
        public int SongId { get; set; }
        public string ArtistName { get; set; }
        public string SongName { get; set; }

        public FindSong(int songId, string artistName, string songName)
        {
            SongId = songId;
            ArtistName = artistName;
            SongName = songName;
        }
    }
}