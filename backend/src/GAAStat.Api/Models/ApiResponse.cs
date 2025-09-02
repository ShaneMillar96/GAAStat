namespace GAAStat.Api.Models;

/// <summary>
/// Standard API response wrapper for consistent response format across all endpoints
/// </summary>
/// <typeparam name="T">Type of data being returned</typeparam>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public IEnumerable<string> Errors { get; set; } = [];
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Helper class for creating consistent API responses
/// </summary>
public static class ApiResponse
{
    public static ApiResponse<T> Success<T>(T data, string? message = null) => 
        new() 
        { 
            Success = true, 
            Data = data, 
            Message = message 
        };

    public static ApiResponse<object> Success(string message) => 
        new() 
        { 
            Success = true, 
            Message = message 
        };

    public static ApiResponse<object> Error(string message, IEnumerable<string>? errors = null) => 
        new() 
        { 
            Success = false, 
            Message = message, 
            Errors = errors ?? [] 
        };

    public static ApiResponse<T> Error<T>(string message, IEnumerable<string>? errors = null) =>
        new()
        {
            Success = false,
            Message = message,
            Errors = errors ?? []
        };
}