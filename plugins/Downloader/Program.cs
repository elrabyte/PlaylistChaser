using Downloader;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
builder.Services.AddSingleton<Worker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<Worker>());
var app = builder.Build();

var musicRoot = Path.GetFullPath(app.Configuration["MUSIC_ROOT"] ?? "/music");
var configDir = app.Configuration["CONFIG_DIR"] ?? "/config";
var appUrl = app.Configuration["APP_URL"] ?? "http://app:8080";
var token = app.Configuration["PLUGIN_TOKEN"] ?? "";

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/manifest", () => new { name = "Download music" });

// the proxy in PlaylistChaser sets this to the signed-in user; the plugin only acts for that user
static string? UserOf(HttpContext c) => c.Request.Headers["X-PlaylistChaser-User"].ToString() is { Length: > 0 } id ? id : null;

app.MapGet("/api/options", async (HttpContext c, IHttpClientFactory http) =>
{
    var userId = UserOf(c);
    if (userId == null) return Results.Unauthorized();
    var client = new AppClient(http.CreateClient(), appUrl, token, userId);
    var services = await client.Services();
    var playlists = await client.Playlists();
    var settings = await Library.LoadAsync<Settings>(Library.SettingsPath(configDir));
    return Results.Ok(new
    {
        playlists,
        downloadFrom = services.Where(s => s.Capabilities.Contains("downloadable")).Select(s => new { s.Id, s.Name }),
        metadataFrom = services.Where(s => s.Capabilities.Contains("metadata")).Select(s => new { s.Id, s.Name }),
        settings,
    });
});

app.MapPut("/api/settings", async (Settings req, HttpContext c) =>
{
    var userId = UserOf(c);
    if (userId == null) return Results.Unauthorized();
    var folder = Path.GetFullPath(req.Folder);
    if (folder != musicRoot && !folder.StartsWith(musicRoot + Path.DirectorySeparatorChar))
        return Results.BadRequest($"The folder must be inside {musicRoot}, the folder mounted into this container.");
    if (req.DownloadFrom != "youtube")
        return Results.BadRequest("Only YouTube can be downloaded from for now."); // shortcut: other sources need their own audio download
    if (!Cronos.CronExpression.TryParse(req.Cron, out _))
        return Results.BadRequest("The schedule is not a valid cron expression.");
    try
    {
        Directory.CreateDirectory(folder);
        var probe = Path.Combine(folder, ".playlistchaser-write-test");
        await File.WriteAllTextAsync(probe, "ok");
        File.Delete(probe);
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
        return Results.BadRequest($"Can't write to {folder}: {ex.Message}. Check the folder's permissions on the NAS.");
    }
    await Library.SaveAsync(Library.SettingsPath(configDir), req with { UserId = userId, Folder = folder });
    return Results.NoContent();
});

app.MapPost("/api/run", (HttpContext c, Worker worker) =>
    UserOf(c) == null ? Results.Unauthorized()
    : worker.RunNow() ? Results.Accepted() : Results.Conflict("A download is already running."));

app.Run();

