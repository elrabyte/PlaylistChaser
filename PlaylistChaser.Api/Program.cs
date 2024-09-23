using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Writers;
using PlaylistChaser.Api.Database;
using PlaylistChaser.Model;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

builder.Services
    .AddIdentity<User, IdentityRole<int>>()
    .AddEntityFrameworkStores<AdminDBContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 4;
});

builder.Services.AddDbContext<AdminDBContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    var frontEndUrl = builder.Configuration.GetValue<string>("FrontEndUrl");
    options.AddPolicy("AllowFrontend",
        builder => builder
            .WithOrigins(frontEndUrl)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            );
});

var app = builder.Build();

app.UseCors("AllowFrontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c =>
    {
        c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
        {
            using (var streamWriter = new StringWriter())
            {
                var openApiWriter = new OpenApiJsonWriter(streamWriter);
                swaggerDoc.SerializeAsV3(openApiWriter);
                var json = streamWriter.ToString();
                File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "swagger.json"), json);
            }
        });
    });

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PlaylistChaser API V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
