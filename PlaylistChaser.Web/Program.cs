using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using PlaylistChaser.Web.Database;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

//session
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession();


// Add configuration sources
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
environment ??= "Production"; //TODO: do it in right
builder.Configuration
       .SetBasePath(builder.Environment.ContentRootPath)
       .AddJsonFile($"appsettings.{environment}.json", optional: false, reloadOnChange: true);

#if DEBUG == false
builder.Services.AddDbContext<PlaylistChaserDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ServerConnectionString")));
#else
builder.Services.AddDbContext<PlaylistChaserDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("LocalConnectionString")));
#endif

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Playlist/Error");
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Playlist}/{action=Index}/{id?}");


app.Run();
