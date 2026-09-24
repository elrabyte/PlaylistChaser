using Microsoft.AspNetCore.Mvc;
using PlaylistChaser.Web.Database.Abstractions;

namespace PlaylistChaser.Web.Controllers.Api
{
    /// <summary>
    /// JSON/REST endpoints for playlists, backed by <see cref="IPlaylistDataStoreFactory"/> so
    /// they work against either database backend (SQL Server with native per-user security, or
    /// the portable EF Core path used e.g. with PostgreSQL in Docker) transparently.
    /// </summary>
    [Route("api/playlists")]
    public class PlaylistsController : ApiControllerBase
    {
        private readonly IPlaylistDataStoreFactory dataStoreFactory;

        public PlaylistsController(IPlaylistDataStoreFactory dataStoreFactory)
        {
            this.dataStoreFactory = dataStoreFactory;
        }

        [HttpGet]
        public async Task<IActionResult> GetPlaylists()
        {
            var store = await dataStoreFactory.CreateAsync(GetUserId());
            return Ok(await store.GetPlaylistsAsync(GetUserId()));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPlaylist(int id)
        {
            var store = await dataStoreFactory.CreateAsync(GetUserId());
            var playlists = await store.GetPlaylistsAsync(GetUserId(), id);
            var playlist = playlists.SingleOrDefault();
            if (playlist == null)
                return NotFound();
            return Ok(playlist);
        }

        [HttpGet("{id:int}/songs")]
        public async Task<IActionResult> GetPlaylistSongs(int id, [FromQuery] int? limit = null)
        {
            var store = await dataStoreFactory.CreateAsync(GetUserId());
            return Ok(await store.GetPlaylistSongsAsync(GetUserId(), id, limit));
        }
    }
}
