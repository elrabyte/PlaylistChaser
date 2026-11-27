using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PlaylistChaser.Data;
using PlaylistChaser.Plugins;
using PlaylistChaser.Sources;

namespace PlaylistChaser.Tests;

public class PluginTests
{
    /// <summary>What a plugin container sends: the shared token and the user it acts for.</summary>
    private static HttpClient AsPlugin(TestApp app, string userName)
    {
        var userId = app.Db(db => db.Users.Single(u => u.UserName == userName).Id);
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add("X-Plugin-Token", TestApp.PluginToken);
        client.DefaultRequestHeaders.Add("X-PlaylistChaser-User", userId);
        return client;
    }

    [Theory]
    [InlineData("download=http://downloader:8080", "download", "http://downloader:8080")]
    [InlineData(" download = http://downloader:8080/ ", "download", "http://downloader:8080")]
    public void PLUGINS_entries_are_parsed(string setting, string id, string url)
    {
        var plugin = Assert.Single(PluginConfig.Parse(setting));
        Assert.Equal(new PluginDef(id, url), plugin);
    }

    [Theory]
    [InlineData("download")]
    [InlineData("=http://x")]
    public void A_PLUGINS_entry_without_id_or_url_is_rejected(string setting) =>
        Assert.Throws<InvalidOperationException>(() => PluginConfig.Parse(setting));

    [Fact]
    public async Task The_plugin_api_refuses_missing_or_wrong_tokens_and_unknown_users()
    {
        await using var app = new TestApp();
        await app.Login();
        var userId = app.Db(db => db.Users.Single().Id);
        var noToken = app.CreateClient();
        var wrong = app.CreateClient();
        wrong.DefaultRequestHeaders.Add("X-Plugin-Token", "nope");
        wrong.DefaultRequestHeaders.Add("X-PlaylistChaser-User", userId);
        var unknownUser = AsPlugin(app, "admin");
        unknownUser.DefaultRequestHeaders.Remove("X-PlaylistChaser-User");
        unknownUser.DefaultRequestHeaders.Add("X-PlaylistChaser-User", "not-a-user");

        Assert.Equal(HttpStatusCode.Unauthorized, (await noToken.GetAsync("/api/plugin/playlists")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await wrong.GetAsync("/api/plugin/playlists")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await unknownUser.GetAsync("/api/plugin/playlists")).StatusCode);
    }

    [Fact]
    public async Task A_plugin_sees_only_the_user_it_acts_for()
    {
        await using var app = new TestApp();
        app.Sources.Spotify.Track("a", "A").Playlist("SP1", "Mine", false, "a");
        var admin = await app.Login();
        var bob = await app.NewUser(admin, "bob");
        await admin.PostAsJsonAsync("/api/playlists", new { url = "https://open.spotify.com/playlist/SP1" });

        var mine = await AsPlugin(app, "admin").GetFromJsonAsync<List<JsonElement>>("/api/plugin/playlists");
        var bobs = await AsPlugin(app, "bob").GetFromJsonAsync<List<JsonElement>>("/api/plugin/playlists");

        Assert.Equal("Mine", mine!.Single().GetProperty("name").GetString());
        Assert.Empty(bobs!);
    }

    [Fact]
    public async Task A_plugin_can_match_a_song_and_read_its_metadata()
    {
        await using var app = new TestApp();
        app.Sources.Spotify.Track("sa", "A", "Artist A").Playlist("SP1", "One", false, "sa");
        app.Sources.Youtube.Track("ya", "A", "Artist A");
        var admin = await app.Login();
        var plugin = AsPlugin(app, "admin");
        await admin.PostAsJsonAsync("/api/playlists", new { url = "https://open.spotify.com/playlist/SP1" });
        await app.Run("admin", JobKind.Pull, app.Db(db => db.Playlists.Single().Id));
        var songId = app.Db(db => db.Songs.Single().Id);

        var match = await (await plugin.PostAsync($"/api/plugin/songs/{songId}/match/youtube", null)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ya", match.GetProperty("externalId").GetString());
        Assert.Equal(LinkStatus.Found, app.Db(db => db.SongLinks.Single(l => l.SongId == songId && l.Source == Source.Youtube).Status));

        var songs = await plugin.GetFromJsonAsync<List<JsonElement>>($"/api/plugin/playlists/{app.Db(db => db.Playlists.Single().Id)}/songs");
        Assert.Equal("Artist A", songs!.Single().GetProperty("artist").GetString());

        var metadata = await (await plugin.GetAsync("/api/plugin/metadata/spotify/sa")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Album of A", metadata.GetProperty("album").GetString());
    }

    [Fact]
    public async Task A_plugin_reports_its_job_and_the_owner_sees_the_lines()
    {
        await using var app = new TestApp();
        await app.Login();
        var plugin = AsPlugin(app, "admin");

        var job = await (await plugin.PostAsJsonAsync("/api/plugin/jobs", new { title = "Download Road Trip", total = 3 })).Content.ReadFromJsonAsync<JsonElement>();
        var id = job.GetProperty("id").GetInt32();
        await plugin.PatchAsJsonAsync($"/api/plugin/jobs/{id}", new { status = "done", done = 3, message = (string?)null });
        await plugin.PostAsJsonAsync($"/api/plugin/jobs/{id}/logs", new { level = "warn", message = "2 songs not on YouTube" });

        var stored = app.Db(db => db.Jobs.Single(j => j.Id == id));
        Assert.Equal((JobKind.Plugin, "Download Road Trip", JobStatus.Done), (stored.Kind, stored.Title, stored.Status));
        var admin = await app.Login();
        var lines = await admin.GetFromJsonAsync<List<JsonElement>>($"/api/jobs/{id}/logs");
        Assert.Equal("2 songs not on YouTube", Assert.Single(lines!).GetProperty("message").GetString());
    }
}
