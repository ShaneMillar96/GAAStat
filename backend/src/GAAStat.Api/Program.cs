using Microsoft.EntityFrameworkCore;
using GAAStat.Dal.Contexts;
using GAAStat.Dal.Interfaces;
using GAAStat.Dal;
using GAAStat.Services.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for large file uploads
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 104857600; // 100MB
});

// Add services to the container.
builder.Services.AddDbContext<GAAStatDbContext>(options =>
    options.UseNpgsql(EnvironmentVariables.DatabaseConnectionString));

builder.Services.AddScoped<IGAAStatDbContext>(provider =>
    provider.GetService<GAAStatDbContext>()!);

// Register ETL services (match and player statistics)
builder.Services.AddGAAStatisticsEtlServices();

// Configure file upload limits
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 104857600; // 100MB
    options.ValueLengthLimit = 104857600;
    options.MultipartHeadersLengthLimit = 104857600;
});

builder.Services.AddControllers();

// Configure CORS for frontend development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();
