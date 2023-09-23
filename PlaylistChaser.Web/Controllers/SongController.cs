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

namespace PlaylistChaser.Web.Controllers
{
    public class SongController : BaseController
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
        #endregion

        public SongController(IConfiguration configuration, PlaylistChaserDbContext db, IHubContext<ProgressHub> hubContext)
            : base(configuration, db, hubContext) { }

        #region Views

        public async Task<ActionResult> Index()
        {
            var model = new SongIndexModel
            {
                AddSongStates = true,
            };
            return View(model);
        }

        #region Partial

        public ActionResult _PlaylistSongsGridPartial(int playlistId, bool addSongStates, int? limit = null, int pageSize = 50)
        {
            ViewBag.AddSongStates = addSongStates;
            ViewBag.PageSize = pageSize;
            ViewBag.Sources = db.GetSources();

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
                ViewBag.NumPages = Math.Ceiling(filteredSongs.Count() / (double)searchModel.PageSize);
                filteredSongs = filteredSongs.Skip(skip).Take(searchModel.PageSize).ToList();
            }

            if (addSongStates)
            {
                var playlistSongs = db.PlaylistSongReadOnly.Where(ps => ps.PlaylistId == searchModel.PlaylistId);
                var songIds = playlistSongs.Select(s => s.SongId).ToList();
                var playlistSongIds = playlistSongs.Select(s => s.Id).ToList();
                var songInfos = db.SongAdditionalInfoReadOnly.Where(ss => songIds.Contains(ss.SongId));
                var playlistSongStates = db.PlaylistSongStateReadOnly.Where(pss => playlistSongIds.Contains(pss.PlaylistSongId));

                if (filterSongStates)
                {
                    if (searchModel.Source != null)
                    {
                        songInfos = songInfos.Where(ss => ss.SourceId == searchModel.Source);
                        playlistSongStates = playlistSongStates.Where(pss => pss.SourceId == searchModel.Source);
                    }
                    if (searchModel.SongState != null)
                        songInfos = songInfos.Where(ss => ss.StateId == searchModel.SongState);
                    if (searchModel.PlaylistSongState != null)
                        playlistSongStates = playlistSongStates.Where(pss => pss.StateId == searchModel.PlaylistSongState);


                    //filter songs
                    filteredSongs = filteredSongs.Where(s => songInfos.Select(ss => ss.SongId).Contains(s.SongId));

                    ViewBag.NumPages = Math.Ceiling(filteredSongs.Count() / (double)searchModel.PageSize);
                    filteredSongs = filteredSongs.Skip(skip).Take(searchModel.PageSize).ToList();

                }

                foreach (var song in filteredSongs)
                {
                    var songStatess = songInfos.Where(ss => ss.SongId == song.SongId).ToList();
                    song.SongInfos = songStatess;
                    song.PlaylistSongStates = playlistSongStates.Where(ss => ss.PlaylistSongId == song.PlaylistSongId).ToList();
                }

            }

            ViewBag.AddSongStates = addSongStates;
            ViewBag.Sources = db.GetSources();

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
                var songInfos = db.SongAdditionalInfoReadOnly;
                if (filterSongStates)
                {
                    if (searchModel.Source != null)
                        songInfos = songInfos.Where(ss => ss.SourceId == searchModel.Source);
                    if (searchModel.SongState != null)
                        songInfos = songInfos.Where(ss => ss.StateId == searchModel.SongState);


                    //filter songs
                    filteredSongs = filteredSongs.Where(s => songInfos.Select(ss => ss.SongId).Contains(s.Id));

                    ViewBag.NumPages = Math.Ceiling(filteredSongs.Count() / (double)limit);
                    filteredSongs = filteredSongs.Skip(skip).Take(limit).ToList();
                }

                foreach (var song in filteredSongs)
                    song.SongInfos = songInfos.Where(ss => ss.SongId == song.Id).ToList();
            }

            ViewBag.AddSongStates = addSongStates;
            ViewBag.Sources = db.GetSources();

            return PartialView(filteredSongs.ToList());
        }
        #endregion

        #endregion

        [HttpPost]
        public ActionResult FindSongs(Sources source, string? songIds = null, int? playlistId = null)
        {
            try
            {
                IQueryable<Song> songs;

                if (songIds != null)
                {
                    var songIdsList = songIds.Split(',').Select(i => int.Parse(i));
                    songs = db.SongReadOnly.Where(s => songIdsList.Contains(s.Id));
                    songs = songs.Where(s => db.SongAdditionalInfoReadOnly.Single(ss => ss.SongId == s.Id).StateId == SongStates.NotChecked);
                }
                else if (playlistId != null)
                {
                    var songIdsList = db.PlaylistSongReadOnly.Where(ps => ps.PlaylistId == playlistId).Select(ps => ps.SongId);
                    songs = db.SongReadOnly.Where(s => songIdsList.Contains(s.Id));
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
            //create info if not available
            createInfosForMissingSongs(source, missingSongs);

            //only songs that weren't checked before
            var missingSongsList = missingSongs.Where(s => db.SongAdditionalInfoReadOnly.Single(ss => ss.SongId == s.Id && ss.SourceId == source).StateId == SongStates.NotChecked);

            if (!missingSongsList.Any())
                return null;

            //check if songs exists
            var findSongs = missingSongsList.Select(s => new FindSong(s.Id, s.ArtistName, s.SongName)).ToList();

            int nFound = 0;
            var foundSongs = new List<FoundSong>();
            FoundSong foundSong;

            await progressHub.InitProgressToast("Finding Songs...", findSongs.Count);
            foreach (var findSong in findSongs)
            {
                switch (source)
                {
                    case Sources.Youtube:
                        foundSong = ytHelper.FindSongId(findSong);
                        break;
                    case Sources.Spotify:
                        foundSong = spottyHelper.FindSongId(findSong);
                        break;
                    default:
                        throw new NotImplementedException(ErrorHelper.NotImplementedForThatSource);
                }

                addFoundSong(source, foundSong, foundSong.ExactMatch ? SongStates.Available : SongStates.MaybeAvailable);
                await progressHub.UpdateProgressToast(nFound, $"{++nFound} / {findSongs.Count} found.");
                foundSongs.Add(foundSong);
            }
            await progressHub.EndProgressToast();

            return foundSongs;
        }

        private void createInfosForMissingSongs(Sources source, List<Song> missingSongs)
        {
            var songIdsNoInfo = missingSongs.Select(s => s.Id).Where(id => !db.SongAdditionalInfoReadOnly.Any(i => i.SourceId == source && i.SongId == id)).ToList();
            foreach (var songId in songIdsNoInfo)
            {
                var newSongInfo = new SongAdditionalInfo()
                {
                    SongId = songId,
                    SourceId = source,
                    StateId = SongStates.NotChecked,
                    LastChecked = DateTime.Now
                };
                db.SongAdditionalInfo.Add(newSongInfo);
            }
            db.SaveChanges();
        }

        private void addFoundSong(Sources source, FoundSong foundSong, SongStates newState)
        {
            //add songInfo
            var songInfo = db.SongAdditionalInfo.SingleOrDefault(i => i.SongId == foundSong.Id && i.SourceId == source);

            if (songInfo == null)
            {
                songInfo = new SongAdditionalInfo();
                db.SongAdditionalInfo.Add(songInfo);
            }
            songInfo.SongIdSource = foundSong.IdAtSource;
            songInfo.SourceId = source;
            songInfo.StateId = newState;
            songInfo.LastChecked = DateTime.Now;

            db.SaveChanges();
        }

        internal List<Song> AddSongsToDb(List<SongAdditionalInfo> songsToAdd, Sources source)
        {
            var addedSongs = new List<Song>();

            //check if songs are already in db
            //TODO: for now only check if it wasnt added from same source
            //      on youtube songname & artist name dont have to be a unique combination
            //      fuck you anguish with your stupid ass song titles
            var songInfos = db.SongAdditionalInfoReadOnly.Where(i => i.SourceId == source);
            var newSongInfos = songsToAdd.Where(s => !songInfos.Any(dbSong => dbSong.SongIdSource == s.SongIdSource)).ToList();

            //add new songs
            foreach (var newSongInfo in newSongInfos)
            {
                var newSong = Helper.InfoToSong(newSongInfo);
                db.Song.Add(newSong);
                db.SaveChanges();
                newSongInfo.SongId = newSong.Id;
                newSongInfo.StateId = SongStates.Available;
                newSongInfo.LastChecked = DateTime.Now;
                db.SongAdditionalInfo.Add(newSongInfo);
            };
            db.SaveChanges();

            return addedSongs;
        }
    }
}