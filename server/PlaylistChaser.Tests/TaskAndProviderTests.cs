using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PlaylistChaser.Data;
using PlaylistChaser.Jobs;
using PlaylistChaser.Sources;
using PlaylistChaser.Tasks;

namespace PlaylistChaser.Tests;

public class TaskAndProviderTests
{
    [Fact]
    public void Declared_capabilities_match_what_each_provider_implements()
    {
        var types = new Dictionary<Source, Type> { [Source.Youtube] = typeof(YoutubeSource), [Source.Spotify] = typeof(SpotifySource) };
        foreach (var p in Providers.All)
        {
            Assert.Equal(p.Capabilities.HasFlag(Capability.Source), types[p.Id].IsAssignableTo(typeof(IPlaylistSource)));
            Assert.Equal(p.Capabilities.HasFlag(Capability.Destination), types[p.Id].IsAssignableTo(typeof(IPlaylistDestination)));
        }
        Assert.Equal(Enum.GetValues<Source>().Order(), Providers.All.Select(p => p.Id).Order()); // every service is listed
    }

    [Fact]
    public async Task Provider_list_tells_the_client_names_and_capabilities()
    {
        await using var app = new TestApp();
        var admin = await app.Login();

        var spotify = (await admin.GetFromJsonAsync<List<JsonElement>>("/api/sources"))!.Single(s => s.GetProperty("source").GetString() == "spotify");

        Assert.Equal("Spotify", spotify.GetProperty("name").GetString());
        Assert.Equal(["source", "destination", "metadata"], spotify.GetProperty("capabilities").EnumerateArray().Select(c => c.GetString()));
    }

    [Fact]
    public async Task New_users_get_the_default_six_hourly_tasks()
    {
        await using var app = new TestApp();
        var admin = await app.Login();
        var bob = await app.NewUser(admin, "bob");

        foreach (var client in new[] { admin, bob })
        {
            var tasks = (await client.GetFromJsonAsync<List<JsonElement>>("/api/tasks"))!;
            Assert.Equal(["pullAll|0 */6 * * *", "pushAll|30 */6 * * *"],
                tasks.Select(t => $"{t.GetProperty("kind").GetString()}|{t.GetProperty("cron").GetString()}"));
        }
    }

    [Fact]
    public async Task Tasks_validate_cron_and_belong_to_their_user()
    {
        await using var app = new TestApp();
        var admin = await app.Login();
        var bob = await app.NewUser(admin, "bob");

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/tasks", new { kind = "pullAll", cron = "every 6 hours", enabled = true })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/tasks", new { kind = "pull", cron = "0 * * * *", enabled = true })).StatusCode);

        var created = await (await admin.PostAsJsonAsync("/api/tasks", new { kind = "syncAll", cron = "15 3 * * 1", enabled = true }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetInt32();
        Assert.True(created.GetProperty("nextRunAt").GetDateTime() > DateTime.UtcNow);

        Assert.Equal(HttpStatusCode.NotFound, (await bob.PutAsJsonAsync($"/api/tasks/{id}", new { kind = "syncAll", cron = "0 * * * *", enabled = false })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.PostAsync($"/api/tasks/{id}/run", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.DeleteAsync($"/api/tasks/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/tasks/{id}")).StatusCode);
    }

    [Fact]
    public async Task Scheduler_runs_due_tasks_once_and_schedules_the_next_run()
    {
        await using var app = new TestApp();
        await app.Login();
        var now = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();
        var queue = scope.ServiceProvider.GetRequiredService<JobQueue>();
        var userId = db.Users.Single().Id;
        var (pull, push) = (db.ScheduledTasks.Single(t => t.Kind == JobKind.PullAll), db.ScheduledTasks.Single(t => t.Kind == JobKind.PushAll));
        pull.NextRunAt = now.AddMinutes(-1); // due
        push.Enabled = false;                // disabled tasks never run
        db.ScheduledTasks.Add(new ScheduledTask { UserId = userId, Kind = JobKind.SyncAll, Cron = "0 * * * *", Enabled = true }); // never scheduled yet
        await db.SaveChangesAsync();

        await Scheduler.RunDue(db, queue, now);
        await Scheduler.RunDue(db, queue, now); // a second tick at the same moment must not run it again

        Assert.Equal([JobKind.PullAll], db.Jobs.Select(j => j.Kind).ToList());
        Assert.True(pull.NextRunAt > now);
        Assert.Equal(now, pull.LastRunAt);
        Assert.NotNull(db.ScheduledTasks.Single(t => t.Kind == JobKind.SyncAll).NextRunAt); // scheduled, not run
    }

    [Fact]
    public async Task Fetch_only_and_copy_only_tasks_do_just_that()
    {
        await using var app = new TestApp();
        app.Sources.Spotify.Track("a", "A").Track("b", "B").Playlist("SP1", "One", false, "a");
        app.Sources.Youtube.Track("ya", "A").Track("yb", "B");
        var admin = await app.Login();
        var res = await admin.PostAsJsonAsync("/api/playlists", new { url = "https://open.spotify.com/playlist/SP1" });
        var id = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        await app.Run("admin", JobKind.Pull, id);
        await app.Run("admin", JobKind.Push, id, Source.Youtube);
        var copy = app.Sources.Youtube.Playlists.Single(p => p.Key.Contains("created")).Value;
        app.Sources.Spotify.Playlists["SP1"].Tracks.Add("b"); // a new song appears on Spotify

        await app.Run("admin", JobKind.PushAll);
        Assert.Equal(["ya"], copy.Tracks); // copy-only: B wasn't fetched yet, so nothing new to copy

        await app.Run("admin", JobKind.PullAll);
        Assert.Equal(["ya"], copy.Tracks); // fetch-only: B is fetched but not copied

        await app.Run("admin", JobKind.PushAll);
        Assert.Equal(["ya", "yb"], copy.Tracks);
    }
}
