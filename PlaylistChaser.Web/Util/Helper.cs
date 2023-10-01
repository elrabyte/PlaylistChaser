using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using PlaylistChaser.Web.Util.API;
using System.Text.Encodings.Web;
using static PlaylistChaser.Web.Util.BuiltInIds;

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

        #region Toast
        public static string GetToastId()
            => Guid.NewGuid().ToString("N").Substring(0, 5);
        public static string ToastMessageDisplay(bool success, int total, DateTime startTime, ref List<int> secondsElapsed, ref int nCompleted, ref int nSkipped, string progressDisplay = "{0} / {1} completed. ", string skippedDisplay = "{0} skipped.", string timeLeftDisplay = "\nTime elapsed: {0} / left: {1}")
        {
            if (success) { nCompleted++; } else { nSkipped++; }

            var timeElapsed = (int)Math.Round((DateTime.Now - startTime).TotalSeconds);
            secondsElapsed.Add(timeElapsed);

            progressDisplay = string.Format(progressDisplay, nCompleted, total);
            skippedDisplay = string.Format(skippedDisplay, nSkipped);

            var totalTimeElapsed = secondsElapsed.Sum();
            var avgTimeElapsed = (int)Math.Round(secondsElapsed.Average());
            var nLeft = (total - nCompleted);
            timeLeftDisplay = string.Format(timeLeftDisplay, TimeDisplay(totalTimeElapsed), TimeDisplay(avgTimeElapsed * nLeft));

            string messageDisplay = progressDisplay;
            if (nSkipped > 0)
                messageDisplay += skippedDisplay;
            messageDisplay += timeLeftDisplay;

            return messageDisplay;
        }
        public static string TimeDisplay(int totalSeconds)
        {
            var hours = totalSeconds / 3600;
            var minutes = (totalSeconds % 3600) / 60;
            var seconds = totalSeconds % 60;
            string displayTime;
            if (hours > 0)
                displayTime = $"{hours} h {minutes} min. {seconds} sec.";
            else if (minutes > 0)
                displayTime = $"{minutes} min. {seconds} sec.";
            else
                displayTime = $"{seconds} sec.";

            return displayTime;
        }
        #endregion

        #region JsonResponse
        public static JsonResult JsonResponse()
         => new JsonResult(new { success = true });
        public static JsonResult JsonResponse(bool success, string message)
         => new JsonResult(new { success = success, message = message });
        public static JsonResult JsonResponse(Exception ex)
         => new JsonResult(new { success = false, message = ex.Message });
        #endregion

        public static string SourcesToJs(List<Models.Source> sources)
        {
            var sourcesJs = string.Join(',', sources.Select(src => Newtonsoft.Json.JsonConvert.SerializeObject(src)));
            return $"[{sourcesJs}]";
        }

        public static string GetPlaylistUrlStart(this Sources source)
        {
            switch (source)
            {
                case Sources.Youtube:
                    return YoutubeApiHelper.PlaylistUrlStart;
                case Sources.Spotify:
                    return SpotifyApiHelper.PlaylistUrlStart;
                default:
                    throw new NotImplementedException(ErrorHelper.NotImplementedForThatSource);
            }
        }
    }
}
