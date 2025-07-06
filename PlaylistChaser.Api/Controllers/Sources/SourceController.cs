using Microsoft.AspNetCore.Mvc;
using PlaylistChaser.Api.Database;
using PlaylistChaser.Core.Sources;
using PlaylistChaser.Model;

namespace PlaylistChaser.Api.Controllers.Sources
{
    public class SourceController : SourceBaseController<ISource>
    {
        public SourceController(AdminDBContext adminDBContext, IHttpContextAccessor httpContextAccessor) : base(adminDBContext, httpContextAccessor) { }

        internal override ISource GetApiHelper(Type type)
        {
            var accessToken = dbHelper.GetAccessToken(sourceId);
            return (ISource)Activator.CreateInstance(type, new object[] { accessToken });
        }

        [HttpGet]
        [Route("get-playlist/{url}")]
        public PlaylistInfo GetPlaylist(string url)
        {
            var playlistInfo = apiHelper.GetPlaylistById(url);
            return playlistInfo;
        }
        [HttpPost]
        [Route("validate-playlist-url")]
        public bool ValidatePlaylistUrl(string url)
        {
            bool valid = apiHelper.ValidatePlaylistUrl(url);
            return valid;
        }
        [HttpPost]
        [Route("add-playlist")]
        public async Task<IActionResult> AddPlaylist(string url)
        {
            try
            {
                var playlistInfo = apiHelper.GetPlaylistByUrl(url);
                var playlistThumbnail = await apiHelper.GetPlaylistThumbnail(playlistInfo.PlaylistIdSource);
                dbHelper.AddPlaylist(apiHelper.SourceId, playlistInfo, playlistThumbnail);

                return Ok();
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new { message = ex.Message });
            }
        }

    }
}