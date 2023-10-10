using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PlaylistChaser.Web.Database;
using PlaylistChaser.Web.Models;
using PlaylistChaser.Web.Models.SearchModel;
using PlaylistChaser.Web.Util;
using PlaylistChaser.Web.Util.API;
using System.Data.Entity;
using System.Security.Claims;
using static PlaylistChaser.Web.Util.BuiltInIds;
using static PlaylistChaser.Web.Util.Helper;
using Playlist = PlaylistChaser.Web.Models.Playlist;
using Thumbnail = PlaylistChaser.Web.Models.Thumbnail;

namespace PlaylistChaser.Web.Controllers
{
    public class PlaylistController : BaseController
    {
        public PlaylistController(IConfiguration configuration, SongController songController, IHubContext<ProgressHub> hubContext, IMemoryCache memoryCache, AdminDBContext dbAdmin)
            : base(configuration, hubContext, memoryCache, dbAdmin)
        {
            this.songController = songController;
            RecurringJob.AddOrUpdate("SyncPlaylists", () => SyncPlaylists(), Cron.Daily);
        }

        #region Properties
        private YoutubeApiHelper _ytHelper;
        private YoutubeApiHelper ytHelper
        {
            get
            {
                if (_ytHelper == null)
                {
                    var userId = getCurrentUserId();
                    if (userId == null)
                        return null;

                    var oAuth = UserDbContext.GetCachedList(UserDbContext.OAuth2Credential).Single(c => c.UserId == userId && c.Provider == Sources.Youtube.ToString());
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
                    var userId = getCurrentUserId();
                    if (userId == null)
                        return null;

                    var oAuth = UserDbContext.GetCachedList(UserDbContext.OAuth2Credential).Single(c => c.UserId == userId && c.Provider == Sources.Spotify.ToString());
                    _spottyHelper = new SpotifyApiHelper(oAuth.AccessToken);
                }
                return _spottyHelper;
            }
        }
        private SongController songController;
        #endregion

        #region Views

        #region View
        public async Task<ActionResult> Index()
        {
            var playlists = await UserDbContext.GetPlaylists();

            //get infos
            playlists.ForEach(p => p.Infos = UserDbContext.GetCachedList(UserDbContext.PlaylistInfo).Where(i => i.PlaylistId == p.Id).ToList());

            var model = new PlaylistIndexModel
            {
                Playlists = playlists
            };

            return View(model);
        }

        public async Task<ActionResult> Details(int id)
        {
            var userId = getCurrentUserId();
            if (userId == null)
                return null;

            var playlist = (await UserDbContext.GetPlaylists(id)).Single();
            var model = new PlaylistDetailsModel
            {
                Playlist = playlist,
                AddSongStates = true,
                Infos = UserDbContext.GetCachedList(UserDbContext.PlaylistInfo).Where(i => i.PlaylistId == id).ToList()
            };

            return View(model);
        }
        #endregion

        #region Partials

        #region Details
        public ActionResult _PlaylistInfosDetailsPartial(int playlistId)
        {

            var playlistInfos = UserDbContext.GetCachedList(UserDbContext.PlaylistInfo).Where(s => s.PlaylistId == playlistId).ToList();
            return PartialView(playlistInfos);
        }

        public ActionResult _PlaylistInfoDetailsPartial(Sources source, int playlistId)
        {
            var playlistInfo = UserDbContext.GetCachedList(UserDbContext.PlaylistInfo).Single(s => s.SourceId == source && s.PlaylistId == playlistId);
            return PartialView(playlistInfo);
        }

        public ActionResult _DetailsPartial(int playlistId)
        {
            var playlist = UserDbContext.GetCachedList(UserDbContext.Playlist).Single(p => p.Id == playlistId);
            return PartialView(playlist);
        }

        public ActionResult _CombinedPlaylistEntriesDetailsPartial(int playlistId)
        {
            var playlistIds = UserDbContext.GetCachedList(UserDbContext.CombinedPlaylistEntry).Where(c => c.CombinedPlaylistId == playlistId).Select(c => c.PlaylistId).ToList();
            var playlists = UserDbContext.GetCachedList(UserDbContext.Playlist).Where(p => playlistIds.Contains(p.Id)).ToList();
            return PartialView(playlists);
        }

        #endregion

        #region Edit


        [HttpGet]
        public ActionResult _EditPartial(int id)
        {
            var playlist = UserDbContext.GetCachedList(UserDbContext.Playlist).Single(p => p.Id == id);
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
                    var userId = getCurrentUserId();
                    if (userId == null)
                        throw new Exception("can't get userId");
                    playlist = new Playlist { UserId = userId.Value };
                    UserDbContext.Playlist.Add(playlist);
                }
                else
                    playlist = UserDbContext.Playlist.Single(p => p.Id == id);

                playlist.Name = model.Name;
                playlist.Description = model.Description;

                UserDbContext.SaveChanges();

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }
        #endregion

        public ActionResult _AddSimplePlaylistPartial(Sources source)
            => PartialView(source);
        public ActionResult _AddCombinedPlaylistPartial()
        {
            ViewBag.SelectedSource = Sources.Youtube;
            return PartialView();
        }

        public ActionResult _PlaylistSongStatesSummaryPartial(int id)
        {
            var playlistSongStates = UserDbContext.GetCachedList(UserDbContext.PlaylistSongState).Where(pss => UserDbContext.GetCachedList(UserDbContext.PlaylistSong).Where(ps => ps.PlaylistId == id).Select(ps => ps.Id).Contains(pss.PlaylistSongId)).ToList();
            var model = playlistSongStates.GroupBy(pss => pss.SourceId).OrderBy(pss => pss.Key.ToString()).ToList();

            ViewBag.PlaylistId = id;

            return PartialView(model);
        }
        #endregion

        #endregion

        #region Add Playlists
        public async Task<ActionResult> AddSimplePlaylist(string playlistUrl, Sources source)
        {
            try
            {
                PlaylistInfo info;
                SourceThumbnail sourceThumbnail;
                switch (source)
                {
                    case Sources.Youtube:
                        //get playlist from youtube
                        info = ytHelper.GetPlaylistByUrl(playlistUrl);
                        sourceThumbnail = await ytHelper.GetPlaylistThumbnail(info.PlaylistIdSource);

                        break;
                    case Sources.Spotify:
                        //get playlist from spotify
                        info = spottyHelper.GetPlaylistByUrl(playlistUrl);

                        sourceThumbnail = await spottyHelper.GetPlaylistThumbnail(info.PlaylistIdSource);
                        break;
                    default:
                        throw new NotImplementedException(ErrorHelper.NotImplementedForThatSource);
                }

                //add thumbnail
                var thumbnailId = getThumbnailId(sourceThumbnail, null);
                //add to playlist db
                var playlistId = addPlaylistToDb(info.Name, info.CreatorName, PLaylistTypes.Simple, source, info.PlaylistIdSource, info.IsMine, info.Url, info.Description, thumbnailId);
                //sync 
                syncPlaylistFromSimple(playlistId, source);

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        /// <returns>Playlistid</returns>
        private int addPlaylistToDb(string playlistName, string channelName, PLaylistTypes playlistType, Sources source, string playlistIdSource, bool isMine, string url, string description = null, int? thumbnailId = null)
        {
            var userId = getCurrentUserId();
            if (userId == null)
                throw new Exception("can't get userId");

            //add playlist to db
            var playlist = new Playlist { Name = playlistName, ChannelName = channelName, PlaylistTypeId = playlistType, Description = description, ThumbnailId = thumbnailId, MainSourceId = source, UserId = userId.Value };
            UserDbContext.Playlist.Add(playlist);
            UserDbContext.SaveChanges();

            //additional Info
            var playlistInfo = new PlaylistInfo
            {
                PlaylistId = playlist.Id,
                SourceId = source,
                PlaylistIdSource = playlistIdSource,
                Name = playlistName,
                CreatorName = channelName,
                IsMine = isMine,
                Description = description,
                Url = url,
                LastSynced = new DateTime(2000, 1, 1)
            };
            UserDbContext.PlaylistInfo.Add(playlistInfo);
            UserDbContext.SaveChanges();

            return playlist.Id;
        }
        #endregion

        #region Sync Playlists
        public ActionResult SyncPlaylists()
        {
            try
            {
                var toastId = GetToastId();
                progressHub.InitProgressToast("Sync Playlists", toastId, true);
                int nSynced = 0;
                int nSkipped = 0;
                var playlists = UserDbContext.GetCachedList(UserDbContext.Playlist).ToList();
                var timeElapsedList = new List<int>();
                var returnObj = new ReturnModel();
                foreach (var playlist in playlists)
                {
                    if (IsCancelled(toastId, out var startTime)) break;

                    if (!playlist.MainSourceId.HasValue)
                        continue;

                    #region Sync playlist from
                    var tmpToastId = GetToastId();
                    progressHub.InitProgressToast($"Syncing playlist {playlist.Name} from {playlist.MainSourceId.Value}", tmpToastId, true);
                    returnObj = syncPlaylistFrom(playlist.Id, playlist.MainSourceId.Value);
                    progressHub.EndProgressToast(tmpToastId);
                    if (!returnObj.Success)
                        break;
                    #endregion

                    #region Sync playlist to
                    var infos = UserDbContext.GetCachedList(UserDbContext.PlaylistInfo).Where(i => i.IsMine && i.SourceId != (playlist.MainSourceId ?? 0) && i.PlaylistId == playlist.Id).ToList();
                    foreach (var info in infos)
                    {
                        tmpToastId = GetToastId();
                        progressHub.InitProgressToast($"Syncing playlist {playlist.Name} to {info.SourceId}", tmpToastId, true);
                        returnObj = syncPlaylistTo(info.SourceId, info.PlaylistId).Result;
                        progressHub.EndProgressToast(tmpToastId);
                    }
                    #endregion

                    var msgDisplay = ToastMessageDisplay(returnObj.Success, playlists.Count, startTime, ref timeElapsedList, ref nSynced, ref nSkipped);
                    progressHub.UpdateProgressToast($"Syncing Playlist...", nSynced, playlists.Count, msgDisplay, toastId, true);
                }
                progressHub.EndProgressToast(toastId);

                return JsonResponse(returnObj);
            }
            catch (Exception ex)
            {
                return JsonResponse(ex);
            }
        }
        #endregion

        #region Sync Playlists From
        public ActionResult SyncPlaylistsFrom()
        {
            try
            {
                var toastId = GetToastId();
                progressHub.InitProgressToast("Sync Playlists", toastId, true);
                int nSynced = 0;
                int nSkipped = 0;
                var playlists = UserDbContext.GetCachedList(UserDbContext.Playlist).ToList();
                var timeElapsedList = new List<int>();
                var returnObj = new ReturnModel();
                foreach (var playlist in playlists)
                {
                    if (IsCancelled(toastId, out var startTime)) break;

                    if (!playlist.MainSourceId.HasValue)
                        continue;

                    returnObj = syncPlaylistFrom(playlist.Id, playlist.MainSourceId.Value);
                    if (!returnObj.Success)
                        break;

                    var msgDisplay = ToastMessageDisplay(returnObj.Success, playlists.Count, startTime, ref timeElapsedList, ref nSynced, ref nSkipped);

                    progressHub.UpdateProgressToast($"Syncing Playlist {playlist.Name} from {playlist.MainSourceId.Value.ToString()}", nSynced, playlists.Count, msgDisplay, toastId, true);
                }
                progressHub.EndProgressToast(toastId);

                return JsonResponse(returnObj);
            }
            catch (Exception ex)
            {
                return JsonResponse(ex);
            }
        }
        #endregion
        #region Sync Playlist From

        public ActionResult SyncPlaylistFrom(int id, Sources source)
        {
            var returnObj = syncPlaylistFrom(id, source);

            return JsonResponse(returnObj);
        }
        private ReturnModel syncPlaylistFrom(int id, Sources? source = null)
        {
            try
            {
                var playlist = UserDbContext.GetCachedList(UserDbContext.Playlist).Single(p => p.Id == id);
                switch (playlist.PlaylistTypeId)
                {
                    case PLaylistTypes.Simple:
                        if (!source.HasValue)
                            return new ReturnModel("No Source defined!");

                        var returnModel = syncPlaylistFromSimple(id, source.Value);
                        return returnModel;
                    case PLaylistTypes.Combined:
                        syncPlaylistFromCombined(id);
                        break;
                    default:
                        throw new NotImplementedException(ErrorHelper.NotImplementedForThatType);
                }
                return new ReturnModel();
            }
            catch (Exception ex)
            {
                return new ReturnModel(ex.Message);
            }
        }

        private void syncPlaylistFromCombined(int id)
        {
            //sync all attached playlists by their main source
            var playlistIds = UserDbContext.GetCachedList(UserDbContext.CombinedPlaylistEntry).Where(cp => cp.CombinedPlaylistId == id).Select(cp => cp.PlaylistId).ToList();
            var playlists = UserDbContext.GetCachedList(UserDbContext.Playlist).Where(p => playlistIds.Contains(p.Id)).ToList();

            var toastId = GetToastId();
            progressHub.InitProgressToast("Sync Combined Playlists", toastId, true);
            int nCompleted = 0;
            int nSkipped = 0;
            var timeElapsedList = new List<int>();
            foreach (var playlist in playlists)
            {
                if (IsCancelled(toastId, out var startTime)) break;

                var returnObj = syncPlaylistFromSimple(playlist.Id, playlist.MainSourceId.Value);
                var msgDisplay = ToastMessageDisplay(returnObj.Success, playlists.Count, startTime, ref timeElapsedList, ref nCompleted, ref nSkipped);

                progressHub.UpdateProgressToast("Syncing Combined Playlists...", nCompleted, playlists.Count, msgDisplay, toastId, true);
            }
            progressHub.EndProgressToast(toastId);

            //add songs to combined 
            syncCombinedPlaylistLocal(id);
        }

        private ReturnModel syncPlaylistFromSimple(int id, Sources source)
        {
            var info = UserDbContext.PlaylistInfo.Single(i => i.PlaylistId == id && i.SourceId == source);

            //skip if last sync was less than 5min ago
            if (info.LastSynced > DateTime.Now.AddMinutes(-5))
                return new ReturnModel("Already recently Synced");

            List<SongInfo> songInfos;

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
            addSongsToDb(songInfos);

            //get local variants from songs at source
            var songSourceIds = songInfos.Select(i => i.SongIdSource).ToList();
            var songIds = songSourceIds.Select(id => UserDbContext.GetCachedList(UserDbContext.SongInfo).Single(i => i.SourceId == source && i.SongIdSource == id)).Select(i => i.SongId).ToList();
            var songs = UserDbContext.GetCachedList(UserDbContext.Song).Where(s => songIds.Contains(s.Id)).ToList();

            addSongsToLocalPlaylist(source, id, songs);

            //update playlistInfo
            info.LastSynced = DateTime.Now;
            UserDbContext.SaveChanges();

            return new ReturnModel();
        }

        private List<Song> addSongsToDb(List<SongInfo> songsFromPlaylist)
            => songController.AddSongsToDb(songsFromPlaylist);

        /// <summary>
        /// add songs if not already and connect to playlist
        /// </summary>
        /// <param name="playlistId"></param>
        /// <param name="songsFromPlaylist"></param>
        private void addSongsToLocalPlaylist(Sources source, int playlistId, List<Song> songsToAdd)
        {
            //only add new songs
            var curSongIds = UserDbContext.GetCachedList(UserDbContext.PlaylistSong).Where(ps => ps.PlaylistId == playlistId).Select(ps => ps.SongId).ToList();
            var newSongIds = songsToAdd.Select(s => s.Id).Where(i => !curSongIds.Contains(i)).ToList();

            var newPlaylistSongs = newSongIds.Select(i => new PlaylistSong { PlaylistId = playlistId, SongId = i }).ToList();
            addPlaylistSongs(source, newPlaylistSongs);

        }
        private void addPlaylistSongs(Sources source, List<PlaylistSong> playlistSongs)
        {
            UserDbContext.PlaylistSong.AddRange(playlistSongs);
            UserDbContext.SaveChanges();

            playlistSongs.ForEach(ps =>
            {
                UserDbContext.PlaylistSongState.Add(new PlaylistSongState { PlaylistSongId = ps.Id, SourceId = source, StateId = PlaylistSongStates.Added, LastChecked = DateTime.Now });
            });
            UserDbContext.SaveChanges();
        }

        #endregion

        #region Sync Playlists To
        public async Task<ActionResult> SyncPlaylistsTo()
        {
            try
            {
                var toastId = GetToastId();
                progressHub.InitProgressToast("Sync Playlists To", toastId, true);
                int nSynced = 0;
                int nSkipped = 0;
                var playlists = UserDbContext.GetCachedList(UserDbContext.Playlist).ToList();
                var timeElapsedList = new List<int>();
                var returnObj = new ReturnModel();

                var playlistIds = playlists.Select(p => p.Id).ToList();

                var infos = new List<PlaylistInfo>();
                foreach (var playlist in playlists)
                    infos = UserDbContext.GetCachedList(UserDbContext.PlaylistInfo).Where(i => i.IsMine && i.SourceId != (playlist.MainSourceId ?? 0) && i.PlaylistId == playlist.Id).ToList();

                foreach (var info in infos)
                {
                    if (IsCancelled(toastId, out var startTime)) break;

                    returnObj = await syncPlaylistTo(info.SourceId, info.PlaylistId);

                    if (!returnObj.Success)
                        break;

                    var msgDisplay = ToastMessageDisplay(returnObj.Success, playlists.Count, startTime, ref timeElapsedList, ref nSynced, ref nSkipped);

                    progressHub.UpdateProgressToast($"Syncing Playlist {info.Name} to {info.SourceId.ToString()}", nSynced, playlists.Count, msgDisplay, toastId, true);
                }
                progressHub.EndProgressToast(toastId);

                return JsonResponse(returnObj);
            }
            catch (Exception ex)
            {
                return JsonResponse(ex);
            }
        }
        #endregion
        #region Sync Playlist To

        public async Task<ActionResult> SyncPlaylistTo(int id, Sources source)
        {
            var returnObj = await syncPlaylistTo(source, id);
            return JsonResponse(returnObj);
        }
        private async Task<ReturnModel> syncPlaylistTo(Sources source, int id)
        {
            var playlist = UserDbContext.GetCachedList(UserDbContext.Playlist).Single(p => p.Id == id);

            //create info - first time for that source
            var info = UserDbContext.PlaylistInfo.SingleOrDefault(i => i.SourceId == source && i.PlaylistId == id);
            if (info != null && !info.IsMine)
                throw new Exception("No permission to edit playlist at source");


            //create playlist if doesn't exists
            if (string.IsNullOrEmpty(info?.PlaylistIdSource))
            {
                PlaylistInfo newInfo;
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

                string descriptionText = "Copied from $OriginalSource$: \n$OriginalPlaylistName$ by $OriginalCreatorName$. \n$SongsUploaded$ / $SongsTotal$ - $LastChangeDate$";
                info = addPlaylistInfoToDb(id, source, newInfo.PlaylistIdSource, newInfo.Name, newInfo.CreatorName, true, newInfo.Url, descriptionText);
            }

            //get missing songs
            var missingSongs = getMissingSongs(id, source);
            if (!missingSongs.Any())
                return new ReturnModel(true, "Already up to date!");


            //check if songs exists at source
            var foundSongs = await findSongs(missingSongs, source);

            //add songs to playlist
            //  prepare
            var songsToUpload = missingSongs.Select(s => new UploadSong(s.Id, UserDbContext.GetCachedList(UserDbContext.SongInfo).SingleOrDefault(i => i.SourceId == source && i.SongId == s.Id)?.SongIdSource)).ToList();
            songsToUpload = songsToUpload.Where(s => !string.IsNullOrEmpty(s.SongIdSource)).ToList();
            songsToUpload = songsToUpload.DistinctBy(s => s.SongIdSource).ToList();

            //  upload
            var returnObj = new ReturnModel();
            if (songsToUpload.Any())
                returnObj = await uploadSongsToPlaylist(source, info.PlaylistIdSource, songsToUpload);
            if (!returnObj.Success)
                return returnObj;

            //update Playlist
            var playlistDescription = getPlaylistDescriptionText(info);
            returnObj = await updatePlaylist(source, info.PlaylistIdSource, info.Name, playlistDescription);
            if (!returnObj.Success)
                return returnObj;

            return returnObj;
        }
        private string getPlaylistDescriptionText(PlaylistInfo info)
        {
            var playlistDescription = info.Description;
            var playlist = UserDbContext.GetCachedList(UserDbContext.Playlist).Single(p => p.Id == info.PlaylistId);
            var playlistSongIds = UserDbContext.GetCachedList(UserDbContext.PlaylistSong).Where(ps => ps.PlaylistId == playlist.Id).Select(ps => ps.Id).ToList();
            var playlistSongStates = UserDbContext.GetCachedList(UserDbContext.PlaylistSongState).Where(pss => pss.SourceId == info.SourceId && pss.StateId == PlaylistSongStates.Added && playlistSongIds.Contains(pss.PlaylistSongId)).ToList();

            var songsUploaded = playlistSongStates.Count();
            var songsTotal = playlistSongIds.Count();
            var lastChangeDate = playlistSongStates.Max(pss => pss.LastChecked);

            switch (playlist.PlaylistTypeId)
            {
                case PLaylistTypes.Simple:

                    var originalInfo = UserDbContext.GetCachedList(UserDbContext.PlaylistInfo).Single(i => i.SourceId == playlist.MainSourceId && i.PlaylistId == playlist.Id);


                    var originalSource = originalInfo.SourceId.ToString();
                    var originalPlaylistName = originalInfo.Name;
                    var originalCreatorName = originalInfo.CreatorName;

                    playlistDescription = playlistDescription.Replace("$OriginalSource$", originalSource);
                    playlistDescription = playlistDescription.Replace("$OriginalPlaylistName$", originalPlaylistName);
                    playlistDescription = playlistDescription.Replace("$OriginalCreatorName$", originalCreatorName);
                    playlistDescription = playlistDescription.Replace("$SongsUploaded$", songsUploaded.ToString());
                    playlistDescription = playlistDescription.Replace("$SongsTotal$", songsTotal.ToString());
                    playlistDescription = playlistDescription.Replace("$LastChangeDate$", lastChangeDate.ToShortDateString());

                    return playlistDescription;
                case PLaylistTypes.Combined:
                    playlistDescription = "A combined playlist consisting of:\n";

                    var playlistIds = UserDbContext.GetCachedList(UserDbContext.CombinedPlaylistEntry).Where(e => e.CombinedPlaylistId == playlist.Id).Select(e => e.PlaylistId).ToList();
                    var playlists = UserDbContext.GetCachedList(UserDbContext.Playlist).Where(p => playlistIds.Contains(p.Id)).ToList();
                    foreach (var entryPlaylist in playlists)
                        playlistDescription += $"{entryPlaylist.ChannelName} - {entryPlaylist.Name}\n";

                    playlistDescription += "$SongsUploaded$ / $SongsTotal$ - $LastChangeDate$";
                    playlistDescription = playlistDescription.Replace("$SongsUploaded$", songsUploaded.ToString());
                    playlistDescription = playlistDescription.Replace("$SongsTotal$", songsTotal.ToString());
                    playlistDescription = playlistDescription.Replace("$LastChangeDate$", lastChangeDate.ToShortDateString());

                    return playlistDescription;
                default:
                    throw new NotImplementedException(ErrorHelper.NotImplementedForThatType);
            }

        }
        private PlaylistInfo addPlaylistInfoToDb(int playlistId, Sources source, string playlistIdSource, string playlistName, string creatorName, bool isMine, string url, string? description = null)
        {
            var playlistInfo = new PlaylistInfo
            {
                PlaylistId = playlistId,
                SourceId = source,
                PlaylistIdSource = playlistIdSource,
                Name = playlistName,
                CreatorName = creatorName,
                IsMine = isMine,
                Description = description,
                Url = url,
                LastSynced = DateTime.Now
            };
            UserDbContext.PlaylistInfo.Add(playlistInfo);
            UserDbContext.SaveChanges();
            return playlistInfo;
        }

        /// <summary>
        /// Get songs not yet added to playlist at that source
        /// </summary>
        private List<Song> getMissingSongs(int playlistId, Sources source)
        {
            var songs = UserDbContext.GetCachedList(UserDbContext.Song);
            var songStates = UserDbContext.GetCachedList(UserDbContext.SongState);
            var playlistSongs = UserDbContext.GetCachedList(UserDbContext.PlaylistSong);
            var playlistSongStates = UserDbContext.GetCachedList(UserDbContext.PlaylistSongState);


            //get songs that should be in playlist
            playlistSongs = playlistSongs.Where(ps => ps.PlaylistId == playlistId).ToList();

            //get PlaylistSongs with StateId NotAdded or no PlaylistSongState-Entry
            var notAddedPlaylistSongs = playlistSongs.Where(ps => !playlistSongStates.Where(pss => pss.SourceId == source && pss.PlaylistSongId == ps.Id).Any()
                                                                  || playlistSongStates.Single(pss => pss.SourceId == source && pss.PlaylistSongId == ps.Id).StateId == PlaylistSongStates.NotAdded).ToList();

            //get songs from playlistsongs
            var notAddedSongIds = notAddedPlaylistSongs.Select(pss => pss.SongId).ToList();
            var notAddedSongs = songs.Where(s => notAddedSongIds.Contains(s.Id)).ToList();

            //ignore unavailable songs
            notAddedSongs = notAddedSongs.Where(s => songStates.SingleOrDefault(ss => ss.SourceId == source && ss.SongId == s.Id)?.StateId != SongStates.NotAvailable).ToList();

            return notAddedSongs;
        }
        private async Task<List<FoundSong>> findSongs(List<Song> missingSongs, Sources source)
            => await songController.FindSongs(missingSongs, source);
        private async Task<ReturnModel> uploadSongsToPlaylist(Sources source, string playlistIdSource, List<UploadSong> songsToUpload)
        {
            if (!songsToUpload.Any())
                throw new Exception("List can't be empty");

            if (songsToUpload.Any(s => string.IsNullOrEmpty(s.SongIdSource)))
                throw new Exception("SongIdSource can't be null");

            if (songsToUpload.GroupBy(x => x.SongIdSource).Where(x => x.Count() > 1).Any())
                throw new Exception("There are duplicates");

            var songStates = UserDbContext.GetCachedList(UserDbContext.SongState).Where(i => i.SourceId == source && songsToUpload.Select(s => s.SongId).Contains(i.SongId)).ToList();
            if (songStates.Any(i => !(i.StateId == SongStates.Available)))
                throw new Exception("All songs must be available");

            var toastId = GetToastId();
            await progressHub.InitProgressToast("Add songs to playlist", toastId, true);
            var timeElapsedList = new List<int>();
            int nAdded = 0;
            int nSkipped = 0;
            var returnObj = new ReturnModel();

            switch (source)
            {
                case Sources.Youtube:
                    foreach (var song in songsToUpload)
                    {
                        if (IsCancelled(toastId, out var startTime)) break;

                        returnObj = ytHelper.AddSongToPlaylist(playlistIdSource, song.SongIdSource);
                        updatePlaylistSongState(source, playlistIdSource, song.SongIdSource, returnObj.Success);

                        var msgDisplay = ToastMessageDisplay(returnObj.Success, songsToUpload.Count, startTime, ref timeElapsedList, ref nAdded, ref nSkipped, "{0} / {1} uploaded");

                        await progressHub.UpdateProgressToast("Uploading songs to playlist...", nAdded, songsToUpload.Count, msgDisplay, toastId, true);
                        if (!returnObj.Success)
                            break;
                    }
                    break;
                case Sources.Spotify:
                    //max 100 items per request
                    const int maxItemsRequest = 100;
                    var nRounds = Math.Ceiling(songsToUpload.Count / (double)maxItemsRequest);
                    for (int i = 0; i < nRounds; i++)
                    {
                        if (IsCancelled(toastId, out var startTime)) break;

                        var batch = songsToUpload.Skip(i * maxItemsRequest).Take(maxItemsRequest).ToList();
                        returnObj = spottyHelper.AddSongsToPlaylistBatch(playlistIdSource, batch.Select(b => b.SongIdSource).ToList());

                        updatePlaylistSongsState(source, playlistIdSource, batch.Select(s => s.SongIdSource).ToList(), returnObj.Success);
                        if (returnObj.Success)
                        {
                            batch.ForEach(s => s.Uploaded = true);
                            nAdded = (i + 1) * maxItemsRequest;

                            var msgDisplay = ToastMessageDisplay(returnObj.Success, songsToUpload.Count, startTime, ref timeElapsedList, ref nAdded, ref nSkipped);

                            await progressHub.UpdateProgressToast("Adding songs to playlist...", nAdded, songsToUpload.Count, msgDisplay, toastId, true);
                        }
                        else
                            break;
                    }
                    break;
                default:
                    throw new NotImplementedException(ErrorHelper.NotImplementedForThatSource);
            }
            await progressHub.EndProgressToast(toastId);
            return returnObj;
        }
        private async Task<ReturnModel> updatePlaylist(Sources source, string playlistIdSource, string playlistName, string description)
        {
            ReturnModel returnObj;
            switch (source)
            {
                case Sources.Youtube:
                    returnObj = await ytHelper.UpdatePlaylist(playlistIdSource, playlistName, description);
                    break;
                case Sources.Spotify:
                    returnObj = await spottyHelper.UpdatePlaylist(playlistIdSource, playlistName, description);
                    break;
                default:
                    throw new NotImplementedException(ErrorHelper.NotImplementedForThatSource);
            }
            return returnObj;
        }

        #region update PlaylistSongState
        private void updatePlaylistSongsState(Sources source, string playlistIdSource, List<string> songIdsSource, bool success)
        {
            var playlistId = UserDbContext.GetCachedList(UserDbContext.PlaylistInfo).Single(i => i.SourceId == source && i.PlaylistIdSource == playlistIdSource).PlaylistId;
            var songIds = UserDbContext.GetCachedList(UserDbContext.SongInfo).Where(s => s.SourceId == source && songIdsSource.Contains(s.SongIdSource)).Select(s => s.SongId);
            var playlistSongIds = UserDbContext.GetCachedList(UserDbContext.PlaylistSong).Where(ps => ps.PlaylistId == playlistId && songIds.Contains(ps.SongId)).Select(ps => ps.Id).ToList();
            var stateId = (success ? PlaylistSongStates.Added : PlaylistSongStates.NotAdded);
            updatePlaylistSongsState(source, playlistSongIds, stateId);
        }
        private void updatePlaylistSongsState(Sources source, List<int> playlistSongIds, PlaylistSongStates state)
        {
            var playlistSongStates = UserDbContext.PlaylistSongState.Where(pss => pss.SourceId == source);

            playlistSongIds.ForEach(psId =>
            {
                var playlistSongState = playlistSongStates.SingleOrDefault(pss => pss.PlaylistSongId == psId);
                if (playlistSongState == null)
                {
                    playlistSongState = new PlaylistSongState { PlaylistSongId = psId, SourceId = source, StateId = state, LastChecked = DateTime.Now };
                    UserDbContext.PlaylistSongState.Add(playlistSongState);
                }
                else
                {
                    playlistSongState.StateId = state;
                    playlistSongState.LastChecked = DateTime.Now;
                }
                UserDbContext.SaveChanges();
            });
        }
        private void updatePlaylistSongState(Sources source, string playlistIdSource, string songIdSource, bool success)
        {
            var playlistId = UserDbContext.GetCachedList(UserDbContext.PlaylistInfo).Single(i => i.SourceId == source && i.PlaylistIdSource == playlistIdSource).PlaylistId;
            var songId = UserDbContext.GetCachedList(UserDbContext.SongInfo).Single(s => s.SourceId == source && s.SongIdSource == songIdSource).SongId;
            var playlistSongId = UserDbContext.GetCachedList(UserDbContext.PlaylistSong).Single(ps => ps.PlaylistId == playlistId && ps.SongId == songId).Id;
            var stateId = (success ? PlaylistSongStates.Added : PlaylistSongStates.NotAdded);
            updatePlaylistSongState(source, playlistSongId, stateId);
        }
        private void updatePlaylistSongState(Sources source, int playlistSongId, PlaylistSongStates stateId)
        {
            var playlistSongState = UserDbContext.PlaylistSongState.SingleOrDefault(pss => pss.SourceId == source && pss.PlaylistSongId == playlistSongId);
            if (playlistSongState == null)
            {
                playlistSongState = new PlaylistSongState { PlaylistSongId = playlistSongId, SourceId = source, StateId = stateId, LastChecked = DateTime.Now };
                UserDbContext.PlaylistSongState.Add(playlistSongState);
            }
            else
            {
                playlistSongState.StateId = stateId;
                playlistSongState.LastChecked = DateTime.Now;
            }
            UserDbContext.SaveChanges();
        }
        #endregion

        #endregion

        #region Create Playlist
        public async Task<ActionResult> CreateCombinedPlaylist(string playlistName, string playlistIds, Sources source)
        {
            //add playlist references
            var playlistIdsList = playlistIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(i => int.Parse(i)).ToList();

            PlaylistInfo info;
            switch (source)
            {
                case Sources.Youtube:
                    //add playlist to YT
                    var playlistdescription = string.Format("songs by: {0}", string.Join(',', UserDbContext.GetCachedList(UserDbContext.Playlist).Where(p => playlistIdsList.Contains(p.Id)).Select(p => p.ChannelName)));
                    info = await ytHelper.CreatePlaylist(playlistName, playlistdescription);
                    break;
                default:
                    throw new NotImplementedException(ErrorHelper.NotImplementedForThatSource);
            }

            var playlistId = addPlaylistToDb(info.Name, info.CreatorName, PLaylistTypes.Simple, source, info.PlaylistIdSource, info.IsMine, info.Url, info.Description);

            UserDbContext.CombinedPlaylistEntry.AddRange(playlistIdsList.Select(i => new CombinedPlaylistEntry { CombinedPlaylistId = playlistId, PlaylistId = i }));
            UserDbContext.SaveChanges();

            //add playlistSongs
            syncCombinedPlaylistLocal(playlistId);


            return new JsonResult(new { success = true });
        }

        private void syncCombinedPlaylistLocal(int playlistId)
        {
            var playlistIds = UserDbContext.GetCachedList(UserDbContext.CombinedPlaylistEntry).Where(cp => cp.CombinedPlaylistId == playlistId).Select(cp => cp.PlaylistId);
            var availSongIds = UserDbContext.GetCachedList(UserDbContext.PlaylistSong).Where(ps => playlistIds.Contains(ps.PlaylistId)).Select(ps => ps.SongId).Distinct();
            var curSongIds = UserDbContext.GetCachedList(UserDbContext.PlaylistSong).Where(ps => ps.PlaylistId == playlistId).Select(ps => ps.SongId).Distinct();
            var newPlaylistSongs = availSongIds.Where(s => !curSongIds.Contains(s)).Select(i => new PlaylistSong { PlaylistId = playlistId, SongId = i }).ToList();
            UserDbContext.PlaylistSong.AddRange(newPlaylistSongs);
            UserDbContext.SaveChanges();
            foreach (Sources src in Enum.GetValues(typeof(Sources)))
                UserDbContext.PlaylistSongState.AddRange(newPlaylistSongs.Select(ps => new PlaylistSongState { PlaylistSongId = ps.Id, SourceId = src, StateId = PlaylistSongStates.NotAdded, LastChecked = DateTime.Now }));

            UserDbContext.SaveChanges();
        }

        #endregion

        #region Delete

        public async Task<ActionResult> DeletePLaylistAtSource(int id, Sources source)
        {
            try
            {
                var playlist = UserDbContext.GetCachedList(UserDbContext.Playlist).Single(p => p.Id == id);

                //delete at source
                //  check if it exists
                var info = UserDbContext.PlaylistInfo.SingleOrDefault(i => i.PlaylistId == id && i.SourceId == source);
                if (info == null)
                    return new JsonResult(new { success = false, message = "you don't have a playlist at this source" });

                //  check if its own
                if (!info.IsMine)
                    return new JsonResult(new { success = false, message = "you don't own the playlist" });

                ReturnModel returnObj;
                switch (source)
                {
                    case Sources.Youtube:
                        returnObj = await ytHelper.DeletePlaylist(info.PlaylistIdSource);
                        break;
                    case Sources.Spotify:
                        returnObj = await spottyHelper.DeletePlaylist(info.PlaylistIdSource);
                        break;
                    default:
                        throw new NotImplementedException(ErrorHelper.NotImplementedForThatSource);
                }

                if (!returnObj.Success)
                    return new JsonResult(new { success = false, message = $"couldn't delete playlist on {source.ToString()}" });

                //remove info
                UserDbContext.PlaylistInfo.Remove(info);
                UserDbContext.SaveChanges();

                //remove PlaylistSongStates
                var playlistSongIds = UserDbContext.GetCachedList(UserDbContext.PlaylistSong).Where(ps => ps.PlaylistId == id).Select(ps => ps.Id).ToList();
                var playlistSongStates = UserDbContext.PlaylistSongState.Where(pss => pss.SourceId == source && playlistSongIds.Contains(pss.PlaylistSongId));
                UserDbContext.PlaylistSongState.RemoveRange(playlistSongStates);
                UserDbContext.SaveChanges();

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
                var playlist = UserDbContext.Playlist.Single(p => p.Id == id);

                //check if all sources where deleted
                if (UserDbContext.GetCachedList(UserDbContext.PlaylistInfo).Where(i => i.PlaylistId == id && i.IsMine).Any())
                    return new JsonResult(new { success = false, message = "You still have Playlists at sources" });

                //remove foreign playlistinfos
                UserDbContext.PlaylistInfo.RemoveRange(UserDbContext.PlaylistInfo.Where(i => i.PlaylistId == id));

                //delete from db
                //  remove playlist songs 
                var playlistSongs = UserDbContext.PlaylistSong.Where(ps => ps.PlaylistId == id);
                //      remove their states
                UserDbContext.PlaylistSongState.RemoveRange(UserDbContext.PlaylistSongState.Where(pss => playlistSongs.Select(ps => ps.Id).Contains(pss.PlaylistSongId))); ;
                UserDbContext.SaveChanges();
                UserDbContext.PlaylistSong.RemoveRange(playlistSongs);
                UserDbContext.SaveChanges();

                //  remove playlist
                UserDbContext.Playlist.Remove(playlist);
                UserDbContext.SaveChanges();

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
                if (!UserDbContext.GetCachedList(UserDbContext.Song).Any(s => s.ThumbnailId == thumbnailId))
                {
                    UserDbContext.Thumbnail.Remove(UserDbContext.Thumbnail.Single(t => t.Id == thumbnailId));
                    UserDbContext.SaveChanges();
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


        public ActionResult RemoveDuplicatesFromPlaylist(Sources source, int playlistId)
        {
            try
            {
                var info = UserDbContext.GetCachedList(UserDbContext.PlaylistInfo).Single(i => i.SourceId == source && i.PlaylistId == playlistId);
                List<SongInfo> songInfos;
                switch (source)
                {
                    case Sources.Youtube:
                        var success = ytHelper.RemoveDuplicatesFromPlaylist(info.PlaylistIdSource);
                        break;
                    case Sources.Spotify:
                        throw new NotImplementedException(ErrorHelper.NotImplementedForThatSource);
                    default:
                        throw new NotImplementedException(ErrorHelper.NotImplementedForThatSource);
                }

                return JsonResponse();
            }
            catch (Exception ex)
            {
                return JsonResponse(ex);
            }
        }
        #endregion

        #region Thumbnail
        public async Task<ActionResult> SyncSongsThumbnail(int id, Sources source, bool onlyWithNoThumbnails = true)
        {
            try
            {
                var songInfos = UserDbContext.GetCachedList(UserDbContext.SongInfo).Where(i => i.SourceId == source);
                var songIds = UserDbContext.GetCachedList(UserDbContext.PlaylistSong).Where(ps => ps.PlaylistId == id).Select(ps => ps.SongId);
                var songs = UserDbContext.Song.Where(p => songIds.Contains(p.Id));
                if (onlyWithNoThumbnails)
                    songs = songs.Where(s => s.ThumbnailId == null);
                var sourceIds = songInfos.Where(i => songs.Any(s => s.Id == i.SongId)).Select(i => i.SongIdSource);

                Dictionary<string, SourceThumbnail> thumbnails;
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

                    var thumbnailId = getThumbnailId(thumbnailSource.Value, song.ThumbnailId);
                    song.ThumbnailId = thumbnailId;
                }
                UserDbContext.SaveChanges();
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<ActionResult> SyncPlaylistThumbnailsFrom(Sources? source = null)
        {
            try
            {
                var playlists = UserDbContext.Playlist.ToList();
                var toastId = GetToastId();
                await progressHub.InitProgressToast("Update playlist thumbnails", toastId, true);
                var timeElapsedList = new List<int>();
                var nCompleted = 0;
                var nSkipped = 0;
                foreach (var playlist in playlists)
                {
                    if (IsCancelled(toastId, out var startTime)) break;

                    SourceThumbnail sourceThumbnail;
                    var infos = UserDbContext.GetCachedList(UserDbContext.PlaylistInfo).Where(i => i.PlaylistId == playlist.Id);

                    var info = infos.SingleOrDefault(i => i.SourceId == (source ?? playlist.MainSourceId));
                    if (info == null)
                        continue;

                    switch (info.SourceId)
                    {
                        case Sources.Youtube:
                            sourceThumbnail = await ytHelper.GetPlaylistThumbnailReadOnly(info.PlaylistIdSource);
                            break;
                        case Sources.Spotify:
                            sourceThumbnail = await spottyHelper.GetPlaylistThumbnail(info.PlaylistIdSource);
                            break;
                        default:
                            throw new NotImplementedException(ErrorHelper.NotImplementedForThatSource);
                    }

                    var thumbnailId = getThumbnailId(sourceThumbnail, playlist.ThumbnailId);
                    playlist.ThumbnailId = thumbnailId;

                    var msgDisplay = ToastMessageDisplay(true, playlists.Count, startTime, ref timeElapsedList, ref nCompleted, ref nSkipped);
                    await progressHub.UpdateProgressToast("Updating playlist thumbnails...", nCompleted, playlists.Count, msgDisplay, toastId, true);
                }
                UserDbContext.SaveChanges();

                await progressHub.EndProgressToast(toastId);
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        private int getThumbnailId(SourceThumbnail sourceThumbnail, int? thumbnailId)
        {
            //if new thumbnail already in db
            var thumbnail = UserDbContext.GetCachedList(UserDbContext.Thumbnail).SingleOrDefault(t => t.Url == sourceThumbnail.Url);
            if (thumbnail != null)
                return thumbnail.Id;

            //overwrite existing thumbnail
            if (thumbnailId.HasValue)
            {
                thumbnail = UserDbContext.Thumbnail.Single(t => t.Id == thumbnailId);
                thumbnail.FileContents = sourceThumbnail.FileContents;
                UserDbContext.SaveChanges();
                return thumbnail.Id;
            }

            //add new Thumbnail to db
            thumbnail = new Thumbnail { Url = sourceThumbnail.Url, FileContents = sourceThumbnail.FileContents };
            UserDbContext.Thumbnail.Add(thumbnail);
            thumbnail.FileContents = sourceThumbnail.FileContents;
            UserDbContext.SaveChanges();
            return thumbnail.Id;
        }

        #endregion

        #region  Helper

        public async Task<ActionResult> GetThumbnail(int? thumbnailId)
        {
            if (!thumbnailId.HasValue)
                return null;

            var thumbnail = UserDbContext.GetCachedList(UserDbContext.Thumbnail).SingleOrDefault(t => t.Id == thumbnailId);
            if (thumbnail == null)
                return null;

            return File(thumbnail.FileContents, "image/jpeg");
        }

        #endregion

        #region SyncPlaylistSongStatesFrom
        public ActionResult SyncPlaylistSongStatesFrom(Sources source, int playlistId)
        {
            try
            {
                var info = UserDbContext.GetCachedList(UserDbContext.PlaylistInfo).Single(i => i.SourceId == source && i.PlaylistId == playlistId);

                List<SongInfo> songInfos;
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

                removeAllPlaylistSongStates(source, playlistId);
                addPlaylistSongStates(source, playlistId, songInfos.Select(i => i.SongIdSource).ToList());

                return JsonResponse();
            }
            catch (Exception ex)
            {
                return JsonResponse(ex);
            }
        }

        private void removeAllPlaylistSongStates(Sources source, int playlistId)
        {
            var playlistSongIds = UserDbContext.GetCachedList(UserDbContext.PlaylistSong).Where(ps => ps.PlaylistId == playlistId).Select(ps => ps.Id).ToList();
            var playlistSongStates = UserDbContext.PlaylistSongState.Where(pss => pss.SourceId == source && playlistSongIds.Contains(pss.PlaylistSongId));
            UserDbContext.PlaylistSongState.RemoveRange(playlistSongStates);
            UserDbContext.SaveChanges();
        }

        private void addPlaylistSongStates(Sources source, int playlistId, List<string> songIdsAtSource, PlaylistSongStates playlistSongState = PlaylistSongStates.Added)
        {
            var songids = UserDbContext.GetCachedList(UserDbContext.SongInfo).Where(i => i.SourceId == source && songIdsAtSource.Contains(i.SongIdSource)).Select(i => i.SongId).ToList();
            var playlistSongIds = UserDbContext.GetCachedList(UserDbContext.PlaylistSong).Where(ps => ps.PlaylistId == playlistId && songids.Contains(ps.SongId)).Select(ps => ps.Id).ToList();

            addPlaylistSongStates(source, playlistSongIds, playlistSongState);
        }
        private void addPlaylistSongStates(Sources source, List<int> playlistSongIds, PlaylistSongStates playlistSongState)
        {
            var newPlaylistSongStates = playlistSongIds.Select(id => new PlaylistSongState { PlaylistSongId = id, SourceId = source, StateId = playlistSongState, LastChecked = DateTime.Now });
            UserDbContext.PlaylistSongState.AddRange(newPlaylistSongStates);
            UserDbContext.SaveChanges();
        }
        #endregion
    }
}