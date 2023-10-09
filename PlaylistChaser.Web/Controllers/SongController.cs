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

namespace PlaylistChaser.Web.Controllers
{
    public class SongController : BaseController
    {
        public SongController(IConfiguration configuration, PlaylistChaserDbContext db, IHubContext<ProgressHub> hubContext, IMemoryCache memoryCache)
            : base(configuration, db, hubContext, memoryCache) { }

        #region Properties
        private YoutubeApiHelper _ytHelper;
        private YoutubeApiHelper ytHelper
        {
            get
            {
                if (_ytHelper == null)
                {
                    if (!int.TryParse(HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value, out var userId))
                        return null;

                    var oAuth = db.GetCachedList(db.OAuth2Credential).Single(c => c.UserId == userId && c.Provider == Sources.Youtube.ToString());
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
                    if (!int.TryParse(HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value, out var userId))
                        return null;

                    var oAuth = db.GetCachedList(db.OAuth2Credential).Single(c => c.UserId == userId && c.Provider == Sources.Spotify.ToString());
                    _spottyHelper = new SpotifyApiHelper(oAuth.AccessToken);
                }
                return _spottyHelper;
            }
        }
        #endregion

        #region Views

        #region View
        public async Task<ActionResult> Index()
        {
            var model = new SongIndexModel
            {
                AddSongStates = true,
            };
            return View(model);
        }
        #endregion

        #region Partials

        #region Edit

        [HttpGet]
        public ActionResult _EditPartial(int id)
        {
            var song = db.GetCachedList(db.Song).Single(s => s.Id == id);
            return PartialView(song);
        }
        [HttpPost]
        public ActionResult _EditPartial(int id, Song uiSong)
        {
            var song = db.Song.Single(s => s.Id == id);

            song.SongName = uiSong.SongName;
            song.ArtistName = uiSong.ArtistName;

            db.SaveChanges();
            return JsonResponse();
        }

        [HttpGet]
        public ActionResult _SongInfoEditPartial(Sources source, int songId)
        {
            var songInfo = db.GetCachedList(db.SongInfo).Single(s => s.SourceId == source && s.SongId == songId);
            return PartialView(songInfo);
        }
        [HttpPost]
        public ActionResult _SongInfoEditPartial(Sources sourceId, int songId, SongInfo uiSongInfo)
        {
            var songInfo = db.SongInfo.Single(s => s.SourceId == sourceId && s.SongId == songId);
            songInfo.Name = uiSongInfo.Name;
            songInfo.ArtistName = uiSongInfo.ArtistName;
            songInfo.SongIdSource = uiSongInfo.SongIdSource;
            songInfo.Url = uiSongInfo.Url;

            db.SaveChanges();
            return JsonResponse();
        }

        [HttpGet]
        public ActionResult _PlaylistSongStateEditPartial(Sources source, int playlistSongId)
        {
            var playlistSongState = db.GetCachedList(db.PlaylistSongState).SingleOrDefault(pss => pss.SourceId == source && pss.PlaylistSongId == playlistSongId);
            return PartialView(playlistSongState);
        }
        [HttpPost]
        public ActionResult _PlaylistSongStateEditPartial(Sources sourceId, int playlistSongId, PlaylistSongState uiPlaylistSongState)
        {
            var playlistSongState = db.PlaylistSongState.Single(s => s.SourceId == sourceId && s.PlaylistSongId == playlistSongId);
            playlistSongState.StateId = uiPlaylistSongState.StateId;

            db.SaveChanges();
            return JsonResponse();
        }
        #endregion

        #region Grid
        public ActionResult _PlaylistSongsGridPartial(int playlistId, bool addSongStates, int? limit = null, int? pageSize = null)
        {
            ViewBag.AddSongStates = addSongStates;
            ViewBag.PageSize = pageSize;

            var searchModel = new PlaylistSongSearchModel
            {
                PlaylistId = playlistId,
                Limit = limit,
                PageSize = pageSize
            };
            return PartialView(searchModel);
        }
        public async Task<ActionResult> _PlaylistSongsGridDataPartial(PlaylistSongSearchModel searchModel, bool addSongStates, int skip)
        {
            var pageSize = searchModel.PageSize;

            var filteredSources = new List<Sources>();
            if (searchModel.Source.HasValue)
                filteredSources.Add(searchModel.Source.Value);

            var songs = await db.GetPlaylistSongs(searchModel.PlaylistId, searchModel.Limit);

            //filter
            var filterSongStates = searchModel.Source != null || searchModel.SongState != null || searchModel.PlaylistSongState != null;

            var filteredSongs = songs.OrderByDescending(s => s.SongId).AsEnumerable();
            if (searchModel.SongName != null)
                filteredSongs = songs.Where(s => s.SongName.StartsWith(searchModel.SongName) || s.SongName.EndsWith(searchModel.SongName));
            if (searchModel.ArtistName != null)
                filteredSongs = filteredSongs.Where(s => s.ArtistName != null && (s.ArtistName.StartsWith(searchModel.ArtistName) || s.ArtistName.EndsWith(searchModel.ArtistName)));

            if (!filterSongStates)
            {
                if (!pageSize.HasValue)
                    ViewBag.NumPages = 1;
                else
                {
                    ViewBag.NumPages = Math.Ceiling(filteredSongs.Count() / (double)pageSize.Value);
                    filteredSongs = filteredSongs.Skip(skip).Take(pageSize.Value).ToList();
                }
            }

            if (addSongStates)
            {
                var playlistSongs = db.GetCachedList(db.PlaylistSong).Where(ps => ps.PlaylistId == searchModel.PlaylistId);
                var songIds = playlistSongs.Select(s => s.SongId).ToList();
                var playlistSongIds = playlistSongs.Select(s => s.Id).ToList();
                var songInfos = db.GetCachedList(db.SongInfo).Where(ss => songIds.Contains(ss.SongId));
                var playlistSongStates = db.GetCachedList(db.PlaylistSongState).Where(pss => playlistSongIds.Contains(pss.PlaylistSongId));

                if (filterSongStates)
                {
                    if (searchModel.Source != null)
                    {
                        songInfos = songInfos.Where(ss => ss.SourceId == searchModel.Source);
                        playlistSongStates = playlistSongStates.Where(pss => pss.SourceId == searchModel.Source);
                    }
                    //filter out 
                    if (searchModel.SongState != null)
                        songInfos = songInfos.Where(si => db.GetCachedList(db.SongState).Single(ss => ss.SourceId == si.SourceId && ss.SongId == si.SongId).StateId == searchModel.SongState);
                    if (searchModel.PlaylistSongState != null)
                        playlistSongStates = playlistSongStates.Where(pss => pss.StateId == searchModel.PlaylistSongState);


                    //filter songs
                    filteredSongs = filteredSongs.Where(s => songInfos.Select(ss => ss.SongId).Contains(s.SongId));

                    if (!pageSize.HasValue)
                        ViewBag.NumPages = 1;
                    else
                    {
                        ViewBag.NumPages = Math.Ceiling(filteredSongs.Count() / (double)pageSize.Value);
                        filteredSongs = filteredSongs.Skip(skip).Take(pageSize.Value).ToList();
                    }
                }

                foreach (var song in filteredSongs)
                {
                    song.SongStates = db.GetCachedList(db.SongState).Where(ss => ss.SongId == song.SongId).ToList();
                    song.PlaylistSongStates = playlistSongStates.Where(ss => ss.PlaylistSongId == song.PlaylistSongId).ToList();
                }

            }

            ViewBag.AddSongStates = addSongStates;
            ViewBag.FilteredSources = db.GetSources(filteredSources);

            return PartialView(filteredSongs.ToList());
        }

        public ActionResult _SongsGridPartial(bool addSongStates, int limit, int pageSize = 50)
        {
            ViewBag.AddSongStates = addSongStates;
            ViewBag.PageSize = pageSize;

            var searchModel = new SongSearchModel
            {
                Limit = limit,
                PageSize = pageSize
            };
            return PartialView(searchModel);
        }
        public async Task<ActionResult> _SongsGridDataPartial(SongSearchModel searchModel, bool addSongStates, int skip, int limit)
        {
            var songs = await db.GetSongs();
            //filter
            var filterSongStates = searchModel.Source != null || searchModel.SongState != null;

            var filteredSongs = songs.OrderByDescending(s => s.Id).AsEnumerable();
            if (searchModel.SongName != null)
                filteredSongs = songs.Where(s => s.SongName.StartsWith(searchModel.SongName) || s.SongName.EndsWith(searchModel.SongName));
            if (searchModel.ArtistName != null)
                filteredSongs = filteredSongs.Where(s => s.ArtistName != null && (s.ArtistName.StartsWith(searchModel.ArtistName) || s.ArtistName.EndsWith(searchModel.ArtistName)));

            if (!filterSongStates)
            {
                ViewBag.NumPages = Math.Ceiling(filteredSongs.Count() / (double)limit);
                filteredSongs = filteredSongs.Skip(skip).Take(limit).ToList();
            }

            if (addSongStates)
            {
                var songInfos = db.GetCachedList(db.SongInfo);
                var songStates = db.GetCachedList(db.SongState);
                if (filterSongStates)
                {
                    if (searchModel.Source != null)
                    {
                        songInfos = songInfos.Where(ss => ss.SourceId == searchModel.Source).ToList();
                        songStates = songStates.Where(ss => ss.SourceId == searchModel.Source).ToList();
                    }
                    if (searchModel.SongState != null)
                    {
                        songInfos = songInfos.Where(si => db.GetCachedList(db.SongState).Single(ss => ss.SourceId == si.SourceId && ss.SongId == si.SongId).StateId == searchModel.SongState).ToList();
                        songStates = songStates.Where(ss => ss.StateId == searchModel.SongState).ToList();
                    }


                    //filter songs
                    filteredSongs = filteredSongs.Where(s => songInfos.Select(ss => ss.SongId).Contains(s.Id));
                    filteredSongs = filteredSongs.Where(s => songStates.Select(ss => ss.SongId).Contains(s.Id));

                    ViewBag.NumPages = Math.Ceiling(filteredSongs.Count() / (double)limit);
                    filteredSongs = filteredSongs.Skip(skip).Take(limit).ToList();
                }

                foreach (var song in filteredSongs)
                {
                    song.SongStates = songStates.Where(ss => ss.SongId == song.Id).ToList();
                }
            }

            ViewBag.AddSongStates = addSongStates;

            return PartialView(filteredSongs.ToList());
        }

        #endregion

        public ActionResult _SongStatesSummaryPartial(int playlistId)
        {
            var songIds = db.GetCachedList(db.PlaylistSong).Where(ps => ps.PlaylistId == playlistId).Select(ps => ps.SongId);
            var songStates = db.GetCachedList(db.SongState).Where(ss => songIds.Contains(ss.SongId));
            var model = songStates.GroupBy(pss => pss.SourceId).AsEnumerable().OrderBy(pss => pss.Key.ToString()).ToList();
            return PartialView(model);
        }

        public ActionResult _SongInfosEditPartial(int songId)
        {
            var songInfos = db.GetCachedList(db.SongInfo).Where(s => s.SongId == songId).ToList();
            return PartialView(songInfos);
        }

        public ActionResult _PlaylistSongEditpartial(int playlistSongId)
        {
            var playlistSong = db.GetCachedList(db.PlaylistSong).Single(ps => ps.Id == playlistSongId);
            return PartialView(playlistSong);
        }

        #endregion

        #endregion

        #region Find songs
        [HttpPost]
        public ActionResult FindSongs(Sources source, string? songIds = null, int? playlistId = null)
        {
            try
            {
                List<Song> songs;

                if (songIds != null)
                {
                    var songIdsList = songIds.Split(',').Select(i => int.Parse(i));
                    songs = db.GetCachedList(db.Song).Where(s => songIdsList.Contains(s.Id)).ToList();

                    //get all songs that weren't checked before
                    songs = songs.Where(s => db.SongState.SingleOrDefault(ss => ss.SourceId == source && ss.SongId == s.Id) == null
                                             || db.SongState.SingleOrDefault(ss => ss.SourceId == source && ss.SongId == s.Id).StateId == SongStates.NotChecked).ToList();
                }
                else if (playlistId != null)
                {
                    var songIdsList = db.GetCachedList(db.PlaylistSong).Where(ps => ps.PlaylistId == playlistId).Select(ps => ps.SongId);
                    songs = db.GetCachedList(db.Song).Where(s => songIdsList.Contains(s.Id)).ToList();
                }
                else
                    return new JsonResult(new { success = false, message = "no songIds or playlistId passed" });

                var res = FindSongs(songs.ToList(), source);

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<List<FoundSong>> FindSongs(List<Song> missingSongs, Sources source)
        {
            var toastId = GetToastId();
            await progressHub.InitProgressToast("Find Songs", toastId, true);

            //only songs that weren't checked before
            var missingSongsList = missingSongs.Where(s => db.SongState.SingleOrDefault(ss => ss.SongId == s.Id && ss.SourceId == source) == null
                                                           || db.SongState.Single(ss => ss.SongId == s.Id && ss.SourceId == source).StateId == SongStates.NotChecked);

            if (!missingSongsList.Any())
            {
                await progressHub.EndProgressToast(toastId);
                return null;
            }

            //check if songs exists
            var findSongs = missingSongsList.Select(s => new FindSong(s.Id, s.ArtistName, s.SongName)).ToList();

            int nFound = 0;
            int nSkipped = 0;
            var foundSongs = new List<FoundSong>();
            FoundSong foundSong;
            var timeElapsedList = new List<int>();
            foreach (var findSong in findSongs)
            {
                if (IsCancelled(toastId, out var startTime)) break;

                switch (source)
                {
                    case Sources.Youtube:
                        foundSong = ytHelper.FindSong(findSong);
                        break;
                    case Sources.Spotify:
                        foundSong = spottyHelper.FindSong(findSong);
                        break;
                    default:
                        throw new NotImplementedException(ErrorHelper.NotImplementedForThatSource);
                }

                var newSongInfo = foundSong.NewSongInfo;
                var stateId = SongStates.Available;
                if (newSongInfo.ArtistName == "NotAvailable")
                    stateId = SongStates.NotAvailable;
                var returnObj = addFoundSongToDb(newSongInfo.SongId, newSongInfo.Name, newSongInfo.ArtistName, newSongInfo.SourceId, newSongInfo.SongIdSource, newSongInfo.Url, stateId);

                var msgDisplay = ToastMessageDisplay(returnObj.Success, findSongs.Count, startTime, ref timeElapsedList, ref nFound, ref nSkipped);

                await progressHub.UpdateProgressToast("Finding songs...", nFound, findSongs.Count, msgDisplay, toastId, true);

                foundSongs.Add(foundSong);
            }
            await progressHub.EndProgressToast(toastId);

            return foundSongs;
        }
        #endregion

        #region Add song to db
        private ReturnModel addFoundSongToDb(int songId, string songName, string artistName, Sources source, string songIdSource, string url, SongStates stateId = SongStates.Available)
        {
            try
            {
                if (db.GetCachedList(db.SongInfo).Any(i => i.SongId == songId && i.SourceId == source))
                    return new ReturnModel("A songInfo already exists for that source");

                if (db.GetCachedList(db.SongInfo).Any(i => i.SongIdSource == songIdSource && i.SourceId == source))
                    return new ReturnModel("There's already a songinfo with that SongIdSource");

                //add song info
                var newSongInfo = new SongInfo { SongId = songId, SourceId = source, SongIdSource = songIdSource, Name = songName, ArtistName = artistName, Url = url };
                db.SongInfo.Add(newSongInfo);

                //add song state
                var newSongState = new SongState { SongId = songId, SourceId = source, StateId = stateId, LastChecked = DateTime.Now };
                db.SongState.AddRange(newSongState);

                db.SaveChanges();
                return new ReturnModel();
            }
            catch (Exception ex)
            {
                return new ReturnModel(ex.Message);
            }
        }

        internal List<Song> AddSongsToDb(List<SongInfo> songsToAdd)
        {
            {
                var addedSongs = new List<Song>();

                //check if songs are already in db
                //TODO: for now only check if it wasnt added from same source
                //      on youtube songname & artist name dont have to be a unique combination
                //      fuck you anguish with your stupid ass song titles


                //remove duplicates
                songsToAdd = songsToAdd.DistinctBy(i => i.SongIdSource).ToList();

                //add song
                foreach (var newSong in songsToAdd)
                {
                    //skip already added
                    if (db.GetCachedList(db.SongInfo).Any(s => s.SourceId == newSong.SourceId && s.SongIdSource == newSong.SongIdSource))
                        continue;

                    var success = addSongs(newSong.Name, newSong.ArtistName, newSong.SourceId, newSong.SongIdSource, newSong.Url);
                };

                return addedSongs;
            }
        }
        private ReturnModel addSongs(string songName, string artistName, Sources source, string songIdSource, string url)
        {
            try
            {
                //add song
                var newSong = new Song { SongName = songName, ArtistName = artistName };
                db.Song.Add(newSong);
                db.SaveChanges();

                //add song info
                var newSongInfo = new SongInfo { SongId = newSong.Id, SourceId = source, SongIdSource = songIdSource, Name = songName, ArtistName = artistName, Url = url };
                db.SongInfo.Add(newSongInfo);

                //add song state
                var newSongState = new SongState { SongId = newSong.Id, SourceId = source, StateId = SongStates.Available, LastChecked = DateTime.Now };
                db.SongState.AddRange(newSongState);

                db.SaveChanges();

                return new ReturnModel();
            }
            catch (Exception ex)
            {
                return new ReturnModel(ex.Message);
            }
        }
        #endregion       
    }
}