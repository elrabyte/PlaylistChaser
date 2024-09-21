using PlaylistChaser.Core.Sources;
using PlaylistChaser.Model.BuiltInIds;

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
        public static ISource GetApiHelper(this SourceId sourceId, string accessToken)
        {
            var type = GetApiHelperType(sourceId);
            return (ISource)Activator.CreateInstance(type, new object[] { accessToken });
        }
    }
}