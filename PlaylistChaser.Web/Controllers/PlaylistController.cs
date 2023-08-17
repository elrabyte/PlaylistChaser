using Google.Apis.YouTube.v3.Data;
using Microsoft.AspNetCore.Mvc;
using PlaylistChaser.Database;
using PlaylistChaser.Models;
using PlaylistChaser.Web.Util;
using SpotifyAPI.Web;
using System.Diagnostics;

namespace PlaylistChaser.Controllers
{
	public class PlaylistController : Controller
	{
		private readonly ILogger<PlaylistController> _logger;
		private PlaylistChaserDbContext db;

		public PlaylistController(ILogger<PlaylistController> logger)
		{
			_logger = logger;
			db = new PlaylistChaserDbContext();
		}

		#region error
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
		#endregion

		#region views
		public IActionResult Index()
		{
			var playlists = db.Playlist.ToList();
			return View(playlists);
		}

		public ActionResult Edit_Delete(int id, bool deleteAtSource = false)
		{
			var playlist = db.Playlist.Single(p => p.Id == id);

			if (deleteAtSource)
			{
				////delete spotify playlist
				//var spotifyHelper = new SpotifyApiHelper(HttpContext);
				//if (playlist.SpotifyUrl != null)
				//    if (!spotifyHelper.DeletePlaylist(playlist).Result)
				//        return RedirectToAction("Index");


				////delete from YT
				//var ytHelper = new YoutubeApiHelper();
				//ytHelper.DeletePlaylist(playlist.YoutubeId);
			}

			//delete from db
			var songs = db.Song.Where(s => s.PlaylistId == id).ToList();
			db.Song.RemoveRange(songs);
			db.SaveChanges();
			db.Playlist.Remove(db.Playlist.Single(p => p.Id == id));
			db.SaveChanges();

			return RedirectToAction("Index");
		}


		public ActionResult Details(int id)
		{
			var playlist = db.Playlist.Single(p => p.Id == id);
			playlist.Songs = db.Song.Where(s => s.PlaylistId == id).ToList();
			return View(playlist);
		}
		#endregion

		#region Index
		#region Search
		public ActionResult SeachYTPlaylist(string ytPlaylistUrl)
		{
			var ytHelper = new YoutubeApiHelper();
			//add playlist
			var playlist = new PlaylistModel();
			playlist.YoutubeId = ytHelper.GetPlaylistIdFromUrl(ytPlaylistUrl);
			playlist.YoutubeUrl = ytPlaylistUrl;
			playlist.PlaylistTypeId = BuiltInIds.PLaylistTypes.Simple;
			playlist = ytHelper.SyncPlaylist(playlist);
			db.Playlist.Add(playlist);
			db.SaveChanges();

			//add songs
			playlist.Songs = new List<SongModel>();
			db.Song.AddRange(ytHelper.GetPlaylistSongs(playlist));
			db.SaveChanges();

			return new JsonResult(new { success = true });
		}
		public ActionResult SyncPlaylistYoutube(int id)
		{
			var playlist = db.Playlist.Single(p => p.Id == id);
			playlist.Songs = db.Song.Where(s => s.PlaylistId == id).ToList();
			db.Song.AddRange(new YoutubeApiHelper().GetPlaylistSongs(playlist));
			db.SaveChanges();

			return new JsonResult(new { success = true });
		}
		public async Task<ActionResult> UpdateSpotifySongLinks(int id)
		{
			var songs = db.Song.Where(s => s.PlaylistId == id && !s.FoundOnSpotify.Value).ToList();
			await findSongsSpotify(songs);

			return new JsonResult(new { success = true });
		}
		[HttpPost]
		public async Task<ActionResult> _ChooseSong(int songId, string newSpotifyId)
		{
			//get id from link
			newSpotifyId = newSpotifyId.Replace("https://open.spotify.com/track/", "");
			newSpotifyId = newSpotifyId.Remove(newSpotifyId.IndexOf("?"));

			var song = db.Song.Single(s => s.Id == songId);
			if (!string.IsNullOrEmpty(newSpotifyId) && song.SpotifyId != newSpotifyId)
			{
				var spotifyHelper = new SpotifyApiHelper(HttpContext);
				var spotifySong = await spotifyHelper.GetSong(newSpotifyId);

				song.SpotifyId = spotifySong.Uri;
				song.AddedToSpotify = false;
				song.IsNotOnSpotify = null;
				song.FoundOnSpotify = true;
				song.ArtistName = string.Join(",", spotifySong.Artists.Select(a => a.Name).ToList());
				song.SongName = spotifySong.Name;
				db.SaveChanges();

				//add song to spotify
				var playlist = db.Playlist.Single(p => p.Id == song.PlaylistId);
				if (!await spotifyHelper.UpdatePlaylist(playlist.SpotifyUrl, new List<string> { song.SpotifyId }))
					return new JsonResult(new { success = false });
				song.AddedToSpotify = true;
				db.SaveChanges();

				return new JsonResult(new { success = true });
			}
			else
				return new JsonResult(new { success = false });


		}
		#endregion

		#endregion

		#region Details
		#region spotify

		private async Task<bool> findSongsSpotify(List<SongModel> songs)
		{
			var spotifyHelper = new SpotifyApiHelper(HttpContext);

			foreach (var song in songs)
			{
				var response = await spotifyHelper.SearchSong(SearchRequest.Types.Track, song.YoutubeSongName);
				//first song found
				if (response.Tracks.Items?.Count > 0)
				{
					var spotifySong = response.Tracks.Items.First();
					var dbSong = db.Song.Single(s => s.Id == song.Id);
					dbSong.FoundOnSpotify = true;
					dbSong.SpotifyId = spotifySong.Uri;
					dbSong.ArtistName = string.Join(",", spotifySong.Artists.Select(a => a.Name).ToList());
					dbSong.SongName = spotifySong.Name;
					db.SaveChanges();
				}
			}
			return true;

		}
		public async Task<ActionResult> UpdatePlaylistSpotify(int id)
		{
			var playlist = db.Playlist.Single(p => p.Id == id);
			playlist.Songs = db.Song.Where(s => s.PlaylistId == id).ToList();
			var spotifyHelper = new SpotifyApiHelper(HttpContext);
			//create if not exist
			if (string.IsNullOrEmpty(playlist.SpotifyUrl))
			{
				var spotifyPlaylist = await spotifyHelper.CreatePlaylist(playlist.Name);
				playlist.SpotifyUrl = spotifyPlaylist.Id;
				db.SaveChanges();
			}

			//update
			var songsToAdd = playlist.Songs.Where(s => s.FoundOnSpotify.Value
													&& !s.AddedToSpotify.Value
													&& (!s.IsNotOnSpotify ?? true)
													).ToList();
			var playlistDescription = string.Format("Last updated on {1} - Found {2}/{3} Songs - This playlist is a copy of the youtube playlist \"{4}\" by {5}: {0}. ", await BitlyApiHelper.GetShortUrl(playlist.YoutubeUrl),
																																										 DateTime.Now,
																																										 playlist.Songs.Where(s => s.FoundOnSpotify.Value).Count(),
																																										 playlist.Songs.Count(),
																																										 playlist.Name,
																																										 playlist.ChannelName);
			if (!await spotifyHelper.UpdatePlaylist(playlist.SpotifyUrl, songsToAdd.Select(s => s.SpotifyId).ToList(), playlistDescription))
				return new JsonResult(new { success = false });

			//remember added songs
			foreach (var song in songsToAdd)
			{
				song.AddedToSpotify = true;
				db.SaveChanges();
			}

			return new JsonResult(new { success = true });
		}
		public async Task<ActionResult> SyncPlaylistThumbnail(int id)
		{
			var playlist = db.Playlist.Single(p => p.Id == id);
			playlist.ImageBytes64 = await new YoutubeApiHelper().GetPlaylistThumbnailBase64(playlist.YoutubeId);
			db.SaveChanges();
			return new JsonResult(new { success = true });
		}
		public async Task<ActionResult> SyncSongsThumbnail(int id, bool onlyWithNoThumbnails = true)
		{
			var playlistYoutubeId = db.Playlist.Single(p => p.Id == id).YoutubeId;
			var songs = db.Song.Where(p => p.PlaylistId == id);
			if (onlyWithNoThumbnails)
				songs = songs.Where(s => s.ImageBytes64 == null);

			var thumbnails = await new YoutubeApiHelper().GetSongsThumbnailBase64ByPlaylist(playlistYoutubeId);

			foreach (var song in songs.ToList())
			{
				if (thumbnails.TryGetValue(song.YoutubeId, out var imageBytes64))
					song.ImageBytes64 = imageBytes64;
			}
			db.SaveChanges();
			return new JsonResult(new { success = true });
		}
		public async Task<ActionResult> SongIsNotOnSpotify(int playlistId, int songId)
		{
			var playlist = db.Playlist.Single(p => p.Id == playlistId);
			var song = db.Song.Single(s => s.Id == songId);

			var spotifyHelper = new SpotifyApiHelper(HttpContext);
			if (await spotifyHelper.RemovePlaylistSong(playlist.SpotifyUrl, song.SpotifyId))
			{
				song.AddedToSpotify = false;
				song.FoundOnSpotify = false;
				song.IsNotOnSpotify = true;
				song.ArtistName = null;
				song.SongName = null;
				song.SpotifyId = null;
				db.SaveChanges();
			}

			db.SaveChanges();
			return new JsonResult(new { success = true });
		}


		#endregion
		#endregion
	}
}