namespace GAAStat.Services.Dashboard.Models;

/// <summary>
/// Player season statistics aggregated DTO
/// </summary>
public class PlayerSeasonStatsDto
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public int JerseyNumber { get; set; }
    public string Position { get; set; } = string.Empty;
    public int MatchesPlayed { get; set; }
    public int TotalMinutesPlayed { get; set; }
    public double AverageMinutesPlayed { get; set; }

    // Scoring
    public decimal TotalPoints { get; set; }
    public int ShotsPlayTotal { get; set; }
    public decimal? ShotsPlayPercentage { get; set; }

    // Possession
    public int TotalPossessions { get; set; }
    public int TotalPsr { get; set; }
    public double AveragePsr { get; set; }

    // Defensive
    public int TotalTackles { get; set; }
    public decimal? TacklesPercentage { get; set; }
    public int TotalInterceptions { get; set; }

    // Assists
    public int TotalAssists { get; set; }

    // Discipline
    public int YellowCards { get; set; }
    public int BlackCards { get; set; }
    public int RedCards { get; set; }
}
