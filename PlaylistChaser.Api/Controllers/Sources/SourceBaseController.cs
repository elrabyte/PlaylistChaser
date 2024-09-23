using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using PlaylistChaser.Api.Database;
using PlaylistChaser.Core.Sources;
using PlaylistChaser.Model.BuiltInIds;
using System.Collections.Generic;

namespace PlaylistChaser.Api.Controllers.Sources
{
    [ApiController]
    [Route("api/{sourceId}/[controller]")]

    public abstract class SourceBaseController<T> : Controller where T : ISourceBase
    {
        public readonly DbHelper dbHelper;
        public readonly SourceId sourceId;
        public readonly List<SourceId> validSourceIds;
        public T apiHelper => GetApiHelper(typeof(SpotifyApiHelper));

        public SourceBaseController(AdminDBContext adminDBContext, IHttpContextAccessor httpContextAccessor)
        {
            sourceId = (SourceId)Enum.Parse(typeof(SourceId), httpContextAccessor.HttpContext?.GetRouteValue("sourceId").ToString());
            dbHelper = new DbHelper(adminDBContext);

            validSourceIds = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(T).IsAssignableFrom(type) && type.IsClass)
                .Select(type => GetApiHelper(type).SourceId).ToList();

            if (!IsValidSourceId()) throw new Exception("Not a valid sourceId for this Method");
        }
        internal virtual T GetApiHelper(Type type)
        {
            return (T)Activator.CreateInstance(type, new object[] { });
        }

        private bool IsValidSourceId()
        {
            return validSourceIds.Contains(sourceId);
        }

        [HttpGet]
        [Route(nameof(GetValidSourceIdsList))]
        public ActionResult<List<SourceId>> GetValidSourceIdsList()
        {
            return validSourceIds;
        }

    }
}