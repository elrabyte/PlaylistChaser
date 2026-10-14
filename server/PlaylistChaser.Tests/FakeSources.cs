using PlaylistChaser.Data;
using PlaylistChaser.Sources;

namespace PlaylistChaser.Tests;

public class FakeSources : ISourceProvider
{
    public FakeSource Youtube { get; } = new(Source.Youtube, maxBatch: 1);
    public FakeSource Spotify { get; } = new(Source.Spotify, maxBatch: 100);

    public Task<ISource> Get(string userId, Source source) => Task.FromResult<ISource>(source == Source.Youtube ? Youtube : Spotify);
}

/// <summary>An in-memory music service. Search matches by exact track name.</summary>
public class FakeSource(Source kind, int maxBatch) : ISource
{
    public record FakePlaylist(string Name, bool IsMine, List<string> Tracks);

    public Dictionary<string, RemoteTrack> Catalog { get; } = [];
    public Dictionary<string, FakePlaylist> Playlists { get; } = [];
    /// <summary>How many more AddTracks calls succeed before a quota error; null = unlimited.</summary>
    public int? QuotaLeft { get; set; }
    public int MaxBatch { get; set; } = maxBatch;

    public FakeSource Track(string id, string name, string artist = "artist")
    {
        Catalog[id] = new RemoteTrack(id, name, artist, null);
        return this;
    }

    public FakeSource Playlist(string id, string name, bool isMine, params string[] trackIds)
    {
        Playlists[id] = new FakePlaylist(name, isMine, [.. trackIds]);
        return this;
    }

    public Task<RemotePlaylist> GetPlaylist(string id) =>
        Task.FromResult(new RemotePlaylist(id, Playlists[id].Name, null, "creator", null, Playlists[id].IsMine));

    public Task<List<RemotePlaylist>> GetMyPlaylists() =>
        Task.FromResult(Playlists.Select(p => new RemotePlaylist(p.Key, p.Value.Name, null, "creator", null, p.Value.IsMine)).ToList());

    /// <summary>Playlists whose tracks were read, in order.</summary>
    public List<string> Listed { get; } = [];
    /// <summary>Track ids the service refuses to add.</summary>
    public HashSet<string> Rejects { get; } = [];
    /// <summary>When set, every add fails with this (e.g. the whole playlist is gone).</summary>
    public Exception? AddFailure { get; set; }

    public Task<List<RemoteTrack>> GetTracks(string playlistId)
    {
        Listed.Add(playlistId);
        return Task.FromResult(Playlists[playlistId].Tracks.Select(t => Catalog[t]).ToList());
    }

    public Task<RemoteTrack?> GetTrack(string id) => Task.FromResult(Catalog.GetValueOrDefault(id));

    public Task<RemoteTrack?> Search(string name, string artist) =>
        Task.FromResult(Catalog.Values.FirstOrDefault(t => t.Name == name));

    public Task<string> CreatePlaylist(string name, string description)
    {
        var id = $"{kind}-created-{Playlists.Count}";
        Playlists[id] = new FakePlaylist(name, true, []);
        return Task.FromResult(id);
    }

    public Task AddTracks(string playlistId, IReadOnlyList<string> trackIds)
    {
        if (QuotaLeft-- <= 0) throw new QuotaExceededException("quota");
        if (AddFailure != null) throw AddFailure;
        if (trackIds.FirstOrDefault(Rejects.Contains) is { } rejected) throw new TrackRejectedException($"{rejected} refused");
        Playlists[playlistId].Tracks.AddRange(trackIds);
        return Task.CompletedTask;
    }

    public Task UpdatePlaylist(string playlistId, string name, string description) => Task.CompletedTask;

    public Task DeletePlaylist(string playlistId)
    {
        Playlists.Remove(playlistId);
        return Task.CompletedTask;
    }
}
