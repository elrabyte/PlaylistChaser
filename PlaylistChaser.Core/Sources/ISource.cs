using PlaylistChaser.Model;
using PlaylistChaser.Model.SearchModel;

namespace PlaylistChaser.Core.Sources
{
    public interface ISource : ISourceBase 
    {
        #region Playlist

        PlaylistInfo GetPlaylistByUrl(string url);
        PlaylistInfo GetPlaylistById(string playlistId);

        #endregion

        #region Song
        List<SongInfo> GetPlaylistSongs(string playlistId);
        Task<Thumbnail> GetPlaylistThumbnail(string playlistIdSource);
        bool ValidatePlaylistUrl(string url);
        FoundSong FindSong(FindSong findSong);
        #endregion
    }
}
