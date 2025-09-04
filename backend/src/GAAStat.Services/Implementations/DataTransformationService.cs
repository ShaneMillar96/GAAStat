using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GAAStat.Dal.Contexts;
using GAAStat.Dal.Models.Application;
using GAAStat.Services.Interfaces;
using GAAStat.Services.Models;

namespace GAAStat.Services.Implementations;

/// <summary>
/// Service for transforming Excel data to database entities
/// Implements the comprehensive mapping documented in excel-to-database-mapping.md
/// </summary>
public class DataTransformationService : IDataTransformationService
{
    private readonly GAAStatDbContext _context;
    private readonly ILogger<DataTransformationService> _logger;

    // Regex patterns for data parsing
    private static readonly Regex ScoreRegex = new(@"(\d+)-(\d+)", RegexOptions.Compiled);
    
    // Multiple date patterns to handle different formats
    private static readonly Regex DateRegex = new(@"(\d{2}\.\d{2}\.\d{2})", RegexOptions.Compiled);
    private static readonly Regex AlternateDateRegex = new(@"(\d{1,2}[\./\-]\d{1,2}[\./\-]\d{2,4})", RegexOptions.Compiled);
    private static readonly Regex PartialDateRegex = new(@"(\d{1,2}\.\d{1,2})(?:\.\d)?(?:\s|$)", RegexOptions.Compiled); // Handles "21.06" or "21.06.2"
    
    private static readonly Regex SheetNameRegex = new(@"(\d+)\.\s*(.*?)\s+vs\s+(.*?)(?:\s+(\d{2}\.\d{2}\.\d{2}))?$", RegexOptions.Compiled);

    // Excel header to database field mappings based on comprehensive mapping
    private static readonly Dictionary<string, string> ExcelToDbFieldMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        // Core statistics
        { "Min", "minutes_played" },
        { "TE", "total_engagements" },
        { "TE/PSR", "engagement_efficiency" },
        { "Scores", "scores" },
        { "PSR", "possession_success_rate" },
        { "PSR/TP", "possessions_per_te" },
        { "TP", "total_possessions" },
        { "ToW", "turnovers_won" },
        { "Int", "interceptions" },
        
        // Attack statistics
        { "TA", "total_attacks" },
        { "KR", "kick_retained" },
        { "KL", "kick_lost" },
        { "CR", "carry_retained" },
        { "CL", "carry_lost" },
        
        // Shooting statistics
        { "Tot", "shots_total" },
        { "Pts", "points" },
        { "Gls", "goals" },
        { "Wid", "wides" },
        
        // Tackle statistics
        { "TS", "tackles_total" },
        { "Con", "tackles_contact" },
        { "Mis", "tackles_missed" },
        
        // Disciplinary statistics
        { "Yel", "yellow_cards" },
        { "Bla", "black_cards" },
        { "Red", "red_cards" },
        
        // Goalkeeper statistics
        { "TKo", "kickouts_total" },
        { "KoR", "kickouts_retained" },
        { "KoL", "kickouts_lost" },
        { "Saves", "saves" }
    };

    public DataTransformationService(GAAStatDbContext context, ILogger<DataTransformationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Parses GAA score format (e.g., "2-06" -> goals=2, points=6)
    /// </summary>
    public (int goals, int points) ParseScore(string scoreText)
    {
        if (string.IsNullOrWhiteSpace(scoreText))
            return (0, 0);

        var match = ScoreRegex.Match(scoreText);
        if (match.Success)
        {
            if (int.TryParse(match.Groups[1].Value, out var goals) && 
                int.TryParse(match.Groups[2].Value, out var points))
            {
                return (goals, points);
            }
        }

        _logger.LogWarning("Failed to parse score: {ScoreText}", scoreText);
        return (0, 0);
    }

    /// <summary>
    /// Parses complex player score format (e.g., "1-03(2f)" -> goals=1, points=3, frees=2)
    /// Handles both simple "G-PP" and complex "G-PP(Nf)" formats
    /// </summary>
    public (int goals, int points, int frees) ParsePlayerScore(string scoreText)
    {
        if (string.IsNullOrWhiteSpace(scoreText))
            return (0, 0, 0);

        try
        {
            // Handle complex format with frees: "1-03(2f)"
            var complexMatch = Regex.Match(scoreText, @"(\d+)-(\d+)\((\d+)f\)");
            if (complexMatch.Success)
            {
                var goals = int.Parse(complexMatch.Groups[1].Value);
                var points = int.Parse(complexMatch.Groups[2].Value);
                var frees = int.Parse(complexMatch.Groups[3].Value);
                return (goals, points, frees);
            }

            // Handle simple format: "0-03"
            var simpleMatch = ScoreRegex.Match(scoreText);
            if (simpleMatch.Success)
            {
                var goals = int.Parse(simpleMatch.Groups[1].Value);
                var points = int.Parse(simpleMatch.Groups[2].Value);
                return (goals, points, 0);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing player score: {ScoreText}", scoreText);
        }

        _logger.LogDebug("Could not parse player score format: {ScoreText}", scoreText);
        return (0, 0, 0);
    }

    /// <summary>
    /// Extracts match date from Excel sheet name with flexible pattern matching
    /// </summary>
    public DateOnly? ExtractDateFromSheetName(string sheetName)
    {
        if (string.IsNullOrWhiteSpace(sheetName))
            return null;

        // Try primary date format first (DD.MM.YY)
        var match = DateRegex.Match(sheetName);
        if (match.Success)
        {
            var datePart = match.Groups[1].Value; // e.g., "03.08.25"
            var result = ParseDatePart(datePart, ".");
            if (result.HasValue)
                return result;
        }

        // Try alternate date formats (DD/MM/YY, DD-MM-YY, etc.)
        var alternateMatch = AlternateDateRegex.Match(sheetName);
        if (alternateMatch.Success)
        {
            var datePart = alternateMatch.Groups[1].Value;
            
            // Determine separator and parse accordingly
            char separator = datePart.Contains('.') ? '.' : 
                            datePart.Contains('/') ? '/' : '-';
                            
            var result = ParseDatePart(datePart, separator.ToString());
            if (result.HasValue)
                return result;
        }

        // Try partial date format (DD.MM or DD.MM.D) - assume current year 2025
        var partialMatch = PartialDateRegex.Match(sheetName);
        if (partialMatch.Success)
        {
            var partialDatePart = partialMatch.Groups[1].Value; // e.g., "21.06"
            var fullDatePart = partialDatePart + ".25"; // Assume 2025
            
            _logger.LogDebug("Found partial date '{PartialDate}' in sheet name, assuming year 2025", partialDatePart);
            
            var result = ParseDatePart(fullDatePart, ".");
            if (result.HasValue)
                return result;
        }

        // Check if sheet name appears to be truncated (exactly 31 chars or ends with incomplete date)
        if (sheetName.Length >= 31 || sheetName.EndsWith(" ") || 
            System.Text.RegularExpressions.Regex.IsMatch(sheetName, @"\d+\.?\d*$"))
        {
            _logger.LogInformation("Sheet name '{SheetName}' appears to be truncated (Excel 31-char limit). " +
                                 "Date parsing failed - will save match with null date for manual entry later", sheetName);
        }
        else
        {
            _logger.LogDebug("No date found in sheet name: {SheetName}", sheetName);
        }
        
        return null;
    }

    /// <summary>
    /// Parse date components with flexible separator handling
    /// </summary>
    private DateOnly? ParseDatePart(string datePart, string separator)
    {
        var parts = datePart.Split(separator);
        if (parts.Length >= 3 && 
            int.TryParse(parts[0], out var day) && 
            int.TryParse(parts[1], out var month) && 
            int.TryParse(parts[2], out var year))
        {
            // Handle different year formats
            if (year < 50)
                year += 2000;  // 25 -> 2025
            else if (year < 100)
                year += 1900;  // 99 -> 1999
            // else assume full year format (2025)

            try
            {
                return new DateOnly(year, month, day);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                _logger.LogWarning(ex, "Invalid date components: {Day}/{Month}/{Year}", day, month, year);
            }
        }

        return null;
    }

    /// <summary>
    /// Derives venue from Excel sheet naming patterns
    /// </summary>
    public string DetermineVenue(string sheetName)
    {
        if (string.IsNullOrWhiteSpace(sheetName))
            return "Away"; // Default assumption

        var normalizedName = sheetName.ToLowerInvariant();

        // Home venue patterns (Drum listed first)
        if (normalizedName.Contains("drum vs"))
            return "Home";

        // Neutral venue patterns (Cup competitions)
        if (normalizedName.Contains("neal carlin") || normalizedName.Contains("cup"))
            return "Neutral";

        // Away venue (default for other patterns)
        return "Away";
    }

    /// <summary>
    /// Extracts competition and opposition team from sheet name
    /// </summary>
    public (string competition, string oppositionTeam) ExtractMatchTeams(string sheetName)
    {
        if (string.IsNullOrWhiteSpace(sheetName))
            return ("Unknown", "Unknown");

        var match = SheetNameRegex.Match(sheetName);
        if (match.Success)
        {
            var beforeVs = match.Groups[2].Value.Trim();
            var afterVs = match.Groups[3].Value.Trim();

            // If "Drum" appears before "vs", then beforeVs is home team context, afterVs is opposition
            if (beforeVs.Equals("Drum", StringComparison.OrdinalIgnoreCase))
            {
                return ("League", afterVs); // Default competition for home games
            }
            
            // If beforeVs contains competition name (Championship, etc.), then afterVs is opposition
            if (beforeVs.Contains("Championship", StringComparison.OrdinalIgnoreCase) ||
                beforeVs.Contains("Neal Carlin", StringComparison.OrdinalIgnoreCase) ||
                beforeVs.Contains("Cup", StringComparison.OrdinalIgnoreCase))
            {
                return (beforeVs, afterVs);
            }

            // Default: beforeVs is competition, afterVs is opposition
            return (beforeVs, afterVs);
        }

        // Fallback parsing - look for "vs" manually
        var vsIndex = sheetName.IndexOf(" vs ", StringComparison.OrdinalIgnoreCase);
        if (vsIndex > 0)
        {
            var before = sheetName[..vsIndex].Trim();
            var after = sheetName[(vsIndex + 4)..].Trim();
            
            // Remove match number prefix if present
            var spaceIndex = before.IndexOf(' ');
            if (spaceIndex > 0 && before[..spaceIndex].All(c => char.IsDigit(c) || c == '.'))
            {
                before = before[(spaceIndex + 1)..].Trim();
            }

            return (before, after);
        }

        _logger.LogWarning("Could not parse teams from sheet name: {SheetName}", sheetName);
        return ("Unknown", "Unknown");
    }

    /// <summary>
    /// Resolves player ID from name with fuzzy matching
    /// Creates new player record if not found
    /// </summary>
    public async Task<ServiceResult<int>> ResolvePlayerIdAsync(string playerName, int jerseyNumber)
    {
        try
        {
            var normalizedName = NormalizePlayerName(playerName);
            
            // Validate that the player name is not an invalid template placeholder or summary row
            if (!ExcelColumnMappings.IsValidPlayerName(normalizedName))
            {
                _logger.LogWarning("🚫 BLOCKING invalid player creation: '{PlayerName}' - appears to be template placeholder or summary row", normalizedName);
                return ServiceResult<int>.Failed($"Invalid player name: {normalizedName}");
            }
            
            // Exact match first
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.PlayerName.ToLower() == normalizedName.ToLower());

            if (player != null)
            {
                return ServiceResult<int>.Success(player.PlayerId);
            }

            // Fuzzy match using similarity (if supported by database)
            // For now, using LIKE pattern matching as fallback
            var candidates = await _context.Players
                .Where(p => EF.Functions.Like(p.PlayerName.ToLower(), $"%{normalizedName.ToLower()}%") ||
                           EF.Functions.Like(normalizedName.ToLower(), $"%{p.PlayerName.ToLower()}%"))
                .ToListAsync();

            if (candidates.Any())
            {
                // Return first candidate - in production, implement proper fuzzy matching
                var candidate = candidates.First();
                _logger.LogInformation("Fuzzy matched player '{PlayerName}' to existing player '{ExistingName}'", 
                    normalizedName, candidate.PlayerName);
                return ServiceResult<int>.Success(candidate.PlayerId);
            }

            // Create new player
            var newPlayer = new Player
            {
                PlayerName = normalizedName,
                JerseyNumber = jerseyNumber,
                IsActive = true
            };

            await _context.Players.AddAsync(newPlayer);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new player: {PlayerName} (#{JerseyNumber})", 
                normalizedName, jerseyNumber);
            
            return ServiceResult<int>.Success(newPlayer.PlayerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve player ID for {PlayerName}", playerName);
            return ServiceResult<int>.Failed("Failed to resolve player ID");
        }
    }

    /// <summary>
    /// Processes Excel percentage values with null handling
    /// </summary>
    public decimal? ProcessPercentage(object? excelValue)
    {
        if (excelValue == null)
            return null;

        var stringValue = excelValue.ToString();
        if (string.IsNullOrWhiteSpace(stringValue) || stringValue.Equals("NaN", StringComparison.OrdinalIgnoreCase))
            return null;

        if (double.TryParse(stringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            // Ensure the value is within 0-1 range for percentages
            if (value < 0) value = 0;
            if (value > 1) value = 1;
            
            return Math.Round((decimal)value, 6); // Match database precision DECIMAL(8,6)
        }

        return null;
    }

    /// <summary>
    /// Processes nullable numeric values from Excel with type safety
    /// </summary>
    public T? ProcessNullableValue<T>(object? excelValue) where T : struct
    {
        if (excelValue == null)
            return null;

        var stringValue = excelValue.ToString();
        if (string.IsNullOrWhiteSpace(stringValue) || 
            stringValue.Equals("NaN", StringComparison.OrdinalIgnoreCase))
            return null;

        try
        {
            return (T)Convert.ChangeType(excelValue, typeof(T), CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to convert value '{Value}' to type {Type}", stringValue, typeof(T).Name);
            return null;
        }
    }


    /// <summary>
    /// Validates and normalizes player name
    /// </summary>
    public string NormalizePlayerName(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return string.Empty;

        // Remove extra whitespace and normalize case
        var normalized = playerName.Trim();
        
        // Convert to title case for consistency
        var textInfo = CultureInfo.InvariantCulture.TextInfo;
        normalized = textInfo.ToTitleCase(normalized.ToLowerInvariant());
        
        return normalized;
    }

    /// <summary>
    /// Maps Excel column headers to database field names
    /// </summary>
    public Dictionary<string, string> MapExcelHeadersToDbFields(IEnumerable<string> excelHeaders)
    {
        var mapping = new Dictionary<string, string>();
        
        foreach (var header in excelHeaders)
        {
            var cleanHeader = header?.Trim() ?? string.Empty;
            if (ExcelToDbFieldMapping.TryGetValue(cleanHeader, out var dbField))
            {
                mapping[cleanHeader] = dbField;
            }
        }

        return mapping;
    }
}