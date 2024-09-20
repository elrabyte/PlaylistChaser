using Microsoft.AspNetCore.Mvc;
using PlaylistChaser.Model;

namespace PlaylistChaser.Api.Util
{
    internal interface ISource
    {
        #region Playlist
        internal PlaylistInfo GetPlaylistById(string playlistId);
        internal Task<PlaylistInfo> CreatePlaylist(string playlistName, string? description = null, bool isPublic = true);
        internal Task<ActionResult> UpdatePlaylist(string IdAtSource, string? playlistName = null, string? playlistDescription = null, bool isPublic = true);
        internal Task<ActionResult> DeletePlaylist(string youtubePlaylistId);
        #endregion

        #region Song
        internal List<SongInfo> GetPlaylistSongs(string playlistId);
        #endregion

    }
}
