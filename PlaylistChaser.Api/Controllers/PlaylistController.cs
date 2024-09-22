using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlaylistChaser.Api.Controllers.Sources;
using PlaylistChaser.Api.Database;
using PlaylistChaser.Model;
using PlaylistChaser.Model.BuiltInIds;
using PlaylistChaser.Model.ViewModel;

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
            var playlists = adminDBContext.Playlist.ToList();
            return playlists; 
        }

        [HttpGet("{id}")]
        public ActionResult<Playlist> GetPlaylist(int id)
        {
            var playlist = adminDBContext.Playlist.SingleOrDefault(playlist => playlist.Id == id);
            if (playlist == null) return NotFound();

            return playlist;
        }
        [HttpGet]
        [Route("get-playlist-populated/{id}")]
        public ActionResult<PlaylistPopulated> GetPlaylistPopulated(int id)
        {
            var playlistPopulated = new PlaylistPopulated
            {
                Playlist = adminDBContext.Playlist.Include(p => p.Thumbnail).SingleOrDefault(playlist => playlist.Id == id),
                Songs = adminDBContext.PlaylistSong.Include(ps => ps.Song).Where(ps => ps.PlaylistId == id).Select(ps => ps.Song).ToList()
            };

            return playlistPopulated;
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
        [HttpPost]
        [Route("sync-from-origin/{playlistId}")]
        public async Task<ActionResult> SyncPlaylistFromOrigin(int playlistId)
        {
            var playlist = adminDBContext.Playlist.Single(p => p.Id == playlistId);
            var originSourceId = playlist.OriginSourceId;
            var accessToken = adminDBContext.OAuth2Credential.Single(oau => oau.UserId == 1 && oau.Provider == originSourceId.ToString()).AccessToken;
            var apiHelper = SourcesHelper.GetApiHelper(originSourceId, accessToken);
            var playlistInfo = adminDBContext.PlaylistInfo.Single(p => p.SourceId == originSourceId && p.PlaylistId == playlistId);
            var songInfos = apiHelper.GetPlaylistSongs(playlistInfo.PlaylistIdSource);


            //add songs to db
            var dbHelper = new DbHelper(adminDBContext);
            dbHelper.AddSongsToDb(songInfos);

            //get local variants from songs at source
            var songSourceIds = songInfos.Select(i => i.SongIdSource).ToList();
            var songIds = songSourceIds.Select(id => adminDBContext.SongInfo.Single(i => i.SourceId == originSourceId && i.SongIdSource == id)).Select(i => i.SongId).ToList();
            var songs = adminDBContext.Song.Where(s => songIds.Contains(s.Id)).ToList();

            dbHelper.AddSongsToLocalPlaylist(originSourceId, playlistId, songs);

            //update playlistInfo
            playlistInfo.LastSynced = DateTime.UtcNow;
            adminDBContext.SaveChanges();

            return new OkResult();
        }
    }
}
