using PlaylistChaser.Web.Database;
using PlaylistChaser.Web.Models;
using static PlaylistChaser.Web.Util.BuiltInIds;

namespace PlaylistChaser.Web.Util
{
    internal class DbHelper
    {
        UserDbContext db;

        /// <summary>
        /// used by both song and playlist controller, or is needed in one but doesn't fit there
        /// </summary>
        internal DbHelper(UserDbContext db)
        {
            this.db = db;
        }

        internal ReturnModel AddFoundSongToDb(int songId, string songName, string artistName, Sources source, string songIdSource, string url, SongStates stateId = SongStates.Available)
        {
            try
            {
                if (db.GetCachedList(db.SongInfo).Any(i => i.SongId == songId && i.SourceId == source))
                    return new ReturnModel("A songInfo already exists for that source");

                if (db.GetCachedList(db.SongInfo).Any(i => i.SongIdSource == songIdSource && i.SourceId == source))
                    return new ReturnModel("There's already a songinfo with that SongIdSource");

                //add song info
                var newSongInfo = new SongInfo { SongId = songId, SourceId = source, SongIdSource = songIdSource, Name = songName, ArtistName = artistName, Url = url };
                db.SongInfo.Add(newSongInfo);

                //add song state
                var newSongState = new SongState { SongId = songId, SourceId = source, StateId = stateId, LastChecked = DateTime.Now };
                db.SongState.AddRange(newSongState);

                db.SaveChanges();
                return new ReturnModel();
            }
            catch (Exception ex)
            {
                return new ReturnModel(ex.Message);
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
                if (db.GetCachedList(db.SongInfo).Any(s => s.SourceId == newSong.SourceId && s.SongIdSource == newSong.SongIdSource))
                    continue;

                var success = addSongs(newSong.Name, newSong.ArtistName, newSong.SourceId, newSong.SongIdSource, newSong.Url);
            };

            return addedSongs;
        }

        private ReturnModel addSongs(string songName, string artistName, Sources source, string songIdSource, string url)
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
                var newSongState = new SongState { SongId = newSong.Id, SourceId = source, StateId = SongStates.Available, LastChecked = DateTime.Now };
                db.SongState.AddRange(newSongState);

                db.SaveChanges();

                return new ReturnModel();
            }
            catch (Exception ex)
            {
                return new ReturnModel(ex.Message);
            }
        }

    }
}
