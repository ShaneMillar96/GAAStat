namespace GAAStat.Services.Models;

public class TeamStatisticRow
{
    public int MatchId { get; set; }
    public int MetricDefinitionId { get; set; }
    public decimal? DrumFirstHalf { get; set; }
    public decimal? DrumSecondHalf { get; set; }
    public decimal? DrumFullGame { get; set; }
    public decimal? OppositionFirstHalf { get; set; }
    public decimal? OppositionSecondHalf { get; set; }
    public decimal? OppositionFullGame { get; set; }
}