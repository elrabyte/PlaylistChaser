using Microsoft.AspNetCore.Mvc;
using PlaylistChaser.Model;

namespace PlaylistChaser.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlaylistController : ControllerBase
    {
        [HttpGet]
        public ActionResult<IEnumerable<Playlist>> GetPlaylists()
        {
            // Example response, replace with actual logic
            return new List<Playlist> { new Playlist { Id = 1, Name = "test" } };
        }

        [HttpGet("{id}")]
        public ActionResult<string> GetPlaylist(int id)
        {
            // Example response, replace with actual logic
            return "Playlist " + id;
        }
    }
}
