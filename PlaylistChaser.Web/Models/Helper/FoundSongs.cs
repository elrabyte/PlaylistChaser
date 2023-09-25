namespace PlaylistChaser.Web.Models
{
    public class FoundSong
    {
        public int Id { get; set; }
        public string IdAtSource { get; set; }
        public bool ExactMatch { get; set; }
        public FoundSong(int id, string idAtSource, bool exactMatch)
        {
            Id = id;
            IdAtSource = idAtSource;
            ExactMatch = exactMatch;
        }
    }
}