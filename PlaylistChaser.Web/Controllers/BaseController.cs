using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using PlaylistChaser.Web.Database;
using PlaylistChaser.Web.Models;
using PlaylistChaser.Web.Util;

namespace PlaylistChaser.Web.Controllers
{
    [Authorize]
    public class BaseController : Controller
    {
        protected readonly IConfiguration configuration;
        protected readonly PlaylistChaserDbContext db;
        protected readonly ProgressHub progressHub;
        protected readonly IMemoryCache memoryCache;

        public User CurrentUser { get; private set; }

        public BaseController(IConfiguration configuration, PlaylistChaserDbContext db, IHubContext<ProgressHub> hubContext, IMemoryCache memoryCache)
        {
            this.configuration = configuration;
            this.db = db;
            progressHub = new ProgressHub(hubContext);
            this.memoryCache = memoryCache;
        }

        #region Overrides
        private void setViewBags()
        {
            ViewBag.Sources = db.GetSources();
        }

        #region PartialView
        public override PartialViewResult PartialView()
        {
            setViewBags();
            return base.PartialView();
        }
        public override PartialViewResult PartialView(string? viewName)
        {
            setViewBags();
            return base.PartialView(viewName);
        }
        public override PartialViewResult PartialView(object? model)
        {
            setViewBags();
            return base.PartialView(model);
        }
        #endregion

        #region View
        public override ViewResult View()
        {
            setViewBags();
            return base.View();
        }

        public override ViewResult View(string? viewName)
        {
            setViewBags();
            return base.View(viewName);
        }

        public override ViewResult View(object? model)
        {
            setViewBags();
            return base.View(model);
        }

        public override ViewResult View(string? viewName, object? model)
        {
            setViewBags();
            return base.View(viewName, model);
        }
        #endregion

        #endregion

        #region Toast
        public ActionResult CancelAction(string toastId)
        {
            memoryCache.Set(toastId, true);

            return new JsonResult(new { success = true });
        }

        public bool IsCancelled(string toastId, out DateTime startTime)
        {
            startTime = DateTime.Now;
            return IsCancelled(toastId);
        }
        public bool IsCancelled(string toastId)
        {
            if ((bool?)memoryCache.Get(toastId) == true)
            {
                memoryCache.Remove(toastId);
                return true;
            }
            return false;
        }
        #endregion        
    }
}