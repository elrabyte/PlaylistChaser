using BitlyAPI;

namespace PlaylistChaser
{
    internal static class BitlyApiHelper
    {
        public static async Task<string> GetShortUrl(string longUrl)
        {
            var accessToken = Helper.ReadSecret("Bitly", "AccessToken");
            var bitly = new Bitly(accessToken);
            var linkResponse = await bitly.PostShorten(longUrl);
            return linkResponse.Link;
        }
    }
}
