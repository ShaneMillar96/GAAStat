namespace GAAStat.Services.Dashboard.Models;

/// <summary>
/// Top performer DTO for ranking players by metrics
/// </summary>
public class TopPerformerDto
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public int JerseyNumber { get; set; }
    public string Position { get; set; } = string.Empty;
    public int MatchesPlayed { get; set; }
    public int TotalMinutesPlayed { get; set; }
    public string MetricType { get; set; } = string.Empty;
    public decimal MetricValue { get; set; }
}
