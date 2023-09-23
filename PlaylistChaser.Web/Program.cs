using Microsoft.EntityFrameworkCore;
using PlaylistChaser.Web.Controllers;
using PlaylistChaser.Web.Database;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

//session
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession();

builder.Services.AddSignalR();

// Add configuration sources
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
environment ??= "Production"; //TODO: do it in right
builder.Configuration
       .SetBasePath(builder.Environment.ContentRootPath)
       .AddJsonFile($"appsettings.{environment}.json", optional: false, reloadOnChange: true);

builder.Services.AddDbContext<PlaylistChaserDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ServerConnectionString")));

builder.Services.AddScoped<SongController>();

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

app.UseAuthorization();

app.UseSession();

app.MapHub<ProgressHub>("/progressHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Playlist}/{action=Index}/{id?}");


app.Run();
