using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;

namespace PlaylistChaser.Core
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

        public static string? Url(this IUrlHelper helper, string? action, string? controller, object? values = null)
            => helper.Action(action, controller, values);

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