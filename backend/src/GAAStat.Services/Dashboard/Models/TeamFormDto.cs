namespace GAAStat.Services.Dashboard.Models;

/// <summary>
/// Team form DTO tracking recent results
/// </summary>
public class TeamFormDto
{
    public int LastNMatches { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Draws { get; set; }
    public string FormString { get; set; } = string.Empty; // e.g., "WWLDW"
    public decimal WinPercentage { get; set; }
}
