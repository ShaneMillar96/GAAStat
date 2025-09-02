namespace GAAStat.Api.Models.Etl;

/// <summary>
/// Response after successful file upload and ETL job creation
/// </summary>
public class EtlUploadResponse
{
    public int JobId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public EtlFileAnalysisResponse? FileAnalysis { get; set; }
}

/// <summary>
/// File analysis information included in upload response
/// </summary>
public class EtlFileAnalysisResponse
{
    public int SheetCount { get; set; }
    public bool IsValidGaaFile { get; set; }
    public IEnumerable<EtlSheetInfoResponse> Sheets { get; set; } = [];
    public IEnumerable<string> ValidationWarnings { get; set; } = [];
}

/// <summary>
/// Information about individual Excel sheets
/// </summary>
public class EtlSheetInfoResponse
{
    public string Name { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
    public bool ContainsMatchData { get; set; }
    public string? DetectedTeamNames { get; set; }
    public DateOnly? DetectedMatchDate { get; set; }
}

/// <summary>
/// ETL job progress information
/// </summary>
public class EtlProgressResponse
{
    public int JobId { get; set; }
    public string Stage { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? TotalSteps { get; set; }
    public int? CompletedSteps { get; set; }
    public double? ProgressPercentage { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public TimeSpan? ElapsedTime { get; set; }
}

/// <summary>
/// Summary of ETL job for list view
/// </summary>
public class EtlJobSummaryResponse
{
    public int JobId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? ProcessingDuration { get; set; }
    public string? CreatedBy { get; set; }
    public string? ErrorSummary { get; set; }
    public int SheetsProcessed { get; set; }
    public int MatchesCreated { get; set; }
}

/// <summary>
/// Paginated list of ETL jobs
/// </summary>
public class EtlJobListResponse
{
    public IEnumerable<EtlJobSummaryResponse> Jobs { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

/// <summary>
/// Validation error details for a specific ETL job
/// </summary>
public class EtlValidationErrorResponse
{
    public int JobId { get; set; }
    public string? SheetName { get; set; }
    public int? RowNumber { get; set; }
    public string? ColumnName { get; set; }
    public string ErrorType { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string? SuggestedFix { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Collection of validation errors for an ETL job
/// </summary>
public class EtlJobErrorsResponse
{
    public int JobId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public IEnumerable<EtlValidationErrorResponse> ValidationErrors { get; set; } = [];
    public int TotalErrorCount { get; set; }
    public IEnumerable<string> ErrorTypeSummary { get; set; } = [];
}