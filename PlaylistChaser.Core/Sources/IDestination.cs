using Microsoft.AspNetCore.Mvc;
using PlaylistChaser.Model;

namespace PlaylistChaser.Core.Sources
{
    public interface IDestination : ISourceBase, IAuth
    {
        #region Playlist        
        Task<PlaylistInfo> CreatePlaylist(string playlistName, string? description = null, bool isPublic = true);
        Task<IActionResult> UpdatePlaylist(string IdAtSource, string? playlistName = null, string? playlistDescription = null, bool isPublic = true);
        Task<IActionResult> DeletePlaylist(string youtubePlaylistId);
        #endregion

        #region Song
        bool AddSongToPlaylist(string playlistIdSource, string songIdSource);
        #endregion
    }
}
