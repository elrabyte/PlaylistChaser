using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using YoutubeExplode;
using YoutubeExplode.Exceptions;

namespace PlaylistChaser.Sources;

/// <summary>
/// Writes and listing go through the official Data API (quota: list = 1 unit, insert = 50 units, 10k/day).
/// Search and single-video lookups go through YoutubeExplode, which uses no quota.
/// </summary>
public class YoutubeSource(string accessToken, string? channelId) : ISource
{
    private readonly YouTubeService api = new(new BaseClientService.Initializer
    {
        HttpClientInitializer = GoogleCredential.FromAccessToken(accessToken),
        ApplicationName = "PlaylistChaser",
    });
    private static readonly YoutubeClient Explode = new();

    public int MaxBatch => 1;

    public async Task<RemotePlaylist> GetPlaylist(string id)
    {
        if (id == SourceUrls.YoutubeLiked) return Liked;
        var request = api.Playlists.List("snippet");
        request.Id = id;
        var playlist = (await Call(() => request.ExecuteAsync())).Items?.SingleOrDefault()
                       ?? throw new InvalidOperationException("YouTube playlist not found (is it private?).");
        var t = playlist.Snippet.Thumbnails;
        var thumb = t?.Maxres ?? t?.High ?? t?.Medium ?? t?.Standard ?? t?.Default__;
        return new RemotePlaylist(playlist.Id, playlist.Snippet.Title, NullIfEmpty(playlist.Snippet.Description),
            playlist.Snippet.ChannelTitle, thumb?.Url, playlist.Snippet.ChannelId == channelId);
    }

    // the API returns no metadata for LL, and it can't be written to like a normal playlist
    private static readonly RemotePlaylist Liked = new(SourceUrls.YoutubeLiked, "Liked videos", null, null, null, false);

    public async Task<List<RemotePlaylist>> GetMyPlaylists()
    {
        var playlists = new List<RemotePlaylist> { Liked };
        var request = api.Playlists.List("snippet");
        request.Mine = true;
        request.MaxResults = 50;
        do
        {
            var page = await Call(() => request.ExecuteAsync());
            playlists.AddRange(page.Items.Select(p => new RemotePlaylist(p.Id, p.Snippet.Title, NullIfEmpty(p.Snippet.Description),
                p.Snippet.ChannelTitle, p.Snippet.Thumbnails?.Medium?.Url ?? p.Snippet.Thumbnails?.Default__?.Url, true)));
            request.PageToken = page.NextPageToken;
        } while (request.PageToken != null);
        return playlists;
    }

    public async Task<List<RemoteTrack>> GetTracks(string playlistId)
    {
        var tracks = new List<RemoteTrack>();
        var request = api.PlaylistItems.List("snippet");
        request.PlaylistId = playlistId;
        request.MaxResults = 50;
        do
        {
            var page = await Call(() => request.ExecuteAsync());
            // deleted/private videos have no owner channel
            tracks.AddRange(page.Items
                .Where(i => i.Snippet.VideoOwnerChannelTitle != null)
                .Select(i => new RemoteTrack(i.Snippet.ResourceId.VideoId, i.Snippet.Title,
                    CleanArtist(i.Snippet.VideoOwnerChannelTitle),
                    i.Snippet.Thumbnails?.Medium?.Url ?? i.Snippet.Thumbnails?.Default__?.Url)));
            request.PageToken = page.NextPageToken;
        } while (request.PageToken != null);
        return tracks;
    }

    public async Task<RemoteTrack?> GetTrack(string id)
    {
        try
        {
            var video = await Explode.Videos.GetAsync(id);
            return new RemoteTrack(video.Id, video.Title, CleanArtist(video.Author.ChannelTitle),
                video.Thumbnails.OrderBy(t => t.Resolution.Area).FirstOrDefault()?.Url);
        }
        catch (VideoUnavailableException) { return null; }
    }

    public async Task<RemoteTrack?> Search(string name, string artist)
    {
        await foreach (var video in Explode.Search.GetVideosAsync($"{artist} {name}"))
            return new RemoteTrack(video.Id, video.Title, CleanArtist(video.Author.ChannelTitle),
                video.Thumbnails.OrderBy(t => t.Resolution.Area).FirstOrDefault()?.Url);
        return null;
    }

    public async Task<string> CreatePlaylist(string name, string description)
    {
        var playlist = new Google.Apis.YouTube.v3.Data.Playlist
        {
            Snippet = new PlaylistSnippet { Title = name, Description = CleanDescription(description) },
            Status = new PlaylistStatus { PrivacyStatus = "public" },
        };
        return (await Call(() => api.Playlists.Insert(playlist, "snippet,status").ExecuteAsync())).Id;
    }

    public async Task AddTracks(string playlistId, IReadOnlyList<string> trackIds)
    {
        foreach (var videoId in trackIds)
        {
            var item = new PlaylistItem
            {
                Snippet = new PlaylistItemSnippet
                {
                    PlaylistId = playlistId,
                    ResourceId = new ResourceId { Kind = "youtube#video", VideoId = videoId },
                },
            };
            await Call(() => api.PlaylistItems.Insert(item, "snippet").ExecuteAsync());
        }
    }

    public async Task UpdatePlaylist(string playlistId, string name, string description)
    {
        var request = api.Playlists.List("snippet");
        request.Id = playlistId;
        var playlist = (await Call(() => request.ExecuteAsync())).Items.Single();
        playlist.Snippet.Title = name;
        playlist.Snippet.Description = CleanDescription(description);
        await Call(() => api.Playlists.Update(playlist, "snippet").ExecuteAsync());
    }

    public Task DeletePlaylist(string playlistId) => Call(() => api.Playlists.Delete(playlistId).ExecuteAsync());

    private static async Task<T> Call<T>(Func<Task<T>> action)
    {
        try { return await action(); }
        catch (GoogleApiException ex) when (ex.Error?.Errors?.Any(e => e.Reason is "quotaExceeded" or "rateLimitExceeded") == true)
        {
            throw new QuotaExceededException("YouTube quota reached; the rest continues on the next sync.", ex);
        }
    }

    private static string CleanArtist(string artist) => artist.EndsWith(" - Topic") ? artist[..^8] : artist;
    // YouTube rejects '<' and '>' in descriptions and caps them at 5000 characters.
    private static string CleanDescription(string text) => new string(text.Where(c => c is not ('<' or '>')).Take(5000).ToArray());
    private static string? NullIfEmpty(string? s) => string.IsNullOrEmpty(s) ? null : s;
}
