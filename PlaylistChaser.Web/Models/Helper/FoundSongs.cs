namespace PlaylistChaser.Web.Models
{
    public class FoundSongs
    {
        public List<FoundSong> Exact { get; set; }
        public List<FoundSong> NotExact { get; set; }
        public FoundSongs(List<FoundSong> exact, List<FoundSong> notExact)
        {
            Exact = exact;
            NotExact = notExact;
        }
    }

    public class FoundSong
    {
        public int Id { get; set; }
        public string IdAtSource { get; set; }
        public FoundSong(int id, string idAtSource)
        {
            Id = id;
            IdAtSource = idAtSource;
        }
    }
}