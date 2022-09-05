using Microsoft.AspNetCore.Mvc;
using PlaylistChaser.Database;
using PlaylistChaser.Models;
using System.Diagnostics;

namespace PlaylistChaser.Controllers
{
    public class PlaylistController : Controller
    {
        private readonly ILogger<PlaylistController> _logger;
        private PlaylistChaserDbContext db;

        public PlaylistController(ILogger<PlaylistController> logger)
        {
            _logger = logger;
            db = new PlaylistChaserDbContext();
        }

        #region error
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        #endregion

        #region index
        public IActionResult Index()
        {
            return View(db.Playlist.ToList());
        }
        #endregion

        #region edit
        public ActionResult Edit_Delete(int id)
        {
            try
            {
                db.Playlist.Remove(db.Playlist.Single(p => p.Id == id));
                db.Song.RemoveRange(db.Song.Where(s => s.PlaylistId == id));
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index");
            }
        }

        #endregion

        #region details
        public ActionResult Details(int id)
        {
            return View(db.Playlist.Single(p => p.Id == id));
        }
        #endregion

        #region songs
        //[HttpGet]
        //public ActionResult _SongsPartial(int playlistId)
        //{
        //    return PartialView(db.Song.Where(s => s.PlaylistId == playlistId).ToList());
        //}
        #endregion

        #region search
        public async Task<ActionResult> SeachYTPlaylist(string ytPlaylistUrl)
        {
            try
            {
                var playlist = await new YoutubeApiHelper().GetPlaylist(ytPlaylistUrl);
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.InnerException });
            }
        }

        public async Task<ActionResult> CheckPlaylist(int id)
        {
            try
            {
                var songs = db.Song.Where(s => s.PlaylistId == id).ToList();
                foreach (var song in songs)
                {
                    await checkSong(song);
                }
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.InnerException });
            }
        }
        private async Task<bool> checkSong(SongModel song)
        {
            var spotifyHelper = new SpotifyApiHelper(HttpContext);

            var response = await spotifyHelper.SearchSong(SpotifyAPI.Web.SearchRequest.Types.Track, song.YoutubeSongName);
            //single song found
            if (response.Tracks.Items?.Count == 1)
            {
                var spotifySong = response.Tracks.Items[0];
                var dbSong = db.Song.Single(s => s.Id == song.Id);
                dbSong.FoundOnSpotify = true;
                dbSong.SpotifyId = spotifySong.Id;
                dbSong.ArtistName = string.Join(",", spotifySong.Artists.Select(a => a.Name).ToList());
                db.SaveChanges();
                return true;
            }

            return false;
        }
        #endregion
    }
}