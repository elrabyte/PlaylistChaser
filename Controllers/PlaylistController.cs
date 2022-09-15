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

        #region views
        public IActionResult Index()
        {
            var playlists = db.Playlist.ToList();
            return View(playlists);
        }

        public ActionResult Edit_Delete(int id)
        {
            var playlist = db.Playlist.Single(p => p.Id == id);

            //delete spotify playlist
            var spotifyHelper = new SpotifyApiHelper(HttpContext);
            if(playlist.SpotifyUrl != null)
                if (!spotifyHelper.DeletePlaylist(playlist).Result)
                    return RedirectToAction("Index");

            //delete from db
            var songs = db.Song.Where(s => s.PlaylistId == id).ToList();
            db.Song.RemoveRange(songs);
            db.SaveChanges();
            db.Playlist.Remove(db.Playlist.Single(p => p.Id == id));
            db.SaveChanges();

            return RedirectToAction("Index");
        }


        public ActionResult Details(int id)
        {
            var playlist = db.Playlist.Single(p => p.Id == id);
            playlist.Songs = db.Song.Where(s => s.PlaylistId == id).ToList();
            return View(playlist);
        }
        #endregion

        #region search
        public async Task<ActionResult> SeachYTPlaylist(string ytPlaylistUrl)
        {
            var ytHelper = new YoutubeApiHelper();
            //add playlist
            var playlist = new PlaylistModel();
            playlist.YoutubeUrl = ytPlaylistUrl;
            playlist = await ytHelper.UpdatePlaylist(playlist);
            db.Playlist.Add(playlist);
            db.SaveChanges();

            //add songs
            playlist.Songs = new List<SongModel>();
            db.Song.AddRange(await ytHelper.GetPlaylistSongs(playlist));
            db.SaveChanges();

            return new JsonResult(new { success = true });
        }

        public async Task<ActionResult> CheckPlaylistYoutube(int id)
        {
            var playlist = db.Playlist.Single(p => p.Id == id);
            playlist.Songs = db.Song.Where(s => s.PlaylistId == id).ToList();
            db.Song.AddRange(await new YoutubeApiHelper().GetPlaylistSongs(playlist));
            db.SaveChanges();

            return new JsonResult(new { success = true });
        }

        public async Task<ActionResult> UpdateSpotifySongLinks(int id)
        {
            var songs = db.Song.Where(s => s.PlaylistId == id && !s.FoundOnSpotify.Value).ToList();
            await findSongsSpotify(songs);

            return new JsonResult(new { success = true });
        }
        #endregion

        #region spotify

        private async Task<bool> findSongsSpotify(List<SongModel> songs)
        {
            var spotifyHelper = new SpotifyApiHelper(HttpContext);

            foreach (var song in songs)
            {
                var response = await spotifyHelper.SearchSong(SpotifyAPI.Web.SearchRequest.Types.Track, song.YoutubeSongName);
                //first song found
                if (response.Tracks.Items?.Count > 0)
                {
                    var spotifySong = response.Tracks.Items.First();
                    var dbSong = db.Song.Single(s => s.Id == song.Id);
                    dbSong.FoundOnSpotify = true;
                    dbSong.SpotifyId = spotifySong.Uri;
                    dbSong.ArtistName = string.Join(",", spotifySong.Artists.Select(a => a.Name).ToList());
                    dbSong.SongName = spotifySong.Name;
                    db.SaveChanges();
                }
            }
            return true;

        }

        public async Task<ActionResult> UpdatePlaylistSpotify(int id)
        {
            var playlist = db.Playlist.Single(p => p.Id == id);
            playlist.Songs = db.Song.Where(s => s.PlaylistId == id).ToList();
            var spotifyHelper = new SpotifyApiHelper(HttpContext);
            //create if not exist
            if (string.IsNullOrEmpty(playlist.SpotifyUrl))
            {
                var spotifyPlaylist = await spotifyHelper.CreatePlaylist(playlist.Name);
                playlist.SpotifyUrl = spotifyPlaylist.Id;
                db.SaveChanges();
            }

            //update
            var songsToAdd = playlist.Songs.Where(s => s.FoundOnSpotify.Value && !s.AddedToSpotify.Value).ToList();
            var playlistDescription = string.Format("Last updated on {1} - Found {2}/{3} Songs - This playlist is a copy of this youtube playlist: \"{0}\". ", playlist.YoutubeUrl,
                                                                                                                                                               DateTime.Now,
                                                                                                                                                               playlist.Songs.Where(s => s.FoundOnSpotify.Value).Count(),
                                                                                                                                                               playlist.Songs.Count());
            if (!await spotifyHelper.UpdatePlaylist(playlist.SpotifyUrl, songsToAdd.Select(s => s.SpotifyId).ToList(), playlistDescription))
                return new JsonResult(new { success = false });

            //remember added songs
            foreach (var song in songsToAdd)
            {
                song.AddedToSpotify = true;
                db.SaveChanges();
            }

            return new JsonResult(new { success = true });
        }

        public async Task<ActionResult> UpdatePlaylistThumbnail(int id)
        {
            var playlist = db.Playlist.Single(p => p.Id == id);
            playlist.ImageBytes64 = await new YoutubeApiHelper().GetPlaylistThumbnailBase64(playlist.YoutubeUrl);
            db.SaveChanges();
            return new JsonResult(new { success = true });
        }
        public async Task<ActionResult> UpdateSongsThumbnail(int id, bool onlyWithNoThumbnails = true)
        {
            var songs = db.Song.Where(p => p.PlaylistId == id);
            if (onlyWithNoThumbnails)
                songs = songs.Where(s => s.ImageBytes64 == null);

            foreach (var song in songs.ToList())
            {
                song.ImageBytes64 = await new YoutubeApiHelper().GetSongThumbnailBase64(song.YoutubeId);
                db.SaveChanges();
            }
            return new JsonResult(new { success = true });
        }

        public ActionResult loginToSpotify(string code)
        {
            if (code == null)
                return Redirect(SpotifyApiHelper.getLoginUri().ToString());

            var spotifyHelper = new SpotifyApiHelper(HttpContext, code);
            return new JsonResult(new { success = true });
        }
        #endregion
    }
}