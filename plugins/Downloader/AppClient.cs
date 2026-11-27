using System.Net.Http.Json;
using System.Text.Json;

namespace Downloader;

public record AppPlaylist(int Id, string Name);
public record AppLink(string Source, string Status, string? ExternalId);
public record AppSong(int Id, string Name, string Artist, List<AppLink> Links);
public record AppMetadata(string Title, string Artist, string? Album, int? Year, int? TrackNumber, string? CoverUrl);
public record AppService(string Id, string Name, List<string> Capabilities);

/// <summary>Talks to PlaylistChaser's plugin API as the user who configured the plugin.</summary>
public class AppClient(HttpClient http, string baseUrl, string token, string userId)
{
    private HttpRequestMessage Request(HttpMethod method, string path, object? body = null)
    {
        var req = new HttpRequestMessage(method, baseUrl + "/api/plugin" + path);
        req.Headers.Add("X-Plugin-Token", token);
        req.Headers.Add("X-PlaylistChaser-User", userId);
        if (body != null) req.Content = JsonContent.Create(body);
        return req;
    }

    private async Task<T> Send<T>(HttpMethod method, string path, object? body = null)
    {
        using var res = await http.SendAsync(Request(method, path, body));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<T>())!;
    }

    // for calls the app answers with 204 No Content
    private async Task Send(HttpMethod method, string path, object? body = null)
    {
        using var res = await http.SendAsync(Request(method, path, body));
        res.EnsureSuccessStatusCode();
    }

    public Task<List<AppService>> Services() => Send<List<AppService>>(HttpMethod.Get, "/sources");
    public Task<List<AppPlaylist>> Playlists() => Send<List<AppPlaylist>>(HttpMethod.Get, "/playlists");
    public Task<List<AppSong>> Songs(int playlistId) => Send<List<AppSong>>(HttpMethod.Get, $"/playlists/{playlistId}/songs");

    /// <summary>Searches the service for a song that has no link there yet; the app remembers the answer.</summary>
    public async Task<string?> Match(int songId, string source)
    {
        var res = await Send<JsonElement>(HttpMethod.Post, $"/songs/{songId}/match/{source}");
        return res.GetProperty("externalId").GetString();
    }

    public async Task<AppMetadata?> Metadata(string source, string trackId)
    {
        using var res = await http.SendAsync(Request(HttpMethod.Get, $"/metadata/{source}/{trackId}"));
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<AppMetadata>();
    }

    public async Task<int> StartJob(string title, int total)
    {
        var res = await Send<JsonElement>(HttpMethod.Post, "/jobs", new { title, total });
        return res.GetProperty("id").GetInt32();
    }

    public Task Progress(int jobId, int done, int total, string? message = null) =>
        Send(HttpMethod.Patch, $"/jobs/{jobId}", new { done, total, message });

    public Task Finish(int jobId, bool ok, string? message) =>
        Send(HttpMethod.Patch, $"/jobs/{jobId}", new { status = ok ? "done" : "failed", message });

    /// <summary>level: info, warn or error.</summary>
    public Task Log(int jobId, string level, string message) =>
        Send(HttpMethod.Post, $"/jobs/{jobId}/logs", new { level, message });
}
