using System.Security.Claims;
using Cronos;
using Microsoft.EntityFrameworkCore;
using PlaylistChaser.Data;
using PlaylistChaser.Jobs;

namespace PlaylistChaser.Tasks;

/// <summary>Runs users' scheduled tasks when their cron says so. Cron times use the server's time zone (TZ).</summary>
public class Scheduler(IServiceScopeFactory scopes, JobQueue queue) : BackgroundService
{
    public static readonly JobKind[] Kinds = [JobKind.PullAll, JobKind.PushAll, JobKind.SyncAll];

    /// <summary>Every new user starts with these; they can change or delete them on the Tasks page.</summary>
    public static void AddDefaults(AppDb db, string userId) => db.ScheduledTasks.AddRange(
        Default(userId, JobKind.PullAll, "0 */6 * * *"),
        Default(userId, JobKind.PushAll, "30 */6 * * *")); // after the fetch

    private static ScheduledTask Default(string userId, JobKind kind, string cron) =>
        new() { UserId = userId, Kind = kind, Cron = cron, Enabled = true, NextRunAt = NextRun(cron, DateTime.UtcNow) };

    public static DateTime? NextRun(string cron, DateTime after) =>
        CronExpression.TryParse(cron, out var expression) ? expression.GetNextOccurrence(after, TimeZoneInfo.Local) : null;

    /// <summary>Queues every enabled task that is due and schedules its next run.</summary>
    public static async Task RunDue(AppDb db, JobQueue queue, DateTime now)
    {
        var due = await db.ScheduledTasks.Where(t => t.Enabled && (t.NextRunAt == null || t.NextRunAt <= now)).ToListAsync();
        foreach (var task in due)
        {
            if (task.NextRunAt != null) // null = never scheduled yet: just schedule, don't run immediately
            {
                await queue.Enqueue(db, task.UserId, task.Kind);
                task.LastRunAt = now;
            }
            task.NextRunAt = NextRun(task.Cron, now);
        }
        await db.SaveChangesAsync();
    }

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stop))
        {
            using var scope = scopes.CreateScope();
            await RunDue(scope.ServiceProvider.GetRequiredService<AppDb>(), queue, DateTime.UtcNow);
        }
    }
}

public static class TaskEndpoints
{
    public record TaskRequest(JobKind Kind, string Cron, bool Enabled);

    public static void MapTaskEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/tasks", async (ClaimsPrincipal user, AppDb db) =>
            await db.ScheduledTasks.Where(t => t.UserId == user.Id()).OrderBy(t => t.Id).ToListAsync());

        api.MapPost("/tasks", async (TaskRequest req, ClaimsPrincipal user, AppDb db) =>
        {
            var task = new ScheduledTask { UserId = user.Id() };
            if (Apply(task, req) is { } problem) return problem;
            db.ScheduledTasks.Add(task);
            await db.SaveChangesAsync();
            return Results.Ok(task);
        });

        api.MapPut("/tasks/{id:int}", async (int id, TaskRequest req, ClaimsPrincipal user, AppDb db) =>
        {
            var task = await db.ScheduledTasks.SingleOrDefaultAsync(t => t.Id == id && t.UserId == user.Id());
            if (task == null) return Results.NotFound();
            if (Apply(task, req) is { } problem) return problem;
            await db.SaveChangesAsync();
            return Results.Ok(task);
        });

        api.MapDelete("/tasks/{id:int}", async (int id, ClaimsPrincipal user, AppDb db) =>
            await db.ScheduledTasks.Where(t => t.Id == id && t.UserId == user.Id()).ExecuteDeleteAsync() == 0
                ? Results.NotFound() : Results.NoContent());

        api.MapPost("/tasks/{id:int}/run", async (int id, ClaimsPrincipal user, AppDb db, JobQueue queue) =>
        {
            var task = await db.ScheduledTasks.SingleOrDefaultAsync(t => t.Id == id && t.UserId == user.Id());
            if (task == null) return Results.NotFound();
            task.LastRunAt = DateTime.UtcNow;
            var job = await queue.Enqueue(db, task.UserId, task.Kind);
            await db.SaveChangesAsync();
            return Results.Ok(job);
        });
    }

    private static IResult? Apply(ScheduledTask task, TaskRequest req)
    {
        if (!Scheduler.Kinds.Contains(req.Kind))
            return Results.Problem("Unknown task type.", statusCode: 400);
        var next = Scheduler.NextRun(req.Cron.Trim(), DateTime.UtcNow);
        if (next == null)
            return Results.Problem("That isn't a valid cron schedule (5 fields: minute hour day month weekday, e.g. \"0 */6 * * *\").", statusCode: 400);
        (task.Kind, task.Cron, task.Enabled, task.NextRunAt) = (req.Kind, req.Cron.Trim(), req.Enabled, next);
        return null;
    }
}
