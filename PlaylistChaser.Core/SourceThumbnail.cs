namespace PlaylistChaser.Core
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