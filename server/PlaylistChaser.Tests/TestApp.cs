using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PlaylistChaser.Data;
using PlaylistChaser.Jobs;
using PlaylistChaser.Sources;
using PlaylistChaser.Sync;

namespace PlaylistChaser.Tests;

/// <summary>The real app on a throwaway database (SQLite file, or Postgres when TEST_POSTGRES is set, as in CI)
/// with fake music services and no background workers.</summary>
public sealed class TestApp : WebApplicationFactory<Program>
{
    public const string AdminPassword = "admin-password-1";
    public FakeSources Sources { get; } = new();
    private readonly string dir = Directory.CreateTempSubdirectory("playlistchaser-test").FullName;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var postgres = Environment.GetEnvironmentVariable("TEST_POSTGRES");
        builder.UseSetting("DB_CONNECTION", postgres is { Length: > 0 }
            ? $"{postgres};Database=pc_test_{Guid.NewGuid():N}"
            : $"Data Source={Path.Combine(dir, "test.db")}");
        builder.UseSetting("PUBLIC_URL", "http://localhost");
        builder.UseSetting("DATA_DIR", dir);
        builder.UseSetting("ADMIN_USERNAME", "admin");
        builder.UseSetting("ADMIN_PASSWORD", AdminPassword);
        builder.UseSetting("YOUTUBE_CLIENT_ID", "yt-id");
        builder.UseSetting("YOUTUBE_CLIENT_SECRET", "yt-secret");
        builder.UseSetting("SPOTIFY_CLIENT_ID", "sp-id");
        builder.UseSetting("SPOTIFY_CLIENT_SECRET", "sp-secret");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ISourceProvider>();
            services.AddSingleton<ISourceProvider>(Sources);
            foreach (var worker in services.Where(d => d.ImplementationType == typeof(JobRunner) || d.ImplementationType == typeof(DailySync)).ToList())
                services.Remove(worker);
        });
    }

    public async Task<HttpClient> Login(string user = "admin", string password = AdminPassword)
    {
        var client = CreateClient();
        (await client.PostAsJsonAsync("/api/auth/login", new { userName = user, password })).EnsureSuccessStatusCode();
        return client;
    }

    public async Task<HttpClient> NewUser(HttpClient admin, string name)
    {
        (await admin.PostAsJsonAsync("/api/users", new { userName = name, password = "user-password-1", isAdmin = false })).EnsureSuccessStatusCode();
        return await Login(name, "user-password-1");
    }


    /// <summary>Runs a job inline, the way the background worker would.</summary>
    public async Task<Job> Run(string userName, JobKind kind, int? playlistId = null, Source? target = null)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();
        var userId = db.Users.Single(u => u.UserName == userName).Id;
        var job = new Job { UserId = userId, Kind = kind, PlaylistId = playlistId, Target = target, Status = JobStatus.Running, CreatedAt = DateTime.UtcNow };
        db.Jobs.Add(job);
        await db.SaveChangesAsync();
        await scope.ServiceProvider.GetRequiredService<SyncService>().Run(job);
        await db.SaveChangesAsync();
        return job;
    }

    public T Db<T>(Func<AppDb, T> query)
    {
        using var scope = Services.CreateScope();
        return query(scope.ServiceProvider.GetRequiredService<AppDb>());
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        try { Directory.Delete(dir, true); } catch (IOException) { } // SQLite may still hold the file briefly
    }
}
