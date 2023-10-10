using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlaylistChaser.Web.Controllers;
using PlaylistChaser.Web.Database;
using PlaylistChaser.Web.Models;

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

#region Hangfire
// Add Hangfire services.
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireConnection")));

// Add the processing server as IHostedService
builder.Services.AddHangfireServer();
#endregion

// Add configuration sources
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
environment ??= "Production"; //TODO: do it in right
builder.Configuration
       .SetBasePath(builder.Environment.ContentRootPath)
       .AddJsonFile($"appsettings.{environment}.json", optional: false, reloadOnChange: true);

builder.Services.AddDbContext<AdminDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ServerConnectionString")));

builder.Services.AddScoped<SongController>();

builder.Services.AddIdentity<User, IdentityRole<int>>()
        .AddEntityFrameworkStores<AdminDBContext>()
        .AddDefaultTokenProviders();

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

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapHub<ProgressHub>("/progressHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Playlist}/{action=Index}/{id?}");

app.UseHangfireDashboard();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHangfireDashboard();
});

app.Run();
