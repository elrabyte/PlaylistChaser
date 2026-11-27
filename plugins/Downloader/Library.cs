using System.Text;
using System.Text.Json;

namespace Downloader;

/// <summary>A song the user wants on disk, with what it is called there.</summary>
public record Wanted(int SongId, string YoutubeId, string Artist, string Album, string Title, int? Track, int? Year, string? CoverUrl)
{
    public string RelativePath => $"{Library.Safe(Artist, "Unknown artist")}/{Library.Safe(Album, "Singles")}/{Library.FileName(Title, Track)}.m4a";
}

/// <summary>What is on disk: the YouTube id it was downloaded from and its path relative to the library folder.</summary>
public record Stored(string YoutubeId, string RelativePath);

public static class Library
{
    public static string Safe(string name, string fallback)
    {
        // a fixed list: the files are written on Linux and must read the same on Windows and Samba shares
        const string unsafeChars = "/\\:*?\"<>|";
        var cleaned = new string(name.Select(c => unsafeChars.Contains(c) || char.IsControl(c) ? '_' : c).ToArray()).Trim().TrimEnd('.');
        if (cleaned.Length == 0) return fallback;
        return cleaned.Length > 120 ? cleaned[..120] : cleaned;
    }

    public static string FileName(string title, int? track) =>
        track is > 0 ? $"{track:00} {Safe(title, "Untitled")}" : Safe(title, "Untitled");

    /// <summary>Downloads what is new or changed, and deletes what is no longer wanted.</summary>
    public static (List<Wanted> Download, List<int> Remove) Diff(IReadOnlyList<Wanted> wanted, IReadOnlyDictionary<int, Stored> stored)
    {
        var download = wanted.Where(w => !stored.TryGetValue(w.SongId, out var s) || s.YoutubeId != w.YoutubeId || s.RelativePath != w.RelativePath).ToList();
        var keep = wanted.Select(w => w.SongId).ToHashSet();
        var remove = stored.Keys.Where(id => !keep.Contains(id)).ToList();
        return (download, remove);
    }

    /// <summary>A playlist file the music apps read: one relative path per line, in the playlist's order.</summary>
    public static string PlaylistFile(IEnumerable<string> relativePaths)
    {
        var sb = new StringBuilder("#EXTM3U\n");
        foreach (var path in relativePaths) sb.Append("../").Append(path).Append('\n');
        return sb.ToString();
    }

    public static string SettingsPath(string configDir) => Path.Combine(configDir, "settings.json");
    public static string StatePath(string configDir) => Path.Combine(configDir, "state.json");

    public static async Task<T?> LoadAsync<T>(string path) where T : class =>
        File.Exists(path) ? JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(path)) : null;

    public static Task SaveAsync<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(tmp, path, overwrite: true); // a crash mid-save keeps the old file
        return Task.CompletedTask;
    }
}

/// <summary>What the user picked on the config page.</summary>
public record Settings(string UserId, string Folder, List<int> PlaylistIds, string DownloadFrom, string MetadataFrom, string Cron, bool Enabled);
