using PlaylistChaser.Core.Sources;
using PlaylistChaser.Model.BuiltInIds;
using System.Reflection;

namespace PlaylistChaser.Api.Controllers.Sources
{
    public static class SourcesHelper
    {
        public static Type GetApiHelperType(this SourceId sourceId)
        {
            Type type;
            switch (sourceId)
            {
                case SourceId.Spotify:
                    type = typeof(SpotifyApiHelper);
                    break;
                default:
                    throw new NotImplementedException("not implemented for sourceId: " + sourceId);
            }
            return type;

        }
        public static T GetApiHelper<T>(this SourceId sourceId, string accessToken) where T : ISourceBase
        {
            var type = GetApiHelperType(sourceId);
            return (T)Activator.CreateInstance(type, new object[] { accessToken });
        }
    }
    public class SourcesHelper<T>
    {
        public virtual List<SourceId> GetValidSourceIds()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            IEnumerable<Type> sourceImplementations = assembly.GetTypes().Where(t => typeof(ISource).IsAssignableFrom(t) && t.IsClass);
            var sourceIds = sourceImplementations.Select(type => ((ISource)Activator.CreateInstance(type)).SourceId).ToList();
            return sourceIds;
        }

        //public virtual T GetApiHelper()
        //{
        //    return (T)Activator.CreateInstance(typeof(T), new object[] { });
        //}
    }
}