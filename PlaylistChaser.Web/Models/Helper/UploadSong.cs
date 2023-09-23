namespace PlaylistChaser.Web.Models
{
    public class UploadSong
    {
        public int SongId { get; set; }
        public string SongIdSource { get; set; }
        public bool Uploaded { get; set; }
        public UploadSong(int songId, string songIdSource)
        {
            SongId = songId;
            SongIdSource = songIdSource;
            Uploaded = false;
        }
    }
}