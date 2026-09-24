using PlaylistChaser.Web.Models;

namespace PlaylistChaser.Web.Util
{
    /// <summary>
    /// Abstraction over a music-streaming backend (YouTube, Spotify, ...). Implement this
    /// interface and register it in <see cref="API.SourceFactory"/> to plug in a new
    /// music-service backend without touching controllers or other call sites.
    /// </summary>
    public interface ISource
    {
        #region Playlist
        PlaylistInfo GetPlaylistById(string playlistId);
        Task<PlaylistInfo> CreatePlaylist(string playlistName, string? description = null, bool isPublic = true);
        Task<ReturnModel> UpdatePlaylist(string IdAtSource, string? playlistName = null, string? playlistDescription = null, bool isPublic = true);
        Task<ReturnModel> DeletePlaylist(string youtubePlaylistId);
        #endregion

        #region Song
        List<SongInfo> GetPlaylistSongs(string playlistId);
        FoundSong FindSong(FindSong song);
        ReturnModel AddSongToPlaylist(string playlistId, string songId);
        #endregion

        #region Get Thumbnail
        Task<SourceThumbnail> GetPlaylistThumbnail(string id);
        Task<Dictionary<string, SourceThumbnail>> GetSongsThumbnailBySongIds(List<string> songIds);
        #endregion
    }
}
