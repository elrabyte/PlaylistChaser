namespace PlaylistChaser
{
    internal static class Helper
    {
        public static async Task<string> GetImageToBase64(string url)
        {
            using (var c = new HttpClient())
            using (var s = await c.GetStreamAsync(url))
            using (var ms = new MemoryStream())
            {
                await s.CopyToAsync(ms);
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }
}
