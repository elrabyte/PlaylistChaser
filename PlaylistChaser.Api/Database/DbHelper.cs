using Microsoft.AspNetCore.Mvc;
using PlaylistChaser.Model;
using PlaylistChaser.Model.BuiltInIds;

namespace PlaylistChaser.Api.Database
{
    public class DbHelper
    {
        AdminDBContext db;

        /// <summary>
        /// used by both song and playlist controller, or is needed in one but doesn't fit there
        /// </summary>
        internal DbHelper(AdminDBContext db)
        {
            this.db = db;
        }

        internal ActionResult AddFoundSongToDb(int songId, string songName, string artistName, SourceId source, string songIdSource, string url, SongStates stateId = SongStates.Available)
        {
            try
            {
                if (db.SongInfo.Any(i => i.SongId == songId && i.SourceId == source))
                    return new OkObjectResult(new { message = "A songInfo already exists for that source" });

                if (db.SongInfo.Any(i => i.SongIdSource == songIdSource && i.SourceId == source))
                    return new OkObjectResult(new { message = "There's already a songinfo with that SongIdSource" });

                //add song info
                var newSongInfo = new SongInfo { SongId = songId, SourceId = source, SongIdSource = songIdSource, Name = songName, ArtistName = artistName, Url = url };
                db.SongInfo.Add(newSongInfo);

                //add song state
                var newSongState = new SongState { SongId = songId, SourceId = source, StateId = stateId, LastChecked = DateTime.Now };
                db.SongState.AddRange(newSongState);

                db.SaveChanges();
                return new OkResult();
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new { message = ex.Message });
            }
        }

        public List<Song> AddSongsToDb(List<SongInfo> songsFromPlaylist)
        {
            var addedSongs = new List<Song>();

            //check if songs are already in db
            //TODO: for now only check if it wasnt added from same source
            //      on youtube songname & artist name dont have to be a unique combination
            //      fuck you anguish with your stupid ass song titles


            //remove duplicates
            songsFromPlaylist = songsFromPlaylist.DistinctBy(i => i.SongIdSource).ToList();

            //add song
            foreach (var newSong in songsFromPlaylist)
            {
                //skip already added
                if (db.SongInfo.Any(s => s.SourceId == newSong.SourceId && s.SongIdSource == newSong.SongIdSource))
                    continue;

                var success = InsertSong(newSong.Name, newSong.ArtistName, newSong.SourceId, newSong.SongIdSource, newSong.Url);
            };

            return addedSongs;
        }

        private ActionResult InsertSong(string songName, string artistName, SourceId source, string songIdSource, string url)
        {
            try
            {
                //add song
                var newSong = new Song { SongName = songName, ArtistName = artistName };
                db.Song.Add(newSong);
                db.SaveChanges();

                //add song info
                var newSongInfo = new SongInfo { SongId = newSong.Id, SourceId = source, SongIdSource = songIdSource, Name = songName, ArtistName = artistName, Url = url };
                db.SongInfo.Add(newSongInfo);

                //add song state
                var newSongState = new SongState { SongId = newSong.Id, SourceId = source, StateId = SongStates.Available, LastChecked = DateTime.UtcNow };
                db.SongState.AddRange(newSongState);

                db.SaveChanges();

                return new OkResult();
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new { message = ex.Message });
            }
        }

        public void AddSongsToLocalPlaylist(SourceId sourceId, int playlistId, List<Song> songsToAdd)
        {
            //only add new songs
            var curSongIds = db.PlaylistSong.Where(ps => ps.PlaylistId == playlistId).Select(ps => ps.SongId).ToList();
            var newSongIds = songsToAdd.Select(s => s.Id).Where(i => !curSongIds.Contains(i)).ToList();

            var newPlaylistSongs = newSongIds.Select(i => new PlaylistSong { PlaylistId = playlistId, SongId = i }).ToList();
            InsertPlaylistSongs(sourceId, newPlaylistSongs);
        }

        public void InsertPlaylistSongs(SourceId sourceId, List<PlaylistSong> playlistSongs)
        {
            db.PlaylistSong.AddRange(playlistSongs);
            db.SaveChanges();

            playlistSongs.ForEach(ps =>
            {
                db.PlaylistSongState.Add(new PlaylistSongState { PlaylistSongId = ps.Id, SourceId = sourceId, StateId = PlaylistSongStates.Added, LastChecked = DateTime.Now });
            });
            db.SaveChanges();
        }
    }
}
