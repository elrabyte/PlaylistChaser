using PlaylistChaser.Database;
using PlaylistChaser.Models;
using System.Text.RegularExpressions;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

namespace PlaylistChaser
{
    internal class YoutubeApiHelper
    {
        private YoutubeClient youtube;
        private PlaylistChaserDbContext db;
        internal YoutubeApiHelper()
        {
            youtube = new YoutubeClient();
            db = new PlaylistChaserDbContext();
        }
       
        /// <param name="url">can be the url or the playlist id</param>
        /// <returns>returns null if the playlist doesnt exist</returns>
        internal async Task<PlaylistModel> GetPlaylist(string url)
        {
            var playlist = addPlaylist(await getPlaylist(url));
            addSongs(await getPlaylistSongs(url), playlist.Id.Value);

            return playlist;
        }
        private async Task<Playlist> getPlaylist(string url)
        {
            return await youtube.Playlists.GetAsync(url);
        }
        private async Task<List<PlaylistVideo>> getPlaylistSongs(string playlistUrl)
        {
            return (await youtube.Playlists.GetVideosAsync(playlistUrl)).ToList();
        }

        #region model
        private PlaylistModel addPlaylist(Playlist ytPlaylist)
        {
            try
            {
                var playlist = new PlaylistModel
                {
                    Name = ytPlaylist.Title,
                    YoutubeUrl = ytPlaylist.Url,
                    ChannelName = ytPlaylist.Author?.ChannelTitle,
                    Description = ytPlaylist.Description
                    //,ImageBytes64 = await Helper.GetImageToBase64(ytPlaylist.Thumbnails.OrderBy(t => t.Resolution.Area).First().Url)
                };
                db.Playlist.Add(playlist);
                db.SaveChanges();
                return playlist;

            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private void addSongs(List<PlaylistVideo> ytSongs, int playlistId)
        {
            try
            {
                foreach (var ytSong in ytSongs)
                {
                    db.Song.Add(
                        new SongModel
                        {
                            YoutubeSongName = ytSong.Title,
                            YoutubeId = ytSong.Url,
                            FoundOnSpotify = false,
                            AddedToSpotify = false,
                            PlaylistId = playlistId
                            //,ImageBytes64 = await Helper.GetImageToBase64(ytSong.Thumbnails.OrderBy(t => t.Resolution.Area).First().Url)
                        });
                }
                db.SaveChanges();
            }
            catch (Exception ex)
            {
            }
        }
        #endregion        
    }
}
