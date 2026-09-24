using static PlaylistChaser.Web.Util.BuiltInIds;

namespace PlaylistChaser.Web.Util.API
{
    /// <summary>
    /// Creates the <see cref="ISource"/> implementation for a given music-service backend.
    /// This is the seam for adding a new backend: implement <see cref="ISource"/> and add a
    /// case to <see cref="SourceFactory"/> (or register your own <see cref="ISourceFactory"/>) -
    /// no controller or call-site changes are required.
    /// </summary>
    public interface ISourceFactory
    {
        ISource Create(Sources source, string accessToken);
    }
}
