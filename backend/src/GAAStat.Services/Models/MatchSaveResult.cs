using GAAStat.Services.Interfaces;

namespace GAAStat.Services.Models;

/// <summary>
/// Result of saving matches to database with match ID mapping
/// </summary>
public class MatchSaveResult
{
    /// <summary>
    /// Number of matches successfully created
    /// </summary>
    public int MatchesCreated { get; set; }
    
    /// <summary>
    /// Mapping of sheet names to database match IDs
    /// Used for linking player statistics to matches
    /// </summary>
    public Dictionary<string, int> MatchIdMap { get; set; } = new();
    
    /// <summary>
    /// Validation errors encountered during match creation
    /// </summary>
    public List<ValidationError> ValidationErrors { get; set; } = new();
}