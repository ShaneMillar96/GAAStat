using Microsoft.EntityFrameworkCore;
using GAAStat.Dal.Contexts;
using GAAStat.Dal.Interfaces;
using GAAStat.Dal;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<GAAStatDbContext>(options =>
    options.UseNpgsql(EnvironmentVariables.DatabaseConnectionString));

builder.Services.AddScoped<IGAAStatDbContext>(provider =>
    provider.GetService<GAAStatDbContext>()!);

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
