using Microsoft.AspNetCore.Mvc;
using PlaylistChaser.Web.Database;

namespace PlaylistChaser.Web.Controllers
{
    public class BaseController : Controller
    {
        protected readonly IConfiguration configuration;
        protected readonly PlaylistChaserDbContext db;

        public BaseController(IConfiguration configuration, PlaylistChaserDbContext db)
        {
            this.configuration = configuration;
            this.db = db;
        }
    }
}