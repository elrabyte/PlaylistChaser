using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PlaylistChaser.Data;
using PlaylistChaser.Status;
using PlaylistChaser.Tasks;

namespace PlaylistChaser.Tests;

public class StatusTests
{
    private static async Task<int> AddPlaylist(HttpClient client, string url) =>
        (await (await client.PostAsJsonAsync("/api/playlists", new { url })).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

    private static List<(JobLogLevel Level, string Message, bool IsQuota)> LogsOf(TestApp app, int jobId) =>
        app.Db(db => db.JobLogs.Where(l => l.JobId == jobId).OrderBy(l => l.Id).AsEnumerable().Select(l => (l.Level, l.Message, l.IsQuota)).ToList());

    [Fact]
    public async Task Pull_and_push_write_plain_language_log_lines()
    {
        await using var app = new TestApp();
        app.Sources.Spotify.Track("a", "A").Track("b", "B").Playlist("SP1", "Road Trip", false, "a", "b");
        app.Sources.Youtube.Track("ya", "A"); // B doesn't exist on YouTube
        var admin = await app.Login();
        var id = await AddPlaylist(admin, "https://open.spotify.com/playlist/SP1");

        var pull = await app.Run("admin", JobKind.Pull, id);
        var push = await app.Run("admin", JobKind.Push, id, Source.Youtube);

        Assert.Equal(["Road Trip: 2 songs on Spotify, 2 new."], LogsOf(app, pull.Id).Select(l => l.Message));
        var lines = LogsOf(app, push.Id);
        Assert.Contains(lines, l => l.Level == JobLogLevel.Warn && l.Message == "Road Trip: searched 2 songs on Youtube, 1 not found: B.");
        Assert.Contains(lines, l => l.Level == JobLogLevel.Info && l.Message == "Road Trip: added 1 of 1 songs to Youtube.");
    }

    [Fact]
    public async Task A_reached_daily_limit_is_logged_and_shown_on_the_status_page_until_it_resets()
    {
        await using var app = new TestApp();
        app.Sources.Spotify.Track("a", "A").Track("b", "B").Playlist("SP1", "One", false, "a", "b");
        app.Sources.Youtube.Track("ya", "A").Track("yb", "B");
        app.Sources.Youtube.QuotaLeft = 1;
        var admin = await app.Login();
        var id = await AddPlaylist(admin, "https://open.spotify.com/playlist/SP1");
        await app.Run("admin", JobKind.Pull, id);

        await Assert.ThrowsAsync<PlaylistChaser.Sources.QuotaExceededException>(() => app.Run("admin", JobKind.Push, id, Source.Youtube));

        var limitLine = app.Db(db => db.JobLogs.Single(l => l.IsQuota));
        Assert.Equal("One: Youtube's daily limit was reached. 1 songs wait until it resets.", limitLine.Message);
        var status = await admin.GetFromJsonAsync<JsonElement>("/api/status");
        Assert.Equal(limitLine.Message, status.GetProperty("limit").GetProperty("message").GetString());
        Assert.True(status.GetProperty("limit").GetProperty("resetsAt").GetDateTime() > DateTime.UtcNow);

        // once the reset time is behind us, the notice is gone
        app.Db(db => { db.JobLogs.Single(l => l.IsQuota).At = DateTime.UtcNow.AddDays(-2); db.SaveChanges(); return 0; });
        status = await admin.GetFromJsonAsync<JsonElement>("/api/status");
        Assert.Equal(JsonValueKind.Null, status.GetProperty("limit").ValueKind);
    }

    [Fact]
    public async Task Status_lists_tasks_with_their_last_result_and_playlists_with_their_last_runs()
    {
        await using var app = new TestApp();
        app.Sources.Spotify.Track("a", "A").Playlist("SP1", "One", false, "a");
        var admin = await app.Login();
        var id = await AddPlaylist(admin, "https://open.spotify.com/playlist/SP1");
        await app.Run("admin", JobKind.Pull, id);
        await app.Run("admin", JobKind.PullAll);

        var status = await admin.GetFromJsonAsync<JsonElement>("/api/status");

        var pullTask = status.GetProperty("tasks").EnumerateArray().Single(t => t.GetProperty("kind").GetString() == "pullAll");
        Assert.Equal("0 */6 * * *", pullTask.GetProperty("cron").GetString());
        Assert.NotEqual(JsonValueKind.Null, pullTask.GetProperty("nextRunAt").ValueKind);
        Assert.Equal("done", pullTask.GetProperty("lastResult").GetProperty("status").GetString());
        var pushTask = status.GetProperty("tasks").EnumerateArray().Single(t => t.GetProperty("kind").GetString() == "pushAll");
        Assert.Equal(JsonValueKind.Null, pushTask.GetProperty("lastResult").ValueKind); // never ran
        var playlist = status.GetProperty("playlists").EnumerateArray().Single();
        Assert.Equal("One", playlist.GetProperty("name").GetString());
        Assert.NotEqual(JsonValueKind.Null, playlist.GetProperty("lastPulledAt").ValueKind);
    }

    [Fact]
    public async Task Job_logs_and_server_logs_are_only_for_who_may_see_them()
    {
        await using var app = new TestApp();
        app.Sources.Spotify.Track("a", "A").Playlist("SP1", "One", false, "a");
        var admin = await app.Login();
        var bob = await app.NewUser(admin, "bob");
        var id = await AddPlaylist(admin, "https://open.spotify.com/playlist/SP1");
        var job = await app.Run("admin", JobKind.Pull, id);

        Assert.Single(await admin.GetFromJsonAsync<List<JsonElement>>($"/api/jobs/{job.Id}/logs") ?? []);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync($"/api/jobs/{job.Id}/logs")).StatusCode); // someone else's job
        Assert.Equal(HttpStatusCode.Forbidden, (await bob.GetAsync("/api/logs")).StatusCode);              // raw logs: admin only
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/logs")).StatusCode);
    }

    [Fact]
    public async Task Failed_jobs_keep_their_log_lines_and_old_jobs_are_purged_with_theirs()
    {
        await using var app = new TestApp();
        app.Sources.Spotify.Track("a", "A").Playlist("SP1", "One", false, "a");
        var admin = await app.Login();
        var id = await AddPlaylist(admin, "https://open.spotify.com/playlist/SP1");
        var old = await app.Run("admin", JobKind.Pull, id);
        var fresh = await app.Run("admin", JobKind.Pull, id);
        app.Db(db => { db.Jobs.Single(j => j.Id == old.Id).FinishedAt = DateTime.UtcNow.AddDays(-31); db.SaveChanges(); return 0; });

        app.Db(db => { Scheduler.Purge(db, DateTime.UtcNow).GetAwaiter().GetResult(); return 0; });

        var remaining = app.Db(db => db.Jobs.Select(j => j.Id).ToList());
        Assert.DoesNotContain(old.Id, remaining);
        Assert.Contains(fresh.Id, remaining);
        Assert.Empty(LogsOf(app, old.Id)); // its lines went with it
        Assert.NotEmpty(LogsOf(app, fresh.Id));
    }

    [Fact]
    public async Task The_real_worker_keeps_the_log_lines_of_playlists_that_failed_in_a_finished_job()
    {
        await using var app = new TestApp();
        app.Sources.Spotify.Track("a", "A").Playlist("SP1", "Works", false, "a");
        var admin = await app.Login();
        await AddPlaylist(admin, "https://open.spotify.com/playlist/SP1");
        app.Db(db => // a playlist whose service no longer knows it
        {
            var userId = db.Users.Single().Id;
            db.Playlists.Add(new Playlist { UserId = userId, Name = "Gone", Kind = PlaylistKind.Simple, MainSource = Source.Spotify,
                Links = [new PlaylistLink { Source = Source.Spotify, ExternalId = "deleted", Url = "x", IsMine = false }] });
            return db.SaveChanges();
        });

        // the host runs no worker in tests; start the real one for this job
        var queue = app.Services.GetRequiredService<PlaylistChaser.Jobs.JobQueue>();
        var worker = new PlaylistChaser.Jobs.JobRunner(app.Services.GetRequiredService<IServiceScopeFactory>(), queue,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<PlaylistChaser.Jobs.JobRunner>.Instance);
        await worker.StartAsync(CancellationToken.None);
        var job = app.Db(db => { var userId = db.Users.Single().Id; return userId; });
        using (var scope = app.Services.CreateScope())
            await queue.Enqueue(scope.ServiceProvider.GetRequiredService<AppDb>(), job, JobKind.PullAll);
        for (var i = 0; i < 100 && app.Db(db => db.Jobs.Any(j => j.Kind == JobKind.PullAll && j.FinishedAt == null)); i++)
            await Task.Delay(50);
        await worker.StopAsync(CancellationToken.None);

        var done = app.Db(db => db.Jobs.Single(j => j.Kind == JobKind.PullAll));
        Assert.Equal(JobStatus.Done, done.Status);
        var lines = LogsOf(app, done.Id);
        Assert.Contains(lines, l => l.Level == JobLogLevel.Info && l.Message.StartsWith("Works:"));
        Assert.Contains(lines, l => l.Level == JobLogLevel.Error && l.Message.StartsWith("Gone: update failed"));
    }

    [Fact]
    public void The_next_reset_of_a_YouTube_limit_is_midnight_Pacific_time()
    {
        // 2026-10-20 08:00 UTC is 01:00 in Los Angeles (PDT, UTC-7); the day ends 07:00 UTC the next morning
        Assert.Equal(new DateTime(2026, 10, 21, 7, 0, 0, DateTimeKind.Utc), StatusEndpoints.NextPacificMidnight(new DateTime(2026, 10, 20, 8, 0, 0, DateTimeKind.Utc)));
        // after the autumn clock change the offset is UTC-8
        Assert.Equal(new DateTime(2026, 11, 28, 8, 0, 0, DateTimeKind.Utc), StatusEndpoints.NextPacificMidnight(new DateTime(2026, 11, 27, 12, 0, 0, DateTimeKind.Utc)));
    }
}
