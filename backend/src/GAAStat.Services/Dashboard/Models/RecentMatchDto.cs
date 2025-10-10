namespace GAAStat.Services.Dashboard.Models;

/// <summary>
/// Recent match summary DTO
/// </summary>
public class RecentMatchDto
{
    public int MatchId { get; set; }
    public DateTime MatchDate { get; set; }
    public string Competition { get; set; } = string.Empty;
    public string CompetitionType { get; set; } = string.Empty;
    public string Opponent { get; set; } = string.Empty;
    public string DrumScore { get; set; } = string.Empty;
    public string OpponentScore { get; set; } = string.Empty;
    public int DrumPoints { get; set; }
    public int OpponentPoints { get; set; }
    public string Result { get; set; } = string.Empty; // "Win", "Loss", "Draw"
    public string Venue { get; set; } = string.Empty;
}
