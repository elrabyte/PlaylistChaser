﻿using PlaylistChaser.Web.Models;
using SpotifyAPI.Web;
using System.Text.RegularExpressions;

namespace PlaylistChaser.Web.Util.API
{
    internal class SpotifyApiHelper : ISource
    {
        private SpotifyClient spotify;
        private const string spotifyAccessTokenKey = "spotifyAccessToken";
        private const string redirectUri = "https://localhost:7245/Login/LoginToSpotify";

        internal SpotifyApiHelper(HttpContext context)
        {
            var accessToken = context.Session.GetString(spotifyAccessTokenKey);
            if (accessToken == null)
                throw new Exception("Not logged in yet");

            spotify = new SpotifyClient(accessToken);
        }

        #region Authenticate
        internal SpotifyApiHelper(HttpContext context, string code = null)
        {
            var clientId = Helper.ReadSecret("Spotify", "ClientId");
            var clientSecret = Helper.ReadSecret("Spotify", "ClientSecret");

            var accessToken = new OAuthClient().RequestToken(new AuthorizationCodeTokenRequest(clientId, clientSecret, code, new Uri(redirectUri))).Result.AccessToken;
            context.Session.SetString(spotifyAccessTokenKey, accessToken);

            spotify = new SpotifyClient(accessToken);
        }

        public static Uri getLoginUri()
        {
            var clientId = Helper.ReadSecret("Spotify", "ClientId");
            var loginRequest = new LoginRequest(new Uri(redirectUri), clientId, LoginRequest.ResponseType.Code)
            {
                Scope = new[] { Scopes.PlaylistModifyPrivate, Scopes.PlaylistModifyPublic, Scopes.UserReadPrivate }
            };
            return loginRequest.ToUri();
        }

        #endregion

        #region Queries

        #region Song
        public (List<(int Id, string IdAtSource)> Exact, List<(int Id, string IdAtSource)> NotExact) FindSongs(List<(int SongId, string ArtistName, string SongName)> songs)
        {
            var foundSongsExact = new List<(int Id, string SpotifyId)>();
            var foundSongs = new List<(int Id, string SpotifyId)>();
            try
            {
                foreach (var song in songs)
                {
                    var spotifySong = searchSongExact(SearchRequest.Types.Track, song.ArtistName, song.SongName).Result;
                    if (spotifySong != null)
                        foundSongsExact.Add(new(song.SongId, spotifySong.Id));
                    else
                    {
                        spotifySong = searchSong(SearchRequest.Types.Track, song.SongName).Result;
                        if (spotifySong != null)
                            foundSongs.Add(new(song.SongId, spotifySong.Id));
                    }
                }
                return (foundSongsExact, foundSongs);
            }
            catch (Exception ex)
            {
                return (foundSongsExact, foundSongs);
            }
        }

        private async Task<FullTrack> searchSongExact(SearchRequest.Types type, string artistName, string songName)
        {
            var query = string.Format("artist:\"{0}\" track:\"{1}\"", artistName, songName);
            var searchRequest = new SearchRequest(type, query);

            var response = await spotify.Search.Item(searchRequest);
            if (response.Tracks.Items?.Count == 1)
                return response.Tracks.Items.Single();
            else
                return null;
        }
        private async Task<FullTrack> searchSong(SearchRequest.Types type, string songName)
        {
            var searchRequest = new SearchRequest(type, songName);

            var response = await spotify.Search.Item(searchRequest);
            if (response.Tracks.Items?.Count >= 1)
                return response.Tracks.Items.First();
            else
                return null;
        }
        public async Task<FullTrack> GetSong(string spotifySongId)
        {
            return await spotify.Tracks.Get(spotifySongId);
        }
        #endregion

        #region Playlist
        private async Task<FullPlaylist> createPlaylist(string playlistName, string? description = null, bool isPublic = true)
        {
            var request = new PlaylistCreateRequest(playlistName);
            request.Public = isPublic;
            request.Description = description;
            var userId = (await spotify.UserProfile.Current()).Id;
            return await spotify.Playlists.Create(userId, request);
        }
        public async Task<bool> UpdatePlaylist(string spotifyPlaylistId, List<string> spotifySongIds, string playlistDescription = null)
        {
            //can add max. 100 songs per request
            var rounds = Math.Ceiling(spotifySongIds.Count / 100d);
            for (int i = 0; i < rounds; i++)
                await spotify.Playlists.AddItems(spotifyPlaylistId, new PlaylistAddItemsRequest(spotifySongIds.Skip(i * 100).Take(100).ToList()));

            //update playlistdescription
            var request = new PlaylistChangeDetailsRequest();
            request.Description = playlistDescription;

            return await spotify.Playlists.ChangeDetails(spotifyPlaylistId, request);
        }

        public async Task<bool> DeletePlaylist(string plalyistId)
        {
            //only sets private for the moment. couldnt find api            
            var request = new PlaylistChangeDetailsRequest();
            request.Public = false;

            return await spotify.Playlists.ChangeDetails(plalyistId, request);
        }
        #endregion

        #region PlaylistSong
        public async Task<bool> RemovePlaylistSong(string spotifyPlaylistId, string spotifySongId)
        {
            return await removePlaylistSongs(spotifyPlaylistId, new List<string> { spotifySongId });
        }
        private async Task<bool> removePlaylistSongs(string spotifyPlaylistId, List<string> spotifySongIds)
        {
            //can add max. 100 songs per request
            var rounds = Math.Ceiling(spotifySongIds.Count / 100d);
            for (int i = 0; i < rounds; i++)
                await spotify.Playlists.RemoveItems(spotifyPlaylistId, new PlaylistRemoveItemsRequest
                {
                    Tracks = spotifySongIds.Skip(i * 100)
                                           .Take(100)
                                           .Select(s => new PlaylistRemoveItemsRequest.Item { Uri = s })
                                           .ToList()
                });

            return true;
        }

        #endregion
        #endregion

        PlaylistAdditionalInfo ISource.SyncPlaylistInfo(PlaylistAdditionalInfo info)
        {
            throw new NotImplementedException();
        }

        public async Task<PlaylistAdditionalInfo> CreatePlaylist(string playlistName, string? description, bool isPublic = true)
            => toPlaylistModel(await createPlaylist(playlistName, description, isPublic));

        Task<bool> ISource.DeletePlaylist(string youtubePlaylistId)
        {
            throw new NotImplementedException();
        }

        public List<SongAdditionalInfo> GetPlaylistSongs(string playlistId)
        => toSongModels(getPlaylistSongs(playlistId));

        private List<FullTrack> getPlaylistSongs(string playlistId)
        {
            var listRequest = spotify.Playlists.GetItems(playlistId).Result;

            var totalResults = listRequest.Total;
            var resultsShown = listRequest.Limit;

            var songs = listRequest.Items.Select(i => (FullTrack)i.Track).ToList();
            if (totalResults == 0)
                return songs;

            while (resultsShown <= totalResults)
            {
                listRequest = spotify.Playlists.GetItems(playlistId, new PlaylistGetItemsRequest { Offset = resultsShown }).Result;


                resultsShown += listRequest.Total;
                songs.AddRange(listRequest.Items.Select(i => (FullTrack)i.Track).ToList());
            }
            return songs;
        }
        private List<SongAdditionalInfo> toSongModels(List<FullTrack> songs)
        {
            return songs.Select(s => new SongAdditionalInfo
            {
                SongIdSource = s.Id,
                Name = s.Name,
                ArtistName = string.Join(',', s.Artists.Select(a => a.Name).ToList()),
                SourceId = BuiltInIds.Sources.Spotify
            }).ToList();

        }

        public async Task<byte[]> GetPlaylistThumbnail(string id)
        {
            var playlist = await spotify.Playlists.Get(id);
            var thumbnail = playlist.Images.OrderByDescending(i => i.Height).FirstOrDefault();

            return thumbnail != null ? await Helper.GetImageByUrl(thumbnail.Url) : null;
        }

        public async Task<Dictionary<string, byte[]>> GetSongsThumbnailBySongIds(List<string> songIds)
        {
            //throw new NotImplementedException();
            var songThumbnails = new Dictionary<string, byte[]>();

            var songs = getSongs(songIds);

            foreach (var song in songs)
            {
                if (!songThumbnails.ContainsKey(song.Id))
                    songThumbnails.Add(song.Id, await Helper.GetImageByUrl(song.Album.Images.OrderBy(i => i.Height).First().Url));
            }
            return songThumbnails;
        }

        private List<FullTrack> getSongs(List<string> songIds)
        {
            const int requestLimit = 50;
            var songs = new List<FullTrack>();

            //split request 
            for (var i = 0; i <= songIds.Count; i += requestLimit)
            {
                var rangeCount = requestLimit;
                //if rangeCount exceeds maxResults, calc rest count
                if (i + requestLimit > songIds.Count)
                    rangeCount = songIds.Count - i;

                var curSongIds = songIds.GetRange(i, rangeCount);
                songs.AddRange(spotify.Tracks.GetSeveral(new TracksRequest(curSongIds)).Result.Tracks);
            }
            return songs;
        }

        public List<string> AddSongsToPlaylist(string playlistId, List<string> songIds)
        {
            var uploadedSongs = new List<string>();
            //can add max. 100 songs per request
            var rounds = Math.Ceiling(songIds.Count / 100d);
            for (int i = 0; i < rounds; i++)
            {
                var curSongids = songIds.Skip(i * 100).Take(100);
                var trackUris = curSongids.Select(i => string.Format("spotify:track:{0}", i)).ToList();
                var response = spotify.Playlists.AddItems(playlistId, new PlaylistAddItemsRequest(trackUris)).Result;
                uploadedSongs.AddRange(curSongids);
            }
            return uploadedSongs;
        }

        public PlaylistAdditionalInfo GetPlaylistById(string playlistId)
            => toPlaylistModel(spotify.Playlists.Get(playlistId).Result);

        #region model 
        private PlaylistAdditionalInfo toPlaylistModel(FullPlaylist spotifyPlaylist)
        {
            var info = new PlaylistAdditionalInfo
            {
                Name = spotifyPlaylist.Name,
                CreatorName = spotifyPlaylist.Owner.DisplayName,
                Description = string.IsNullOrEmpty(spotifyPlaylist.Description) ? null : spotifyPlaylist.Description,
                PlaylistIdSource = spotifyPlaylist.Id,
                SourceId = BuiltInIds.Sources.Spotify,
                IsMine = false
            };
            return info;
        }

        #endregion

        #region helper
        internal string GetPlaylistIdFromUrl(string url)
        {
            var pattern = @"playlist/(\w+)";
            Regex rg = new Regex(pattern);
            var match = rg.Match(url);
            return match.Groups[1].Value;
        }
        #endregion
    }
}
