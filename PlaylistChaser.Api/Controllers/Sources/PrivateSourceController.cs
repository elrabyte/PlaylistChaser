using Microsoft.AspNetCore.Mvc;
using PlaylistChaser.Api.Database;
using PlaylistChaser.Core.Sources;
using PlaylistChaser.Model.BuiltInIds;

namespace PlaylistChaser.Api.Controllers.Sources
{
    public class PrivateSourceController<T> : Controller where T : ISource, IAuth
    {
        public PrivateSourceController(SourceId sourceId, AdminDBContext adminDBContext) { }
    }
}