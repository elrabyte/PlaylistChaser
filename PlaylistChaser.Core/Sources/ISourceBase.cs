using PlaylistChaser.Model.BuiltInIds;

namespace PlaylistChaser.Core.Sources
{
    public interface ISourceBase
    {
        public SourceId SourceId { get; }
    }
}
