using Microsoft.AspNetCore.Mvc;
using PlaylistChaser.Web.Database;
using PlaylistChaser.Web.Models.SearchModel;
using System.Data.Entity;

namespace PlaylistChaser.Web.Controllers
{
    public class SongController : BaseController
    {
        public SongController(IConfiguration configuration, PlaylistChaserDbContext db) : base(configuration, db) { }

        public async Task<ActionResult> Index()
        {
            var model = new SongIndexModel
            {
                AddSongStates = true,
            };
            return View(model);
        }

        #region Partial

        public ActionResult _PlaylistSongsGridPartial(int playlistId, bool addSongStates, int pageSize = 50)
        {
            ViewBag.AddSongStates = addSongStates;
            ViewBag.PageSize = pageSize;

            var searchModel = new PlaylistSongSearchModel { PlaylistId = playlistId };
            return PartialView(searchModel);
        }

        public async Task<ActionResult> _PlaylistSongsGridDataPartial(PlaylistSongSearchModel searchModel, bool addSongStates, int skip, int limit)
        {
            var songs = await db.GetPlaylistSongs(searchModel.PlaylistId);
            //filter
            var filterSongStates = searchModel.Source != null || searchModel.SongState != null;

            var filteredSongs = songs.OrderByDescending(s => s.PlaylistSongId).AsEnumerable();
            if (searchModel.SongName != null)
                filteredSongs = filteredSongs.Where(s => s.SongName.StartsWith(searchModel.SongName) || s.SongName.EndsWith(searchModel.SongName));
            if (searchModel.ArtistName != null)
                filteredSongs = filteredSongs.Where(s => s.ArtistName != null && (s.ArtistName.StartsWith(searchModel.ArtistName) || s.ArtistName.EndsWith(searchModel.ArtistName)));

            if (!filterSongStates)
            {
                ViewBag.NumPages = Math.Ceiling(filteredSongs.Count() / (double)limit);
                filteredSongs = filteredSongs.Skip(skip).ToList();
                filteredSongs = filteredSongs.Take(limit).ToList();
            }

            if (addSongStates)
            {
                var songStates = db.SongState.AsQueryable();
                var playlistSongStates = db.PlaylistSongState.AsQueryable();
                if (filterSongStates)
                {
                    if (searchModel.Source != null)
                        songStates = songStates.Where(ss => ss.SourceId == searchModel.Source);
                    if (searchModel.SongState != null)
                        songStates = songStates.Where(ss => ss.StateId == searchModel.SongState);

                    //filterSongs
                    if (searchModel.Source != null)
                        filteredSongs = filteredSongs.Where(s => s.SongStates.SingleOrDefault(ss => ss.SourceId == searchModel.Source) != null);
                    if (searchModel.SongState != null)
                        filteredSongs = filteredSongs.Where(s => s.SongStates.SingleOrDefault(ss => ss.StateId == searchModel.SongState) != null);
                }

                foreach (var song in filteredSongs)
                {
                    song.SongStates = songStates.Where(ss => ss.SongId == song.SongId).ToList();
                    song.PlaylistSongStates = playlistSongStates.Where(ss => ss.PlaylistSongId == song.PlaylistSongId).ToList();
                }

            }

            ViewBag.AddSongStates = addSongStates;
            ViewBag.Sources = db.Source.OrderBy(s => s.Name).ToList();

            if (filterSongStates)
            {
                ViewBag.NumPages = Math.Ceiling(filteredSongs.Count() / (double)limit);
                filteredSongs = filteredSongs.Skip(skip).ToList();
                filteredSongs = filteredSongs.Take(limit).ToList();
            }

            return PartialView(filteredSongs.ToList());
        }
        public ActionResult _SongsGridPartial(bool addSongStates, int pageSize = 50)
        {
            ViewBag.AddSongStates = addSongStates;
            ViewBag.PageSize = pageSize;

            var searchModel = new SongSearchModel();
            return PartialView(searchModel);
        }
        public async Task<ActionResult> _SongsGridDataPartial(SongSearchModel searchModel, bool addSongStates, int skip, int limit)
        {
            var songs = await db.GetSongs();
            //filter
            var filterSongStates = searchModel.Source != null || searchModel.SongState != null;

            var filteredSongs = songs.AsEnumerable();
            if (searchModel.SongName != null)
                filteredSongs = songs.Where(s => s.SongName.StartsWith(searchModel.SongName) || s.SongName.EndsWith(searchModel.SongName));
            if (searchModel.ArtistName != null)
                filteredSongs = filteredSongs.Where(s => s.ArtistName != null && (s.ArtistName.StartsWith(searchModel.ArtistName) || s.ArtistName.EndsWith(searchModel.ArtistName)));

            if (!filterSongStates)
            {
                ViewBag.NumPages = Math.Ceiling(filteredSongs.Count() / (double)limit);
                filteredSongs = filteredSongs.Skip(skip).ToList();
                filteredSongs = filteredSongs.Take(limit).ToList();
            }

            if (addSongStates)
            {
                var songStates = db.SongState.AsQueryable();
                if (filterSongStates)
                {
                    if (searchModel.Source != null)
                        songStates = songStates.Where(ss => ss.SourceId == searchModel.Source);
                    if (searchModel.SongState != null)
                        songStates = songStates.Where(ss => ss.StateId == searchModel.SongState);

                    //filterSongs
                    if (searchModel.Source != null)
                    {
                        filteredSongs = filteredSongs.Where(s => s.SongStates.SingleOrDefault(ss => ss.SourceId == searchModel.Source) != null);


                    }
                    if (searchModel.SongState != null)
                        filteredSongs = filteredSongs.Where(s => s.SongStates.SingleOrDefault(ss => ss.StateId == searchModel.SongState) != null);
                }

                foreach (var song in filteredSongs)
                    song.SongStates = songStates.Where(ss => ss.SongId == song.Id).ToList();
            }

            ViewBag.AddSongStates = addSongStates;
            ViewBag.Sources = db.Source.OrderBy(s => s.Name).ToList();

            if (filterSongStates)
            {
                ViewBag.NumPages = Math.Ceiling(filteredSongs.Count() / (double)limit);
                filteredSongs = filteredSongs.Skip(skip).ToList();
                filteredSongs = filteredSongs.Take(limit).ToList();
            }

            return PartialView(filteredSongs.ToList());
        }
        #endregion
    }
}