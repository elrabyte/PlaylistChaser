using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlaylistChaser.Web.Database;
using PlaylistChaser.Web.Models;
using PlaylistChaser.Web.Models.ViewModel;
using PlaylistChaser.Web.Util.API;
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

        const string notImplementedForThatSource = "Not yet implemented for that source!";
        const string notImplementedForThatType = "Not yet implemented for that type!";
        #endregion

        #region Views
        public async Task<ActionResult> Index()
        {
            var playlists = await db.GetPlaylists();
            var ytHelper = new YoutubeApiHelper(); //initial Auth
            return View(playlists);
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

        public async Task<ActionResult> AddSimplePlaylist(string playlistUrl, Sources source)
        {
            try
            {
                PlaylistAdditionalInfo info;
                Thumbnail thumbnail = null;
                string playlistId;
                switch (source)
                {
                    case Sources.Youtube:
                        var ytHelper = new YoutubeApiHelper();
                        //get playlist from youtube
                        playlistId = ytHelper.GetPlaylistIdFromUrl(playlistUrl);
                        info = ytHelper.GetPlaylistById(playlistId);

                        //add thumbnail
                        thumbnail = new Thumbnail { FileContents = await ytHelper.GetPlaylistThumbnail(playlistId) };

                        break;
                    case Sources.Spotify:
                        var spotyHelper = new SpotifyApiHelper(HttpContext);
                        //get playlist from spotify
                        playlistId = spotyHelper.GetPlaylistIdFromUrl(playlistUrl);
                        info = spotyHelper.GetPlaylistById(spotyHelper.GetPlaylistIdFromUrl(playlistUrl));

                        //add thumbnail
                        thumbnail = new Thumbnail { FileContents = await spotyHelper.GetPlaylistThumbnail(playlistId) };
                        break;
                    default:
                        throw new NotImplementedException(notImplementedForThatSource);
                }

                //  add playlist
                var newPlaylist = infoToPlaylist(info, PLaylistTypes.Simple);
                db.Playlist.Add(newPlaylist);
                db.SaveChanges();

                //additional Info
                info.PlaylistId = newPlaylist.Id;
                db.PlaylistAdditionalInfo.Add(info);
                db.SaveChanges();

                //add thumbnail
                db.Thumbnail.Add(thumbnail);
                db.SaveChanges();
                newPlaylist.ThumbnailId = thumbnail.Id;
                db.SaveChanges();

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// adds a youtube playlist to the local db
        /// </summary>
        /// <param name="ytPlaylistUrl">youtube playlist url</param>
        /// <returns></returns>
        public async Task<ActionResult> SearchYTPlaylistAsync(string ytPlaylistUrl)
        {
            var ytHelper = new YoutubeApiHelper();
            //add playlist to db
            //  get playlist from youtube
            var playlistId = ytHelper.GetPlaylistIdFromUrl(ytPlaylistUrl);
            var newInfo = ytHelper.GetPlaylistById(playlistId);

            //  add playlist
            var newPlaylist = infoToPlaylist(newInfo, PLaylistTypes.Simple);
            db.Playlist.Add(newPlaylist);
            db.SaveChanges();

            //additional Info
            newInfo.PlaylistId = newPlaylist.Id;
            db.PlaylistAdditionalInfo.Add(newInfo);
            db.SaveChanges();

            //add thumbnail
            var thumbnail = new Thumbnail { FileContents = await new YoutubeApiHelper().GetPlaylistThumbnail(playlistId) };
            db.Thumbnail.Add(thumbnail);
            db.SaveChanges();
            newPlaylist.ThumbnailId = thumbnail.Id;
            db.SaveChanges();

            return RedirectToAction("Index");
        } //TODO: remove

        /// <summary>
        /// add songs if not already and connect to playlist
        /// </summary>
        /// <param name="playlistId"></param>
        /// <param name="songsFromPlaylist"></param>
        private void addSongsToPlaylist(int playlistId, List<SongAdditionalInfo> songsFromPlaylist, Sources source)
        {


            //check if songs are already in db
            //TODO: for now only check if it wasnt added from same source
            //      on youtube songname & artist name dont have to be a unique combination
            //      fuck you anguish with your stupid ass song titles
            var songInfos = db.SongAdditionalInfo.Where(i => i.SourceId == source);
            var newSongInfos = songsFromPlaylist.Where(s => !songInfos.Any(dbSong => dbSong.SongIdSource == s.SongIdSource)).ToList();

            //add new songs and infos
            newSongInfos.ForEach(i =>
            {
                var newSong = infoToSong(i);
                db.Song.Add(newSong);
                db.SaveChanges();
                i.SongId = newSong.Id;
                db.SongAdditionalInfo.Add(i);
            });
            db.SaveChanges();

            //add SongState
            db.SongState.AddRange(newSongInfos.Select(i => new SongState { SongId = i.SongId, SourceId = source, StateId = SongStates.Available, LastChecked = DateTime.Now }));
            db.SaveChanges();


            //add new songs to PlaylistSong
            var songsPopulated = songInfos.AsEnumerable() // Switch to client-side evaluation
                                        .Where(dbSong => songsFromPlaylist.Any(s => s.SongIdSource == dbSong.SongIdSource))
                                        .ToList();
            var curPlaylistSongIds = db.PlaylistSong.Where(ps => ps.PlaylistId == playlistId).Select(ps => ps.SongId).ToList();
            var newPLaylistSongIds = songsPopulated.Where(s => !curPlaylistSongIds.Contains(s.SongId)).Select(s => s.SongId).ToList();
            var newPlaylistSongs = newPLaylistSongIds.Select(i => new PlaylistSong { PlaylistId = playlistId, SongId = i }).ToList();
            db.PlaylistSong.AddRange(newPlaylistSongs);
            db.SaveChanges();

            //add PlaylistSongState
            db.PlaylistSongState.AddRange(newPlaylistSongs.Select(ps => new PlaylistSongState { PlaylistSongId = ps.Id, SourceId = Sources.Youtube, StateId = PlaylistSongStates.Added, LastChecked = DateTime.Now }));
            db.SaveChanges();
        }

        /// <summary>
        /// brings the local db up to date with the youtube playlist
        /// </summary>
        /// <param name="id">local playlist id</param>
        /// <returns></returns>

        public ActionResult SyncPlaylistFrom(int id, Sources source)
        {
            try
            {
                var playlist = db.Playlist.Single(p => p.Id == id);
                var additionalInfo = getInfo(id, source).Result;
                List<SongAdditionalInfo> songInfos = null;
                //filtered
                var plInfos = db.PlaylistAdditionalInfo.Where(i => i.SourceId == source);

                switch (source)
                {
                    case Sources.Youtube:
                        var ytHelper = new YoutubeApiHelper();
                        switch (playlist.PlaylistTypeId)
                        {
                            case PLaylistTypes.Simple:
                                songInfos = ytHelper.GetPlaylistSongs(additionalInfo.PlaylistIdSource);
                                break;
                            case PLaylistTypes.Combined:
                                //sync all attached playlists
                                var playlistIds = db.CombinedPlaylistEntry.Where(cp => cp.CombinedPlaylistId == id).Select(cp => cp.PlaylistId).ToList();
                                foreach (var plInfo in plInfos.Where(i => playlistIds.Contains(i.PlaylistId)))
                                    songInfos = ytHelper.GetPlaylistSongs(plInfo.PlaylistIdSource);

                                break;
                            default:
                                throw new NotImplementedException(notImplementedForThatType);
                        }
                        break;
                    case Sources.Spotify:
                        switch (playlist.PlaylistTypeId)
                        {
                            case PLaylistTypes.Simple:
                                var spotyHelper = new SpotifyApiHelper(HttpContext);
                                songInfos = spotyHelper.GetPlaylistSongs(additionalInfo.PlaylistIdSource);

                                break;
                            default:
                                throw new NotImplementedException(notImplementedForThatType);
                        }
                        break;
                    default:
                        throw new NotImplementedException(notImplementedForThatSource);
                }

                if (playlist.PlaylistTypeId == PLaylistTypes.Combined)
                {
                    //add add songs to combined 
                    syncCombinedPlaylistLocal(id, source);
                }


                //addSongsToPlaylist
                addSongsToPlaylist(id, songInfos, source);


                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<ActionResult> SyncPlaylistTo(int id, Sources source)
        {
            try
            {
                var playlist = db.Playlist.Single(p => p.Id == id);
                var playlistSongs = db.PlaylistSong.Where(ps => ps.PlaylistId == id);

                //filtered tables
                var songStates = db.SongState.Where(ss => ss.SourceId == source);
                var playlistSongStates = db.PlaylistSongState.Where(pss => pss.SourceId == source);
                var songInfos = db.SongAdditionalInfo.Where(i => i.SourceId == source);

                //get missing songs
                var missingSongs = getMissingSongs(id, source);
                if (!missingSongs.Any())
                    return new JsonResult(new { success = true, message = "Already up to date!" });
                var missingSongsIdsAtSource = songInfos.Where(i => missingSongs.Any(s => s.Id == i.SongId)).Select(i => i.SongIdSource).ToList();

                //check if songs exists
                findSongs(missingSongs, source);

                //get additionalPlaylistInfo
                var info = await getInfo(id, source);


                IQueryable<int> uploadedPlaylistSongIds = null;
                PlaylistAdditionalInfo newInfo;
                List<string> uploadedSongsIdsSource = null;

                switch (source)
                {
                    case Sources.Youtube:
                        {
                            var ytHelper = new YoutubeApiHelper();

                            //create Playlist
                            if (string.IsNullOrEmpty(info.PlaylistIdSource))
                            {
                                newInfo = await ytHelper.CreatePlaylist(playlist.Name, playlist.Description);
                                info.PlaylistIdSource = newInfo.PlaylistIdSource;
                                db.SaveChanges();
                            }

                            //add to playlist on youtube 
                            uploadedSongsIdsSource = ytHelper.AddSongsToPlaylist(info.PlaylistIdSource, missingSongsIdsAtSource);
                            break;
                        }
                    case Sources.Spotify:
                        {
                            var spotifyHelper = new SpotifyApiHelper(HttpContext);
                            //create Playlist
                            if (string.IsNullOrEmpty(info.PlaylistIdSource))
                            {
                                newInfo = await spotifyHelper.CreatePlaylist(playlist.Name, playlist.Description);
                                info.PlaylistIdSource = newInfo.PlaylistIdSource;
                                db.SaveChanges();
                            }

                            //add to playlist on spotify
                            uploadedSongsIdsSource = spotifyHelper.AddSongsToPlaylist(info.PlaylistIdSource, missingSongsIdsAtSource);

                            break;
                        }
                    default:
                        throw new NotImplementedException(notImplementedForThatSource);
                }



                //update PlaylistSongStates
                var uploadedSongIds = songInfos.Where(i => uploadedSongsIdsSource.Any(id => id == i.SongIdSource)).Select(i => i.SongId);
                uploadedPlaylistSongIds = playlistSongs.Where(ps => uploadedSongIds.Contains(ps.SongId)).Select(i => i.Id);
                await updatePlaylistSongsState(uploadedPlaylistSongIds, source, PlaylistSongStates.Added);

                if (uploadedPlaylistSongIds.Count() != missingSongs.Count())
                    return new JsonResult(new { success = false, message = "Not all Songs were added" });

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        private IQueryable<Song> getMissingSongs(int playlistId, Sources source)
        {
            var playlistSongs = db.PlaylistSong.Where(ps => ps.PlaylistId == playlistId).ToList();
            var states = new List<PlaylistSongStates?> { PlaylistSongStates.Added };
            var notAddedPlaylistSongs = playlistSongs.Where(ps => db.PlaylistSongState.Where(ps => ps.SourceId == source).SingleOrDefault(pss => pss.PlaylistSongId == ps.Id)?.StateId != PlaylistSongStates.Added).ToList();
            var notAddedPlaylistSongIds = notAddedPlaylistSongs.Select(pss => pss.SongId);
            var notAddedSongs = db.Song.Where(s => notAddedPlaylistSongIds.Contains(s.Id));
            return notAddedSongs;
        }

        private async Task<bool> updatePlaylistSongsState(IQueryable<int> playlistSongIds, Sources source, PlaylistSongStates state)
        {
            var playlistSongStates = db.PlaylistSongState.Where(pss => pss.SourceId == source);
            await playlistSongIds.ForEachAsync(psId =>
            {
                var playlistSongState = playlistSongStates.SingleOrDefault(pss => pss.PlaylistSongId == psId);
                if (playlistSongState == null)
                {
                    playlistSongState = new PlaylistSongState { PlaylistSongId = psId, SourceId = source, StateId = PlaylistSongStates.Added, LastChecked = DateTime.Now };
                    db.PlaylistSongState.Add(playlistSongState);
                }
                else
                {
                    playlistSongState.StateId = PlaylistSongStates.Added;
                    playlistSongState.LastChecked = DateTime.Now;
                }
                db.SaveChanges();
            });

            return true;
        }

        private void findSongs(IQueryable<Song> missingSongs, Sources source)
        {
            //filtered tables
            var songStates = db.SongState.Where(ss => ss.SourceId == source);
            var songInfos = db.SongAdditionalInfo.Where(i => i.SourceId == source);
            var missingSongsList = missingSongs.Where(s => !songInfos.Any(i => i.SongId == s.Id)).ToList();

            //check if songs exists
            List<(int Id, string IdAtSource)> foundSongs = null;
            switch (source)
            {
                case Sources.Youtube:
                    var ytHelper = new YoutubeApiHelper();
                    foundSongs = ytHelper.FindSongs(missingSongsList.Select(s => (s.Id, s.ArtistName, s.SongName)).ToList());
                    break;
                case Sources.Spotify:
                    var spotifyHelper = new SpotifyApiHelper(HttpContext);
                    foundSongs = spotifyHelper.FindSongs(missingSongsList.Select(s => (s.Id, s.ArtistName, s.SongName)).ToList());
                    break;
                default:
                    break;
            }

            foreach (var foundSong in foundSongs)
            {
                var song = db.Song.Single(s => s.Id == foundSong.Id);

                //add songInfo
                var songInfo = songToInfo(song, foundSong.IdAtSource, source);
                db.SongAdditionalInfo.Add(songInfo);
                db.SaveChanges();

                //add song state
                var songState = songStates.SingleOrDefault(ss => ss.SongId == foundSong.Id);
                if (songState == null)
                {
                    songState = new SongState { SongId = foundSong.Id, SourceId = source, StateId = SongStates.Available, LastChecked = DateTime.Now };
                    db.SongState.Add(songState);
                }
                else
                {
                    songState.StateId = SongStates.Available;
                    songState.LastChecked = DateTime.Now;
                }
                db.SaveChanges();

            }
        }

        private async Task<PlaylistAdditionalInfo> getInfo(int playlistId, Sources source)
        {
            //additional info                            
            var info = db.PlaylistAdditionalInfo.SingleOrDefault(i => i.PlaylistId == playlistId && i.SourceId == source);
            if (info == null)
            {
                var playlist = db.Playlist.Single(p => p.Id == playlistId);

                //create Playlist
                PlaylistAdditionalInfo newInfo;
                switch (source)
                {
                    case Sources.Youtube:
                        var ytHelper = new YoutubeApiHelper();
                        newInfo = await ytHelper.CreatePlaylist(playlist.Name, playlist.Description);
                        break;
                    case Sources.Spotify:
                        var spotyHelper = new SpotifyApiHelper(HttpContext);
                        newInfo = await spotyHelper.CreatePlaylist(playlist.Name, playlist.Description);
                        break;
                    default:
                        throw new NotImplementedException(notImplementedForThatSource);
                }

                info = new PlaylistAdditionalInfo
                {
                    PlaylistId = playlistId,
                    SourceId = source,
                    Name = newInfo.Name,
                    Description = newInfo.Description,
                    PlaylistIdSource = newInfo.PlaylistIdSource,
                    CreatorName = newInfo.CreatorName,
                    Url = newInfo.Url,
                    IsMine = newInfo.IsMine,
                };

                db.PlaylistAdditionalInfo.Add(info);
                db.SaveChanges();
            }
            return info;
        }
        #endregion

        #region Create Playlist
        public async Task<ActionResult> CreateCombinedPlaylist(string playlistName, string playlistIds, Sources source)
        {
            //add playlist references
            var playlistIdsList = playlistIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(i => int.Parse(i)).ToList();

            PlaylistAdditionalInfo info;
            switch (source)
            {
                case Sources.Youtube:
                    var ytHelper = new YoutubeApiHelper();
                    //add playlist to YT
                    var playlistdescription = string.Format("songs by: {0}", string.Join(',', db.Playlist.Where(p => playlistIdsList.Contains(p.Id)).Select(p => p.ChannelName)));
                    info = await ytHelper.CreatePlaylist(playlistName, playlistdescription);
                    break;
                default:
                    throw new NotImplementedException(notImplementedForThatSource);
            }

            //set infos
            var playlist = infoToPlaylist(info, PLaylistTypes.Combined);

            //add locally
            db.Playlist.Add(playlist);
            db.SaveChanges();

            info.PlaylistId = playlist.Id;
            db.PlaylistAdditionalInfo.Add(info);
            db.SaveChanges();

            db.CombinedPlaylistEntry.AddRange(playlistIdsList.Select(i => new CombinedPlaylistEntry { CombinedPlaylistId = playlist.Id, PlaylistId = i }));
            db.SaveChanges();

            //add playlistSongs
            syncCombinedPlaylistLocal(playlist.Id, source);


            return new JsonResult(new { success = true });
        }

        private void syncCombinedPlaylistLocal(int playlistId, Sources source)
        {
            var playlistIds = db.CombinedPlaylistEntry.Where(cp => cp.CombinedPlaylistId == playlistId).Select(cp => cp.PlaylistId);
            var availSongIds = db.PlaylistSong.Where(ps => playlistIds.Contains(ps.PlaylistId)).Select(ps => ps.SongId).Distinct();
            var curSongIds = db.PlaylistSong.Where(ps => ps.PlaylistId == playlistId).Select(ps => ps.SongId).Distinct();
            var newPlaylistSongs = availSongIds.Where(s => !curSongIds.Contains(s)).Select(i => new PlaylistSong { PlaylistId = playlistId, SongId = i }).ToList();
            db.PlaylistSong.AddRange(newPlaylistSongs);
            db.SaveChanges();
            db.PlaylistSongState.AddRange(newPlaylistSongs.Select(ps => new PlaylistSongState { PlaylistSongId = ps.Id, SourceId = source, StateId = PlaylistSongStates.NotAdded, LastChecked = DateTime.Now }));
            db.SaveChanges();
        }

        #endregion

        #endregion

        #region Details Functions

        #region Delete
        public ActionResult Edit_Delete(int id, bool deleteAtSources = false)
        {
            deletePlaylist(id, deleteAtSources);

            return RedirectToAction("Index");
        }

        private void deletePlaylist(int playlistId, bool deleteAtSources = false)
        {
            if (deleteAtSources)
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
            var playlist = db.Playlist.Single(p => p.Id == playlistId);
            //  remove songs
            db.PlaylistSong.RemoveRange(db.PlaylistSong.Where(ps => ps.PlaylistId == playlistId));
            db.SaveChanges();


            //TODO: needs check, could be same as song Thumbnail
            ////  remove thumbnail
            //if (playlist.ThumbnailId != null)
            //{
            //    db.Thumbnail.Remove(db.Thumbnail.Single(t => t.Id == playlist.ThumbnailId));
            //    db.SaveChanges();
            //}

            //  delete info
            var infos = db.PlaylistAdditionalInfo.Where(i => i.PlaylistId == playlistId);
            db.PlaylistAdditionalInfo.RemoveRange(infos);
            db.SaveChanges();

            //  deleete playlist
            db.Playlist.Remove(playlist);
            db.SaveChanges();

            //songCleanUp();
        }

        /// <summary>
        /// removes songs that arent in any playlist
        /// </summary>
        private void songCleanUp()
        {
            var deletableSongs = db.Song.Where(s => !db.PlaylistSong.Select(ps => ps.SongId).Contains(s.Id));
            db.Song.RemoveRange(deletableSongs);
            db.SaveChanges();
        }

        #endregion

        #region Thumbnail
        public async Task<ActionResult> SyncPlaylistThumbnails(Sources source, int? id = null)
        {
            try
            {
                var playlists = db.Playlist;
                foreach (var playlist in playlists.ToList())
                {
                    byte[] thumbnailImg;
                    var infos = db.PlaylistAdditionalInfo.Where(i => i.PlaylistId == playlist.Id);
                    switch (source)
                    {
                        case Sources.Youtube:
                            var info = infos.SingleOrDefault(i => i.SourceId == source);
                            if (info == null)
                                continue;

                            thumbnailImg = await new YoutubeApiHelper().GetPlaylistThumbnail(info.PlaylistIdSource);

                            break;
                        default:
                            throw new NotImplementedException(notImplementedForThatSource);
                    }

                    var thumbnail = db.Thumbnail.SingleOrDefault(t => t.Id == playlist.ThumbnailId);
                    if (thumbnail == null)
                    {
                        thumbnail = new Thumbnail { FileContents = thumbnailImg };
                        db.Thumbnail.Add(thumbnail);
                        db.SaveChanges();
                        playlist.ThumbnailId = thumbnail.Id;
                    }
                    else
                        thumbnail.FileContents = thumbnailImg;
                }
                db.SaveChanges();
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
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
                var info = await getInfo(id, source);
                var songInfos = db.SongAdditionalInfo.Where(i => i.SourceId == source);


                var playlistIdSource = info.PlaylistIdSource;
                var songIds = db.PlaylistSong.Where(ps => ps.PlaylistId == id).Select(ps => ps.SongId);
                var songs = db.Song.Where(p => songIds.Contains(p.Id));
                if (onlyWithNoThumbnails)
                    songs = songs.Where(s => s.ThumbnailId == null);
                var sourceIds = songInfos.Where(i => songs.Any(s => s.Id == i.SongId)).Select(i => i.SongIdSource);


                Dictionary<string, byte[]> thumbnails;
                switch (source)
                {
                    case Sources.Youtube:
                        var ytHelper = new YoutubeApiHelper();
                        thumbnails = await ytHelper.GetSongsThumbnailBySongIds(sourceIds.ToList());
                        break;
                    case Sources.Spotify:
                        var spotyHelper = new SpotifyApiHelper(HttpContext);
                        thumbnails = await spotyHelper.GetSongsThumbnailBySongIds(sourceIds.ToList());
                        break;
                    default:
                        throw new NotImplementedException(notImplementedForThatSource);
                }

                foreach (var thumbnailSource in thumbnails)
                {
                    var songInfo = songInfos.Single(i => i.SongIdSource == thumbnailSource.Key);
                    var song = songs.Single(s => s.Id == songInfo.SongId);

                    var thumbnail = db.Thumbnail.SingleOrDefault(t => t.Id == song.ThumbnailId);
                    if (thumbnail == null)
                    {
                        thumbnail = new Thumbnail { FileContents = thumbnailSource.Value };
                        db.Thumbnail.Add(thumbnail);
                        db.SaveChanges();
                        song.ThumbnailId = thumbnail.Id;
                    }
                    else
                        thumbnail.FileContents = thumbnailSource.Value;
                }
                db.SaveChanges();
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #endregion

        #region  Helper
        public ActionResult GetThumbnail(int thumbnailId)
        {
            var thumbnail = db.Thumbnail.SingleOrDefault(t => t.Id == thumbnailId);
            if (thumbnail == null)
                return null;
            return File(thumbnail.FileContents, "image/jpeg");
        }

        private Playlist infoToPlaylist(PlaylistAdditionalInfo info, PLaylistTypes playlistType, int? thumbnailId = null)
         => new Playlist { Name = info.Name, ChannelName = info.CreatorName, Description = info.Description, PlaylistTypeId = playlistType, ThumbnailId = thumbnailId };

        private SongAdditionalInfo songToInfo(Song song, string songIdSource, Sources sourceId, string url = null)
         => new SongAdditionalInfo { Name = song.SongName, ArtistName = song.ArtistName, SongId = song.Id, SongIdSource = songIdSource, SourceId = sourceId, Url = url };

        private Song infoToSong(SongAdditionalInfo info, int? thumbnailId = null)
            => new Song { SongName = info.Name, ArtistName = info.ArtistName, ThumbnailId = thumbnailId };
        #endregion
    }
}