using static PlaylistChaser.Web.Util.BuiltInIds;

namespace PlaylistChaser.Web.Util.API
{
    /// <summary>
    /// Default <see cref="ISourceFactory"/> mapping the built-in <see cref="Sources"/> to their
    /// <see cref="ISource"/> implementations. Registered via DI in Program.cs.
    /// </summary>
    public class SourceFactory : ISourceFactory
    {
        public ISource Create(Sources source, string accessToken)
        {
            return source switch
            {
                Sources.Youtube => new YoutubeApiHelper(accessToken),
                Sources.Spotify => new SpotifyApiHelper(accessToken),
                _ => throw new NotSupportedException($"No {nameof(ISource)} implementation registered for source '{source}'."),
            };
        }
    }
}
