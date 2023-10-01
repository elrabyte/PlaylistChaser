using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using PlaylistChaser.Web.Database;
using PlaylistChaser.Web.Models;

namespace PlaylistChaser.Web.Controllers
{
    public class AdminController : BaseController
    {
        public AdminController(IConfiguration configuration, PlaylistChaserDbContext db, IHubContext<ProgressHub> hubContext, IMemoryCache memoryCache)
            : base(configuration, db, hubContext, memoryCache) { }

        #region Views

        #region View
        public ActionResult Index()
            => View();
        #endregion

        #region Grid
        public ActionResult _SourceGridPartial()
            => PartialView(db.GetCachedList(db.Source).ToList());
        #endregion

        #region Edit
        [HttpGet]
        public ActionResult _SourceEditPartial(int id)
            => PartialView(db.GetCachedList(db.Source).Single(s => s.Id == id));

        [HttpPost]
        public ActionResult _SourceEditPartial(int id, Source uiSource)
        {
            try
            {
                var source = db.Source.Single(s => s.Id == id);
                source.IconHtml = uiSource.IconHtml;
                source.ColorHex = uiSource.ColorHex;

                db.SaveChanges();
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }
        #endregion

        #endregion
    }
}