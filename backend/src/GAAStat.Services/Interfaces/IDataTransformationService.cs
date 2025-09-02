using GAAStat.Services.Models;

namespace GAAStat.Services.Interfaces;

/// <summary>
/// Service for transforming Excel data to database entities
/// Implements the comprehensive mapping documented in excel-to-database-mapping.md
/// </summary>
public interface IDataTransformationService
{
    /// <summary>
    /// Parses GAA score format (e.g., "2-06" -> goals=2, points=6)
    /// </summary>
    /// <param name="scoreText">Score text from Excel (e.g., "2-06", "1-08(2f)")</param>
    /// <returns>Parsed goals and points</returns>
    (int goals, int points) ParseScore(string scoreText);
    
    /// <summary>
    /// Parses complex player score format (e.g., "1-03(2f)" -> goals=1, points=3, frees=2)
    /// </summary>
    /// <param name="scoreText">Player score text from Excel (e.g., "0-03", "1-03(2f)")</param>
    /// <returns>Parsed goals, points, and free kicks</returns>
    (int goals, int points, int frees) ParsePlayerScore(string scoreText);
    
    /// <summary>
    /// Extracts match date from Excel sheet name
    /// </summary>
    /// <param name="sheetName">Sheet name (e.g., "07. Drum vs Lissan 03.08.25")</param>
    /// <returns>Parsed date or null if not found</returns>
    DateOnly? ExtractDateFromSheetName(string sheetName);
    
    /// <summary>
    /// Derives venue from Excel sheet naming patterns
    /// </summary>
    /// <param name="sheetName">Sheet name for pattern analysis</param>
    /// <returns>Venue identifier: Home, Away, or Neutral</returns>
    string DetermineVenue(string sheetName);
    
    /// <summary>
    /// Extracts competition and opposition team from sheet name
    /// </summary>
    /// <param name="sheetName">Sheet name to parse</param>
    /// <returns>Competition name and opposition team name</returns>
    (string competition, string oppositionTeam) ExtractMatchTeams(string sheetName);
    
    /// <summary>
    /// Resolves player ID from name with fuzzy matching
    /// Creates new player record if not found
    /// </summary>
    /// <param name="playerName">Player name from Excel</param>
    /// <param name="jerseyNumber">Jersey number for validation</param>
    /// <returns>Player ID from database</returns>
    Task<ServiceResult<int>> ResolvePlayerIdAsync(string playerName, int jerseyNumber);
    
    /// <summary>
    /// Processes Excel percentage values with null handling
    /// </summary>
    /// <param name="excelValue">Raw value from Excel (may be NaN, null, or numeric)</param>
    /// <returns>Decimal percentage or null</returns>
    decimal? ProcessPercentage(object? excelValue);
    
    /// <summary>
    /// Processes nullable numeric values from Excel with type safety
    /// </summary>
    /// <param name="excelValue">Raw value from Excel</param>
    /// <returns>Typed value or null if invalid</returns>
    T? ProcessNullableValue<T>(object? excelValue) where T : struct;
    
    /// <summary>
    /// Validates and normalizes player name
    /// </summary>
    /// <param name="playerName">Raw player name from Excel</param>
    /// <returns>Normalized player name</returns>
    string NormalizePlayerName(string playerName);
    
    /// <summary>
    /// Maps Excel column headers to database field names
    /// </summary>
    /// <param name="excelHeaders">Column headers from Excel sheet</param>
    /// <returns>Dictionary mapping Excel headers to database fields</returns>
    Dictionary<string, string> MapExcelHeadersToDbFields(IEnumerable<string> excelHeaders);
}