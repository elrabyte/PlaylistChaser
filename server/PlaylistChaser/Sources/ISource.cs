using System.Text.RegularExpressions;
using PlaylistChaser.Data;

namespace PlaylistChaser.Sources;

public record RemotePlaylist(string Id, string Name, string? Description, string? Creator, string? ThumbnailUrl, bool IsMine);
public record RemoteTrack(string Id, string Name, string Artist, string? ThumbnailUrl);

/// <summary>One music service, acting as the connected user.</summary>
public interface ISource
{
    /// <summary>How many tracks one AddTracks call may take.</summary>
    int MaxBatch { get; }
    Task<RemotePlaylist> GetPlaylist(string id);
    /// <summary>The connected user's own and followed playlists, private ones included, liked songs first.</summary>
    Task<List<RemotePlaylist>> GetMyPlaylists();
    Task<List<RemoteTrack>> GetTracks(string playlistId);
    Task<RemoteTrack?> GetTrack(string id);
    Task<RemoteTrack?> Search(string name, string artist);
    Task<string> CreatePlaylist(string name, string description);
    Task AddTracks(string playlistId, IReadOnlyList<string> trackIds);
    Task UpdatePlaylist(string playlistId, string name, string description);
    Task DeletePlaylist(string playlistId);
}

public interface ISourceProvider
{
    /// <summary>Throws <see cref="SourceNotConnectedException"/> when the user has no usable account for the source.</summary>
    Task<ISource> Get(string userId, Source source);
}

/// <summary>The service refused because the daily quota or rate limit is used up; retrying later works.</summary>
public class QuotaExceededException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>The service refused one specific track (removed, region-locked, ...); the rest of the push can go on.</summary>
public class TrackRejectedException(string message, Exception? inner = null) : Exception(message, inner);

public class SourceNotConnectedException(Source source)
    : Exception($"{source} is not connected. Connect it in Settings first.");

public static partial class SourceUrls
{
    // Liked songs aren't a real playlist on either service; these ids stand in for them.
    public const string YoutubeLiked = "LL";
    public const string YoutubeLikedMusic = "LM"; // YouTube Music's "Liked music"
    public const string SpotifyLiked = "liked";

    [GeneratedRegex(@"[?&]list=([\w-]+)")] private static partial Regex YoutubePlaylist();
    [GeneratedRegex(@"open\.spotify\.com/(?:[\w-]+/)?(?:playlist/(\w+)|collection/(tracks))")] private static partial Regex SpotifyPlaylist();
    [GeneratedRegex(@"(?:youtube\.com/(?:[^/]+/.+/|(?:v|e(?:mbed)?|shorts)/|.*[?&]v=)|youtu\.be/)([\w-]{11})")] private static partial Regex YoutubeVideo();
    [GeneratedRegex(@"open\.spotify\.com/(?:[\w-]+/)?track/(\w+)")] private static partial Regex SpotifyTrack();

    public static bool TryParsePlaylist(string url, out Source source, out string id)
    {
        var m = SpotifyPlaylist().Match(url);
        source = m.Success ? Source.Spotify : Source.Youtube;
        if (!m.Success) m = YoutubePlaylist().Match(url);
        id = m.Groups[2].Success ? SpotifyLiked : m.Groups[1].Value;
        return m.Success;
    }

    public static string? TryParseTrack(Source source, string url)
    {
        var m = (source == Source.Youtube ? YoutubeVideo() : SpotifyTrack()).Match(url);
        return m.Success ? m.Groups[1].Value : null;
    }

    public static string Playlist(Source source, string id) =>
        source == Source.Youtube ? $"https://{(id == YoutubeLikedMusic ? "music" : "www")}.youtube.com/playlist?list={id}"
        : id == SpotifyLiked ? "https://open.spotify.com/collection/tracks"
        : $"https://open.spotify.com/playlist/{id}";

    public static string Track(Source source, string id) => source == Source.Youtube
        ? $"https://www.youtube.com/watch?v={id}"
        : $"https://open.spotify.com/track/{id}";
}
