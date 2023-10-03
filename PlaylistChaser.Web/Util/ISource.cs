using PlaylistChaser.Web.Models;

namespace PlaylistChaser.Web.Util
{
    internal interface ISource
    {
        #region Playlist
        internal PlaylistInfo GetPlaylistById(string playlistId);
        internal Task<PlaylistInfo> CreatePlaylist(string playlistName, string? description = null, bool isPublic = true);
        internal Task<ReturnModel> UpdatePlaylist(string IdAtSource, string? playlistName = null, string? playlistDescription = null, bool isPublic = true);
        internal Task<ReturnModel> DeletePlaylist(string youtubePlaylistId);
        #endregion

        #region Song
        internal List<SongInfo> GetPlaylistSongs(string playlistId);
        internal FoundSong FindSong(FindSong song);
        internal ReturnModel AddSongToPlaylist(string playlistId, string songId);
        #endregion

        #region Get Thumbnail
        internal Task<SourceThumbnail> GetPlaylistThumbnail(string id);
        internal Task<Dictionary<string, SourceThumbnail>> GetSongsThumbnailBySongIds(List<string> songIds);
        #endregion
    }
}
