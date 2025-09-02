namespace GAAStat.Services.Models;

/// <summary>
/// Generic wrapper for service operation results following GAAStat patterns
/// </summary>
/// <typeparam name="T">Type of data returned on success</typeparam>
public class ServiceResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public IEnumerable<string> ValidationErrors { get; set; } = [];

    public static ServiceResult<T> Success(T data) => new() 
    { 
        IsSuccess = true, 
        Data = data 
    };

    public static ServiceResult<T> Failed(string error) => new() 
    { 
        IsSuccess = false, 
        ErrorMessage = error 
    };

    public static ServiceResult<T> ValidationFailed(IEnumerable<string> errors) => new() 
    { 
        IsSuccess = false, 
        ValidationErrors = errors 
    };
}

/// <summary>
/// Service result without data payload
/// </summary>
public class ServiceResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public IEnumerable<string> ValidationErrors { get; set; } = [];

    public static ServiceResult Success() => new() 
    { 
        IsSuccess = true 
    };

    public static ServiceResult Failed(string error) => new() 
    { 
        IsSuccess = false, 
        ErrorMessage = error 
    };

    public static ServiceResult ValidationFailed(IEnumerable<string> errors) => new() 
    { 
        IsSuccess = false, 
        ValidationErrors = errors 
    };
}