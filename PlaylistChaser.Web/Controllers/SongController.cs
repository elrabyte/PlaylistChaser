using Microsoft.AspNetCore.Mvc;
using PlaylistChaser.Web.Database;
using PlaylistChaser.Web.Models.SearchModel;

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
    }
}