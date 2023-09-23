using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PlaylistChaser.Web.Database;

namespace PlaylistChaser.Web.Controllers
{
    public class BaseController : Controller
    {
        protected readonly IConfiguration configuration;
        protected readonly PlaylistChaserDbContext db;
        protected readonly ProgressHub progressHub;

        public BaseController(IConfiguration configuration, PlaylistChaserDbContext db, IHubContext<ProgressHub> hubContext)
        {
            this.configuration = configuration;
            this.db = db;
            progressHub = new ProgressHub(hubContext);
        }
    }
}