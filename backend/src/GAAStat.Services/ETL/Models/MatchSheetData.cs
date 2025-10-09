namespace GAAStat.Services.ETL.Models;

/// <summary>
/// Represents match-level data extracted from a match sheet in the Excel file.
/// Corresponds to sheets with pattern: "[number]. [Competition] vs [Opposition] [Date]"
/// </summary>
public class MatchSheetData
{
    /// <summary>
    /// Original sheet name from Excel (e.g., "09. Championship vs Slaughtmanus 26.09.25")
    /// </summary>
    public string SheetName { get; set; } = string.Empty;

    /// <summary>
    /// Match number extracted from sheet name (e.g., 9)
    /// </summary>
    public int MatchNumber { get; set; }

    /// <summary>
    /// Competition type (e.g., "Championship", "League")
    /// </summary>
    public string Competition { get; set; } = string.Empty;

    /// <summary>
    /// Opposition team name (e.g., "Slaughtmanus")
    /// </summary>
    public string Opposition { get; set; } = string.Empty;

    /// <summary>
    /// Match date parsed from sheet name
    /// </summary>
    public DateTime MatchDate { get; set; }

    /// <summary>
    /// Venue (typically "Home" for Drum)
    /// </summary>
    public string Venue { get; set; } = "Home";

    // Score fields (6 total: home/away × 1st/2nd/full)

    /// <summary>
    /// Drum's score in 1st half (GAA notation: "0-04")
    /// </summary>
    public string? HomeScoreFirstHalf { get; set; }

    /// <summary>
    /// Drum's score in 2nd half (GAA notation: "1-07")
    /// </summary>
    public string? HomeScoreSecondHalf { get; set; }

    /// <summary>
    /// Drum's full-time score (GAA notation: "1-11")
    /// </summary>
    public string? HomeScoreFullTime { get; set; }

    /// <summary>
    /// Opposition score in 1st half
    /// </summary>
    public string? AwayScoreFirstHalf { get; set; }

    /// <summary>
    /// Opposition score in 2nd half
    /// </summary>
    public string? AwayScoreSecondHalf { get; set; }

    /// <summary>
    /// Opposition full-time score
    /// </summary>
    public string? AwayScoreFullTime { get; set; }

    /// <summary>
    /// Team statistics (6 sets: 3 periods × 2 teams)
    /// </summary>
    public List<TeamStatisticsData> TeamStatistics { get; set; } = new();
}
