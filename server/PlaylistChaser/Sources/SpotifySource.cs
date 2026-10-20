using SpotifyAPI.Web;

namespace PlaylistChaser.Sources;

public class SpotifySource(string accessToken, string? spotifyUserId) : IPlaylistDestination
{
    private readonly SpotifyClient api = new(accessToken);

    public int MaxBatch => 100;

    // Liked Songs is a library, not a playlist, so it can't be pushed to
    private static readonly RemotePlaylist Liked = new(SourceUrls.SpotifyLiked, "Liked Songs", null, null, null, false);

    public async Task<RemotePlaylist> GetPlaylist(string id)
    {
        if (id == SourceUrls.SpotifyLiked) return Liked;
        var p = await Call(() => api.Playlists.Get(id));
        return new RemotePlaylist(p.Id!, p.Name ?? "", string.IsNullOrEmpty(p.Description) ? null : p.Description,
            p.Owner?.DisplayName, p.Images?.OrderByDescending(i => i.Height).FirstOrDefault()?.Url,
            p.Owner?.Id == spotifyUserId);
    }

    public async Task<List<RemotePlaylist>> GetMyPlaylists()
    {
        var all = await NeedsScope(async () => await api.PaginateAll(await api.Playlists.CurrentUsers()));
        return all.Where(p => p.Id != null).Select(p => new RemotePlaylist(p.Id!, p.Name ?? "", null, p.Owner?.DisplayName,
                p.Images?.OrderBy(i => i.Height).FirstOrDefault()?.Url, p.Owner?.Id == spotifyUserId))
            .Prepend(Liked).ToList();
    }

    public async Task<List<RemoteTrack>> GetTracks(string playlistId)
    {
        // skips episodes, local files and removed tracks
        if (playlistId == SourceUrls.SpotifyLiked)
        {
            var saved = await NeedsScope(async () => await api.PaginateAll(await api.Library.GetTracks()));
            return saved.Select(s => s.Track).Where(t => t?.Id != null).Select(ToTrack).ToList();
        }
        var items = await Call(async () => await api.PaginateAll(await api.Playlists.GetPlaylistItems(playlistId)));
        return items.Select(i => i.Track).OfType<FullTrack>().Where(t => t.Id != null).Select(ToTrack).ToList();
    }

    /// <summary>Accounts connected before private playlists/liked songs were supported lack the read scopes.</summary>
    private static async Task<T> NeedsScope<T>(Func<Task<T>> action)
    {
        try { return await Call(action); }
        catch (APIException ex) when (ex.Response?.StatusCode is System.Net.HttpStatusCode.Forbidden or System.Net.HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException("Spotify needs one more permission for this. Reconnect Spotify in Settings, then try again.", ex);
        }
    }

    public async Task<RemoteTrack?> GetTrack(string id)
    {
        try { return ToTrack(await Call(() => api.Tracks.Get(id))); }
        catch (APIException ex) when (ex.Response?.StatusCode is System.Net.HttpStatusCode.NotFound or System.Net.HttpStatusCode.BadRequest) { return null; }
    }

    public async Task<RemoteTrack?> Search(string name, string artist)
    {
        // exact artist + title first; accept only an unambiguous hit
        var exact = await Call(() => api.Search.Item(new SearchRequest(SearchRequest.Types.Track, $"artist:\"{artist}\" track:\"{name}\"")));
        if (exact.Tracks.Items?.Count == 1)
            return ToTrack(exact.Tracks.Items[0]);

        var loose = await Call(() => api.Search.Item(new SearchRequest(SearchRequest.Types.Track, name)));
        return loose.Tracks.Items?.FirstOrDefault() is { } t ? ToTrack(t) : null;
    }

    public async Task<string> CreatePlaylist(string name, string description)
    {
        var created = await Call(() => api.Playlists.Create(new PlaylistCreateRequest(name)
        {
            Public = true,
            Description = CleanDescription(description),
        }));
        return created.Id!;
    }

    public Task AddTracks(string playlistId, IReadOnlyList<string> trackIds) =>
        Call(() => api.Playlists.AddPlaylistItems(playlistId, new PlaylistAddItemsRequest(trackIds.Select(id => $"spotify:track:{id}").ToList())));

    public Task UpdatePlaylist(string playlistId, string name, string description) =>
        Call(() => api.Playlists.ChangeDetails(playlistId, new PlaylistChangeDetailsRequest
        {
            Name = name,
            Description = CleanDescription(description),
        }));

    // Spotify has no delete; removing your own playlist from your library is how its apps delete one.
    public Task DeletePlaylist(string playlistId) =>
        Call(() => api.Library.RemoveItems(new LibraryRemoveItemsRequest([$"spotify:playlist:{playlistId}"])));

    private static async Task<T> Call<T>(Func<Task<T>> action)
    {
        try { return await action(); }
        catch (APITooManyRequestsException ex)
        {
            throw new QuotaExceededException($"Spotify rate limit reached; retry in {ex.RetryAfter.TotalSeconds:0}s or on the next sync.", ex);
        }
    }

    private static RemoteTrack ToTrack(FullTrack t) =>
        new(t.Id, t.Name, t.Artists.FirstOrDefault()?.Name ?? "", t.Album?.Images?.OrderBy(i => i.Height).FirstOrDefault()?.Url);

    // Spotify descriptions are single-line and at most 300 characters.
    private static string CleanDescription(string text) => new string(text.Replace('\n', ' ').Take(300).ToArray());
}
