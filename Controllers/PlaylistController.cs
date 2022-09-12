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
            var playlists = db.Playlist.ToList();
            return View(playlists);
        }
        #endregion

        #region edit
        public ActionResult Edit_Delete(int id)
        {
            try
            {
                var songs = db.Song.Where(s => s.PlaylistId == id).ToList();
                db.Song.RemoveRange(songs);
                db.SaveChanges();
                db.Playlist.Remove(db.Playlist.Single(p => p.Id == id));
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
            var playlist = db.Playlist.Single(p => p.Id == id);
            playlist.Songs = db.Song.Where(s => s.PlaylistId == id).ToList();
            return View(playlist);
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
                    await findSongSpotify(song);
                }
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.InnerException });
            }
        }
        private async Task<bool> findSongSpotify(SongModel song)
        {
            var spotifyHelper = new SpotifyApiHelper(HttpContext);

            var response = await spotifyHelper.SearchSong(SpotifyAPI.Web.SearchRequest.Types.Track, song.YoutubeSongName);
            //first song found
            if (response.Tracks.Items?.Count > 0)
            {
                var spotifySong = response.Tracks.Items.First();
                var dbSong = db.Song.Single(s => s.Id == song.Id);
                dbSong.FoundOnSpotify = true;
                dbSong.SpotifyId = spotifySong.Id;
                dbSong.ArtistName = string.Join(",", spotifySong.Artists.Select(a => a.Name).ToList());
                dbSong.SongName = spotifySong.Name;
                db.SaveChanges();
                return true;
            }

            return false;
        }
        #endregion

        public async Task<ActionResult> UpdateSpotifyPlaylist(int id)
        {
            try
            {
                var playlist = db.Playlist.Single(p => p.Id == id);
                playlist.Songs = db.Song.Where(s => s.PlaylistId == id).ToList();
                var spotifyHelper = new SpotifyApiHelper(HttpContext);
                var spotifyPlaylist = await spotifyHelper.CreatePlaylist(playlist);
                playlist.SpotifyUrl = spotifyPlaylist.Id;
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.InnerException });
            }
        }


        public async Task<ActionResult> loginToSpotify(string code)
        {
            try
            {
                if (code == null)
                    return Redirect(SpotifyApiHelper.getLoginUri().ToString());

                var spotifyHelper = new SpotifyApiHelper(code);
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.InnerException });
            }
        }
    }
}