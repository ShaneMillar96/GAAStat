using GAAStat.Dal.Models.Application;
using GAAStat.Services.Interfaces;

namespace GAAStat.Services.Models;

/// <summary>
/// Represents parsed player statistics data from Excel
/// Contains both the entity and associated validation information
/// </summary>
public class PlayerStatisticsData
{
    /// <summary>
    /// Player name extracted from Excel
    /// </summary>
    public string PlayerName { get; set; } = string.Empty;
    
    /// <summary>
    /// Jersey number (from Min column or derived)
    /// </summary>
    public int? JerseyNumber { get; set; }
    
    /// <summary>
    /// Database player ID (resolved or created during processing)
    /// </summary>
    public int PlayerId { get; set; }
    
    /// <summary>
    /// Match player statistics entity
    /// </summary>
    public MatchPlayerStatistics Statistics { get; set; } = new();
    
    /// <summary>
    /// Row number in Excel sheet (for error tracking)
    /// </summary>
    public int RowNumber { get; set; }
    
    /// <summary>
    /// Sheet name containing this data
    /// </summary>
    public string SheetName { get; set; } = string.Empty;
    
    /// <summary>
    /// Validation errors specific to this player's data
    /// </summary>
    public List<ValidationError> ValidationErrors { get; set; } = new();
    
    /// <summary>
    /// Whether this player data passed validation
    /// </summary>
    public bool IsValid => !ValidationErrors.Any(e => e.ErrorType != "validation_warning");
    
    /// <summary>
    /// Whether this player has any statistics (not all zeros)
    /// </summary>
    public bool HasStatistics => 
        Statistics.TotalEngagements > 0 ||
        Statistics.TotalPossessions > 0 ||
        Statistics.Goals > 0 ||
        Statistics.Points > 0 ||
        Statistics.TacklesTotal > 0;
}

/// <summary>
/// Result of validating player statistics sheet
/// </summary>
public class PlayerStatsValidationResult
{
    /// <summary>
    /// Whether the sheet has valid structure
    /// </summary>
    public bool IsValidStructure { get; set; }
    
    /// <summary>
    /// Number of player rows detected
    /// </summary>
    public int PlayerRowCount { get; set; }
    
    /// <summary>
    /// Number of columns with data
    /// </summary>
    public int ColumnCount { get; set; }
    
    /// <summary>
    /// Whether required columns are present
    /// </summary>
    public bool HasRequiredColumns { get; set; }
    
    /// <summary>
    /// Missing required columns
    /// </summary>
    public List<string> MissingColumns { get; set; } = new();
    
    /// <summary>
    /// Header row validation results
    /// </summary>
    public List<string> HeaderValidationErrors { get; set; } = new();
    
    /// <summary>
    /// Structural validation errors
    /// </summary>
    public List<ValidationError> ValidationErrors { get; set; } = new();
    
    /// <summary>
    /// Summary of sheet analysis
    /// </summary>
    public string Summary => 
        $"Sheet contains {PlayerRowCount} player rows with {ColumnCount} columns. " +
        $"Structure valid: {IsValidStructure}";
}