using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PlaylistChaser.Data;
using PlaylistChaser.Sources;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace PlaylistChaser.Plugins;

public record PluginDef(string Id, string Url);

/// <summary>PLUGINS=download=http://downloader:8080 (comma separated). PLUGIN_TOKEN is the secret each plugin sends back.</summary>
public static class PluginConfig
{
    public static IReadOnlyList<PluginDef> Parse(string? setting) =>
        (setting ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(entry => entry.Split('=', 2).Select(part => part.Trim()).ToArray() is [var id, var url] && id.Length > 0 && url.Length > 0
                ? new PluginDef(id, url.TrimEnd('/'))
                : throw new InvalidOperationException($"PLUGINS entry '{entry}' must look like id=http://host:port."))
            .ToList();

    /// <summary>Routes /plugins/{id}/... to the plugin's container; only signed-in users reach them, and the user id is passed on.</summary>
    public static void AddProxy(this IServiceCollection services, IReadOnlyList<PluginDef> plugins) =>
        services.AddReverseProxy()
            .LoadFromMemory(
                plugins.Select(p => new RouteConfig
                {
                    RouteId = p.Id,
                    ClusterId = p.Id,
                    Match = new RouteMatch { Path = $"/plugins/{p.Id}/{{**rest}}" },
                    Transforms = [new Dictionary<string, string> { ["PathRemovePrefix"] = $"/plugins/{p.Id}" }],
                }).ToList(),
                plugins.Select(p => new ClusterConfig
                {
                    ClusterId = p.Id,
                    Destinations = new Dictionary<string, DestinationConfig> { ["plugin"] = new() { Address = p.Url } },
                }).ToList())
            .AddTransforms(ctx => ctx.RequestTransforms.Add(new PassUserTransform()));

    /// <summary>The plugin gets the user's id, never their login cookie or other credentials.</summary>
    private sealed class PassUserTransform : RequestTransform
    {
        public override ValueTask ApplyAsync(RequestTransformContext context)
        {
            RemoveHeader(context, "Cookie");
            RemoveHeader(context, "Authorization");
            AddHeader(context, "X-PlaylistChaser-User", context.HttpContext.User.Id());
            return ValueTask.CompletedTask;
        }
    }
}

/// <summary>What the plugin can ask PlaylistChaser for. Every call needs the shared token and names the user it acts for.</summary>
public static class PluginApi
{
    public static void MapPluginMenu(this RouteGroupBuilder api, IReadOnlyList<PluginDef> plugins, IHttpClientFactory http) =>
        api.MapGet("/plugins", async () =>
        {
            var client = http.CreateClient("plugins");
            var menu = new List<object>();
            foreach (var p in plugins)
            {
                var name = p.Id;
                try { name = (await client.GetFromJsonAsync<JsonElement>(p.Url + "/manifest")).GetProperty("name").GetString() ?? p.Id; }
                catch (Exception) { } // a plugin that is down still gets its menu entry, under its id
                menu.Add(new { id = p.Id, name });
            }
            return menu;
        });

    private static string UserOf(HttpContext c) => (string)c.Items["user"]!;

    public static void MapPluginApi(this WebApplication app, string? token)
    {
        var api = app.MapGroup("/api/plugin").AddEndpointFilter(async (ctx, next) =>
        {
            var given = Encoding.UTF8.GetBytes(ctx.HttpContext.Request.Headers["X-Plugin-Token"].ToString());
            var expected = Encoding.UTF8.GetBytes(token ?? "");
            if (expected.Length == 0 || !CryptographicOperations.FixedTimeEquals(given, expected)) return Results.Unauthorized();

            var userId = ctx.HttpContext.Request.Headers["X-PlaylistChaser-User"].ToString();
            var db = ctx.HttpContext.RequestServices.GetRequiredService<AppDb>();
            if (userId.Length == 0 || !await db.Users.AnyAsync(u => u.Id == userId)) return Results.Unauthorized();
            ctx.HttpContext.Items["user"] = userId;
            return await next(ctx);
        });

        api.MapGet("/sources", () => Providers.All.Select(p => new
        {
            id = p.Id,
            name = p.Name,
            capabilities = Enum.GetValues<Capability>().Where(c => p.Capabilities.HasFlag(c)).Select(c => c.ToString().ToLowerInvariant()),
        }));

        api.MapGet("/playlists", async (HttpContext c, AppDb db) =>
            await db.Playlists.Owned(UserOf(c)).OrderBy(p => p.Name)
                .Select(p => new { p.Id, p.Name, p.Kind, p.MainSource }).ToListAsync());

        api.MapGet("/playlists/{id:int}/songs", async (int id, HttpContext c, AppDb db) =>
        {
            if (!await db.Playlists.Owned(UserOf(c)).AnyAsync(p => p.Id == id)) return Results.NotFound();
            var songs = await db.Songs.Include(s => s.Links)
                .Where(s => db.PlaylistSongs.Any(ps => ps.PlaylistId == id && ps.SongId == s.Id)).ToListAsync();
            return Results.Ok(songs.Select(s => new
            {
                s.Id,
                s.Name,
                s.Artist,
                links = s.Links.Select(l => new { l.Source, l.Status, l.ExternalId }),
            }));
        });

        // finds the song on a service the way a push does (free search), and remembers the answer
        api.MapPost("/songs/{id:int}/match/{source}", async (int id, string source, HttpContext c, AppDb db, ISourceProvider sources) =>
        {
            if (!ApiHelpers.TryParseSource(source, out var s)) return Results.NotFound();
            var song = await db.Songs.Owned(UserOf(c)).Include(x => x.Links).SingleOrDefaultAsync(x => x.Id == id);
            if (song == null) return Results.NotFound();
            var link = song.Links.SingleOrDefault(l => l.Source == s);
            if (link?.ExternalId == null)
            {
                var found = await (await sources.GetDestination(UserOf(c), s)).Search(song.Name, song.Artist);
                if (link == null) song.Links.Add(link = new SongLink { Source = s });
                (link.ExternalId, link.Url) = (found?.Id, found == null ? null : SourceUrls.Track(s, found.Id));
                (link.Status, link.CheckedAt) = (found == null ? LinkStatus.NotFound : LinkStatus.Found, DateTime.UtcNow);
                await db.SaveChangesAsync();
            }
            return Results.Ok(new { link.ExternalId, link.Status });
        });

        api.MapGet("/metadata/{source}/{trackId}", async (string source, string trackId, HttpContext c, ISourceProvider sources) =>
        {
            if (!ApiHelpers.TryParseSource(source, out var s)) return Results.NotFound();
            var service = await sources.Get(UserOf(c), s);
            if (service is not IMetadataSource metadata) return Results.NotFound();
            var track = await metadata.GetMetadata(trackId);
            return track == null ? Results.NotFound() : Results.Ok(track);
        });

        api.MapPost("/jobs", async (PluginJob req, HttpContext c, AppDb db) =>
        {
            var job = new Job { UserId = UserOf(c), Kind = JobKind.Plugin, Status = JobStatus.Running, Title = req.Title, Total = req.Total ?? 0, CreatedAt = DateTime.UtcNow };
            db.Jobs.Add(job);
            await db.SaveChangesAsync();
            return Results.Ok(new { job.Id });
        });

        api.MapPatch("/jobs/{id:int}", async (int id, JobUpdate req, HttpContext c, AppDb db) =>
        {
            var job = await db.Jobs.SingleOrDefaultAsync(j => j.Id == id && j.UserId == UserOf(c));
            if (job == null) return Results.NotFound();
            if (req.Status is { } status) { job.Status = status; if (status is JobStatus.Done or JobStatus.Failed) job.FinishedAt = DateTime.UtcNow; }
            if (req.Done is { } done) job.Done = done;
            if (req.Total is { } total) job.Total = total;
            if (req.Message is not null) job.Message = req.Message;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        api.MapPost("/jobs/{id:int}/logs", async (int id, PluginLog req, HttpContext c, AppDb db) =>
        {
            if (!await db.Jobs.AnyAsync(j => j.Id == id && j.UserId == UserOf(c))) return Results.NotFound();
            db.JobLogs.Add(new JobLog { JobId = id, At = DateTime.UtcNow, Level = req.Level, Message = req.Message, IsQuota = req.IsQuota });
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}

public record PluginJob(string Title, int? Total);
public record JobUpdate(JobStatus? Status, int? Done, int? Total, string? Message);
public record PluginLog(JobLogLevel Level, string Message, bool IsQuota = false);
