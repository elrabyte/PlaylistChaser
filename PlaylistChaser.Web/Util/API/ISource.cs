using PlaylistChaser.Web.Models;

namespace PlaylistChaser.Web.Util.API
{
    internal interface ISource
    {
        #region Playlist
        /// <summary>
        /// source to local
        /// </summary>
        /// <param name="playlist"></param>
        /// <returns></returns>
        internal Playlist SyncPlaylist(Playlist playlist);
        internal Task<Playlist> CreatePlaylist(string playlistName, string? description = null, string privacyStatus = "public");
        internal Task<bool> DeletePlaylist(string youtubePlaylistId);
        #endregion

        #region Song
        /// <summary>
        /// 
        /// </summary>
        /// <param name="playlistId">playlistId at source</param>
        /// <returns></returns>
        internal List<Song> GetPlaylistSongs(string playlistId);
        #endregion

        #region get Thumbnail
        internal Task<byte[]> GetPlaylistThumbnail(string id);
        internal Task<Dictionary<string, byte[]>> GetSongsThumbnailBySongIds(List<string> songIds);
        #endregion

        /// <summary>
        /// add songs to source
        /// </summary>
        /// <param name="playlistId">playlist id at source</param>
        /// <param name="songIds">song ids at source</param>
        /// <returns>uploaded song ids</returns>
        internal List<string> AddSongsToPlaylist(string playlistId, List<string> songIds);
    }
}
