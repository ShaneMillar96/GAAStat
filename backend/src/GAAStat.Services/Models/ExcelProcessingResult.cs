namespace GAAStat.Services.Models;

/// <summary>
/// Result of Excel ETL processing operation
/// </summary>
public class ExcelProcessingResult
{
    public int JobId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int SheetsProcessed { get; set; }
    public int MatchesCreated { get; set; }
    public int PlayerStatisticsCreated { get; set; }
    public TimeSpan ProcessingDuration { get; set; }
    public IEnumerable<string> WarningMessages { get; set; } = [];
}

/// <summary>
/// Detailed progress update for ETL operations
/// </summary>
public class EtlProgressUpdate
{
    public int JobId { get; set; }
    public string Stage { get; set; } = string.Empty;
    public int? TotalSteps { get; set; }
    public int? CompletedSteps { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    
    public double? ProgressPercentage => 
        TotalSteps.HasValue && CompletedSteps.HasValue && TotalSteps.Value > 0
            ? (double)CompletedSteps.Value / TotalSteps.Value * 100
            : null;
}

/// <summary>
/// Parsed match data from Excel sheet (Phase 1 - basic headers only)
/// </summary>
public class MatchData
{
    public string SheetName { get; set; } = string.Empty;
    public string HomeTeam { get; set; } = string.Empty;
    public string AwayTeam { get; set; } = string.Empty;
    public DateOnly? MatchDate { get; set; }
    public string? HomeScore { get; set; }
    public string? AwayScore { get; set; }
    public int? HomeGoals { get; set; }
    public int? HomePoints { get; set; }
    public int? AwayGoals { get; set; }
    public int? AwayPoints { get; set; }
    public string? Venue { get; set; }
    public string? Competition { get; set; }
    
    /// <summary>
    /// Validation warnings found during parsing
    /// </summary>
    public IList<string> ValidationWarnings { get; set; } = new List<string>();
}

/// <summary>
/// Excel file analysis result for sheet detection and validation
/// </summary>
public class ExcelFileAnalysis
{
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int SheetCount { get; set; }
    public IList<SheetInfo> Sheets { get; set; } = new List<SheetInfo>();
    public bool IsValidGaaFile { get; set; }
    public IList<string> ValidationErrors { get; set; } = new List<string>();
}

/// <summary>
/// Information about individual Excel sheets
/// </summary>
public class SheetInfo
{
    public string Name { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
    public bool ContainsMatchData { get; set; }
    public string? DetectedTeamNames { get; set; }
    public DateOnly? DetectedMatchDate { get; set; }
}

