using Microsoft.AspNetCore.Mvc;
using PlaylistChaser.Model;
using PlaylistChaser.Model.BuiltInIds;

namespace PlaylistChaser.Core.Sources
{
    public interface ISource
    {
        public SourceId SourceId { get; } 
        #region Playlist
        public PlaylistInfo GetPlaylistById(string playlistId);
        public Task<PlaylistInfo> CreatePlaylist(string playlistName, string? description = null, bool isPublic = true);
        public Task<ActionResult> UpdatePlaylist(string IdAtSource, string? playlistName = null, string? playlistDescription = null, bool isPublic = true);
        public Task<ActionResult> DeletePlaylist(string youtubePlaylistId);
        #endregion

        #region Song
        public List<SongInfo> GetPlaylistSongs(string playlistId);
        #endregion
    }
}
