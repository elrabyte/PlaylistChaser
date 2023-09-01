using PlaylistChaser.Web.Models;

namespace PlaylistChaser.Web.Util
{
    internal interface ISource
    {
        #region Playlist

        internal PlaylistAdditionalInfo GetPlaylistById(string playlistId);
        /// <summary>
        /// source to local
        /// </summary>
        /// <param name="playlist"></param>
        /// <returns></returns>
        internal PlaylistAdditionalInfo SyncPlaylistInfo(PlaylistAdditionalInfo playlist);
        internal Task<PlaylistAdditionalInfo> CreatePlaylist(string playlistName, string? description = null, bool isPublic = true);
        internal Task<bool> DeletePlaylist(string youtubePlaylistId);
        #endregion

        #region Song
        /// <summary>
        /// 
        /// </summary>
        /// <param name="playlistId">playlistId at source</param>
        /// <returns></returns>
        internal List<SongAdditionalInfo> GetPlaylistSongs(string playlistId);
        internal (List<(int Id, string IdAtSource)> Exact, List<(int Id, string IdAtSource)> NotExact) FindSongs(List<(int SongId, string ArtistName, string SongName)> songs);
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
