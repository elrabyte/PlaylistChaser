using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlaylistChaser;
using PlaylistChaser.Auth;
using PlaylistChaser.Data;
using PlaylistChaser.Jobs;
using PlaylistChaser.Playlists;
using PlaylistChaser.Sources;
using PlaylistChaser.Sync;
using PlaylistChaser.Tasks;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// PUBLIC_URL is where users reach the app; OAuth redirect URIs and installable-app support need https.
var publicUrl = config["PUBLIC_URL"];
if (!Uri.TryCreate(publicUrl, UriKind.Absolute, out var publicUri) || !(publicUri.Scheme == "https" || publicUri.IsLoopback))
    throw new InvalidOperationException("PUBLIC_URL must be the https address users open the app at (http is only allowed for localhost).");

// DB_CONNECTION overrides (dev/tests, "Data Source=..." means SQLite); otherwise the compose Postgres is used.
var connectionString = config["DB_CONNECTION"] ?? (config["POSTGRES_PASSWORD"] is { Length: > 0 } pw
    ? $"Host={config["DB_HOST"] ?? "db"};Database=playlistchaser;Username=playlistchaser;Password={pw}"
    : throw new InvalidOperationException("Set POSTGRES_PASSWORD (or DB_CONNECTION)."));
builder.Services.AddDbContext<AppDb>(o =>
{
    if (connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase)) o.UseSqlite(connectionString);
    else o.UseNpgsql(connectionString);
});

var dataDir = config["DATA_DIR"] ?? "/data";
builder.Services.AddDataProtection()
    .SetApplicationName("PlaylistChaser")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataDir, "keys")));

builder.Services.AddIdentity<AppUser, IdentityRole>(o =>
    {
        // length over character rules (NIST 800-63B)
        o.Password.RequiredLength = 10;
        o.Password.RequireDigit = o.Password.RequireLowercase = o.Password.RequireUppercase = o.Password.RequireNonAlphanumeric = false;
        o.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<AppDb>();
builder.Services.AddAuthorization();
builder.Services.ConfigureApplicationCookie(o =>
{
    o.Cookie.Name = "playlistchaser";
    o.Cookie.SameSite = SameSiteMode.Strict;
    o.ExpireTimeSpan = TimeSpan.FromDays(30);
    o.SlidingExpiration = true;
    o.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; };
    o.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; };
});

builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear(); // the reverse proxy sits in front of the container on an unknown address
    o.KnownProxies.Clear();
});
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

builder.Services.AddScoped<SourceAccounts>();
builder.Services.AddScoped<ISourceProvider>(sp => sp.GetRequiredService<SourceAccounts>());
builder.Services.AddScoped<SyncService>();
builder.Services.AddSingleton<JobQueue>();
builder.Services.AddHostedService<JobRunner>();
builder.Services.AddHostedService<Scheduler>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    if (db.Database.IsNpgsql()) await db.Database.MigrateAsync();
    else await db.Database.EnsureCreatedAsync();
    await db.Jobs.Where(j => j.Status == JobStatus.Queued || j.Status == JobStatus.Running)
        .ExecuteUpdateAsync(s => s.SetProperty(j => j.Status, JobStatus.Failed)
            .SetProperty(j => j.Message, "Interrupted by a restart.")
            .SetProperty(j => j.FinishedAt, DateTime.UtcNow));
    await AuthEndpoints.SeedAdmin(scope.ServiceProvider, config, app.Logger);
}

app.UseForwardedHeaders();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

var api = app.MapGroup("/api").RequireAuthorization();
api.MapGet("/health", () => "ok").AllowAnonymous();
api.MapAuthEndpoints();
api.MapSourceEndpoints();
api.MapPlaylistEndpoints();
api.MapJobEndpoints();
api.MapTaskEndpoints();
app.MapFallback("/api/{**rest}", () => Results.NotFound());
app.MapFallbackToFile("index.html"); // client-side routes of the SPA

app.Run();

public partial class Program;
