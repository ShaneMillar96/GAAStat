using System.Net;
using System.Text.Json;
using GAAStat.Api.Controllers;
using GAAStat.Services.Models;

namespace GAAStat.Api.Middleware;

/// <summary>
/// Global exception handling middleware for consistent error responses
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next, 
        ILogger<GlobalExceptionMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = new ApiResponse<object>();
        var correlationId = context.TraceIdentifier;

        switch (exception)
        {
            case ValidationException validationEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse = ApiResponse.Error("Validation failed", validationEx.Errors);
                _logger.LogWarning(validationEx, "Validation error occurred. CorrelationId: {CorrelationId}", correlationId);
                break;

            case FileProcessingException fileEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse = ApiResponse.Error("File processing failed", new[] { fileEx.Message });
                _logger.LogWarning(fileEx, "File processing error occurred. CorrelationId: {CorrelationId}", correlationId);
                break;

            case ImportOperationException importEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse = ApiResponse.Error("Import operation failed", new[] { importEx.Message });
                _logger.LogWarning(importEx, "Import operation error occurred. CorrelationId: {CorrelationId}", correlationId);
                break;

            case DatabaseOperationException dbEx:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse = ApiResponse.Error("Database operation failed");
                _logger.LogError(dbEx, "Database operation error occurred. CorrelationId: {CorrelationId}", correlationId);
                break;

            case UnauthorizedAccessException unauthorizedEx:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse = ApiResponse.Error("Unauthorized access");
                _logger.LogWarning(unauthorizedEx, "Unauthorized access attempt. CorrelationId: {CorrelationId}", correlationId);
                break;

            case ArgumentNullException argNullEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse = ApiResponse.Error("Invalid request parameters", new[] { argNullEx.ParamName ?? "Unknown parameter" });
                _logger.LogWarning(argNullEx, "Argument null error occurred. CorrelationId: {CorrelationId}", correlationId);
                break;

            case ArgumentException argEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse = ApiResponse.Error("Invalid request parameters", new[] { argEx.Message });
                _logger.LogWarning(argEx, "Argument error occurred. CorrelationId: {CorrelationId}", correlationId);
                break;

            case TimeoutException timeoutEx:
                response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                errorResponse = ApiResponse.Error("Operation timed out");
                _logger.LogError(timeoutEx, "Timeout error occurred. CorrelationId: {CorrelationId}", correlationId);
                break;

            case OperationCanceledException cancelEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse = ApiResponse.Error("Operation was cancelled");
                _logger.LogInformation(cancelEx, "Operation was cancelled. CorrelationId: {CorrelationId}", correlationId);
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                
                if (_environment.IsDevelopment())
                {
                    errorResponse = ApiResponse.Error("Internal server error", new[] { exception.Message, exception.StackTrace ?? string.Empty });
                }
                else
                {
                    errorResponse = ApiResponse.Error("An unexpected error occurred");
                }
                
                _logger.LogError(exception, "Unexpected error occurred. CorrelationId: {CorrelationId}", correlationId);
                break;
        }

        // Add correlation ID to response headers
        response.Headers.TryAdd("X-Correlation-ID", correlationId);

        // Add additional debugging information in development
        if (_environment.IsDevelopment())
        {
            response.Headers.TryAdd("X-Exception-Type", exception.GetType().Name);
        }

        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await response.WriteAsync(jsonResponse);
    }
}

#region Custom Exception Types

/// <summary>
/// Exception thrown during file processing operations
/// </summary>
public class FileProcessingException : Exception
{
    public FileProcessingException(string message) : base(message) { }
    public FileProcessingException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown during import operations
/// </summary>
public class ImportOperationException : Exception
{
    public string? ImportId { get; }
    
    public ImportOperationException(string message) : base(message) { }
    public ImportOperationException(string message, string importId) : base(message) 
    {
        ImportId = importId;
    }
    public ImportOperationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown during database operations
/// </summary>
public class DatabaseOperationException : Exception
{
    public string? Operation { get; }
    
    public DatabaseOperationException(string message) : base(message) { }
    public DatabaseOperationException(string message, string operation) : base(message) 
    {
        Operation = operation;
    }
    public DatabaseOperationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown during validation operations
/// </summary>
public class ValidationException : Exception
{
    public IEnumerable<string> Errors { get; }
    
    public ValidationException(string message) : base(message) 
    {
        Errors = new[] { message };
    }
    
    public ValidationException(IEnumerable<string> errors) : base("Validation failed")
    {
        Errors = errors;
    }
    
    public ValidationException(string message, IEnumerable<string> errors) : base(message)
    {
        Errors = errors;
    }
}

#endregion