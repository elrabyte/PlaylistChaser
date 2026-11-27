using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PlaylistChaser.Auth;
using PlaylistChaser.Data;

namespace PlaylistChaser.Status;

public record ServerLogLine(DateTime At, LogLevel Level, string Category, string Message);

/// <summary>Keeps the last server log lines in memory so the Status page can show them without a docker shell.</summary>
// shortcut: in memory only, so it is empty after a restart; write to the DB if you want history across restarts
public sealed class ServerLogBuffer : ILoggerProvider
{
    private const int Capacity = 1000;
    private readonly ConcurrentQueue<ServerLogLine> lines = new();

    public IReadOnlyList<ServerLogLine> Recent(LogLevel minimum) =>
        lines.Where(l => l.Level >= minimum).Reverse().ToList();

    public ILogger CreateLogger(string categoryName) => new BufferLogger(this, categoryName);
    public void Dispose() { }

    private void Add(ServerLogLine line)
    {
        lines.Enqueue(line);
        while (lines.Count > Capacity) lines.TryDequeue(out _);
    }

    private sealed class BufferLogger(ServerLogBuffer buffer, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var message = formatter(state, exception);
            if (exception != null) message += " — " + exception.GetType().Name + ": " + exception.Message;
            buffer.Add(new ServerLogLine(DateTime.UtcNow, logLevel, category, message));
        }
    }
}

public static class StatusEndpoints
{
    public static void MapStatusEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/status", async (ClaimsPrincipal user, AppDb db) =>
        {
            var userId = user.Id();
            var tasks = await db.ScheduledTasks.Where(t => t.UserId == userId).OrderBy(t => t.Id).ToListAsync();
            var latestJobs = await db.Jobs.Where(j => j.UserId == userId).OrderByDescending(j => j.Id).Take(200).ToListAsync();

            // three plain queries instead of one nested one: SQLite (tests, dev) can't translate the nested form
            var owned = await db.Playlists.Owned(userId).OrderBy(p => p.Name).AsNoTracking().ToListAsync();
            var ids = owned.Select(p => p.Id).ToList();
            var songCounts = await db.PlaylistSongs.Where(ps => ids.Contains(ps.PlaylistId))
                .GroupBy(ps => ps.PlaylistId).Select(g => new { Id = g.Key, Count = g.Count() }).ToDictionaryAsync(g => g.Id, g => g.Count);
            var links = (await db.PlaylistLinks.Where(l => ids.Contains(l.PlaylistId)).AsNoTracking().ToListAsync()).ToLookup(l => l.PlaylistId);
            var pushed = await db.PushedSongs.Where(x => ids.Contains(x.PlaylistId))
                .GroupBy(x => new { x.PlaylistId, x.Source }).Select(g => new { g.Key.PlaylistId, g.Key.Source, Count = g.Count() }).ToListAsync();
            var playlists = owned.Select(p => new
            {
                p.Id,
                p.Name,
                p.Kind,
                p.MainSource,
                p.LastPulledAt,
                SongCount = songCounts.GetValueOrDefault(p.Id),
                Links = links[p.Id].Select(l => new
                {
                    l.Source,
                    l.LastPushedAt,
                    Pushed = pushed.FirstOrDefault(c => c.PlaylistId == p.Id && c.Source == l.Source)?.Count ?? 0,
                }),
            }).ToList();

            return new
            {
                tasks = tasks.Select(t =>
                {
                    var last = latestJobs.FirstOrDefault(j => j.Kind == t.Kind);
                    return new
                    {
                        t.Id,
                        t.Kind,
                        t.Cron,
                        t.Enabled,
                        t.LastRunAt,
                        t.NextRunAt,
                        lastResult = last == null ? null : new { last.Status, last.FinishedAt, last.Message },
                    };
                }),
                playlists,
                limit = await ActiveLimit(db, userId),
            };
        });

        // a job's own lines, in the order they happened
        api.MapGet("/jobs/{id:int}/logs", async (int id, ClaimsPrincipal user, AppDb db) =>
            !await db.Jobs.AnyAsync(j => j.Id == id && j.UserId == user.Id()) ? Results.NotFound()
            : Results.Ok(await db.JobLogs.Where(l => l.JobId == id).OrderBy(l => l.At).ThenBy(l => l.Id).ToListAsync()));

        // raw server output: it names every user's playlists and errors, so it is for the admin only
        api.MapGet("/logs", (ServerLogBuffer buffer, LogLevel? level) => buffer.Recent(level ?? LogLevel.Information))
            .RequireAuthorization(p => p.RequireRole(AuthEndpoints.AdminRole));
    }

    /// <summary>The newest "daily limit reached" line, while that service's limit has not reset yet. YouTube resets at midnight Pacific time.</summary>
    private static async Task<object?> ActiveLimit(AppDb db, string userId)
    {
        var line = await db.JobLogs.Where(l => l.IsQuota && db.Jobs.Any(j => j.Id == l.JobId && j.UserId == userId))
            .OrderByDescending(l => l.At).FirstOrDefaultAsync();
        if (line == null) return null;
        var resetsAt = NextPacificMidnight(line.At);
        return resetsAt > DateTime.UtcNow ? new { line.Message, line.At, resetsAt } : null;
    }

    public static DateTime NextPacificMidnight(DateTime utc)
    {
        var pacific = TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), pacific);
        var midnight = DateTime.SpecifyKind(local.Date.AddDays(1), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(midnight, pacific);
    }
}
