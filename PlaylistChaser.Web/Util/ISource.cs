using PlaylistChaser.Web.Models;

namespace PlaylistChaser.Web.Util
{
    internal interface ISource
    {
        #region Playlist
        internal PlaylistAdditionalInfo GetPlaylistById(string playlistId);
        internal Task<PlaylistAdditionalInfo> CreatePlaylist(string playlistName, string? description = null, bool isPublic = true);
        internal Task<bool> UpdatePlaylist(string IdAtSource, string? playlistName = null, string? playlistDescription = null, bool isPublic = true);
        internal Task<bool> DeletePlaylist(string youtubePlaylistId);
        #endregion

        #region Song
        internal List<SongAdditionalInfo> GetPlaylistSongs(string playlistId);
        internal FoundSong FindSongId(FindSong song);
        internal bool AddSongToPlaylist(string playlistId, string songId);
        #endregion

        #region Get Thumbnail
        internal Task<byte[]> GetPlaylistThumbnail(string id);
        internal Task<Dictionary<string, byte[]>> GetSongsThumbnailBySongIds(List<string> songIds);
        #endregion

    }
}
