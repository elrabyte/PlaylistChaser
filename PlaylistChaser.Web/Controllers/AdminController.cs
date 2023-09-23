﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PlaylistChaser.Web.Database;
using PlaylistChaser.Web.Models;

namespace PlaylistChaser.Web.Controllers
{
    public class AdminController : BaseController
    {
        public AdminController(IConfiguration configuration, PlaylistChaserDbContext db, IHubContext<ProgressHub> hubContext)
            : base(configuration, db, hubContext) { }

        public ActionResult Index()
            => View();



        public ActionResult _SourceGridPartial()
            => PartialView(db.SourceReadOnly.ToList());

        [HttpGet]
        public ActionResult _SourceEditPartial(int id)
        {
            var model = db.SourceReadOnly.Single(s => s.Id == id);
            return PartialView(model);
        }
        [HttpPost]
        public ActionResult _SourceEditPartial(int id, Source uiSource)
        {
            try
            {
                var source = db.Source.Single(s => s.Id == id);
                source.IconHtml = uiSource.IconHtml;

                db.SaveChanges();
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }
    }
}