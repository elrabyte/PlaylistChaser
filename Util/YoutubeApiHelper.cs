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
            var ytPlaylist = await youtube.Playlists.GetAsync(url);
            var playlist = await addPlaylist(ytPlaylist);
            await addSongs((await youtube.Playlists.GetVideosAsync(url)).ToList(), playlist.Id.Value);

            return playlist;
        }
        /// <param name="url">can be the url or the song id</param>
        /// <returns></returns>
        internal async Task<Video> GetSong(string url)
        {
            return await youtube.Videos.GetAsync(url);
        }

        #region model
        private async Task<PlaylistModel> addPlaylist(Playlist ytPlaylist)
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

        private async Task addSongs(List<PlaylistVideo> ytSongs, int playlistId)
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
        internal void AddSongs(List<Video> ytSongs, int playlistId)
        {
            foreach (var ytSong in ytSongs)
            {
                db.Song.Add(
                    new SongModel
                    {
                        YoutubeSongName = ytSong.Title,
                        YoutubeId = ytSong.Url,
                        FoundOnSpotify = false,
                        PlaylistId = playlistId
                    });
            }
            db.SaveChanges();
        }
        #endregion

        private string getSongFileName(SongModel song)
        {
            var regexAll = "[^0-9a-zA-Z]+";
            var songName = Regex.Replace(song.YoutubeSongName, regexAll, "");
            if (string.IsNullOrEmpty(songName))
                songName = song.Id.ToString();
            var author = Regex.Replace(song.ArtistName, regexAll, "");
            if (!string.IsNullOrEmpty(author))
                author += "-";

            return string.Format("{0}{1}.mp3", author, songName);

        }
    }
}
