using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Writers;
using PlaylistChaser.Api.Database;
using System;
using System.IO;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // Adds Swagger generation

// Register DbContext with PostgreSQL
builder.Services.AddDbContext<AdminDBContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        builder => builder
            .WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
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
