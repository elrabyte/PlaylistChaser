namespace PlaylistChaser.Web.Util
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
        public static string ReadSecret(string sectionName, string key)
        {
            var environment = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddUserSecrets<Program>()
                .AddEnvironmentVariables();
            var configurationRoot = builder.Build();

            return configurationRoot.GetSection(sectionName).GetValue<string>(key);
        }
    }
}
