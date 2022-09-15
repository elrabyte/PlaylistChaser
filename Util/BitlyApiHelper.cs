using BitlyAPI;

namespace PlaylistChaser
{
    internal static class BitlyApiHelper
    {
        private const string accessToken = "1d646f7c0c961bd16fe36eaa72774a15f25cd023";
                
        public static async Task<string> GetShortUrl(string longUrl)
        {
            var bitly = new Bitly(accessToken);
            var linkResponse = await bitly.PostShorten(longUrl);
            return linkResponse.Link;
        }
    }
}
