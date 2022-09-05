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
            var playlist = addPlaylist(ytPlaylist);
            addSongs((await youtube.Playlists.GetVideosAsync(url)).ToList(), playlist.Id.Value);

            return playlist;
        }
        /// <param name="url">can be the url or the song id</param>
        /// <returns></returns>
        internal async Task<Video> GetSong(string url)
        {
            return await youtube.Videos.GetAsync(url);
        }

        #region model
        private PlaylistModel addPlaylist(Playlist ytPlaylist)
        {
            PlaylistModel playlist = null;
            try
            {
                playlist = new PlaylistModel
                {
                    Name = ytPlaylist.Title,
                    YoutubeUrl = ytPlaylist.Url,
                    UploaderName = ytPlaylist.Author.Title,
                    Description = ytPlaylist.Description
                };
                db.Playlist.Add(playlist);
                db.SaveChanges();
                return playlist;

            }
            catch (Exception ex)
            {
                return playlist;
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
                            PlaylistId = playlistId

                        }); ;
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
