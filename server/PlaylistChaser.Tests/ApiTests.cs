using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PlaylistChaser.Data;
using PlaylistChaser.Jobs;
using PlaylistChaser.Sources;

namespace PlaylistChaser.Tests;

public class ApiTests
{
    private const string YtUrl = "https://www.youtube.com/playlist?list=";
    private const string SpUrl = "https://open.spotify.com/playlist/";

    private static async Task<int> AddPlaylist(HttpClient client, string url)
    {
        var res = await client.PostAsJsonAsync("/api/playlists", new { url });
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private static List<string> SongNames(TestApp app, int playlistId) =>
        app.Db(db => db.PlaylistSongs.Where(ps => ps.PlaylistId == playlistId).Select(ps => ps.Song.Name).OrderBy(n => n).ToList());

    [Fact]
    public async Task Admin_comes_from_env_and_there_is_no_open_registration()
    {
        await using var app = new TestApp();
        var anonymous = app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/playlists")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/users", new { userName = "x", password = "long-enough-pw", isAdmin = true })).StatusCode);

        var admin = await app.Login();
        var me = await admin.GetFromJsonAsync<JsonElement>("/api/auth/me");
        Assert.True(me.GetProperty("isAdmin").GetBoolean());

        var bob = await app.NewUser(admin, "bob");
        Assert.Equal(HttpStatusCode.Forbidden, (await bob.GetAsync("/api/users")).StatusCode);
        Assert.Equal(1, app.Db(db => db.Users.Count(u => u.UserName == "admin")));
    }

    [Fact]
    public async Task Users_cannot_see_or_change_each_others_data()
    {
        await using var app = new TestApp();
        app.Sources.Youtube.Track("a", "Song A").Playlist("PL1", "Mine", true, "a");
        var admin = await app.Login();
        var bob = await app.NewUser(admin, "bob");
        var id = await AddPlaylist(admin, YtUrl + "PL1");
        await app.Run("admin", JobKind.Pull, id);
        var songId = app.Db(db => db.Songs.Single().Id);

        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync($"/api/playlists/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync($"/api/playlists/{id}/songs")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.PostAsync($"/api/playlists/{id}/pull", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.DeleteAsync($"/api/playlists/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.PutAsJsonAsync($"/api/songs/{songId}/links/spotify", new { notFound = true })).StatusCode);
        Assert.Empty(await bob.GetFromJsonAsync<List<JsonElement>>("/api/playlists") ?? []);
        Assert.Single(await admin.GetFromJsonAsync<List<JsonElement>>("/api/playlists") ?? []);
    }

    [Fact]
    public async Task Pull_imports_once_and_reuses_songs_across_playlists()
    {
        await using var app = new TestApp();
        app.Sources.Youtube.Track("a", "A").Track("b", "B").Track("c", "C")
            .Playlist("PL1", "One", false, "a", "b", "a")
            .Playlist("PL2", "Two", false, "b", "c");
        var admin = await app.Login();
        var one = await AddPlaylist(admin, YtUrl + "PL1");
        var two = await AddPlaylist(admin, YtUrl + "PL2");

        await app.Run("admin", JobKind.Pull, one);
        await app.Run("admin", JobKind.Pull, one);
        await app.Run("admin", JobKind.Pull, two);

        Assert.Equal(["A", "B"], SongNames(app, one));
        Assert.Equal(["B", "C"], SongNames(app, two));
        Assert.Equal(3, app.Db(db => db.Songs.Count()));
    }

    [Fact]
    public async Task Push_adds_only_missing_tracks_and_keeps_manual_removals()
    {
        await using var app = new TestApp();
        app.Sources.Youtube.Track("a", "A").Track("b", "B").Track("c", "C").Playlist("PL1", "One", false, "a", "b", "c");
        app.Sources.Spotify.Track("sa", "A").Track("sb", "B"); // C doesn't exist on Spotify
        var admin = await app.Login();
        var id = await AddPlaylist(admin, YtUrl + "PL1");
        await app.Run("admin", JobKind.Pull, id);

        await app.Run("admin", JobKind.Push, id, Source.Spotify);
        var target = app.Sources.Spotify.Playlists.Single(p => p.Key.Contains("created")).Value;
        Assert.Equal(["sa", "sb"], target.Tracks);
        Assert.Equal(LinkStatus.NotFound, app.Db(db => db.SongLinks.Single(l => l.Song.Name == "C" && l.Source == Source.Spotify).Status));

        target.Tracks.Remove("sa"); // the user deletes a track on Spotify
        await app.Run("admin", JobKind.Push, id, Source.Spotify);
        Assert.Equal(["sb"], target.Tracks);
    }

    [Fact]
    public async Task Push_stops_on_quota_and_resumes_next_time()
    {
        await using var app = new TestApp();
        app.Sources.Spotify.Track("a", "A").Track("b", "B").Playlist("SP1", "One", false, "a", "b");
        app.Sources.Youtube.Track("ya", "A").Track("yb", "B");
        app.Sources.Youtube.QuotaLeft = 1;
        var admin = await app.Login();
        var id = await AddPlaylist(admin, SpUrl + "SP1");
        await app.Run("admin", JobKind.Pull, id);

        await Assert.ThrowsAsync<QuotaExceededException>(() => app.Run("admin", JobKind.Push, id, Source.Youtube));
        var target = app.Sources.Youtube.Playlists.Single(p => p.Key.Contains("created")).Value;
        Assert.Equal(["ya"], target.Tracks);

        app.Sources.Youtube.QuotaLeft = null;
        await app.Run("admin", JobKind.Push, id, Source.Youtube);
        Assert.Equal(["ya", "yb"], target.Tracks);
    }

    [Fact]
    public async Task Combined_playlist_is_the_union_of_its_members()
    {
        await using var app = new TestApp();
        app.Sources.Youtube.Track("a", "A").Track("b", "B").Playlist("PL1", "One", false, "a", "b");
        app.Sources.Spotify.Track("sb", "B").Track("sc", "C").Playlist("SP1", "Two", false, "sb", "sc");
        var admin = await app.Login();
        var one = await AddPlaylist(admin, YtUrl + "PL1");
        var two = await AddPlaylist(admin, SpUrl + "SP1");
        var res = await admin.PostAsJsonAsync("/api/playlists/combined", new { name = "Both", playlistIds = new[] { one, two } });
        var combined = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        await app.Run("admin", JobKind.Pull, combined);

        // B from YouTube and B from Spotify are different songs until a push links them
        Assert.Equal(["A", "B", "B", "C"], SongNames(app, combined));
    }

    [Fact]
    public async Task Linking_an_existing_playlist_requires_owning_it()
    {
        await using var app = new TestApp();
        app.Sources.Youtube.Playlist("PL1", "One", false);
        app.Sources.Spotify.Playlist("theirs", "Theirs", false).Playlist("mine", "Mine", true);
        var admin = await app.Login();
        var id = await AddPlaylist(admin, YtUrl + "PL1");

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync($"/api/playlists/{id}/links", new { url = SpUrl + "theirs" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsJsonAsync($"/api/playlists/{id}/links", new { url = SpUrl + "mine" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/playlists/{id}/links", new { url = SpUrl + "mine" })).StatusCode);
    }

    [Fact]
    public async Task OAuth_callback_rejects_forged_or_foreign_state()
    {
        await using var app = new TestApp();
        var client = app.CreateClient(new() { AllowAutoRedirect = false });

        var forged = await client.GetAsync("/api/sources/youtube/callback?code=x&state=forged");
        Assert.Contains("/settings?error=", forged.Headers.Location!.OriginalString);

        using var scope = app.Services.CreateScope();
        var accounts = scope.ServiceProvider.GetRequiredService<SourceAccounts>();
        var state = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(accounts.LoginUrl("user-1", Source.Spotify)).Query)["state"].ToString();
        Assert.Equal("user-1", accounts.ReadState(state, Source.Spotify));
        Assert.Null(accounts.ReadState(state, Source.Youtube));
        Assert.Null(accounts.ReadState(state[..^2] + "xx", Source.Spotify));
        Assert.Equal(0, app.Db(db => db.SourceAccounts.Count()));
    }

    [Fact]
    public async Task Deleting_a_playlist_with_copies_elsewhere_is_refused()
    {
        await using var app = new TestApp();
        app.Sources.Youtube.Track("a", "A").Playlist("PL1", "One", false, "a");
        app.Sources.Spotify.Track("sa", "A");
        var admin = await app.Login();
        var id = await AddPlaylist(admin, YtUrl + "PL1");
        await app.Run("admin", JobKind.Pull, id);
        await app.Run("admin", JobKind.Push, id, Source.Spotify);

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.DeleteAsync($"/api/playlists/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/playlists/{id}/links/spotify?deleteRemote=true")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/playlists/{id}")).StatusCode);
        Assert.Equal(0, app.Db(db => db.Songs.Count())); // orphaned songs are cleaned up
    }
}
