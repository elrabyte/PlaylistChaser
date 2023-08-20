using Microsoft.AspNetCore.Mvc;
using PlaylistChaser.Web.Database;
using PlaylistChaser.Web.Models;
using PlaylistChaser.Web.Models.ViewModel;
using PlaylistChaser.Web.Util;
using PlaylistChaser.Web.Util.API;
using SpotifyAPI.Web;
using static PlaylistChaser.Web.Util.BuiltInIds;
using Playlist = PlaylistChaser.Web.Models.Playlist;
using Thumbnail = PlaylistChaser.Web.Models.Thumbnail;

namespace PlaylistChaser.Web.Controllers
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
            return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        #endregion

        #region Views
        public async Task<ActionResult> Index()
        {
            var playlists = await db.GetPlaylists();
            var ytHelper = new YoutubeApiHelper(); //initial Auth
            return View(playlists);
        }

        public ActionResult Edit_Delete(int id, bool deleteAtSource = false)
        {
            if (deleteAtSource)
            {
                ////delete spotify playlist
                //var spotifyHelper = new SpotifyApiHelper(HttpContext);
                //if (playlist.SpotifyUrl != null)
                //    if (!spotifyHelper.DeletePlaylist(playlist).Result)
                //        return RedirectToAction("Index");


                ////delete from YT
                //var ytHelper = new YoutubeApiHelper();
                //ytHelper.DeletePlaylist(playlist.YoutubeId);
            }

            //delete from db
            db.PlaylistSong.RemoveRange(db.PlaylistSong.Where(ps => ps.PlaylistId == id));
            db.SaveChanges();
            //  remove thumbnail
            var playlist = db.Playlist.Single(p => p.Id == id);
            db.Thumbnail.Remove(db.Thumbnail.Single(t => t.Id == playlist.Id));
            db.Playlist.Remove(playlist);
            db.SaveChanges();

            songCleanUp();

            return RedirectToAction("Index");
        }

        /// <summary>
        /// removes songs that arent in any playlist
        /// </summary>
        private void songCleanUp()
        {
            throw new NotImplementedException();
        }

        public async Task<ActionResult> Details(int id)
        {
            var playlist = (await db.GetPlaylists(id)).Single();
            playlist.Songs = await db.GetSongs(playlist.PlaylistId);
            return View(playlist);
        }
        #endregion

        #region Index Functions
        #region Search

        /// <summary>
        /// adds a youtube playlist to the local db
        /// </summary>
        /// <param name="ytPlaylistUrl">youtube playlist url</param>
        /// <returns></returns>
        public async Task<ActionResult> SearchYTPlaylistAsync(string ytPlaylistUrl)
        {
            var ytHelper = new YoutubeApiHelper();
            //add playlist to db
            var playlist = new Playlist();
            var playlistID = ytHelper.GetPlaylistIdFromUrl(ytPlaylistUrl);
            playlist.YoutubeId = playlistID;
            playlist.YoutubeUrl = ytPlaylistUrl;
            playlist.PlaylistTypeId = PLaylistTypes.Simple;
            playlist = ytHelper.SyncPlaylist(playlist);

            //add thumbnail
            var thumbnail = new Thumbnail { Base64String = await new YoutubeApiHelper().GetPlaylistThumbnailBase64(playlistID) };
            db.Thumbnail.Add(thumbnail);
            db.SaveChanges();
            playlist.ThumbnailId = thumbnail.Id;

            db.Playlist.Add(playlist);
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        /// <summary>
        /// add songs if not already and connect to playlist
        /// </summary>
        /// <param name="playlistId"></param>
        /// <param name="songs"></param>
        private void addSongsToPlaylist(int playlistId, List<Song> songs, Sources source)
        {
            switch (source)
            {
                case Sources.Youtube:
                    {
                        //add new Songs to Song
                        //  check new songs by name and artist
                        var newSongs = songs.Where(s => !db.Song.Any(dbSong => dbSong.YoutubeId == s.YoutubeId));
                        db.Song.AddRange(newSongs);
                        db.SaveChanges();

                        //add new songs to PlaylistSong
                        var songsPopulated = db.Song.AsEnumerable() // Switch to client-side evaluation
                                                    .Where(dbSong => songs.Any(s => s.YoutubeId == dbSong.YoutubeId))
                                                    .ToList();
                        var curPlaylistSongIds = db.PlaylistSong.Where(ps => ps.PlaylistId == playlistId).Select(ps => ps.SongId).ToList();

                        var newPLaylistSongIds = songsPopulated.Where(s => !curPlaylistSongIds.Contains(s.Id)).Select(s => s.Id).ToList();

                        var newPlaylistSongs = newPLaylistSongIds.Select(i => new PlaylistSong { PlaylistId = playlistId, SongId = i }).ToList();
                        db.PlaylistSong.AddRange(newPlaylistSongs);
                        db.SaveChanges();

                        //add PlaylistSongState
                        db.PlaylistSongState.AddRange(newPlaylistSongs.Select(ps => new PlaylistSongState { PlaylistSongId = ps.Id, SourceId = Sources.Youtube, StateId = States.Added, LastChecked = DateTime.Now }));
                        db.SaveChanges();
                        break;
                    }

            }

        }

        /// <summary>
        /// brings the local db up to date with the youtube playlist
        /// </summary>
        /// <param name="id">local playlist id</param>
        /// <returns></returns>
        public ActionResult SyncPlaylistYoutube(int id)
        {
            var playlist = db.Playlist.Single(p => p.Id == id);

            //get songs from youtube
            var songs = new YoutubeApiHelper().GetPlaylistSongs(playlist.YoutubeId);
            addSongsToPlaylist(id, songs, Sources.Youtube);

            return new JsonResult(new { success = true });
        }
        public async Task<ActionResult> UpdateSpotifySongLinks(int id)
        {
            var songIds = db.PlaylistSong.Where(ps => ps.PlaylistId == id).Select(ps => ps.SongId);
            var songs = db.Song.Where(s => songIds.Contains(s.Id) && !s.FoundOnSpotify.Value).ToList();
            await findSongsSpotify(songs);

            return new JsonResult(new { success = true });
        }
        [HttpPost]
        public async Task<ActionResult> _ChooseSong(int songId, string newSpotifyId)
        {
            //get id from link
            newSpotifyId = newSpotifyId.Replace("https://open.spotify.com/track/", "");
            newSpotifyId = newSpotifyId.Remove(newSpotifyId.IndexOf("?"));

            var song = db.Song.Single(s => s.Id == songId);
            if (!string.IsNullOrEmpty(newSpotifyId) && song.SpotifyId != newSpotifyId)
            {
                var spotifyHelper = new SpotifyApiHelper(HttpContext);
                var spotifySong = await spotifyHelper.GetSong(newSpotifyId);

                song.SpotifyId = spotifySong.Uri;
                song.AddedToSpotify = false;
                song.IsNotOnSpotify = null;
                song.FoundOnSpotify = true;
                song.ArtistName = string.Join(",", spotifySong.Artists.Select(a => a.Name).ToList());
                song.SongName = spotifySong.Name;
                db.SaveChanges();

                ////add song to spotify
                //var playlist = db.Playlist.Single(p => p.Id == song.PlaylistId);
                //if (!await spotifyHelper.UpdatePlaylist(playlist.SpotifyUrl, new List<string> { song.SpotifyId }))
                //    return new JsonResult(new { success = false });
                //song.AddedToSpotify = true;
                //db.SaveChanges();

                return new JsonResult(new { success = true });
            }
            else
                return new JsonResult(new { success = false });


        }
        #endregion

        #region Create Playlist
        public async Task<ActionResult> CreateCombinedPlaylist(string playlistName)
        {

            //add playlist to YT
            var ytHelper = new YoutubeApiHelper();
            var playlist = await ytHelper.CreatePlaylist(playlistName);
            playlist.PlaylistTypeId = BuiltInIds.PLaylistTypes.Combined;

            db.Playlist.Add(playlist);
            db.SaveChanges();

            return new JsonResult(new { success = true });
        }
        #endregion
        #endregion

        #region Details Functions
        #region spotify

        private async Task<bool> findSongsSpotify(List<Song> songs)
        {
            var spotifyHelper = new SpotifyApiHelper(HttpContext);

            foreach (var song in songs)
            {
                var response = await spotifyHelper.SearchSong(SearchRequest.Types.Track, song.YoutubeSongName);
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
            throw new NotImplementedException();
            //var playlist = db.Playlist.Single(p => p.Id == id);
            //playlist.Songs = db.Song.Where(s => s.PlaylistId == id).ToList();
            //var spotifyHelper = new SpotifyApiHelper(HttpContext);
            ////create if not exist
            //if (string.IsNullOrEmpty(playlist.SpotifyUrl))
            //{
            //    var spotifyPlaylist = await spotifyHelper.CreatePlaylist(playlist.Name);
            //    playlist.SpotifyUrl = spotifyPlaylist.Id;
            //    db.SaveChanges();
            //}

            ////update
            //var songsToAdd = playlist.Songs.Where(s => s.FoundOnSpotify.Value
            //                                        && !s.AddedToSpotify.Value
            //                                        && (!s.IsNotOnSpotify ?? true)
            //                                        ).ToList();
            //var playlistDescription = string.Format("Last updated on {1} - Found {2}/{3} Songs - This playlist is a copy of the youtube playlist \"{4}\" by {5}: {0}. ", await BitlyApiHelper.GetShortUrl(playlist.YoutubeUrl),
            //                                                                                                                                                             DateTime.Now,
            //                                                                                                                                                             playlist.Songs.Where(s => s.FoundOnSpotify.Value).Count(),
            //                                                                                                                                                             playlist.Songs.Count(),
            //                                                                                                                                                             playlist.Name,
            //                                                                                                                                                             playlist.ChannelName);
            //if (!await spotifyHelper.UpdatePlaylist(playlist.SpotifyUrl, songsToAdd.Select(s => s.SpotifyId).ToList(), playlistDescription))
            //    return new JsonResult(new { success = false });

            ////remember added songs
            //foreach (var song in songsToAdd)
            //{
            //    song.AddedToSpotify = true;
            //    db.SaveChanges();
            //}

            return new JsonResult(new { success = true });
        }
        public async Task<ActionResult> SyncPlaylistThumbnails(int id)
        {
            var playlists = db.Playlist;
            foreach (var playlist in playlists.ToList())
            {
                var base64 = await new YoutubeApiHelper().GetPlaylistThumbnailBase64(playlist.YoutubeId);

                var thumbnail = db.Thumbnail.SingleOrDefault(t => t.Id == playlist.ThumbnailId);
                if (thumbnail == null)
                {
                    thumbnail = new Thumbnail { Base64String = base64 };
                    db.Thumbnail.Add(thumbnail);
                    db.SaveChanges();
                    playlist.ThumbnailId = thumbnail.Id;
                }
                else
                    thumbnail.Base64String = base64;
            }
            db.SaveChanges();
            return RedirectToAction("Index");
        }
        public async Task<ActionResult> SyncPlaylistThumbnail(int id)
        {
            var playlist = db.Playlist.Single(p => p.Id == id);
            var thumbnail = db.Thumbnail.SingleOrDefault(t => t.Id == playlist.ThumbnailId);
            if (thumbnail == null)
            {
                thumbnail = new Thumbnail();
                db.Thumbnail.Add(thumbnail);
                playlist.ThumbnailId = thumbnail.Id;
            }

            thumbnail.Base64String = await new YoutubeApiHelper().GetPlaylistThumbnailBase64(playlist.YoutubeId);
            db.SaveChanges();
            return new JsonResult(new { success = true });
        }

        /// <summary>
        /// updates the local thumbnails with the current ones on youtube
        /// </summary>
        /// <param name="id"></param>
        /// <param name="onlyWithNoThumbnails"></param>
        /// <returns></returns>
        public async Task<ActionResult> SyncSongsThumbnail(int id, Sources source, bool onlyWithNoThumbnails = true)
        {
            try
            {
                switch (source)
                {
                    case Sources.Youtube:
                        var playlistYoutubeId = db.Playlist.Single(p => p.Id == id).YoutubeId;
                        var songIds = db.PlaylistSong.Where(ps => ps.PlaylistId == id).Select(ps => ps.SongId);
                        var songs = db.Song.Where(p => songIds.Contains(p.Id));
                        if (onlyWithNoThumbnails)
                            songs = songs.Where(s => s.ThumbnailId == null);


                        var thumbnails = await new YoutubeApiHelper().GetSongsThumbnailBase64BySongIds(songs.Select(s => s.YoutubeId).ToList());

                        foreach (var ytThumbnail in thumbnails)
                        {
                            var song = songs.Single(s => s.YoutubeId == ytThumbnail.Key);

                            var thumbnail = db.Thumbnail.SingleOrDefault(t => t.Id == song.ThumbnailId);
                            if (thumbnail == null)
                            {
                                thumbnail = new Thumbnail { Base64String = ytThumbnail.Value };
                                db.Thumbnail.Add(thumbnail);
                                db.SaveChanges();
                                song.ThumbnailId = thumbnail.Id;
                            }
                            else
                                thumbnail.Base64String = ytThumbnail.Value;
                        }
                        db.SaveChanges();
                        return new JsonResult(new { success = true });
                    default:
                        throw new NotImplementedException();
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }
        public async Task<ActionResult> SongIsNotOnSpotify(int playlistId, int songId)
        {
            var playlist = db.Playlist.Single(p => p.Id == playlistId);
            var song = db.Song.Single(s => s.Id == songId);

            var spotifyHelper = new SpotifyApiHelper(HttpContext);
            if (await spotifyHelper.RemovePlaylistSong(playlist.SpotifyUrl, song.SpotifyId))
            {
                song.AddedToSpotify = false;
                song.FoundOnSpotify = false;
                song.IsNotOnSpotify = true;
                song.ArtistName = null;
                song.SongName = null;
                song.SpotifyId = null;
                db.SaveChanges();
            }

            db.SaveChanges();
            return new JsonResult(new { success = true });
        }


        #endregion
        #endregion
    }
}