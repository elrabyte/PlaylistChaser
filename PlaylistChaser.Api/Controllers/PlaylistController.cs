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
        [Route("add-playlist")]
        public ActionResult AddPlaylist([FromBody] AddPlaylistModel addPlaylist)
        {
            var isValid = validatePlaylistUrl(addPlaylist.PlaylistUrl);
            if (!isValid) return BadRequest("Invalid Playlist-Url");

            var insertThumbnail = new Thumbnail { FileContents = [] };
            adminDBContext.Thumbnail.Add(insertThumbnail);

            var insertPlaylist = new Playlist
            {
                UserId = 1,
                Thumbnail = insertThumbnail,
                Name = "Test",
                ChannelName = "test",
                PlaylistTypeId = PlaylistTypes.Simple,

            };
            adminDBContext.Playlist.Add(insertPlaylist);

            var insertPlaylistInfo = new PlaylistInfo
            {
                Playlist = insertPlaylist,
                SourceId = Sources.Youtube,
                PlaylistIdSource = "ytPlaylistId",
                Name = "test",
                CreatorName = "test",
                IsMine = false,
                Url = addPlaylist.PlaylistUrl,
                LastSynced = DateTime.UtcNow,
            };
            adminDBContext.PlaylistInfo.Add(insertPlaylistInfo);

            adminDBContext.SaveChanges();
            return Ok();
        }
        private bool validatePlaylistUrl(string url)
        {
            return true;
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
    }
}
