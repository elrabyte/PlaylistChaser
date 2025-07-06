using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlaylistChaser.Model;
using PlaylistChaser.Model.BuiltInIds;
using PlaylistChaser.Model.ViewModel;

namespace PlaylistChaser.Api.Database
{
    public class DbHelper
    {
        AdminDBContext db;
        internal AdminDBContext adminDBContext => db;

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

            var playlistInfo = GetPlaylistInfo(sourceId, playlistId);
            playlistInfo.LastSynced = DateTime.UtcNow;
            db.SaveChanges();
        }

        public void InsertPlaylistSongs(SourceId sourceId, List<PlaylistSong> playlistSongs)
        {
            db.PlaylistSong.AddRange(playlistSongs);
            db.SaveChanges();

            playlistSongs.ForEach(ps =>
            {
                db.PlaylistSongState.Add(new PlaylistSongState { PlaylistSongId = ps.Id, SourceId = sourceId, StateId = PlaylistSongStates.Added, LastChecked = DateTime.UtcNow });
            });
            db.SaveChanges();
        }

        public OAuth2Credential GetOauth(SourceId sourceId)
        {
            return db.OAuth2Credential.SingleOrDefault(oau => oau.UserId == 1 && oau.Provider == sourceId.ToString());
        }
        public string GetAccessToken(SourceId sourceId)
        {
            var oAuth = GetOauth(sourceId);
            return oAuth?.AccessToken;
        }

        public void AddPlaylist(SourceId sourceId, PlaylistInfo playlistInfo, Thumbnail thumbnail)
        {
            db.Thumbnail.Add(thumbnail);
            var playlist = new Playlist
            {
                Name = playlistInfo.Name,
                ChannelName = playlistInfo.CreatorName,
                PlaylistTypeId = PlaylistTypes.Simple,
                Description = playlistInfo.Description,
                Thumbnail = thumbnail,
                OriginSourceId = sourceId,
                UserId = 1
            };
            db.Playlist.Add(playlist);
            playlistInfo.Playlist = playlist;

            db.SaveChanges();
        }

        public PlaylistInfo AddPlaylistInfo(SourceId sourceId, int playlistId, PlaylistInfo playlistInfo)
        {
            db.PlaylistInfo.Add(playlistInfo);
            db.SaveChanges();
            return playlistInfo;
        }
        public List<Song> GetMissingSongs(SourceId sourceId, int playlistId)
        {
            var songs = db.Song;
            var songStates = db.SongState;
            var playlistSongStates = db.PlaylistSongState;

            //get songs that should be in playlist
            var playlistSongs = db.PlaylistSong.Where(ps => ps.PlaylistId == playlistId).ToList();

            //get PlaylistSongs with StateId NotAdded or no PlaylistSongState-Entry
            var notAddedPlaylistSongs = playlistSongs.Where(ps => !playlistSongStates.Where(pss => pss.SourceId == sourceId && pss.PlaylistSongId == ps.Id).Any()
                                                                  || playlistSongStates.Single(pss => pss.SourceId == sourceId && pss.PlaylistSongId == ps.Id).StateId == PlaylistSongStates.NotAdded).ToList();

            //get songs from playlistsongs
            var notAddedSongIds = notAddedPlaylistSongs.Select(pss => pss.SongId).ToList();
            var notAddedSongs = songs.Where(s => notAddedSongIds.Contains(s.Id)).ToList();

            //ignore unavailable songs
            notAddedSongs = notAddedSongs.Where(s => songStates.SingleOrDefault(ss => ss.SourceId == sourceId && ss.SongId == s.Id)?.StateId != SongStates.NotAvailable).ToList();

            return notAddedSongs;
        }
        public List<Song> GetSongsMissingSongsNotChecked(SourceId sourceId, int playlistId)
        {
            var missingSongs = GetMissingSongs(sourceId, playlistId);
            var missingSongsList = missingSongs.Where(s => db.SongState.SingleOrDefault(ss => ss.SongId == s.Id && ss.SourceId == sourceId) == null
                                                           || db.SongState.Single(ss => ss.SongId == s.Id && ss.SourceId == sourceId).StateId == SongStates.NotChecked);
            return missingSongsList.ToList();
        }

        public void UpdatePlaylistSongState(SourceId sourceId, string playlistIdSource, string songIdSource, bool success)
        {
            var playlistId = db.PlaylistInfo.Single(i => i.SourceId == sourceId && i.PlaylistIdSource == playlistIdSource).PlaylistId;
            var songId = db.SongInfo.Single(s => s.SourceId == sourceId && s.SongIdSource == songIdSource).SongId;
            var playlistSongId = db.PlaylistSong.Single(ps => ps.PlaylistId == playlistId && ps.SongId == songId).Id;
            var stateId = (success ? PlaylistSongStates.Added : PlaylistSongStates.NotAdded);
            UpdatePlaylistSongState(sourceId, playlistSongId, stateId);
        }
        private void UpdatePlaylistSongState(SourceId sourceId, int playlistSongId, PlaylistSongStates stateId)
        {
            var playlistSongState = db.PlaylistSongState.SingleOrDefault(pss => pss.SourceId == sourceId && pss.PlaylistSongId == playlistSongId);
            if (playlistSongState == null)
            {
                playlistSongState = new PlaylistSongState { PlaylistSongId = playlistSongId, SourceId = sourceId, StateId = stateId, LastChecked = DateTime.Now };
                db.PlaylistSongState.Add(playlistSongState);
            }
            else
            {
                playlistSongState.StateId = stateId;
                playlistSongState.LastChecked = DateTime.Now;
            }
            db.SaveChanges();
        }

        public List<Playlist> GetPlaylists()
        {
            return db.Playlist.ToList();
        }

        public Playlist GetPlaylist(int playlistId)
        {
            return db.Playlist.SingleOrDefault(playlist => playlist.Id == playlistId);
        }
        public PlaylistPopulated GetPlaylistPopulated(int playlistId)
        {
            var playlistPopulated = new PlaylistPopulated
            {
                Playlist = db.Playlist.Include(p => p.Thumbnail).SingleOrDefault(playlist => playlist.Id == playlistId),
                Songs = db.PlaylistSong.Include(ps => ps.Song).Where(ps => ps.PlaylistId == playlistId).Select(ps => ps.Song).ToList()
            };
            return playlistPopulated;
        }

        public void RemovePlaylist(int playlistId)
        {
            var playlist = db.Playlist.Include(p => p.Thumbnail).Single(playlist => playlist.Id == playlistId);
            db.Thumbnail.Remove(playlist.Thumbnail);
            db.Playlist.Remove(playlist);

            var playlistInfos = db.PlaylistInfo.Where(i => i.PlaylistId == playlistId);
            db.PlaylistInfo.RemoveRange(playlistInfos);

            db.SaveChanges();
        }

        public void RemovePlaylists(List<int> playlistIds)
        {
            var playlists = db.Playlist.Include(p => p.Thumbnail).Where(p => playlistIds.Contains(p.Id));
            var thumbnails = playlists.Select(p => p.Thumbnail);
            var playlistInfos = db.PlaylistInfo.Where(i => playlistIds.Contains(i.PlaylistId));
            db.Playlist.RemoveRange(playlists);
            db.Thumbnail.RemoveRange(thumbnails);
            db.PlaylistInfo.RemoveRange(playlistInfos);

            db.SaveChanges();
        }

        public byte[] GetThumbnail(int playlistId)
        {
            var playlist = db.Playlist.Include(p => p.Thumbnail).Single(p => p.Id == playlistId);
            var fileContents = playlist?.Thumbnail?.FileContents;
            return fileContents;
        }

        public PlaylistInfo GetPlaylistInfo(SourceId sourceId, int playlistId)
        {
            return db.PlaylistInfo.Single(p => p.SourceId == sourceId && p.PlaylistId == playlistId);
        }

        public SongInfo GetSongInfo(SourceId sourceId, int songId)
        {
            return db.SongInfo.SingleOrDefault(i => i.SourceId == sourceId && i.SongId == songId);
        }

        public void UpdateOAuthCredential(OAuth2Credential existingOAuth, OAuth2Credential newOAuth)
        {
            db.Entry(existingOAuth).CurrentValues.SetValues(newOAuth);
            db.SaveChanges();
        }

        public void AddOAuthCredential(OAuth2Credential oAuth)
        {
            db.OAuth2Credential.Add(oAuth);
            db.SaveChanges();
        }
    }
}
