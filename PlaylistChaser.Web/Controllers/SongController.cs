﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PlaylistChaser.Web.Database;
using PlaylistChaser.Web.Models;
using PlaylistChaser.Web.Models.SearchModel;
using PlaylistChaser.Web.Util;
using PlaylistChaser.Web.Util.API;
using System.Data;
using System.Data.Entity;
using static PlaylistChaser.Web.Util.BuiltInIds;
using static PlaylistChaser.Web.Util.Helper;

namespace PlaylistChaser.Web.Controllers
{
    public class SongController : BaseController
    {
        public SongController(IConfiguration configuration, IHubContext<ProgressHub> hubContext, IMemoryCache memoryCache, AdminDBContext dbAdmin)
            : base(configuration, hubContext, memoryCache, dbAdmin) { }

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
        #endregion

        #region Views

        #region View
        [AuthorizeRole(Roles.Administrator)]
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
        [AuthorizeRole(Roles.Administrator)]
        public ActionResult _EditPartial(int id)
        {
            var song = UserDbContext.GetCachedList(UserDbContext.Song).Single(s => s.Id == id);
            return PartialView(song);
        }
        [HttpPost]
        [AuthorizeRole(Roles.Administrator)]
        public ActionResult _EditPartial(int id, Song uiSong)
        {
            var song = UserDbContext.Song.Single(s => s.Id == id);

            song.SongName = uiSong.SongName;
            song.ArtistName = uiSong.ArtistName;

            UserDbContext.SaveChanges();
            return JsonResponse();
        }

        [HttpGet]
        [AuthorizeRole(Roles.Administrator)]
        public ActionResult _SongInfoEditPartial(Sources source, int songId)
        {
            var songInfo = UserDbContext.GetCachedList(UserDbContext.SongInfo).SingleOrDefault(s => s.SourceId == source && s.SongId == songId);
            return PartialView(songInfo);
        }
        [HttpPost]
        [AuthorizeRole(Roles.Administrator)]
        public ActionResult _SongInfoEditPartial(Sources sourceId, int songId, SongInfo uiSongInfo)
        {
            var songInfo = UserDbContext.SongInfo.Single(s => s.SourceId == sourceId && s.SongId == songId);
            songInfo.Name = uiSongInfo.Name;
            songInfo.ArtistName = uiSongInfo.ArtistName;
            songInfo.SongIdSource = uiSongInfo.SongIdSource;
            songInfo.Url = uiSongInfo.Url;

            UserDbContext.SaveChanges();
            return JsonResponse();
        }

        [HttpGet]
        public ActionResult _PlaylistSongStateEditPartial(Sources source, int playlistSongId)
        {
            var playlistSongState = UserDbContext.GetCachedList(UserDbContext.PlaylistSongState).SingleOrDefault(pss => pss.SourceId == source && pss.PlaylistSongId == playlistSongId);
            return PartialView(playlistSongState);
        }
        [HttpPost]
        public ActionResult _PlaylistSongStateEditPartial(Sources sourceId, int playlistSongId, PlaylistSongState uiPlaylistSongState)
        {
            var playlistSongState = UserDbContext.PlaylistSongState.Single(s => s.SourceId == sourceId && s.PlaylistSongId == playlistSongId);
            playlistSongState.StateId = uiPlaylistSongState.StateId;

            UserDbContext.SaveChanges();
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

            var songs = await UserDbContext.GetPlaylistSongs(searchModel.PlaylistId, searchModel.Limit);

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
                var playlistSongs = UserDbContext.GetCachedList(UserDbContext.PlaylistSong).Where(ps => ps.PlaylistId == searchModel.PlaylistId);
                var songIds = playlistSongs.Select(s => s.SongId).ToList();
                var playlistSongIds = playlistSongs.Select(s => s.Id).ToList();
                var songInfos = UserDbContext.GetCachedList(UserDbContext.SongInfo).Where(ss => songIds.Contains(ss.SongId));
                var playlistSongStates = UserDbContext.GetCachedList(UserDbContext.PlaylistSongState).Where(pss => playlistSongIds.Contains(pss.PlaylistSongId));

                if (filterSongStates)
                {
                    if (searchModel.Source != null)
                    {
                        songInfos = songInfos.Where(ss => ss.SourceId == searchModel.Source);
                        playlistSongStates = playlistSongStates.Where(pss => pss.SourceId == searchModel.Source);
                    }
                    //filter out 
                    if (searchModel.SongState != null)
                        songInfos = songInfos.Where(si => UserDbContext.GetCachedList(UserDbContext.SongState).Single(ss => ss.SourceId == si.SourceId && ss.SongId == si.SongId).StateId == searchModel.SongState);
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
                    song.SongStates = UserDbContext.GetCachedList(UserDbContext.SongState).Where(ss => ss.SongId == song.SongId).ToList();
                    song.PlaylistSongStates = playlistSongStates.Where(ss => ss.PlaylistSongId == song.PlaylistSongId).ToList();
                }

            }

            ViewBag.AddSongStates = addSongStates;
            ViewBag.FilteredSources = UserDbContext.GetSources(filteredSources);

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
            var songs = await UserDbContext.GetSongs();
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
                var songInfos = UserDbContext.GetCachedList(UserDbContext.SongInfo);
                var songStates = UserDbContext.GetCachedList(UserDbContext.SongState);
                if (filterSongStates)
                {
                    if (searchModel.Source != null)
                    {
                        songInfos = songInfos.Where(ss => ss.SourceId == searchModel.Source).ToList();
                        songStates = songStates.Where(ss => ss.SourceId == searchModel.Source).ToList();
                    }
                    if (searchModel.SongState != null)
                    {
                        songInfos = songInfos.Where(si => UserDbContext.GetCachedList(UserDbContext.SongState).Single(ss => ss.SourceId == si.SourceId && ss.SongId == si.SongId).StateId == searchModel.SongState).ToList();
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
            var songIds = UserDbContext.GetCachedList(UserDbContext.PlaylistSong).Where(ps => ps.PlaylistId == playlistId).Select(ps => ps.SongId);
            var songStates = UserDbContext.GetCachedList(UserDbContext.SongState).Where(ss => songIds.Contains(ss.SongId));
            var model = songStates.GroupBy(pss => pss.SourceId).AsEnumerable().OrderBy(pss => pss.Key.ToString()).ToList();
            return PartialView(model);
        }

        public ActionResult _SongInfosEditPartial(int songId)
        {
            var songInfos = UserDbContext.GetCachedList(UserDbContext.SongInfo).Where(s => s.SongId == songId).ToList();
            return PartialView(songInfos);
        }

        public ActionResult _PlaylistSongEditpartial(int playlistSongId)
        {
            var playlistSong = UserDbContext.GetCachedList(UserDbContext.PlaylistSong).Single(ps => ps.Id == playlistSongId);
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
                    songs = UserDbContext.GetCachedList(UserDbContext.Song).Where(s => songIdsList.Contains(s.Id)).ToList();

                    //get all songs that weren't checked before
                    songs = songs.Where(s => UserDbContext.SongState.SingleOrDefault(ss => ss.SourceId == source && ss.SongId == s.Id) == null
                                             || UserDbContext.SongState.SingleOrDefault(ss => ss.SourceId == source && ss.SongId == s.Id).StateId == SongStates.NotChecked).ToList();
                }
                else if (playlistId != null)
                {
                    var songIdsList = UserDbContext.GetCachedList(UserDbContext.PlaylistSong).Where(ps => ps.PlaylistId == playlistId).Select(ps => ps.SongId);
                    songs = UserDbContext.GetCachedList(UserDbContext.Song).Where(s => songIdsList.Contains(s.Id)).ToList();
                }
                else
                    return new JsonResult(new { success = false, message = "no songIds or playlistId passed" });

                var res = findSongs(songs.ToList(), source);

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        private async Task<List<FoundSong>> findSongs(List<Song> missingSongs, Sources source)
        {
            var toastId = GetToastId();
            await progressHub.InitProgressToast("Find Songs", toastId, true);

            //only songs that weren't checked before
            var missingSongsList = missingSongs.Where(s => UserDbContext.SongState.SingleOrDefault(ss => ss.SongId == s.Id && ss.SourceId == source) == null
                                                           || UserDbContext.SongState.Single(ss => ss.SongId == s.Id && ss.SourceId == source).StateId == SongStates.NotChecked);

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

                var dbHelper = new DbHelper(UserDbContext);
                var returnObj = dbHelper.AddFoundSongToDb(newSongInfo.SongId, newSongInfo.Name, newSongInfo.ArtistName, newSongInfo.SourceId, newSongInfo.SongIdSource, newSongInfo.Url, stateId);

                var msgDisplay = ToastMessageDisplay(returnObj.Success, findSongs.Count, startTime, ref timeElapsedList, ref nFound, ref nSkipped);

                await progressHub.UpdateProgressToast("Finding songs...", nFound, findSongs.Count, msgDisplay, toastId, true);

                foundSongs.Add(foundSong);
            }
            await progressHub.EndProgressToast(toastId);

            return foundSongs;
        }
        #endregion
    }
}