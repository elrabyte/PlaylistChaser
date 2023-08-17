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
		internal PlaylistModel SyncPlaylist(PlaylistModel playlist)
		{
			var ytPlaylist = toPlaylistModel(getPlaylist(playlist.YoutubeId));
			playlist.Name = ytPlaylist.Name;
			playlist.ChannelName = ytPlaylist.ChannelName;
			playlist.Description = ytPlaylist.Description;
			return playlist;
		}

		#region Get Stuff
		internal List<SongModel> GetPlaylistSongs(PlaylistModel playlist)
		{
			var ytSongs = toSongModels(getPlaylistSongs(playlist.YoutubeId), playlist.Id.Value);
			return ytSongs.Where(yt => !playlist.Songs.Select(s => s.YoutubeId).Contains(yt.YoutubeId)).ToList();

		}
		internal async Task<string> GetPlaylistThumbnailBase64(string id)
		{
			var ytPlaylist = getPlaylist(id);

			return await Helper.GetImageToBase64(ytPlaylist.Snippet.Thumbnails.Maxres.Url);
		}
		internal async Task<string> GetSongThumbnailBase64(string id)
		{
			var listRequest = ytService.Videos.List("snippet");
			listRequest.Id = id;
			var resp = listRequest.Execute();
			var song = resp.Items.Single().Snippet;

			return await Helper.GetImageToBase64(song.Thumbnails.Standard.Url);
		}
		internal async Task<Dictionary<string, string>> GetSongsThumbnailBase64ByPlaylist(string playlistId)
		{
			var ytSongs = getPlaylistSongs(playlistId);
			var songThumbnails = new Dictionary<string, string>();
			foreach (var ytSong in ytSongs)
			{
				if (!songThumbnails.ContainsKey(ytSong.ResourceId.VideoId))
					songThumbnails.Add(ytSong.ResourceId.VideoId, await Helper.GetImageToBase64(ytSong.Thumbnails.Default__.Url));
			}
			return songThumbnails;
		}

		private Playlist getPlaylist(string id)
		{
			var listRequest = ytService.Playlists.List("snippet");
			listRequest.Id = id;
			var resp = listRequest.Execute();
			var playlist = resp.Items.Single();
			return playlist;
		}

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

		#region Create Stuff
		/// <summary>
		/// Creates the Playlist on Youtube
		/// </summary>
		/// <param name="playlistName">Name of the Playlist</param>
		/// <returns>returns the YT-Playlist in local Model</returns>
		internal async Task<PlaylistModel> CreatePlaylist(string playlistName, string? description = null, string privacyStatus = "private")
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
		private PlaylistModel toPlaylistModel(Playlist ytPlaylist)
		{
			var playlist = new PlaylistModel
			{
				Name = ytPlaylist.Snippet.Title,
				YoutubeUrl = ytPlaylist.Id,
				ChannelName = ytPlaylist.Snippet.ChannelTitle,
				Description = ytPlaylist.Snippet.Description
			};
			return playlist;
		}

		private List<SongModel> toSongModels(List<PlaylistItemSnippet> ytSongs, int playlistId)
		{
			return ytSongs.Select(s => new SongModel
			{
				YoutubeSongName = s.Title,
				YoutubeId = s.ResourceId.VideoId,
				FoundOnSpotify = false,
				AddedToSpotify = false,
				PlaylistId = playlistId
			}).ToList();

		}
		#endregion

		#region helper
		internal string GetVideoIdFromUrl(string url)
		{
			var pattern = @"(?:youtube\.com\/(?:[^\/]+\/.+\/|(?:v|e(?:mbed)?)\/|.*[?&]v=)|youtu\.be\/)([^""&?\/\s]{11})";
			Regex rg = new Regex(pattern);
			var matches = rg.Matches(url);
			return matches[1].Value;
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
