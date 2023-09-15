using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlaylistChaser.Web.Database;
using PlaylistChaser.Web.Models;
using PlaylistChaser.Web.Models.SearchModel;
using PlaylistChaser.Web.Util.API;
using static PlaylistChaser.Web.Util.BuiltInIds;
using Playlist = PlaylistChaser.Web.Models.Playlist;
using Thumbnail = PlaylistChaser.Web.Models.Thumbnail;

namespace PlaylistChaser.Web.Controllers
{
    public class PlaylistController : BaseController
    {
        public PlaylistController(IConfiguration configuration, PlaylistChaserDbContext db) : base(configuration, db) { }

        #region Properties
        private YoutubeApiHelper _ytHelper;
        private YoutubeApiHelper ytHelper
        {
            get
            {
                if (_ytHelper == null)
                {
                    var oAuth = db.OAuth2Credential.Single(c => c.UserId == 1 && c.Provider == Sources.Youtube.ToString());
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
                    var oAuth = db.OAuth2Credential.Single(c => c.UserId == 1 && c.Provider == Sources.Spotify.ToString());
                    _spottyHelper = new SpotifyApiHelper(oAuth.AccessToken);
                }
                return _spottyHelper;
            }
        }
        #endregion        

        #region Error
        const string notImplementedForThatSource = "Not yet implemented for that source!";
        const string notImplementedForThatType = "Not yet implemented for that type!";
        #endregion

        #region Views
        public async Task<ActionResult> Index()
        {
            var playlists = await db.GetPlaylists();
            var model = new PlaylistIndexModel
            {
                Playlists = playlists
            };
            return View(model);
        }

        public async Task<ActionResult> Details(int id)
        {
            var playlist = (await db.GetPlaylists(id)).Single();
            var model = new PlaylistDetailsModel
            {
                Playlist = playlist,
                AddSongStates = false
            };

            ViewBag.SelectedSource = Sources.Youtube;
            ViewBag.Sources = getSources();

            return View(model);
        }
        #endregion

        #region Index Functions
        #region Search

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
                        throw new NotImplementedException(notImplementedForThatSource);
                }

                //add playlist to db
                var newPlaylist = infoToPlaylist(info, PLaylistTypes.Simple);
                //  set description
                newPlaylist.Description += string.Format("\nPlaylist is from {0} by user {1}", source.ToString(), info.CreatorName);
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
                //add state for each source
                foreach (Sources src in Enum.GetValues(typeof(Sources)))
                    db.SongState.Add(new SongState { SongId = newSong.Id, SourceId = src, StateId = SongStates.NotChecked, LastChecked = DateTime.Now });

                db.SaveChanges();
                var state = db.SongState.Single(ss => ss.SongId == newSong.Id && ss.SourceId == source);
                state.StateId = SongStates.Available;
                state.LastChecked = DateTime.Now;
            });
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
                var info = db.PlaylistAdditionalInfo.Single(i => i.PlaylistId == id && i.SourceId == source);

                List<SongAdditionalInfo> songInfos = null;
                //filtered
                var plInfos = db.PlaylistAdditionalInfo.Where(i => i.SourceId == source);

                switch (source)
                {
                    case Sources.Youtube:
                        switch (playlist.PlaylistTypeId)
                        {
                            case PLaylistTypes.Simple:
                                songInfos = ytHelper.GetPlaylistSongs(info.PlaylistIdSource);
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
                                songInfos = spottyHelper.GetPlaylistSongs(info.PlaylistIdSource);

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
                    //add songs to combined 
                    syncCombinedPlaylistLocal(id);
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

                //check if songs exists
                findSongs(missingSongs, source);

                var missingSongsIdsAtSource = songInfos.Where(i => missingSongs.Any(s => s.Id == i.SongId)).Select(i => i.SongIdSource).ToList();

                //create info - first time for that source
                var info = db.PlaylistAdditionalInfo.SingleOrDefault(i => i.PlaylistId == id && i.SourceId == source);
                if (info == null)
                {
                    info = infoToPlaylist(playlist, source);
                    db.PlaylistAdditionalInfo.Add(info);
                }
                else if (!info.IsMine)
                    return new JsonResult(new { success = false, message = "No permission to edit playlist at source" });

                IQueryable<int> uploadedPlaylistSongIds = null;
                PlaylistAdditionalInfo newInfo;
                List<string> uploadedSongsIdsSource = null;
                switch (source)
                {
                    case Sources.Youtube:
                        //create Playlist - first time for that source
                        if (string.IsNullOrEmpty(info.PlaylistIdSource))
                        {
                            newInfo = await ytHelper.CreatePlaylist(playlist.Name, playlist.Description);
                            info.PlaylistIdSource = newInfo.PlaylistIdSource;
                            info.IsMine = true;
                            db.SaveChanges();
                        }

                        //add to playlist on youtube 
                        uploadedSongsIdsSource = ytHelper.AddSongsToPlaylist(info.PlaylistIdSource, missingSongsIdsAtSource);

                        //update playlist
                        ytHelper.UpdatePlaylist(info.PlaylistIdSource, info.Name, getPlaylistDescriptionText(info.Description, uploadedSongsIdsSource.Count, playlistSongs.Count()));
                        break;
                    case Sources.Spotify:
                        //create Playlist
                        if (string.IsNullOrEmpty(info.PlaylistIdSource))
                        {
                            newInfo = await spottyHelper.CreatePlaylist(playlist.Name, playlist.Description);
                            info.PlaylistIdSource = newInfo.PlaylistIdSource;
                            info.IsMine = true;
                            db.SaveChanges();
                        }

                        //add to playlist on spotify
                        uploadedSongsIdsSource = spottyHelper.AddSongsToPlaylist(info.PlaylistIdSource, missingSongsIdsAtSource);

                        //update playlist
                        spottyHelper.UpdatePlaylist(info.PlaylistIdSource, playlist.Name, getPlaylistDescriptionText(playlist.Description, uploadedSongsIdsSource.Count, playlistSongs.Count()));
                        break;
                    default:
                        throw new NotImplementedException(notImplementedForThatSource);
                }

                //update PlaylistSongStates
                var uploadedSongIds = songInfos.Where(i => uploadedSongsIdsSource.Any(id => id == i.SongIdSource)).Select(i => i.SongId);
                uploadedPlaylistSongIds = playlistSongs.Where(ps => uploadedSongIds.Contains(ps.SongId)).Select(i => i.Id);
                updatePlaylistSongsState(uploadedPlaylistSongIds, source, PlaylistSongStates.Added);

                if (uploadedPlaylistSongIds.Count() != missingSongs.Count())
                    return new JsonResult(new { success = false, message = "Not all Songs were added" });

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }
        private string getPlaylistDescriptionText(string playlistDescription, int uploadedSongsCount, int totalSongsCount)
            => playlistDescription + string.Format("\nFound {0}/{1} Songs, {2}", uploadedSongsCount.ToString(), totalSongsCount.ToString(), DateTime.Now.ToString());

        private IQueryable<Song> getMissingSongs(int playlistId, Sources source)
        {
            var playlistSongs = db.PlaylistSong.Where(ps => ps.PlaylistId == playlistId).ToList();
            var notAddedPlaylistSongs = playlistSongs.Where(ps => db.PlaylistSongState.Where(ps => ps.SourceId == source).SingleOrDefault(pss => pss.PlaylistSongId == ps.Id)?.StateId == PlaylistSongStates.NotAdded).ToList();
            var notAddedPlaylistSongIds = notAddedPlaylistSongs.Select(pss => pss.SongId);
            var notAddedSongs = db.Song.Where(s => notAddedPlaylistSongIds.Contains(s.Id));

            notAddedSongs = notAddedSongs.Where(s => db.SongState.Single(ss => ss.SongId == s.Id && ss.SourceId == source).StateId != SongStates.NotAvailable);

            return notAddedSongs;
        }

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

        private void findSongs(IQueryable<Song> missingSongs, Sources source)
        {

            //filtered tables
            var songStates = db.SongState.Where(ss => ss.SourceId == source);
            var songInfos = db.SongAdditionalInfo.Where(i => i.SourceId == source);

            //only songs that weren't checked before
            missingSongs = missingSongs.Where(s => songStates.Single(ss => ss.SongId == s.Id).StateId == SongStates.NotChecked);

            //check if songs exists
            (List<(int Id, string IdAtSource)> Exact, List<(int Id, string IdAtSource)> NotExact) foundSongs;
            switch (source)
            {
                case Sources.Youtube:
                    foundSongs = ytHelper.FindSongs(missingSongs.ToList().Select(s => (s.Id, s.ArtistName, s.SongName)).ToList());
                    break;
                case Sources.Spotify:
                    //var spottyHelper = new SpotifyApiHelper(HttpContext);
                    foundSongs = spottyHelper.FindSongs(missingSongs.ToList().Select(s => (s.Id, s.ArtistName, s.SongName)).ToList());
                    break;
                default:
                    throw new NotImplementedException(notImplementedForThatSource);
            }
            foreach (var foundSong in foundSongs.Exact)
            {
                var song = db.Song.Single(s => s.Id == foundSong.Id);

                var newState = SongStates.Available;

                //add songInfo
                var songInfo = songToInfo(song, foundSong.IdAtSource, source);

                if (songInfo.ArtistName != null)
                {
                    db.SongAdditionalInfo.Add(songInfo);
                    db.SaveChanges();
                }
                else //e.g. Private video
                    newState = SongStates.NotAvailable;

                //add song state
                var songState = songStates.SingleOrDefault(ss => ss.SongId == foundSong.Id);
                if (songState == null)
                {
                    songState = new SongState { SongId = foundSong.Id, SourceId = source, StateId = newState, LastChecked = DateTime.Now };
                    db.SongState.Add(songState);
                }
                else
                {
                    songState.StateId = newState;
                    songState.LastChecked = DateTime.Now;
                }
                db.SaveChanges();

            }
            foreach (var foundSong in foundSongs.NotExact)
            {
                var song = db.Song.Single(s => s.Id == foundSong.Id);

                var newState = SongStates.MaybeAvailable;

                //add songInfo
                var songInfo = songToInfo(song, foundSong.IdAtSource, source);
                if (songInfo.ArtistName != null)
                {
                    db.SongAdditionalInfo.Add(songInfo);
                    db.SaveChanges();
                }
                else  //e.g. Private video
                    newState = SongStates.NotAvailable;

                //add song state
                var songState = songStates.SingleOrDefault(ss => ss.SongId == foundSong.Id);
                if (songState == null)
                {
                    songState = new SongState { SongId = foundSong.Id, SourceId = source, StateId = newState, LastChecked = DateTime.Now };
                    db.SongState.Add(songState);
                }
                else
                {
                    songState.StateId = newState;
                    songState.LastChecked = DateTime.Now;
                }
                db.SaveChanges();


            }
            ////set not found songs as NotAvailable
            //var stillMissingSongIds = missingSongs.Where(s => !foundSongs.Exact.Select(fs => fs.Id).Contains(s.Id)
            //                                                  && !foundSongs.NotExact.Select(fs => fs.Id).Contains(s.Id)).Select(s => s.Id).ToList();

            //stillMissingSongIds.ForEach(i =>
            //{
            //    //add song state
            //    var songState = songStates.SingleOrDefault(ss => ss.SongId == i);
            //    if (songState == null)
            //    {
            //        songState = new SongState { SongId = i, SourceId = source, StateId = SongStates.NotAvailable, LastChecked = DateTime.Now };
            //        db.SongState.Add(songState);
            //    }
            //    else
            //    {
            //        songState.StateId = SongStates.NotAvailable;
            //        songState.LastChecked = DateTime.Now;
            //    }
            //    db.SaveChanges();
            //});
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
            syncCombinedPlaylistLocal(playlist.Id);


            return new JsonResult(new { success = true });
        }

        private void syncCombinedPlaylistLocal(int playlistId)
        {
            var playlistIds = db.CombinedPlaylistEntry.Where(cp => cp.CombinedPlaylistId == playlistId).Select(cp => cp.PlaylistId);
            var availSongIds = db.PlaylistSong.Where(ps => playlistIds.Contains(ps.PlaylistId)).Select(ps => ps.SongId).Distinct();
            var curSongIds = db.PlaylistSong.Where(ps => ps.PlaylistId == playlistId).Select(ps => ps.SongId).Distinct();
            var newPlaylistSongs = availSongIds.Where(s => !curSongIds.Contains(s)).Select(i => new PlaylistSong { PlaylistId = playlistId, SongId = i }).ToList();
            db.PlaylistSong.AddRange(newPlaylistSongs);
            db.SaveChanges();
            foreach (Sources src in Enum.GetValues(typeof(Sources)))
                db.PlaylistSongState.AddRange(newPlaylistSongs.Select(ps => new PlaylistSongState { PlaylistSongId = ps.Id, SourceId = src, StateId = PlaylistSongStates.NotAdded, LastChecked = DateTime.Now }));

            db.SaveChanges();
        }

        #endregion

        #endregion

        #region Details Functions

        #region Delete

        public async Task<ActionResult> DeletePLaylistAtSource(int id, Sources source)
        {
            try
            {
                var playlist = db.Playlist.Single(p => p.Id == id);

                //delete at source
                var info = db.PlaylistAdditionalInfo.Single(i => i.PlaylistId == id && i.SourceId == source);
                switch (source)
                {
                    case Sources.Youtube:
                        await ytHelper.DeletePlaylist(info.PlaylistIdSource);
                        break;
                    case Sources.Spotify:
                        await spottyHelper.DeletePlaylist(info.PlaylistIdSource);
                        break;
                    default:
                        throw new NotImplementedException(notImplementedForThatSource);
                }

                //remove info
                db.PlaylistAdditionalInfo.Remove(info);
                db.SaveChanges();

                //change states
                var playlistSongIds = db.PlaylistSong.Where(ps => ps.PlaylistId == id).Select(ps => ps.Id);
                db.PlaylistSongState.Where(pss => playlistSongIds.Contains(pss.PlaylistSongId) && pss.SourceId == source).ToList().ForEach(pss => { pss.StateId = PlaylistSongStates.NotAdded; pss.LastChecked = DateTime.Now; });
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
                if (db.PlaylistAdditionalInfo.Any(i => i.PlaylistId == id))
                    return new JsonResult(new { success = false, message = "Playlist still active at sources" });

                //delete from db
                var playlistSongs = db.PlaylistSong.Where(ps => ps.PlaylistId == id);
                //  remove songs states
                db.PlaylistSongState.RemoveRange(db.PlaylistSongState.Where(pss => playlistSongs.Select(ps => ps.Id).Contains(pss.PlaylistSongId))); ;
                db.SaveChanges();
                //  remove playlistsongs
                db.PlaylistSong.RemoveRange(playlistSongs);
                db.SaveChanges();

                //  remove playlist
                db.Playlist.Remove(playlist);
                db.SaveChanges();

                //  remove thumbnail
                //  could be same as song thumbnail
                if (!db.Song.Any(s => s.ThumbnailId == playlist.ThumbnailId))
                {
                    //remove thumbnail
                    if (playlist.ThumbnailId != null)
                    {
                        db.Thumbnail.Remove(db.Thumbnail.Single(t => t.Id == playlist.ThumbnailId));
                        db.SaveChanges();
                    }
                }


                //  remove songs that aren't used anymore
                songCleanUp();
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
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
                    var infos = db.PlaylistAdditionalInfo.Where(i => i.PlaylistId == playlist.Id);
                    switch (source)
                    {
                        case Sources.Youtube:
                            var info = infos.SingleOrDefault(i => i.SourceId == source);
                            if (info == null)
                                continue;

                            thumbnailImg = await ytHelper.GetPlaylistThumbnail(info.PlaylistIdSource);

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
                var info = db.PlaylistAdditionalInfo.Single(i => i.PlaylistId == id && i.SourceId == source);
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
                        thumbnails = await ytHelper.GetSongsThumbnailBySongIds(sourceIds.ToList());
                        break;
                    case Sources.Spotify:
                        thumbnails = await spottyHelper.GetSongsThumbnailBySongIds(sourceIds.ToList());
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
        public async Task<ActionResult> GetThumbnail(int thumbnailId)
        {
            var thumbnail = db.Thumbnail.SingleOrDefault(t => t.Id == thumbnailId);
            if (thumbnail == null)
                return null;
            return File(thumbnail.FileContents, "image/jpeg");
        }

        private List<(Sources source, string icon)> getSources()
        {
            var sources = new List<(Sources source, string icon)>();

            foreach (Sources source in Enum.GetValues(typeof(Sources)))
            {
                var icon = db.Source.SingleOrDefault(s => s.Id == (int)source).IconHtml;
                sources.Add((source, icon));
            }
            return sources;
        }


        private Playlist infoToPlaylist(PlaylistAdditionalInfo info, PLaylistTypes playlistType, int? thumbnailId = null)
            => new Playlist { Name = info.Name, ChannelName = info.CreatorName, Description = info.Description, PlaylistTypeId = playlistType, ThumbnailId = thumbnailId };
        private PlaylistAdditionalInfo infoToPlaylist(Playlist playlist, Sources source, bool isMine = true)
            => new PlaylistAdditionalInfo { Name = playlist.Name, CreatorName = playlist.ChannelName, Description = playlist.Description, IsMine = isMine, PlaylistId = playlist.Id, SourceId = source };

        private SongAdditionalInfo songToInfo(Song song, string songIdSource, Sources sourceId, string url = null)
            => new SongAdditionalInfo { Name = song.SongName, ArtistName = song.ArtistName, SongId = song.Id, SongIdSource = songIdSource, SourceId = sourceId, Url = url };

        private Song infoToSong(SongAdditionalInfo info, int? thumbnailId = null)
            => new Song { SongName = info.Name, ArtistName = info.ArtistName, ThumbnailId = thumbnailId };
        #endregion
    }
}