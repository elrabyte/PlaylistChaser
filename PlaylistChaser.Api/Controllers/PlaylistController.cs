using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlaylistChaser.Api.Database;
using PlaylistChaser.Model;
using PlaylistChaser.Model.BuiltInIds;

namespace PlaylistChaser.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlaylistController : ControllerBase
    {
        private readonly AdminDBContext adminDBContext;
        public PlaylistController(AdminDBContext adminDBContext)
        {
            this.adminDBContext = adminDBContext;
        }
        [HttpGet]
        [Route("get-all-playlists")]
        public ActionResult<IEnumerable<Playlist>> GetPlaylists()
        {
            return adminDBContext.Playlist.ToList();
        }

        [HttpGet("{id}")]
        public ActionResult<Playlist> GetPlaylist(int id)
        {
            var playlist = adminDBContext.Playlist.SingleOrDefault(playlist => playlist.Id == id);
            if (playlist == null) return NotFound();

            return playlist;
        }

        [HttpPut]
        [Route("remove-playlist/{id}")]
        public ActionResult RemovePlaylist(int id)
        {
            var playlist = adminDBContext.Playlist.Include(p => p.Thumbnail).SingleOrDefault(playlist => playlist.Id == id);
            if (playlist == null) return NotFound();

            adminDBContext.Thumbnail.Remove(playlist.Thumbnail);
            adminDBContext.Playlist.Remove(playlist);

            var playlistInfos = adminDBContext.PlaylistInfo.Where(i => i.PlaylistId == id);
            adminDBContext.PlaylistInfo.RemoveRange(playlistInfos);

            adminDBContext.SaveChanges();
            return Ok();
        }
        [HttpPut]
        [Route("remove-playlists")]
        public ActionResult RemovePlaylists([FromBody] List<int> ids)
        {
            var playlists = adminDBContext.Playlist.Include(p => p.Thumbnail).Where(p => ids.Contains(p.Id));
            var thumbnails = playlists.Select(p => p.Thumbnail);
            var playlistInfos = adminDBContext.PlaylistInfo.Where(i => ids.Contains(i.PlaylistId));
            adminDBContext.Playlist.RemoveRange(playlists);
            adminDBContext.Thumbnail.RemoveRange(thumbnails);
            adminDBContext.PlaylistInfo.RemoveRange(playlistInfos);

            adminDBContext.SaveChanges();

            return Ok();
        }

        [HttpGet]
        [Route("get-thumbnail/{playlistId}")]
        public ActionResult GetThumbnail(int playlistId)
        {
            var playlist = adminDBContext.Playlist.Include(p => p.Thumbnail).Single(p => p.Id == playlistId);
            var fileContents = playlist?.Thumbnail?.FileContents;

            return fileContents != null ? File(fileContents, "image/jpeg") : null;
        }
    }
}
