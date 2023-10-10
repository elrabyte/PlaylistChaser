using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using PlaylistChaser.Web.Database;
using PlaylistChaser.Web.Models;
using PlaylistChaser.Web.Util;
using static PlaylistChaser.Web.Util.BuiltInIds;

namespace PlaylistChaser.Web.Controllers
{
    [AuthorizeRole(Roles.Administrator)]
    public class AdminController : BaseController
    {
        public AdminController(IConfiguration configuration, IHubContext<ProgressHub> hubContext, IMemoryCache memoryCache, AdminDBContext dbAdmin)
            : base(configuration, hubContext, memoryCache, dbAdmin) { }

        #region Views

        #region View
        public ActionResult Index()
            => View();
        #endregion

        #region Grid
        public ActionResult _SourceGridPartial()
            => PartialView(UserDbContext.GetCachedList(UserDbContext.Source).ToList());
        #endregion

        #region Edit
        [HttpGet]
        public ActionResult _SourceEditPartial(int id)
            => PartialView(UserDbContext.GetCachedList(UserDbContext.Source).Single(s => s.Id == id));

        [HttpPost]
        public ActionResult _SourceEditPartial(int id, Source uiSource)
        {
            try
            {
                var source = UserDbContext.Source.Single(s => s.Id == id);
                source.IconHtml = uiSource.IconHtml;
                source.ColorHex = uiSource.ColorHex;

                UserDbContext.SaveChanges();
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