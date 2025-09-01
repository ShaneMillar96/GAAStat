using Microsoft.EntityFrameworkCore;
using GAAStat.Dal.Contexts;
using GAAStat.Dal.Interfaces;
using GAAStat.Dal;
using GAAStat.Services.Extensions;
using GAAStat.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container
// Database configuration
builder.Services.AddDbContext<GAAStatDbContext>(options =>
    options.UseNpgsql(EnvironmentVariables.DatabaseConnectionString));

builder.Services.AddScoped<IGAAStatDbContext>(provider =>
    provider.GetService<GAAStatDbContext>()!);

// Register all GAA Statistics services
builder.Services.AddGAAStatServices();

// Add memory caching for analytics performance
builder.Services.AddMemoryCache();

// Add controllers with custom JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Configure CORS for frontend development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000", "http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });

    // Add a more restrictive policy for production
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("https://yourdomain.com")
              .AllowAnyHeader()
              .WithMethods("GET", "POST", "PUT", "DELETE")
              .AllowCredentials();
    });
});

// Configure file validation options
var fileValidationOptions = FileValidationOptions.FromEnvironment();

// Override specific settings from environment variables
if (long.TryParse(Environment.GetEnvironmentVariable("MAX_FILE_SIZE_MB"), out var maxFileSizeMB))
{
    fileValidationOptions.MaxFileSizeBytes = maxFileSizeMB * 1024 * 1024;
}

builder.Services.AddSingleton(fileValidationOptions);

// Configure request size limits for file uploads
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = fileValidationOptions.MaxTotalSizeBytes;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

// Configure Kestrel server limits for large file uploads
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = fileValidationOptions.MaxTotalSizeBytes;
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(EnvironmentVariables.DatabaseConnectionString, name: "database")
    .AddCheck("memory", () => 
    {
        var memoryUsed = GC.GetTotalMemory(false);
        var memoryLimit = 1024 * 1024 * 1024; // 1GB
        return memoryUsed < memoryLimit 
            ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy($"Memory usage: {memoryUsed / (1024 * 1024)}MB")
            : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy($"High memory usage: {memoryUsed / (1024 * 1024)}MB");
    });

// Configure OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "GAA Statistics API",
        Version = "v1",
        Description = "Comprehensive API for GAA match statistics processing and analytics",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "GAA Statistics Team",
            Email = "support@gaastatistics.com"
        }
    });

    // Include XML comments for better API documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "GAA Statistics API v1");
        c.RoutePrefix = "swagger";
    });
}

// Add security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    
    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    }

    await next();
});

// Add custom middleware in correct order
app.UseMiddleware<GlobalExceptionMiddleware>();
// app.UseMiddleware<FileValidationMiddleware>(); // Commented out temporarily

// Add request logging middleware for development
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Request: {Method} {Path}", context.Request.Method, context.Request.Path);
        
        await next();
        
        logger.LogInformation("Response: {StatusCode}", context.Response.StatusCode);
    });
}

app.UseHttpsRedirection();

// Use appropriate CORS policy based on environment
if (app.Environment.IsDevelopment())
{
    app.UseCors();
}
else
{
    app.UseCors("Production");
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Map health check endpoints
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            Status = report.Status.ToString(),
            Checks = report.Entries.Select(x => new
            {
                Name = x.Key,
                Status = x.Value.Status.ToString(),
                Description = x.Value.Description,
                Duration = x.Value.Duration.TotalMilliseconds
            }),
            TotalDuration = report.TotalDuration.TotalMilliseconds
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
});

// Add a simple endpoint for API status
app.MapGet("/", () => new
{
    Service = "GAA Statistics API",
    Version = "1.0.0",
    Status = "Running",
    Environment = app.Environment.EnvironmentName,
    Timestamp = DateTime.UtcNow,
    Endpoints = new
    {
        Analytics = "/api/analytics",
        Matches = "/api/matches",
        Statistics = "/api/statistics",
        Import = "/api/import",
        Health = "/health",
        Swagger = "/swagger"
    }
});

// Ensure database is created and migrations are applied in development
// Commented out for now to test basic server functionality
//if (app.Environment.IsDevelopment())
//{
//    using (var scope = app.Services.CreateScope())
//    {
//        var dbContext = scope.ServiceProvider.GetRequiredService<GAAStatDbContext>();
//        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
//        
//        try
//        {
//            await dbContext.Database.EnsureCreatedAsync();
//            logger.LogInformation("Database connection verified successfully");
//        }
//        catch (Exception ex)
//        {
//            logger.LogError(ex, "Failed to connect to database. Please check connection string");
//        }
//    }
//}

app.Run();
