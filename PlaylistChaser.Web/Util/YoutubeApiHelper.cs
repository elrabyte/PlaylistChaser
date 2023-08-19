using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using PlaylistChaser.Models;
using System.Text.RegularExpressions;
using Playlist = Google.Apis.YouTube.v3.Data.Playlist;

namespace PlaylistChaser
{
    internal class YoutubeApiHelper
    {
        private YouTubeService ytService;

        string[] scopes = { "https://www.googleapis.com/auth/youtube" };

        internal YoutubeApiHelper()
        {
            ytService = new YouTubeService(new BaseClientService.Initializer() { HttpClientInitializer = authenticate() });
        }

        private UserCredential authenticate()
        {

            var clientId = Helper.ReadSecret("Youtube", "ClientId");
            var clientSecret = Helper.ReadSecret("Youtube", "ClientSecret");

            var userCredential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret },
                scopes, "user", CancellationToken.None).Result;

            if (userCredential.Token.IsExpired(SystemClock.Default))
                userCredential.RefreshTokenAsync(CancellationToken.None);

            return userCredential;

        }

        /// <summary>
        /// brings the local playlist up to date with the youtube side
        /// </summary>
        /// <param name="playlist">local playlist</param>
        /// <returns>returns the same playlist</returns>
        internal Models.Playlist SyncPlaylist(Models.Playlist playlist)
        {
            var ytPlaylist = toPlaylistModel(getPlaylist(playlist.YoutubeId));
            playlist.Name = ytPlaylist.Name;
            playlist.ChannelName = ytPlaylist.ChannelName;
            playlist.Description = ytPlaylist.Description;
            return playlist;
        }

        #region Get Stuff
        /// <summary>
        /// Gets List of songs in local Song-Model by youtube playlist id
        /// </summary>
        /// <param name="playlist">local playlist</param>
        /// <returns></returns>
        internal List<Song> GetPlaylistSongs(string playlistId)
            => toSongModels(getPlaylistSongs(playlistId));

        /// <summary>
        /// returns the playlist thumbnail as a base64 string
        /// </summary>
        /// <param name="id">local playlist </param>
        /// <returns></returns>
        internal async Task<string> GetPlaylistThumbnailBase64(string id)
        {
            var ytPlaylist = getPlaylist(id);

            var thumbnail = ytPlaylist.Snippet.Thumbnails.Maxres
                            ?? ytPlaylist.Snippet.Thumbnails.High
                            ?? ytPlaylist.Snippet.Thumbnails.Medium
                            ?? ytPlaylist.Snippet.Thumbnails.Standard
                            ?? ytPlaylist.Snippet.Thumbnails.Default__;


            return thumbnail != null ? await Helper.GetImageToBase64(thumbnail.Url) : null;
        }

        /// <summary>
        /// returns the song thumbnail as a base64 string
        /// </summary>
        /// <param name="id">youtube song id</param>
        /// <returns></returns>
        internal async Task<string> GetSongThumbnailBase64(string id)
        {
            var listRequest = ytService.Videos.List("snippet");
            listRequest.Id = id;
            var resp = listRequest.Execute();
            var song = resp.Items.Single().Snippet;

            return await Helper.GetImageToBase64(song.Thumbnails.Standard.Url);
        }

        /// <summary>
        /// returns a list with a thumbnail for each song
        /// </summary>
        /// <param name="playlistId">youtube playlist id</param>
        /// <returns></returns>
        internal async Task<Dictionary<string, string>> GetSongsThumbnailBase64ByPlaylist(string playlistId)
        {
            var ytSongs = getPlaylistSongs(playlistId);
            var songThumbnails = new Dictionary<string, string>();
            foreach (var ytSong in ytSongs)
            {
                if (!songThumbnails.ContainsKey(ytSong.ResourceId.VideoId))
                    songThumbnails.Add(ytSong.ResourceId.VideoId, ytSong.Thumbnails.Default__ == null ? null : await Helper.GetImageToBase64(ytSong.Thumbnails.Default__.Url));
            }
            return songThumbnails;
        }

        /// <summary>
        /// get youtube playlist by id
        /// </summary>
        /// <param name="id">youtube playlist id</param>
        /// <returns></returns>
        private Playlist getPlaylist(string id)
        {
            var listRequest = ytService.Playlists.List("snippet");
            listRequest.Id = id;
            var resp = listRequest.Execute();
            var playlist = resp.Items.Single();
            return playlist;
        }

        /// <summary>
        /// Returns a list of songs from a playlist
        /// </summary>
        /// <param name="playlistId">youtube playlist id</param>
        /// <returns></returns>
        private List<PlaylistItemSnippet> getPlaylistSongs(string playlistId)
        {
            var listRequest = ytService.PlaylistItems.List("snippet");
            listRequest.MaxResults = 50;
            listRequest.PlaylistId = playlistId;
            var resp = listRequest.Execute();
            var resultsShown = resp.PageInfo.ResultsPerPage;
            var totalResults = resp.PageInfo.TotalResults;

            var songs = resp.Items.Select(i => i.Snippet).ToList();
            while (resultsShown <= totalResults)
            {
                listRequest.PageToken = resp.NextPageToken;
                resp = listRequest.Execute();
                resultsShown += resp.PageInfo.ResultsPerPage;
                songs.AddRange(resp.Items.Select(i => i.Snippet).ToList());
            }
            return songs;
        }
        #endregion

        #region Edit Stuff
        /// <summary>
        /// Creates the Playlist on Youtube
        /// </summary>
        /// <param name="playlistName">Name of the Playlist</param>
        /// <returns>returns the YT-Playlist in local Model</returns>
        internal async Task<Models.Playlist> CreatePlaylist(string playlistName, string? description = null, string privacyStatus = "private")
        {
            // Create a new, private playlist in the authorized user's channel.
            var newPlaylist = new Playlist();
            newPlaylist.Snippet = new PlaylistSnippet();
            newPlaylist.Snippet.Title = playlistName;
            newPlaylist.Snippet.Description = description;
            newPlaylist.Status = new PlaylistStatus();
            newPlaylist.Status.PrivacyStatus = privacyStatus;
            newPlaylist = await ytService.Playlists.Insert(newPlaylist, "snippet,status").ExecuteAsync();

            return toPlaylistModel(newPlaylist);
        }

        internal async Task<bool> DeletePlaylist(string youtubePlaylistId)
        {
            try
            {
                await ytService.Playlists.Delete(youtubePlaylistId).ExecuteAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }

        #endregion

        #region model
        private Models.Playlist toPlaylistModel(Playlist ytPlaylist)
        {
            var playlist = new Models.Playlist
            {
                Name = ytPlaylist.Snippet.Title,
                YoutubeUrl = ytPlaylist.Id,
                ChannelName = ytPlaylist.Snippet.ChannelTitle,
                Description = ytPlaylist.Snippet.Description
            };
            return playlist;
        }

        private List<Song> toSongModels(List<PlaylistItemSnippet> ytSongs)
        {
            return ytSongs.Select(s => new Song
            {
                YoutubeSongName = s.Title,
                YoutubeId = s.ResourceId.VideoId,
                FoundOnSpotify = false,
                AddedToSpotify = false,
                SongName = s.Title,
                ArtistName = s.VideoOwnerChannelTitle
            }).ToList();

        }
        #endregion

        #region helper
        internal string GetVideoIdFromUrl(string url)
        {
            var pattern = @"(?:youtube\.com\/(?:[^\/]+\/.+\/|(?:v|e(?:mbed)?)\/|.*[?&]v=)|youtu\.be\/)([^""&?\/\s]{11})";
            Regex rg = new Regex(pattern);
            var match = rg.Match(url);
            return match.Groups[1].Value;
        }
        internal string GetPlaylistIdFromUrl(string url)
        {
            var pattern = @"[?&]list=([^#\&\?]+)";
            Regex rg = new Regex(pattern);
            var match = rg.Match(url);
            return match.Groups[1].Value;
        }
        #endregion
    }
}
