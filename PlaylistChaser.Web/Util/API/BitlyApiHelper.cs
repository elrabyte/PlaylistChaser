using BitlyAPI;

namespace PlaylistChaser.Web.Util.API
{
    internal static class BitlyApiHelper
    {
        public static async Task<string> GetShortUrl(string longUrl)
        {
            //var accessToken = Helper.ReadSecret("Bitly", "AccessToken");
            var accessToken = "";
            var bitly = new Bitly(accessToken);
            var linkResponse = await bitly.PostShorten(longUrl);
            return linkResponse.Link;
        }
    }
}
