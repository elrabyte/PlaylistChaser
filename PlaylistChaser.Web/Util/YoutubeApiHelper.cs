using Google.Apis.YouTube.v3;
using PlaylistChaser.Models;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;

namespace PlaylistChaser
{
    internal class YoutubeApiHelper
    {
        private YoutubeClient youtubeReadOnly;
        private YouTubeService ytService;

        internal YoutubeApiHelper()
        {
            youtubeReadOnly = new YoutubeClient();
        }
        internal async Task<PlaylistModel> SyncPlaylist(PlaylistModel playlist)
        {
            var ll = ytService.Playlists.List("id");


            var ytPlaylist = toPlaylistModel(await getPlaylist(playlist.YoutubeUrl));
            playlist.Name = ytPlaylist.Name;
            playlist.ChannelName = ytPlaylist.ChannelName;
            playlist.Description = ytPlaylist.Description;
            return playlist;
        }

        internal async Task<List<SongModel>> GetPlaylistSongs(PlaylistModel playlist)
        {
            var ytSongs = toSongModels(await getPlaylistSongs(playlist.YoutubeUrl), playlist.Id.Value);
            return ytSongs.Where(yt => !playlist.Songs.Select(s => s.YoutubeId).Contains(yt.YoutubeId)).ToList();

        }
        internal async Task<string> GetPlaylistThumbnailBase64(string url)
        {
            var ytPlaylist = await getPlaylist(url);
            return await Helper.GetImageToBase64(ytPlaylist.Thumbnails.OrderBy(t => t.Resolution.Area).First().Url);
        }
        internal async Task<string> GetSongThumbnailBase64(string songUrl)
        {
            return await Helper.GetImageToBase64((await youtubeReadOnly.Videos.GetAsync(songUrl)).Thumbnails.OrderBy(t => t.Resolution.Area).First().Url);
        }
        private async Task<Playlist> getPlaylist(string url)
        {
            return await youtubeReadOnly.Playlists.GetAsync(url);
        }
        private async Task<List<PlaylistVideo>> getPlaylistSongs(string playlistUrl)
        {
            return (await youtubeReadOnly.Playlists.GetVideosAsync(playlistUrl)).ToList();
        }


        #region model
        private PlaylistModel toPlaylistModel(Playlist ytPlaylist)
        {
            var playlist = new PlaylistModel
            {
                Name = ytPlaylist.Title,
                YoutubeUrl = ytPlaylist.Url,
                ChannelName = ytPlaylist.Author?.ChannelTitle,
                Description = ytPlaylist.Description
            };
            return playlist;
        }

        private List<SongModel> toSongModels(List<PlaylistVideo> ytSongs, int playlistId)
        {
            return ytSongs.Select(s => new SongModel
            {
                YoutubeSongName = s.Title,
                YoutubeId = s.Url,
                FoundOnSpotify = false,
                AddedToSpotify = false,
                PlaylistId = playlistId
            }).ToList();

        }
        #endregion
    }
}
