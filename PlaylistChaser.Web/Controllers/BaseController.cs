using Microsoft.AspNetCore.Mvc;
using PlaylistChaser.Web.Database;

namespace PlaylistChaser.Web.Controllers
{
    public class BaseController : Controller
    {
        protected readonly IConfiguration Configuration;
        protected readonly PlaylistChaserDbContext db;

        public BaseController(IConfiguration configuration, PlaylistChaserDbContext db)
        {
            Configuration = configuration;
            this.db = db;
        }
    }
}