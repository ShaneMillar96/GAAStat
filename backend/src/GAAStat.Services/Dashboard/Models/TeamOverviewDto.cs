namespace GAAStat.Services.Dashboard.Models;

/// <summary>
/// Team performance overview DTO
/// </summary>
public class TeamOverviewDto
{
    public int TotalMatches { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Draws { get; set; }
    public decimal WinPercentage { get; set; }
    public int TotalPointsScored { get; set; }
    public int TotalPointsConceded { get; set; }
    public decimal AveragePointsScored { get; set; }
    public decimal AveragePointsConceded { get; set; }
    public decimal AveragePossession { get; set; }
}
