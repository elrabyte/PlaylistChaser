using Downloader;

namespace Downloader.Tests;

public class LibraryTests
{
    private static Wanted Song(int id, string yt = "yt", string artist = "Artist", string album = "Album", string title = "Title", int? track = 1) =>
        new(id, yt, artist, album, title, track, 2020, null);

    [Theory]
    [InlineData("AC/DC", "AC_DC")]
    [InlineData("  Trailing dots...  ", "Trailing dots")]
    [InlineData("a:b*c?", "a_b_c_")]
    public void Names_are_safe_for_every_file_system(string name, string expected) =>
        Assert.Equal(expected, Library.Safe(name, "fallback"));

    [Fact]
    public void Empty_names_fall_back_and_long_names_are_cut()
    {
        Assert.Equal("Unknown artist", Library.Safe("  ", "Unknown artist"));
        Assert.Equal(120, Library.Safe(new string('x', 300), "f").Length);
    }

    [Fact]
    public void Tracks_are_numbered_in_their_file_name()
    {
        Assert.Equal("03 Song", Library.FileName("Song", 3));
        Assert.Equal("Song", Library.FileName("Song", null));
        Assert.Equal("Song", Library.FileName("Song", 0));
    }

    [Fact]
    public void Songs_land_in_artist_album_folders()
    {
        var song = Song(1, artist: "AC/DC", album: "", title: "Thunder", track: 2);
        Assert.Equal("AC_DC/Singles/02 Thunder.m4a", song.RelativePath);
    }

    [Fact]
    public void A_new_song_is_downloaded_and_an_unchanged_one_is_not()
    {
        var wanted = new[] { Song(1), Song(2) };
        var stored = new Dictionary<int, Stored> { [1] = new("yt", Song(1).RelativePath) };

        var (download, remove) = Library.Diff(wanted, stored);

        Assert.Equal([2], download.Select(w => w.SongId));
        Assert.Empty(remove);
    }

    [Fact]
    public void A_corrected_match_or_a_changed_path_downloads_the_song_again()
    {
        var before = Song(1, yt: "old");
        var stored = new Dictionary<int, Stored> { [1] = new(before.YoutubeId, before.RelativePath) };

        Assert.Single(Library.Diff([Song(1, yt: "new")], stored).Download);
        Assert.Single(Library.Diff([Song(1, yt: "old", title: "Renamed")], stored).Download);
    }

    [Fact]
    public void A_song_no_longer_in_any_chosen_playlist_is_removed()
    {
        var stored = new Dictionary<int, Stored> { [1] = new("yt", Song(1).RelativePath), [2] = new("yt2", "B/C/02 D.m4a") };

        var (download, remove) = Library.Diff([Song(1, yt: "yt")], stored);

        Assert.Empty(download);
        Assert.Equal([2], remove);
    }

    [Fact]
    public void Playlist_files_list_relative_paths_for_apps_in_the_playlists_folder()
    {
        var text = Library.PlaylistFile(["AC_DC/Singles/01 A.m4a", "B/C/02 D.m4a"]);

        Assert.Equal("#EXTM3U\n../AC_DC/Singles/01 A.m4a\n../B/C/02 D.m4a\n", text);
    }
}
