using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlaylistChaser.Api.Database;
using PlaylistChaser.Api.Util;
using PlaylistChaser.Core.Sources;
using PlaylistChaser.Model;
using PlaylistChaser.Model.BuiltInIds;
using PlaylistChaser.Model.SearchModel;
using PlaylistChaser.Model.ViewModel;

namespace PlaylistChaser.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlaylistController : ControllerBase
    {
        private readonly DbHelper dbHelper;
        private IDestination destinationApiHelper(SourceId sourceId, string accessCode) => SourcesHelper.GetApiHelper<IDestination>(sourceId, accessCode);
        private ISource sourceApiHelper(SourceId sourceId, string accessCode) => SourcesHelper.GetApiHelper<ISource>(sourceId, accessCode);

        public PlaylistController(AdminDBContext adminDBContext)
        {
            dbHelper = new DbHelper(adminDBContext);
        }

        [HttpGet]
        [Route("get-all-playlists")]
        public ActionResult<IEnumerable<Playlist>> GetPlaylists()
        {
            var playlists = dbHelper.GetPlaylists();
            return playlists;
        }

        [HttpGet("{id}")]
        public ActionResult<Playlist> GetPlaylist(int id)
        {
            var playlist = dbHelper.GetPlaylist(id);
            if (playlist == null) return NotFound();

            return playlist;
        }
        [HttpGet]
        [Route("get-playlist-populated/{id}")]
        public ActionResult<PlaylistPopulated> GetPlaylistPopulated(int id)
        {
            return dbHelper.GetPlaylistPopulated(id);
        }

        [HttpPut]
        [Route("remove-playlist/{id}")]
        public ActionResult RemovePlaylist(int id)
        {
            try
            {
                dbHelper.RemovePlaylist(id);
                return Ok();
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new { message = ex.Message });
            }
        }
        [HttpPut]
        [Route("remove-playlists")]
        public ActionResult RemovePlaylists([FromBody] List<int> ids)
        {
            try
            {
                dbHelper.RemovePlaylists(ids);
                return Ok();
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new { message = ex.Message });
            }
        }

        [HttpGet]
        [Route("get-thumbnail/{playlistId}")]
        public ActionResult GetThumbnail(int playlistId)
        {
            var fileContents = dbHelper.GetThumbnail(playlistId);

            return fileContents != null ? File(fileContents, "image/jpeg") : null;
        }
        [HttpPost]
        [Route("sync-from-origin/{playlistId}")]
        public async Task<ActionResult> SyncPlaylistFromOrigin(int playlistId)
        {

            var playlist = dbHelper.GetPlaylist(playlistId);
            var originSourceId = playlist.OriginSourceId;
            var accessToken = dbHelper.GetAccessToken(originSourceId);
            var playlistInfo = dbHelper.GetPlaylistInfo(originSourceId, playlistId);
            var apiHelper = sourceApiHelper(originSourceId, accessToken);
            var songInfos = apiHelper.GetPlaylistSongs(playlistInfo.PlaylistIdSource);


            //add songs to db            
            dbHelper.AddSongsToDb(songInfos);

            //get local variants from songs at source
            var songSourceIds = songInfos.Select(i => i.SongIdSource).ToList();
            var songIds = songSourceIds.Select(id => dbHelper.adminDBContext.SongInfo.Single(i => i.SourceId == originSourceId && i.SongIdSource == id)).Select(i => i.SongId).ToList();
            var songs = dbHelper.adminDBContext.Song.Where(s => songIds.Contains(s.Id)).ToList();

            dbHelper.AddSongsToLocalPlaylist(originSourceId, playlistId, songs);

            return new OkResult();
        }
        [HttpPost]
        [Route("sync-to-destinations/{playlistId}")]
        public async Task<ActionResult> SyncPlaylistToDestinations(int playlistId)
        {

            var playlist = dbHelper.GetPlaylist(playlistId);

            var sourceIds = Enum.GetValues(typeof(SourceId)).Cast<SourceId>().ToList();
            sourceIds.Remove(playlist.OriginSourceId);

            foreach (var sourceId in sourceIds)
            {
                var accessToken = dbHelper.GetAccessToken(sourceId);
                if (string.IsNullOrEmpty(accessToken)) continue;
                var destinationApiHelper = this.destinationApiHelper(sourceId, accessToken);
                var sourceApiHelper = this.sourceApiHelper(sourceId, accessToken);


                //create info - first time for that source
                var info = dbHelper.GetPlaylistInfo(sourceId, playlistId);
                if (info != null && !info.IsMine)
                    throw new Exception($"No permission to edit playlist at {sourceId.ToString()}");


                //create playlist if doesn't exists
                if (string.IsNullOrEmpty(info?.PlaylistIdSource))
                {
                    var newInfo = await destinationApiHelper.CreatePlaylist(playlist.Name, playlist.Description);


                    string descriptionText = "Copied from $OriginalSource$: \n$OriginalPlaylistName$ by $OriginalCreatorName$. \n$SongsUploaded$ / $SongsTotal$ - $LastChangeDate$";
                    newInfo.Description = descriptionText;
                    info = dbHelper.AddPlaylistInfo(sourceId, playlistId, newInfo);
                }

                //get missing songs
                var missingSongs = dbHelper.GetMissingSongs(sourceId, playlistId);
                if (!missingSongs.Any())
                    return new OkObjectResult(new { message = "Already up to date!" });


                var foundSongs = await FindSongs(sourceApiHelper, playlistId);

                //add songs to playlist
                //  prepare
                var songsToUpload = missingSongs.Select(s => new UploadSong
                {
                    SongId = s.Id,
                    SongIdSource = dbHelper.GetSongInfo(sourceId, s.Id)?.SongIdSource
                }).ToList();
                songsToUpload = songsToUpload.Where(s => !string.IsNullOrEmpty(s.SongIdSource)).ToList();
                songsToUpload = songsToUpload.DistinctBy(s => s.SongIdSource).ToList();

                //  upload
                List<UploadSong> uploadedSongs;
                if (songsToUpload.Any())
                    uploadedSongs = await UploadSongsToPlaylist(destinationApiHelper, info.PlaylistIdSource, songsToUpload);

                ////update Playlist
                //var playlistDescription = getPlaylistDescriptionText(info);
                //returnObj = await updatePlaylist(source, info.PlaylistIdSource, info.Name, playlistDescription);
                //if (!returnObj.Success)
                //    return returnObj;

            }

            return new OkResult();
        }

        private async Task<List<FoundSong>> FindSongs(ISource source, int playlistId)
        {
            //var toastId = GetToastId();
            //await progressHub.InitProgressToast("Find Songs", toastId, true);

            //only songs that weren't checked before
            var missingSongsList = dbHelper.GetSongsMissingSongsNotChecked(source.SourceId, playlistId);

            if (!missingSongsList.Any())
            {
                //await progressHub.EndProgressToast(toastId);
                return null;
            }

            //check if songs exists
            var findSongs = missingSongsList.Select(s => new FindSong(s.Id, s.ArtistName, s.SongName)).ToList();

            int nFound = 0;
            int nSkipped = 0;
            var foundSongs = new List<FoundSong>();
            FoundSong foundSong;
            var timeElapsedList = new List<int>();

            //var dbHelper = new DbHelper(adminDBContext);
            var accessToken = dbHelper.GetAccessToken(source.SourceId);

            foreach (var findSong in findSongs)
            {
                //if (IsCancelled(toastId, out var startTime)) break;

                foundSong = source.FindSong(findSong);

                var newSongInfo = foundSong.NewSongInfo;
                var stateId = SongStates.Available;
                if (newSongInfo.ArtistName == "NotAvailable")
                    stateId = SongStates.NotAvailable;

                var returnObj = dbHelper.AddFoundSongToDb(newSongInfo.SongId, newSongInfo.Name, newSongInfo.ArtistName, newSongInfo.SourceId, newSongInfo.SongIdSource, newSongInfo.Url, stateId);

                //var msgDisplay = ToastMessageDisplay(returnObj.Success, findSongs.Count, startTime, ref timeElapsedList, ref nFound, ref nSkipped);

                //await progressHub.UpdateProgressToast("Finding songs...", nFound, findSongs.Count, msgDisplay, toastId, true);

                foundSongs.Add(foundSong);
            }
            //await progressHub.EndProgressToast(toastId);

            return foundSongs;
        }

        private async Task<List<UploadSong>> UploadSongsToPlaylist(IDestination destinationApiHelper, string playlistIdSource, List<UploadSong> songsToUpload)
        {
            var uploadedSongs = new List<UploadSong>();

            if (!songsToUpload.Any())
                throw new Exception("List can't be empty");

            if (songsToUpload.Any(s => string.IsNullOrEmpty(s.SongIdSource)))
                throw new Exception("SongIdSource can't be null");

            if (songsToUpload.GroupBy(x => x.SongIdSource).Where(x => x.Count() > 1).Any())
                throw new Exception("There are duplicates");

            var songStates = dbHelper.adminDBContext.SongState.Where(i => i.SourceId == destinationApiHelper.SourceId && songsToUpload.Select(s => s.SongId).Contains(i.SongId)).ToList();
            if (songStates.Any(i => !(i.StateId == SongStates.Available)))
                throw new Exception("All songs must be available");

            //var toastId = GetToastId();
            //await progressHub.InitProgressToast("Add songs to playlist", toastId, true);
            //var timeElapsedList = new List<int>();
            //int nAdded = 0;
            //int nSkipped = 0;

            foreach (var song in songsToUpload)
            {
                //if (IsCancelled(toastId, out var startTime)) break;

                var success = destinationApiHelper.AddSongToPlaylist(playlistIdSource, song.SongIdSource);
                dbHelper.UpdatePlaylistSongState(destinationApiHelper.SourceId, playlistIdSource, song.SongIdSource, success);
                uploadedSongs.Add(new UploadSong { SongId = song.SongId, SongIdSource = song.SongIdSource, Uploaded = success });
                //var msgDisplay = ToastMessageDisplay(returnObj.Success, songsToUpload.Count, startTime, ref timeElapsedList, ref nAdded, ref nSkipped, "{0} / {1} uploaded");

                //await progressHub.UpdateProgressToast("Uploading songs to playlist...", nAdded, songsToUpload.Count, msgDisplay, toastId, true);
            }

            //await progressHub.EndProgressToast(toastId);
            return uploadedSongs;
        }
    }
}
