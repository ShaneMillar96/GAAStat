namespace GAAStat.Services.Dashboard.Models;

/// <summary>
/// Generic result wrapper for dashboard operations
/// Similar to EtlResult but simplified for read operations
/// </summary>
public class DashboardResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : TimeSpan.Zero;
    public List<DashboardError> Errors { get; set; } = new();
    public List<DashboardWarning> Warnings { get; set; } = new();

    public static DashboardResult<T> CreateSuccess()
    {
        return new DashboardResult<T>
        {
            Success = true,
            StartTime = DateTime.UtcNow
        };
    }

    public static DashboardResult<T> CreateFailure(string code, string message)
    {
        var result = new DashboardResult<T>
        {
            Success = false,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow
        };
        result.Errors.Add(new DashboardError
        {
            Code = code,
            Message = message,
            Timestamp = DateTime.UtcNow
        });
        return result;
    }

    public DashboardResult<T> WithError(string code, string message)
    {
        Success = false;
        EndTime = DateTime.UtcNow;
        Errors.Add(new DashboardError
        {
            Code = code,
            Message = message,
            Timestamp = DateTime.UtcNow
        });
        return this;
    }

    public DashboardResult<T> WithWarning(string code, string message)
    {
        EndTime = DateTime.UtcNow;
        Warnings.Add(new DashboardWarning
        {
            Code = code,
            Message = message,
            Timestamp = DateTime.UtcNow
        });
        return this;
    }
}

public class DashboardError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class DashboardWarning
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
