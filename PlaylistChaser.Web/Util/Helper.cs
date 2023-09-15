using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;

namespace PlaylistChaser.Web.Util
{
    internal static class Helper
    {
        public static async Task<byte[]> GetImageByUrl(string url)
        {
            using (var c = new HttpClient())
            using (var s = await c.GetStreamAsync(url))
            using (var ms = new MemoryStream())
            {
                await s.CopyToAsync(ms);
                return ms.ToArray();
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

        public static string? Url(this IUrlHelper helper, string? action, string? controller, object? values)
        {
            return helper.Action(action, controller, values);
        }

        public static string GetString(this IHtmlContent content)
        {
            using (var writer = new StringWriter())
            {
                content.WriteTo(writer, HtmlEncoder.Default);
                return writer.ToString();
            }
        }

    }
}
