using Microsoft.AspNetCore.Mvc;
using PlaylistChaser.Web.Database.Abstractions;
using PlaylistChaser.Web.Models.Api;

namespace PlaylistChaser.Web.Controllers.Api
{
    [Route("api/songs")]
    public class SongsController : ApiControllerBase
    {
        private readonly IPlaylistDataStoreFactory dataStoreFactory;

        public SongsController(IPlaylistDataStoreFactory dataStoreFactory)
        {
            this.dataStoreFactory = dataStoreFactory;
        }

        [HttpGet]
        public async Task<IActionResult> GetSongs()
        {
            var store = await dataStoreFactory.CreateAsync(GetUserId());
            return Ok(await store.GetSongsAsync(GetUserId()));
        }

        [HttpPost("merge")]
        public async Task<IActionResult> MergeSongs(MergeSongsRequest request)
        {
            var store = await dataStoreFactory.CreateAsync(GetUserId());
            var success = await store.MergeSongsAsync(GetUserId(), request.SongIds, request.MainSongId);
            return Ok(new { success });
        }
    }
}
