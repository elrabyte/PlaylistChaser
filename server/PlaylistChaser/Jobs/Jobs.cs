using System.Security.Claims;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using PlaylistChaser.Data;
using PlaylistChaser.Sources;
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
            var sync = scope.ServiceProvider.GetRequiredService<SyncService>();
            log.LogInformation("Job {JobId} ({Kind}) started", job.Id, job.Kind);
            try
            {
                await sync.Run(job);
                job.Status = JobStatus.Done;
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Job {JobId} ({Kind}) failed", job.Id, job.Kind);
                job.Status = JobStatus.Failed;
                job.Message = ex.Message;
                var quota = ex is QuotaExceededException;
                // a push already wrote a clearer "limit reached" line; reading a playlist did not
                var hasQuotaLine = db.ChangeTracker.Entries<JobLog>().Any(e => e.Entity.IsQuota) || await db.JobLogs.AnyAsync(l => l.JobId == job.Id && l.IsQuota, stop);
                if (!quota || !hasQuotaLine)
                    sync.Note(job, quota ? JobLogLevel.Warn : JobLogLevel.Error, ex.Message, isQuota: quota);
            }
            job.FinishedAt = DateTime.UtcNow;
            // a failed SaveChanges inside the job must not block saving the job's result; its unsaved log lines are kept
            var lines = db.ChangeTracker.Entries<JobLog>().Where(e => e.State == EntityState.Added).Select(e => e.Entity).ToList();
            db.ChangeTracker.Clear();
            db.JobLogs.AddRange(lines);
            db.Jobs.Update(job);
            await db.SaveChangesAsync(stop);
            log.LogInformation("Job {JobId} ({Kind}) {Status}", job.Id, job.Kind, job.Status);
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
