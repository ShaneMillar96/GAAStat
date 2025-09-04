namespace GAAStat.Services.Models;

/// <summary>
/// ETL job status constants matching database schema
/// </summary>
public static class EtlJobStatus
{
    public const string PENDING = "pending";
    public const string PROCESSING = "processing";
    public const string COMPLETED = "completed";
    public const string FAILED = "failed";
}

/// <summary>
/// ETL processing stage constants for progress tracking
/// </summary>
public static class EtlStages
{
    public const string INITIALIZING = "Initializing";
    public const string SEEDING_REFERENCE_DATA = "Seeding Reference Data";
    public const string CLEARING_DATA = "Clearing Existing Data";
    public const string PARSING_EXCEL = "Parsing Excel";
    public const string VALIDATING_DATA = "Validating Data";
    public const string SAVING_MATCHES = "Saving Matches";
    public const string SAVING_TEAM_STATS = "Saving Team Statistics";
    public const string SAVING_PLAYER_STATS = "Saving Player Statistics";
    public const string SAVING_SPECIALIZED_ANALYTICS = "Saving Specialized Analytics";
    public const string FINALIZING = "Finalizing";
}

/// <summary>
/// ETL validation error types
/// </summary>
public static class EtlErrorTypes
{
    public const string FILE_FORMAT = "file_format";
    public const string MISSING_DATA = "missing_data";
    public const string INVALID_DATE = "invalid_date";
    public const string INVALID_SCORE = "invalid_score";
    public const string DUPLICATE_MATCH = "duplicate_match";
    public const string SHEET_STRUCTURE = "sheet_structure";
}

/// <summary>
/// GAA-specific constants for Excel processing
/// </summary>
public static class GaaConstants
{
    public const int POINTS_PER_GOAL = 3;
    public const int MAX_JERSEY_NUMBER = 99;
    public const int MIN_JERSEY_NUMBER = 1;
    public const string DEFAULT_HOME_TEAM = "Drum";
    
    /// <summary>
    /// Expected sheet name patterns for GAA Excel files
    /// </summary>
    public static readonly string[] MATCH_SHEET_INDICATORS = 
    [
        "vs", "v", "game", "match", "drum"
    ];
    
    /// <summary>
    /// Sheet types that should be excluded from match processing
    /// </summary>
    public static readonly string[] EXCLUDED_SHEET_PATTERNS = 
    [
        "cumulative", "summary", "total", "csv file", "overview", "stats summary"
    ];
    
    /// <summary>
    /// Sheet types that contain individual player statistics
    /// </summary>
    public static readonly string[] PLAYER_STATS_SHEET_PATTERNS = 
    [
        "player stats", "stats vs", "analysis vs"
    ];
    
    /// <summary>
    /// Common venue abbreviations in GAA Excel files
    /// </summary>
    public static readonly Dictionary<string, string> VENUE_ABBREVIATIONS = new()
    {
        { "H", "Home" },
        { "A", "Away" },
        { "N", "Neutral" }
    };
}

/// <summary>
/// File processing limits and validation constants
/// </summary>
public static class FileConstants
{
    public const int MAX_FILE_SIZE_MB = 50;
    public const int MAX_FILENAME_LENGTH = 255;
    public const int MAX_SHEET_COUNT = 50;
    public const int MIN_EXPECTED_COLUMNS = 5;
    public const int MIN_EXPECTED_ROWS = 10;
    
    /// <summary>
    /// Allowed Excel file extensions
    /// </summary>
    public static readonly HashSet<string> ALLOWED_EXTENSIONS = new(StringComparer.OrdinalIgnoreCase)
    {
        ".xlsx", ".xls"
    };
    
    /// <summary>
    /// Excel MIME types for validation
    /// </summary>
    public static readonly HashSet<string> ALLOWED_MIME_TYPES = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-excel"
    };
}