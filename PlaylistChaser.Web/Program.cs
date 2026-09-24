using ats.Desk.Web.Util;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PlaylistChaser.Web.Controllers;
using PlaylistChaser.Web.Database;
using PlaylistChaser.Web.Database.Abstractions;
using PlaylistChaser.Web.Models;
using PlaylistChaser.Web.Util;
using PlaylistChaser.Web.Util.API;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddControllersWithViews();

//session
builder.Services.AddDistributedMemoryCache();

#region Hangfire
builder.Services.AddSession();
builder.Services.AddSignalR();
#endregion

builder.Services.AddMemoryCache();

// Add configuration sources
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
environment ??= "Production"; //TODO: do it in right
builder.Configuration
       .SetBasePath(builder.Environment.ContentRootPath)
       .AddJsonFile($"appsettings.{environment}.json", optional: false, reloadOnChange: true);

// Database provider: "SqlServer" (default, existing production behavior with native per-user
// row-level security) or "Postgres" (portable path used e.g. by docker-compose). See
// Database/Abstractions/IPlaylistDataStore.cs and AGENTS.md for the full rationale.
var databaseProvider = builder.Configuration["Database:Provider"] ?? "SqlServer";

#region Hangfire
// Add Hangfire services. SQL Server storage is used in the default/production configuration;
// the Postgres/Docker path uses in-memory storage to avoid an extra required package (jobs
// don't survive a restart there - acceptable for local/dev/demo use, see README.md).
builder.Services.AddHangfire(configuration =>
{
    configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings();

    if (string.Equals(databaseProvider, "SqlServer", StringComparison.OrdinalIgnoreCase))
        configuration.UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireConnection"));
    else
        configuration.UseMemoryStorage();
});

// Add the processing server as IHostedService
builder.Services.AddHangfireServer();
#endregion

builder.Services.AddDbContext<AdminDBContext>(options =>
{
    if (string.Equals(databaseProvider, "SqlServer", StringComparison.OrdinalIgnoreCase))
        options.UseSqlServer(builder.Configuration.GetConnectionString("ServerConnectionString"));
    else
        options.UseNpgsql(builder.Configuration.GetConnectionString("ServerConnectionString"));
});

builder.Services.AddScoped<SongController>();

// Swappable music-service backend seam (see Util/API/ISourceFactory.cs).
builder.Services.AddSingleton<ISourceFactory, SourceFactory>();

// Swappable database backend seam (see Database/Abstractions).
builder.Services.AddScoped<IPlaylistDataStoreFactory, PlaylistDataStoreFactory>();

// JWT bearer auth for the REST API (Controllers/Api) used by the React SPA / mobile clients.
// The existing MVC/Razor UI is untouched and keeps using cookie auth.
builder.Services.AddSingleton<JwtTokenHelper>();

builder.Services.AddIdentity<User, IdentityRole<int>>()
        .AddEntityFrameworkStores<AdminDBContext>()
        .AddDefaultTokenProviders();

builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "PlaylistChaser",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "PlaylistChaser",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey ?? "insecure-development-key-change-me-insecure-development-key")),
        };
    });

// CORS for the React SPA (PlaylistChaser.Client), which runs on a different origin during
// development (Vite dev server) and in Docker (its own nginx container).
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("SpaClient", policy =>
    {
        if (corsOrigins.Length > 0)
            policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});


builder.Services.Configure<IdentityOptions>(options =>
{
    // Password settings.
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings.
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(60);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings.
    options.User.AllowedUserNameCharacters =
    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = false;
});
builder.Services.ConfigureApplicationCookie(options =>
{
    // Cookie settings
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);

    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
});

var app = builder.Build();

// Portable/Docker path: apply EF Core migrations and seed the small set of built-in lookup
// rows (Administrator role, Youtube/Spotify sources) that the SQL Server deployment normally
// gets from PlaylistChaser.DB's InsertRoles.sql/InsertSources.sql. Not used for the default
// SQL Server provider, which keeps using the existing .sqlproj-managed schema.
if (!string.Equals(databaseProvider, "SqlServer", StringComparison.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AdminDBContext>();
    db.Database.Migrate();

    if (!db.Set<IdentityRole<int>>().Any())
        db.Set<IdentityRole<int>>().Add(new IdentityRole<int> { Id = (int)BuiltInIds.Roles.Administrator, Name = "Administrator", NormalizedName = "ADMINISTRATOR" });

    if (!db.Source.Any())
    {
        db.Source.AddRange(
            new Source { Id = (int)BuiltInIds.Sources.Youtube, Name = "Youtube", IconHtml = "<i class=\"bi bi-youtube\"></i>", ColorHex = "ff0000" },
            new Source { Id = (int)BuiltInIds.Sources.Spotify, Name = "Spotify", IconHtml = "<i class=\"bi bi-spotify\"></i>", ColorHex = "1DB954" });
    }

    db.SaveChanges();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    //app.UseExceptionHandler("/Playlist/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "node_modules")
    ),
    RequestPath = "/node_modules"
});

app.UseRouting();

app.UseCors("SpaClient");

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapHub<ProgressHub>("/progressHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Playlist}/{action=Index}/{id?}");

app.UseHangfireDashboard("/hangfire", new DashboardOptions { Authorization = new[] { new HangfireAuthorization() } });

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHangfireDashboard();
});

app.Run();
