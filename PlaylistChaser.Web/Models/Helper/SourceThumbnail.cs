namespace PlaylistChaser.Web.Models
{
    public class SourceThumbnail
    {
        public byte[] FileContents { get; set; }
        public string? Url { get; set; }

        public SourceThumbnail(string? url, byte[] fileContents)
        {
            Url = url;
            FileContents = fileContents;
        }
    }
}