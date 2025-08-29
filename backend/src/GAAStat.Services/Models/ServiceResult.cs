namespace GAAStat.Services.Models;

/// <summary>
/// Standard service operation result wrapper
/// </summary>
/// <typeparam name="T">Type of data returned on success</typeparam>
public class ServiceResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public IEnumerable<string> ValidationErrors { get; set; } = new List<string>();
    public string? OperationId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ServiceResult<T> Success(T data, string? operationId = null) => new()
    {
        IsSuccess = true,
        Data = data,
        OperationId = operationId
    };

    public static ServiceResult<T> Failed(string error, string? operationId = null) => new()
    {
        IsSuccess = false,
        ErrorMessage = error,
        OperationId = operationId
    };

    public static ServiceResult<T> ValidationFailed(IEnumerable<string> errors, string? operationId = null) => new()
    {
        IsSuccess = false,
        ValidationErrors = errors,
        OperationId = operationId
    };

    public static ServiceResult<T> ValidationFailed(string error, string? operationId = null) => new()
    {
        IsSuccess = false,
        ValidationErrors = new[] { error },
        OperationId = operationId
    };
}

/// <summary>
/// Service result for operations that don't return data
/// </summary>
public class ServiceResult : ServiceResult<object>
{
    public static ServiceResult Success(string? operationId = null) => new()
    {
        IsSuccess = true,
        OperationId = operationId
    };

    public new static ServiceResult Failed(string error, string? operationId = null) => new()
    {
        IsSuccess = false,
        ErrorMessage = error,
        OperationId = operationId
    };

    public new static ServiceResult ValidationFailed(IEnumerable<string> errors, string? operationId = null) => new()
    {
        IsSuccess = false,
        ValidationErrors = errors,
        OperationId = operationId
    };
}