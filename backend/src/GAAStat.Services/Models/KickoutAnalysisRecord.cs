namespace GAAStat.Services.Models;

/// <summary>
/// Represents aggregated kickout analysis data for database storage
/// Contains aggregated statistics grouped by match, team, and period
/// </summary>
public class KickoutAnalysisRecord
{
    /// <summary>
    /// Match identifier
    /// </summary>
    public int MatchId { get; set; }
    
    /// <summary>
    /// Time period identifier (First Half, Second Half)
    /// </summary>
    public int TimePeriodId { get; set; }
    
    /// <summary>
    /// Kickout type identifier (Long, Short, etc.)
    /// </summary>
    public int KickoutTypeId { get; set; }
    
    /// <summary>
    /// Team type identifier (Drum, Opposition)
    /// </summary>
    public int TeamTypeId { get; set; }
    
    /// <summary>
    /// Total number of kickout attempts
    /// </summary>
    public int TotalAttempts { get; set; }
    
    /// <summary>
    /// Number of successful kickouts
    /// </summary>
    public int Successful { get; set; }
    
    /// <summary>
    /// Success rate as decimal (0.0 to 1.0)
    /// </summary>
    public decimal SuccessRate { get; set; }
    
    /// <summary>
    /// JSON string containing detailed outcome breakdown
    /// E.g., {"won_clean": 5, "break_won": 3, "break_lost": 2, "sideline_ball": 1}
    /// </summary>
    public string OutcomeBreakdown { get; set; } = string.Empty;
}