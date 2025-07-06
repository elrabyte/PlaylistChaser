using PlaylistChaser.Core.Sources;
using PlaylistChaser.Model.BuiltInIds;

namespace PlaylistChaser.Api.Util
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
                case SourceId.Youtube:
                    type = typeof(YoutubeApiHelper);
                    break;
                default:
                    throw new NotImplementedException("not implemented for sourceId: " + sourceId);
            }
            return type;

        }
        public static T GetApiHelper<T>(this SourceId sourceId, string accessToken) where T : ISourceBase
        {
            var type = sourceId.GetApiHelperType();
            return (T)Activator.CreateInstance(type, new object[] { accessToken });
        }
    }
}