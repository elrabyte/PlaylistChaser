using Microsoft.AspNetCore.Mvc;
using PlaylistChaser.Model;
using PlaylistChaser.Model.BuiltInIds;

namespace PlaylistChaser.Core.Sources
{
    public interface ISource
    {
        SourceId SourceId { get; }


        #region Playlist

        PlaylistInfo GetPlaylistByUrl(string url);
        PlaylistInfo GetPlaylistById(string playlistId);
        Task<PlaylistInfo> CreatePlaylist(string playlistName, string? description = null, bool isPublic = true);
        Task<ActionResult> UpdatePlaylist(string IdAtSource, string? playlistName = null, string? playlistDescription = null, bool isPublic = true);
        Task<ActionResult> DeletePlaylist(string youtubePlaylistId);
        #endregion

        #region Song
        List<SongInfo> GetPlaylistSongs(string playlistId);
        Task<Thumbnail> GetPlaylistThumbnail(string playlistIdSource);
        bool ValidatePlaylistUrl(string url);
        #endregion
    }
}
