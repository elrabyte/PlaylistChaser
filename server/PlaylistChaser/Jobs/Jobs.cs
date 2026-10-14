using System.Security.Claims;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using PlaylistChaser.Data;
using PlaylistChaser.Sync;

namespace PlaylistChaser.Jobs;

// shortcut: in-process queue with one worker, single instance only; move to a shared queue if you ever run 2+ replicas
public class JobQueue
{
    private readonly Channel<int> channel = Channel.CreateUnbounded<int>();
    public ChannelReader<int> Reader => channel.Reader;

    /// <summary>Queues a job, or returns the one already waiting/running for the same work.</summary>
    public async Task<Job> Enqueue(AppDb db, string userId, JobKind kind, int? playlistId = null, Source? target = null)
    {
        var active = await db.Jobs.FirstOrDefaultAsync(j => j.UserId == userId && j.Kind == kind && j.PlaylistId == playlistId
            && j.Target == target && (j.Status == JobStatus.Queued || j.Status == JobStatus.Running));
        if (active != null) return active;

        var job = new Job { UserId = userId, Kind = kind, PlaylistId = playlistId, Target = target, Status = JobStatus.Queued, CreatedAt = DateTime.UtcNow };
        db.Jobs.Add(job);
        await db.SaveChangesAsync();
        await channel.Writer.WriteAsync(job.Id);
        return job;
    }
}

public class JobRunner(IServiceScopeFactory scopes, JobQueue queue, ILogger<JobRunner> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        await foreach (var jobId in queue.Reader.ReadAllAsync(stop))
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            var job = await db.Jobs.FindAsync([jobId], stop);
            if (job == null) continue;
            job.Status = JobStatus.Running;
            await db.SaveChangesAsync(stop);
            try
            {
                await scope.ServiceProvider.GetRequiredService<SyncService>().Run(job);
                job.Status = JobStatus.Done;
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Job {JobId} ({Kind}) failed", job.Id, job.Kind);
                job.Status = JobStatus.Failed;
                job.Message = ex.Message;
            }
            job.FinishedAt = DateTime.UtcNow;
            db.ChangeTracker.Clear(); // a failed SaveChanges inside the job must not block saving the job's result
            db.Jobs.Update(job);
            await db.SaveChangesAsync(stop);
        }
    }
}

/// <summary>Once a day per user: pull every playlist, then push it to the services it was pushed to before.</summary>
public class DailySync(IServiceScopeFactory scopes, JobQueue queue) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(stop))
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            var since = DateTime.UtcNow.AddHours(-23);
            var due = await db.Playlists.Select(p => p.UserId).Distinct()
                .Where(u => !db.Jobs.Any(j => j.UserId == u && j.Kind == JobKind.SyncAll && j.CreatedAt > since))
                .ToListAsync(stop);
            foreach (var userId in due)
                await queue.Enqueue(db, userId, JobKind.SyncAll);
        }
    }
}

public static class JobEndpoints
{
    public static void MapJobEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/jobs", async (ClaimsPrincipal user, AppDb db) =>
            await db.Jobs.Where(j => j.UserId == user.Id()).OrderByDescending(j => j.Id).Take(20).ToListAsync());

        api.MapPost("/sync", async (ClaimsPrincipal user, AppDb db, JobQueue queue) =>
            await queue.Enqueue(db, user.Id(), JobKind.SyncAll));
    }
}
