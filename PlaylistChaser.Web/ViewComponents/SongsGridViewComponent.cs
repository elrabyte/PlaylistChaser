using Microsoft.AspNetCore.Mvc;
using PlaylistChaser.Web.Database;

namespace PlaylistChaser.Web.ViewComponents
{
    public class SongsGridViewComponent : ViewComponent
    {
        PlaylistChaserDbContext db;
        public SongsGridViewComponent(PlaylistChaserDbContext db)
        {
            this.db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync(int? playlistId = null, bool addSongStates = false)
        {
            if (playlistId != null)
                return await getPlaylistSongs(playlistId.Value, addSongStates);
            else
                return await getSongs(addSongStates);
        }

        private async Task<IViewComponentResult> getPlaylistSongs(int playlistId, bool addSongStates)
        {
            var songs = await db.GetPlaylistSongs(playlistId);
            if (addSongStates)
            {
                songs.ForEach(ps => ps.PlaylistSongStates = db.PlaylistSongState.Where(pss => pss.PlaylistSongId == ps.PlaylistSongId).ToList());
            }

            ViewBag.AddSongStates = addSongStates;

            return View("PlaylistSongs", songs);
        }
        private async Task<IViewComponentResult> getSongs(bool addSongStates)
        {
            var songs = await db.GetSongs();
            if (addSongStates)
                songs.ForEach(ps => ps.SongStates = db.SongState.Where(ss => ss.SongId == ps.Id).ToList());

            ViewBag.AddSongStates = addSongStates;

            return View("Songs", songs);
        }
    }
}
