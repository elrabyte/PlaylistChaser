using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PlaylistChaser.Web.Database;
using System.Security.Claims;

namespace PlaylistChaser.Web.Controllers
{
    [Authorize]
    public class BaseController : Controller
    {
        protected readonly IConfiguration configuration;
        protected readonly AdminDBContext dbAdmin;
        protected readonly ProgressHub progressHub;
        protected readonly IMemoryCache memoryCache;
        private UserDbContext _userDbContext;
        public UserDbContext UserDbContext
        {
            get
            {
                if (_userDbContext == null)
                {
                    var userId = getCurrentUserId();
                    if (userId == null)
                        return null;

                    var user = dbAdmin.AspNetUsers.Single(u => u.Id == userId);
                    _userDbContext = new UserDbContext(new DbContextOptions<UserDbContext>(), memoryCache, configuration, user.DbUserName, user.DbPassword);
                }

                return _userDbContext;
            }
        }
        public BaseController(IConfiguration configuration, IHubContext<ProgressHub> hubContext, IMemoryCache memoryCache, AdminDBContext dbAdmin)
        {
            this.configuration = configuration;
            progressHub = new ProgressHub(hubContext);
            this.memoryCache = memoryCache;
            this.dbAdmin = dbAdmin;
        }

        #region Overrides
        private void setViewBags()
        {
            if (UserDbContext != null)
                ViewBag.Sources = UserDbContext.GetSources();
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

        public int? getCurrentUserId()
        {
            if (int.TryParse(User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                return userId;
            else
                return null;
        }
    }
}