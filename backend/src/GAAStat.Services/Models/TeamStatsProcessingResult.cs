namespace GAAStat.Services.Models;

public class TeamStatsProcessingResult
{
    public int TotalStatisticsCreated { get; set; }
    public int ProcessedSheets { get; set; }
    public int FailedSheets { get; set; }
    public long ProcessingTimeMs { get; set; }
}