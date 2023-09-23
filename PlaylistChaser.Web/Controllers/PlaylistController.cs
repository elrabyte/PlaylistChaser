using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PlaylistChaser.Web.Database;
using PlaylistChaser.Web.Models;
using PlaylistChaser.Web.Models.SearchModel;
using PlaylistChaser.Web.Util;
using PlaylistChaser.Web.Util.API;
using System.Data.Entity;
using static PlaylistChaser.Web.Util.BuiltInIds;
using Playlist = PlaylistChaser.Web.Models.Playlist;
using Thumbnail = PlaylistChaser.Web.Models.Thumbnail;

namespace PlaylistChaser.Web.Controllers
{
    public class PlaylistController : BaseController
    {
        #region Properties
        private YoutubeApiHelper _ytHelper;
        private YoutubeApiHelper ytHelper
        {
            get
            {
                if (_ytHelper == null)
                {
                    var oAuth = db.OAuth2CredentialReadOnly.Single(c => c.UserId == 1 && c.Provider == Sources.Youtube.ToString());
                    _ytHelper = new YoutubeApiHelper(oAuth.AccessToken);
                }
                return _ytHelper;
            }
        }

        private SpotifyApiHelper _spottyHelper;
        private SpotifyApiHelper spottyHelper
        {
            get
            {
                if (_spottyHelper == null)
                {
                    var oAuth = db.OAuth2CredentialReadOnly.Single(c => c.UserId == 1 && c.Provider == Sources.Spotify.ToString());
                    _spottyHelper = new SpotifyApiHelper(oAuth.AccessToken);
                }
                return _spottyHelper;
            }
        }
        private SongController songController;
        #endregion        

        public PlaylistController(IConfiguration configuration, PlaylistChaserDbContext db, SongController songController, IHubContext<ProgressHub> hubContext)
            : base(configuration, db, hubContext) { this.songController = songController; }


        #region Views
        public async Task<ActionResult> Index()
        {
            var playlists = await db.GetPlaylists();

            //get infos
            playlists.ForEach(p => p.Infos = db.PlaylistAdditionalInfoReadOnly.Where(i => i.PlaylistId == p.Id).ToList());

            var model = new PlaylistIndexModel
            {
                Playlists = playlists
            };

            ViewBag.Sources = db.GetSources();

            return View(model);
        }

        public async Task<ActionResult> Details(int id)
        {
            var playlist = (await db.GetPlaylists(id)).Single();
            var model = new PlaylistDetailsModel
            {
                Playlist = playlist,
                AddSongStates = true
            };

            ViewBag.Sources = db.GetSources();

            return View(model);
        }


        #region Partials
        [HttpGet]
        public ActionResult _EditPartial(int id)
        {
            var playlist = db.PlaylistReadOnly.Single(p => p.Id == id);
            return PartialView(playlist);
        }

        [HttpPost]
        public ActionResult _EditPartial(int? id, Playlist model)
        {
            try
            {
                var isNew = id == null;
                Playlist playlist;
                if (isNew)
                {
                    playlist = new Playlist();
                    db.Playlist.Add(playlist);
                }
                else
                    playlist = db.Playlist.Single(p => p.Id == id);

                playlist.Name = model.Name;
                playlist.Description = model.Description;

                db.SaveChanges();

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public ActionResult _AddSimplePlaylistPartial(Sources source)
            => PartialView(source);
        public ActionResult _AddCombinedPlaylistPartial()
        {
            ViewBag.SelectedSource = Sources.Youtube;
            ViewBag.Sources = db.GetSources();
            return PartialView();
        }

        public ActionResult _PlaylistSongStatesSummaryPartial(int id)
        {
            var playlistSongStates = db.PlaylistSongStateReadOnly.Where(pss => db.PlaylistSong.Where(ps => ps.PlaylistId == id).Select(ps => ps.Id).Contains(pss.PlaylistSongId));
            return PartialView(playlistSongStates.GroupBy(pss => pss.SourceId).AsEnumerable().OrderBy(pss => pss.Key.ToString()).ToList());
        }
        #endregion
        #endregion

        #region Add Playlists
        public async Task<ActionResult> AddSimplePlaylist(string playlistUrl, Sources source)
        {
            try
            {
                PlaylistAdditionalInfo info;
                Thumbnail thumbnail;
                string playlistId;
                switch (source)
                {
                    case Sources.Youtube:
                        //get playlist from youtube
                        playlistId = ytHelper.GetPlaylistIdFromUrl(playlistUrl);
                        info = ytHelper.GetPlaylistById(playlistId);

                        //add thumbnail
                        thumbnail = new Thumbnail { FileContents = await ytHelper.GetPlaylistThumbnail(playlistId) };

                        break;
                    case Sources.Spotify:
                        //get playlist from spotify
                        playlistId = spottyHelper.GetPlaylistIdFromUrl(playlistUrl);
                        info = spottyHelper.GetPlaylistById(spottyHelper.GetPlaylistIdFromUrl(playlistUrl));

                        //add thumbnail
                        thumbnail = new Thumbnail { FileContents = await spottyHelper.GetPlaylistThumbnail(playlistId) };
                        break;
                    default:
                        throw new NotImplementedException(ErrorHelper.NotImplementedForThatSource);
                }
                

                //add playlist to db
                var newPlaylist = Helper.InfoToPlaylist(info, PLaylistTypes.Simple);
                //  set description
                newPlaylist.Description += string.Format("\nPlaylist is from {0} by user {1}", source.ToString(), info.CreatorName);
                newPlaylist.MainSourceId = source;
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

        #endregion

        #region Sync Playlist From

        /// <summary>
        /// brings the local db up to date with the youtube playlist
        /// </summary>
        /// <param name="id">local playlist id</param>
        /// <returns></returns>

        public ActionResult SyncPlaylistFrom(int id, Sources? source = null)
        {
            try
            {
                var playlist = db.PlaylistReadOnly.Single(p => p.Id == id);
                switch (playlist.PlaylistTypeId)
                {
                    case PLaylistTypes.Simple:
                        syncPlaylistFromSimple(id, source.Value);
                        break;
                    case PLaylistTypes.Combined:
                        syncPlaylistFromCombined(id);
                        break;
                    default:
                        throw new NotImplementedException(ErrorHelper.NotImplementedForThatType);
                }
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        private void syncPlaylistFromSimple(int id, Sources source)
        {
            var info = db.PlaylistAdditionalInfoReadOnly.Single(i => i.PlaylistId == id && i.SourceId == source);

            List<SongAdditionalInfo> songInfos = null;

            switch (source)
            {
                case Sources.Youtube:
                    songInfos = ytHelper.GetPlaylistSongs(info.PlaylistIdSource);
                    break;
                case Sources.Spotify:
                    songInfos = spottyHelper.GetPlaylistSongs(info.PlaylistIdSource);
                    break;
                default:
                    throw new NotImplementedException(ErrorHelper.NotImplementedForThatSource);
            }


            //add songs to db

            var addedSongs = addSongsToDb(songInfos, source);

            addSongsToPlaylist(id, addedSongs, source);
        }

        private List<Song> addSongsToDb(List<SongAdditionalInfo> songsFromPlaylist, Sources source)
            => songController.AddSongsToDb(songsFromPlaylist, source);

        /// <summary>
        /// add songs if not already and connect to playlist
        /// </summary>
        /// <param name="playlistId"></param>
        /// <param name="songsFromPlaylist"></param>
        private void addSongsToPlaylist(int playlistId, List<Song> songsToAdd, Sources source)
        {
            //add new songs to PlaylistSong
            var curPlaylistSongIds = db.PlaylistSongReadOnly.Where(ps => ps.PlaylistId == playlistId).Select(ps => ps.SongId).ToList();
            var newPLaylistSongIds = songsToAdd.Select(s => s.Id).ToList();
            var newPlaylistSongs = newPLaylistSongIds.Select(i => new PlaylistSong { PlaylistId = playlistId, SongId = i }).ToList();
            db.PlaylistSong.AddRange(newPlaylistSongs);
            db.SaveChanges();

            //add PlaylistSongState
            //  add state for each source
            newPlaylistSongs.ForEach(ps =>
            {
                foreach (Sources src in Enum.GetValues(typeof(Sources)))
                    db.PlaylistSongState.Add(new PlaylistSongState { PlaylistSongId = ps.Id, SourceId = src, StateId = PlaylistSongStates.NotAdded, LastChecked = DateTime.Now });
                db.SaveChanges();

                var state = db.PlaylistSongState.Single(pss => pss.PlaylistSongId == ps.Id && pss.SourceId == source);
                state.StateId = PlaylistSongStates.Added;
                state.LastChecked = DateTime.Now;
            });
            db.SaveChanges();

        }

        private void syncPlaylistFromCombined(int id)
        {
            //sync all attached playlists by their main source
            var playlistIds = db.CombinedPlaylistEntryReadOnly.Where(cp => cp.CombinedPlaylistId == id).Select(cp => cp.PlaylistId).ToList();
            var playlists = db.PlaylistReadOnly.Where(p => playlistIds.Contains(p.Id));
            foreach (var playlist in playlists)
                syncPlaylistFromSimple(playlist.Id, playlist.MainSourceId.Value);

            //add songs to combined 
            syncCombinedPlaylistLocal(id);
        }

        #endregion

        #region Sync Playlist To
        public async Task<ActionResult> SyncPlaylistTo(int id, Sources source)
        {
            try
            {
                var playlist = db.PlaylistReadOnly.Single(p => p.Id == id);

                //create info - first time for that source
                var info = db.PlaylistAdditionalInfo.SingleOrDefault(i => i.PlaylistId == id && i.SourceId == source);
                if (info == null)
                {
                    info = Helper.PlaylistToInfo(playlist, source);
                    db.PlaylistAdditionalInfo.Add(info);
                }
                else if (!info.IsMine)
                    return new JsonResult(new { success = false, message = "No permission to edit playlist at source" });

                //create playlist if doesn't exists
                if (string.IsNullOrEmpty(info.PlaylistIdSource))
                {
                    PlaylistAdditionalInfo newInfo;
                    switch (source)
                    {
                        case Sources.Youtube:
                            newInfo = await ytHelper.CreatePlaylist(playlist.Name, playlist.Description);
                            break;
                        case Sources.Spotify:
                            newInfo = await spottyHelper.CreatePlaylist(playlist.Name, playlist.Description);
                            break;
                        default:
                            throw new NotImplementedException(ErrorHelper.NotImplementedForThatSource);
                    }
                    info.PlaylistIdSource = newInfo.PlaylistIdSource;
                    info.IsMine = true;
                    db.SaveChanges();
                }

                //get missing songs
                var missingSongs = getMissingSongs(id, source);
                if (!missingSongs.Any())
                    return new JsonResult(new { success = true, message = "Already up to date!" });

                //check if songs exists
                var foundSongs = await findSongs(missingSongs, source);

                //add songs to playlist
                var songInfos = db.SongAdditionalInfoReadOnly.Where(i => i.SourceId == source);
                var songsToUpload = missingSongs.Select(s => new UploadSong(s.Id, songInfos.Single(i => i.SongId == s.Id).SongIdSource)).ToList();
                await addSongsToPlaylist(source, info.PlaylistIdSource, songsToUpload);
                var uploadedSongIds = songsToUpload.Where(s => s.Uploaded).Select(s => s.SongId).ToList();
                
                //update PlaylistSongStates
                var uploadedPlaylistSongIds = db.PlaylistSongReadOnly.Where(ps => ps.PlaylistId == id && uploadedSongIds.Contains(ps.SongId)).Select(i => i.Id);
                updatePlaylistSongsState(uploadedPlaylistSongIds, source, PlaylistSongStates.Added);

                //update Playlist
                await updatePlaylist(source, info.PlaylistIdSource, info.Name, info.Description);


                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        private List<Song> getMissingSongs(int playlistId, Sources source)
        {
            var playlistSongs = db.PlaylistSongReadOnly.Where(ps => ps.PlaylistId == playlistId).ToList();
            var notAddedPlaylistSongs = playlistSongs.Where(ps => db.PlaylistSongStateReadOnly.Where(ps => ps.SourceId == source).SingleOrDefault(pss => pss.PlaylistSongId == ps.Id)?.StateId == PlaylistSongStates.NotAdded).ToList();
            var notAddedPlaylistSongIds = notAddedPlaylistSongs.Select(pss => pss.SongId);
            var notAddedSongs = db.SongReadOnly.Where(s => notAddedPlaylistSongIds.Contains(s.Id));

            notAddedSongs = notAddedSongs.Where(s => db.SongAdditionalInfoReadOnly.Single(ss => ss.SongId == s.Id && ss.SourceId == source).StateId != SongStates.NotAvailable);

            return notAddedSongs.ToList();
        }
        private async Task<List<FoundSong>> findSongs(List<Song> missingSongs, Sources source)
            => await songController.FindSongs(missingSongs, source);
        private async Task addSongsToPlaylist(Sources source, string playlistIdSource, List<UploadSong> songsToUpload)
        {
            await progressHub.InitProgressToast("adding songs to playlist...", songsToUpload.Count);
            switch (source)
            {
                case Sources.Youtube:
                    int nAdded = 0;
                    foreach (var song in songsToUpload)
                    {
                        song.Uploaded = ytHelper.AddSongToPlaylist(playlistIdSource, song.SongIdSource);
                        await progressHub.UpdateProgressToast(nAdded, $"{++nAdded} / {songsToUpload.Count} added.");
                        if (!song.Uploaded)
                            break;
                    }
                    break;
                case Sources.Spotify:
                    //max 100 items per request
                    const int maxItemsRequest = 100;
                    var nRounds = Math.Ceiling(songsToUpload.Count / (double)maxItemsRequest);
                    for (int i = 0; i < nRounds; i++)
                    {
                        var batch = songsToUpload.Skip(i * maxItemsRequest).Take(maxItemsRequest).ToList();
                        var success = spottyHelper.AddSongsToPlaylistBatch(playlistIdSource, batch.Select(b => b.SongIdSource).ToList());
                        if (success)
                        {
                            batch.ForEach(s => s.Uploaded = true);
                            var progress = (i + 1) * maxItemsRequest;
                            await progressHub.UpdateProgressToast(progress, $"{progress} / {songsToUpload.Count} added.");
                        }
                        else
                            break;
                    }
                    break;
                default:
                    throw new NotImplementedException(ErrorHelper.NotImplementedForThatSource);
            }
            await progressHub.EndProgressToast();
        }
        private void updatePlaylistSongState()
        {

        }
        private async Task<bool> updatePlaylist(Sources source, string playlistIdSource, string playlistName, string description)
        {
            bool updated = false;
            switch (source)
            {
                case Sources.Youtube:
                    updated = await ytHelper.UpdatePlaylist(playlistIdSource, playlistName, description);
                    break;
                case Sources.Spotify:
                    updated = await spottyHelper.UpdatePlaylist(playlistIdSource, playlistName, description);
                    break;
                default:
                    throw new NotImplementedException(ErrorHelper.NotImplementedForThatSource);
            }
            return updated;
        }
        private string getPlaylistDescriptionText(string playlistDescription, int uploadedSongsCount, int totalSongsCount)
            => playlistDescription + string.Format("\nFound {0}/{1} Songs, {2}", uploadedSongsCount.ToString(), totalSongsCount.ToString(), DateTime.Now.ToString());
        private void updatePlaylistSongsState(IQueryable<int> playlistSongIds, Sources source, PlaylistSongStates state)
        {
            var playlistSongStates = db.PlaylistSongState.Where(pss => pss.SourceId == source);
            playlistSongIds.ToList().ForEach(psId =>
            {
                var playlistSongState = playlistSongStates.SingleOrDefault(pss => pss.PlaylistSongId == psId);
                if (playlistSongState == null)
                {
                    playlistSongState = new PlaylistSongState { PlaylistSongId = psId, SourceId = source, StateId = state, LastChecked = DateTime.Now };
                    db.PlaylistSongState.Add(playlistSongState);
                }
                else
                {
                    playlistSongState.StateId = state;
                    playlistSongState.LastChecked = DateTime.Now;
                }
                db.SaveChanges();
            });
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
                    //add playlist to YT
                    var playlistdescription = string.Format("songs by: {0}", string.Join(',', db.PlaylistReadOnly.Where(p => playlistIdsList.Contains(p.Id)).Select(p => p.ChannelName)));
                    info = await ytHelper.CreatePlaylist(playlistName, playlistdescription);
                    break;
                default:
                    throw new NotImplementedException(ErrorHelper.NotImplementedForThatSource);
            }

            //set infos
            var playlist = Helper.InfoToPlaylist(info, PLaylistTypes.Combined);
            playlist.MainSourceId = source;

            //add locally
            db.Playlist.Add(playlist);
            db.SaveChanges();

            info.PlaylistId = playlist.Id;
            db.PlaylistAdditionalInfo.Add(info);
            db.SaveChanges();

            db.CombinedPlaylistEntry.AddRange(playlistIdsList.Select(i => new CombinedPlaylistEntry { CombinedPlaylistId = playlist.Id, PlaylistId = i }));
            db.SaveChanges();

            //add playlistSongs
            syncCombinedPlaylistLocal(playlist.Id);


            return new JsonResult(new { success = true });
        }

        private void syncCombinedPlaylistLocal(int playlistId)
        {
            var playlistIds = db.CombinedPlaylistEntryReadOnly.Where(cp => cp.CombinedPlaylistId == playlistId).Select(cp => cp.PlaylistId);
            var availSongIds = db.PlaylistSongReadOnly.Where(ps => playlistIds.Contains(ps.PlaylistId)).Select(ps => ps.SongId).Distinct();
            var curSongIds = db.PlaylistSongReadOnly.Where(ps => ps.PlaylistId == playlistId).Select(ps => ps.SongId).Distinct();
            var newPlaylistSongs = availSongIds.Where(s => !curSongIds.Contains(s)).Select(i => new PlaylistSong { PlaylistId = playlistId, SongId = i }).ToList();
            db.PlaylistSong.AddRange(newPlaylistSongs);
            db.SaveChanges();
            foreach (Sources src in Enum.GetValues(typeof(Sources)))
                db.PlaylistSongState.AddRange(newPlaylistSongs.Select(ps => new PlaylistSongState { PlaylistSongId = ps.Id, SourceId = src, StateId = PlaylistSongStates.NotAdded, LastChecked = DateTime.Now }));

            db.SaveChanges();
        }

        #endregion

        #region Delete

        public async Task<ActionResult> DeletePLaylistAtSource(int id, Sources source)
        {
            try
            {
                var playlist = db.PlaylistReadOnly.Single(p => p.Id == id);

                //delete at source
                var info = db.PlaylistAdditionalInfo.SingleOrDefault(i => i.PlaylistId == id && i.SourceId == source);
                if (info == null)
                    return new JsonResult(new { success = false, message = "you don't have a playlist at this source" });

                //check if its own
                if (!info.IsMine)
                    return new JsonResult(new { success = false, message = "you don't own the playlist" });

                bool deleteSucceeded;
                switch (source)
                {
                    case Sources.Youtube:
                        deleteSucceeded = await ytHelper.DeletePlaylist(info.PlaylistIdSource);
                        break;
                    case Sources.Spotify:
                        deleteSucceeded = await spottyHelper.DeletePlaylist(info.PlaylistIdSource);
                        break;
                    default:
                        throw new NotImplementedException(ErrorHelper.NotImplementedForThatSource);
                }

                if (!deleteSucceeded)
                    return new JsonResult(new { success = false, message = $"couldn't delete playlist on {source.ToString()}" });

                //remove info
                db.PlaylistAdditionalInfo.Remove(info);
                db.SaveChanges();

                //change states
                var playlistSongIds = db.PlaylistSongReadOnly.Where(ps => ps.PlaylistId == id).Select(ps => ps.Id);
                db.PlaylistSongState.Where(pss => playlistSongIds.Contains(pss.PlaylistSongId) && pss.SourceId == source).ToList()
                                    .ForEach(pss => { pss.StateId = PlaylistSongStates.NotAdded; pss.LastChecked = DateTime.Now; });
                db.SaveChanges();

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }
        public ActionResult DeletePlaylistLocal(int id)
        {
            try
            {
                var playlist = db.Playlist.Single(p => p.Id == id);

                //check if all sources where deleted
                if (db.PlaylistAdditionalInfoReadOnly.Where(i => i.PlaylistId == id && i.IsMine).Any())
                    return new JsonResult(new { success = false, message = "You still have Playlists at sources" });

                //remove foreign playlistinfos
                db.PlaylistAdditionalInfo.RemoveRange(db.PlaylistAdditionalInfo.Where(i => i.PlaylistId == id));

                //delete from db
                //  remove playlist songs 
                var playlistSongs = db.PlaylistSong.Where(ps => ps.PlaylistId == id);
                //      remove their states
                db.PlaylistSongState.RemoveRange(db.PlaylistSongState.Where(pss => playlistSongs.Select(ps => ps.Id).Contains(pss.PlaylistSongId))); ;
                db.SaveChanges();
                db.PlaylistSong.RemoveRange(playlistSongs);
                db.SaveChanges();

                //  remove playlist
                db.Playlist.Remove(playlist);
                db.SaveChanges();

                //  remove thumbnail
                removePlaylistThumbnail(playlist.ThumbnailId);

                //  remove songs that aren't used anymore
                songCleanUp();
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        /// <param name="thumbnailId"></param>
        /// <returns>returns true if thumbnail was deleted</returns>
        private bool removePlaylistThumbnail(int? thumbnailId)
        {
            if (thumbnailId != null)
            {
                //could be same as song thumbnail
                if (!db.SongReadOnly.Any(s => s.ThumbnailId == thumbnailId))
                {
                    db.Thumbnail.Remove(db.Thumbnail.Single(t => t.Id == thumbnailId));
                    db.SaveChanges();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// removes songs that arent in any playlist
        /// </summary>
        private void songCleanUp()
        {
            //var deletableSongs = db.Song.Where(s => !db.PlaylistSong.Select(ps => ps.SongId).Contains(s.Id));
            //if (db.SongAdditionalInfo.Any(i => deletableSongs.Select(s => s.Id).Contains(i.SongId)))
            //    return;
            //db.Song.RemoveRange(deletableSongs);
            //db.SaveChanges();
        }

        #endregion

        #region Thumbnail
        public async Task<ActionResult> SyncPlaylistThumbnailsFrom(Sources source, int? id = null)
        {
            try
            {
                var playlists = db.Playlist;
                foreach (var playlist in playlists.ToList())
                {
                    byte[] thumbnailImg;
                    var infos = db.PlaylistAdditionalInfoReadOnly.Where(i => i.PlaylistId == playlist.Id);
                    switch (source)
                    {
                        case Sources.Youtube:
                            var info = infos.SingleOrDefault(i => i.SourceId == source);
                            if (info == null)
                                continue;

                            thumbnailImg = await ytHelper.GetPlaylistThumbnail(info.PlaylistIdSource);

                            break;
                        default:
                            throw new NotImplementedException(ErrorHelper.NotImplementedForThatSource);
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
                var songInfos = db.SongAdditionalInfoReadOnly.Where(i => i.SourceId == source);
                var songIds = db.PlaylistSongReadOnly.Where(ps => ps.PlaylistId == id).Select(ps => ps.SongId);
                var songs = db.Song.Where(p => songIds.Contains(p.Id));
                if (onlyWithNoThumbnails)
                    songs = songs.Where(s => s.ThumbnailId == null);
                var sourceIds = songInfos.Where(i => songs.Any(s => s.Id == i.SongId)).Select(i => i.SongIdSource);

                Dictionary<string, byte[]> thumbnails;
                switch (source)
                {
                    case Sources.Youtube:
                        thumbnails = await ytHelper.GetSongsThumbnailBySongIds(sourceIds.ToList());
                        break;
                    case Sources.Spotify:
                        thumbnails = await spottyHelper.GetSongsThumbnailBySongIds(sourceIds.ToList());
                        break;
                    default:
                        throw new NotImplementedException(ErrorHelper.NotImplementedForThatSource);
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

        #region  Helper

        public async Task<ActionResult> GetThumbnail(int thumbnailId)
        {
            var thumbnail = db.Thumbnail.SingleOrDefault(t => t.Id == thumbnailId);
            if (thumbnail == null)
                return null;
            return File(thumbnail.FileContents, "image/jpeg");
        }

        #endregion
    }
}