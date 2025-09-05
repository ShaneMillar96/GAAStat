namespace GAAStat.Services.Models;

/// <summary>
/// Represents an individual kickout event extracted from Excel data
/// Used for parsing event-level kickout tracking data with timestamps and outcomes
/// </summary>
public class KickoutEvent
{
    /// <summary>
    /// Sequential event number within the sheet
    /// </summary>
    public int? EventNumber { get; set; }
    
    /// <summary>
    /// Time of the kickout event during the match
    /// </summary>
    public TimeSpan? Time { get; set; }
    
    /// <summary>
    /// Period of the match (1 = First Half, 2 = Second Half)
    /// </summary>
    public int Period { get; set; }
    
    /// <summary>
    /// Team name (Drum or Opposition)
    /// </summary>
    public string TeamName { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of kickout (e.g., "Kickout", "Long", "Short")
    /// </summary>
    public string KickoutType { get; set; } = string.Empty;
    
    /// <summary>
    /// Outcome of the kickout (e.g., "Won Clean", "Break Won", "Break Lost", "Sideline Ball")
    /// </summary>
    public string Outcome { get; set; } = string.Empty;
    
    /// <summary>
    /// Player who took the kickout
    /// </summary>
    public string? Player { get; set; }
    
    /// <summary>
    /// Field location reference (numeric)
    /// </summary>
    public int? Location { get; set; }
    
    /// <summary>
    /// Competition context (e.g., "League", "Championship")
    /// </summary>
    public string? Competition { get; set; }
    
    /// <summary>
    /// Teams involved in the match (e.g., "Drum vs Glack")
    /// </summary>
    public string? Teams { get; set; }
    
    /// <summary>
    /// Flag indicating if this is an opposition team event
    /// </summary>
    public bool IsOpposition { get; set; }
}