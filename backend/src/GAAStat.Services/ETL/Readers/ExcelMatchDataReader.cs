using GAAStat.Services.ETL.Models;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using System.Text.RegularExpressions;

namespace GAAStat.Services.ETL.Readers;

/// <summary>
/// Reads match data from Excel sheets using EPPlus.
/// Extracts match metadata, scores, and team statistics.
/// </summary>
public class ExcelMatchDataReader
{
    private readonly ILogger<ExcelMatchDataReader> _logger;

    // Broader pattern for sheet name detection (with or without dates)
    // Supports multi-word competition names like "Neal Carlin"
    private static readonly Regex MatchSheetPattern = new(@"^(\d+)\.\s+(.+?)\s+vs\s+", RegexOptions.Compiled);

    // Pattern for parsing complete metadata from Cell A1 (includes "Drum")
    private static readonly Regex CellA1Pattern = new(@"^(\d+)\.\s+(\w+)\s+Drum\s+vs\s+(.+?)\s+(\d{2})\.(\d{2})\.(\d{2})$", RegexOptions.Compiled);

    // Valid competition types matching database CHECK constraint
    private static readonly string[] ValidCompetitionTypes = { "Championship", "League", "Cup", "Friendly" };
    private const string DefaultCompetitionType = "League";

    /// <summary>
    /// Initializes a new instance of ExcelMatchDataReader
    /// </summary>
    public ExcelMatchDataReader(ILogger<ExcelMatchDataReader> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Reads all match sheets from the Excel file
    /// </summary>
    /// <param name="filePath">Absolute path to Excel file</param>
    /// <returns>List of match sheet data</returns>
    public async Task<List<MatchSheetData>> ReadMatchSheetsAsync(string filePath)
    {
        // Set EPPlus license context
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        var matchSheets = new List<MatchSheetData>();

        if (!File.Exists(filePath))
        {
            _logger.LogError("Excel file not found: {FilePath}", filePath);
            throw new FileNotFoundException($"Excel file not found: {filePath}");
        }

        _logger.LogInformation("Reading Excel file: {FilePath}", filePath);

        using var package = new ExcelPackage(new FileInfo(filePath));

        foreach (var worksheet in package.Workbook.Worksheets)
        {
            if (IsMatchSheet(worksheet.Name))
            {
                try
                {
                    _logger.LogDebug("Processing match sheet: {SheetName}", worksheet.Name);
                    var matchData = await Task.Run(() => ExtractMatchData(worksheet));
                    matchSheets.Add(matchData);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error extracting data from sheet: {SheetName}", worksheet.Name);
                    throw;
                }
            }
            else
            {
                _logger.LogDebug("Skipping non-match sheet: {SheetName}", worksheet.Name);
            }
        }

        _logger.LogInformation("Found {Count} match sheets", matchSheets.Count);
        return matchSheets;
    }

    /// <summary>
    /// Determines if a sheet is a match sheet based on naming convention
    /// Accepts sheets with or without dates due to Excel 31-char limit
    /// Pattern: "09. Championship vs" or "09. Championship vs Slaughtmanus 26.09.25"
    /// </summary>
    private bool IsMatchSheet(string sheetName)
    {
        // Match sheets starting with: [number]. [word] vs
        // This handles both complete and truncated sheet names
        return MatchSheetPattern.IsMatch(sheetName) && !sheetName.Contains("Player");
    }

    /// <summary>
    /// Extracts match data from a worksheet
    /// </summary>
    private MatchSheetData ExtractMatchData(ExcelWorksheet sheet)
    {
        var matchData = new MatchSheetData
        {
            SheetName = sheet.Name
        };

        // Read Cell B1 (Row 1, Column 2) which contains complete match metadata
        // Cell B1 format: "09. Championship Drum vs Slaughtmanus 26.09.25"
        var cellB1Text = sheet.Cells[1, 2].Text;

        // Parse metadata from Cell B1 (has complete info even if sheet name truncated)
        var metadata = ParseMatchMetadata(cellB1Text, sheet.Name);
        matchData.MatchNumber = metadata.MatchNumber;
        matchData.Competition = metadata.Competition;
        matchData.Opposition = metadata.Opposition;
        matchData.MatchDate = metadata.MatchDate;

        // Extract scores from Row 4
        ExtractScores(sheet, matchData);

        // Extract team statistics (6 sets: 3 periods × 2 teams)
        ExtractTeamStatistics(sheet, matchData);

        return matchData;
    }

    /// <summary>
    /// Parses match metadata from Cell B1 or sheet name
    /// Cell B1 has complete data even if sheet name is truncated (Excel 31-char limit)
    /// </summary>
    /// <param name="cellB1Text">Text from Cell B1 (e.g., "09. Championship Drum vs Slaughtmanus 26.09.25")</param>
    /// <param name="sheetName">Sheet name as fallback (e.g., "09. Championship vs Slaughtmanu")</param>
    private (int MatchNumber, string Competition, string Opposition, DateTime MatchDate) ParseMatchMetadata(string cellB1Text, string sheetName)
    {
        // Try Cell B1 first (includes "Drum" in pattern)
        var cellMatch = CellA1Pattern.Match(cellB1Text);
        if (cellMatch.Success)
        {
            var matchNumber = int.Parse(cellMatch.Groups[1].Value);
            var competition = cellMatch.Groups[2].Value;
            var opposition = cellMatch.Groups[3].Value.Trim();
            var day = int.Parse(cellMatch.Groups[4].Value);
            var month = int.Parse(cellMatch.Groups[5].Value);
            var year = 2000 + int.Parse(cellMatch.Groups[6].Value);

            var matchDate = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);

            // Validate competition type and apply intelligent mapping if invalid
            competition = NormalizeCompetitionType(competition, sheetName);

            _logger.LogDebug("Parsed metadata from Cell B1: Match {MatchNumber}, {Competition}, {Opposition}, {MatchDate:yyyy-MM-dd}",
                matchNumber, competition, opposition, matchDate);

            return (matchNumber, competition, opposition, matchDate);
        }

        // Fallback: Try old pattern from sheet name (for sheets that aren't truncated)
        // Pattern: ^(\d+)\.\s+(\w+)\s+vs\s+(.+?)\s+(\d{2})\.(\d{2})\.(\d{2})$
        var sheetPattern = new Regex(@"^(\d+)\.\s+(\w+)\s+vs\s+(.+?)\s+(\d{2})\.(\d{2})\.(\d{2})$", RegexOptions.Compiled);
        var sheetMatch = sheetPattern.Match(sheetName);
        if (sheetMatch.Success)
        {
            var matchNumber = int.Parse(sheetMatch.Groups[1].Value);
            var competition = sheetMatch.Groups[2].Value;
            var opposition = sheetMatch.Groups[3].Value.Trim();
            var day = int.Parse(sheetMatch.Groups[4].Value);
            var month = int.Parse(sheetMatch.Groups[5].Value);
            var year = 2000 + int.Parse(sheetMatch.Groups[6].Value);

            var matchDate = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);

            // Validate competition type
            competition = NormalizeCompetitionType(competition, sheetName);

            _logger.LogDebug("Parsed metadata from sheet name: Match {MatchNumber}, {Competition}, {Opposition}, {MatchDate:yyyy-MM-dd}",
                matchNumber, competition, opposition, matchDate);

            return (matchNumber, competition, opposition, matchDate);
        }

        throw new FormatException($"Invalid match metadata format in Cell B1: '{cellB1Text}' or sheet name: '{sheetName}'");
    }

    /// <summary>
    /// Normalizes and validates competition type
    /// </summary>
    private string NormalizeCompetitionType(string competition, string contextInfo)
    {
        // Validate competition type and apply intelligent mapping if invalid
        if (!ValidCompetitionTypes.Contains(competition, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Invalid competition type '{InvalidCompetition}' in '{Context}'. Defaulting to '{DefaultType}'.",
                competition, contextInfo, DefaultCompetitionType);

            return DefaultCompetitionType;
        }

        // Normalize to match database constraint (capitalize first letter)
        return char.ToUpper(competition[0]) + competition.Substring(1).ToLower();
    }

    /// <summary>
    /// Extracts scores from Row 4
    /// </summary>
    private void ExtractScores(ExcelWorksheet sheet, MatchSheetData matchData)
    {
        // Row 4, Columns B-G contain scores
        // B4: Drum 1st half, C4: Drum 2nd half, D4: Drum full time
        // E4: Opposition 1st half, F4: Opposition 2nd half, G4: Opposition full time

        matchData.HomeScoreFirstHalf = sheet.Cells[4, 2].Text; // B4
        matchData.HomeScoreSecondHalf = sheet.Cells[4, 3].Text; // C4
        matchData.HomeScoreFullTime = sheet.Cells[4, 4].Text; // D4
        matchData.AwayScoreFirstHalf = sheet.Cells[4, 5].Text; // E4
        matchData.AwayScoreSecondHalf = sheet.Cells[4, 6].Text; // F4
        matchData.AwayScoreFullTime = sheet.Cells[4, 7].Text; // G4

        _logger.LogDebug("Extracted scores - Home: {H1}/{H2}/{HF}, Away: {A1}/{A2}/{AF}",
            matchData.HomeScoreFirstHalf, matchData.HomeScoreSecondHalf, matchData.HomeScoreFullTime,
            matchData.AwayScoreFirstHalf, matchData.AwayScoreSecondHalf, matchData.AwayScoreFullTime);
    }

    /// <summary>
    /// Extracts team statistics from rows 5-23
    /// </summary>
    private void ExtractTeamStatistics(ExcelWorksheet sheet, MatchSheetData matchData)
    {
        var periods = new[] { ("1st", 2), ("2nd", 3), ("Full", 4) }; // Period name, column offset
        var teams = new[] { ("Drum", 2), (matchData.Opposition, 5) }; // Team name, base column

        foreach (var (period, periodCol) in periods)
        {
            foreach (var (teamName, teamCol) in teams)
            {
                var stats = new TeamStatisticsData
                {
                    TeamName = teamName,
                    Period = period,
                    Scoreline = sheet.Cells[4, teamCol + periodCol - 2].Text, // Row 4
                    TotalPossession = ParseDecimal(sheet.Cells[5, teamCol + periodCol - 2].Value) // Row 5
                };

                // Extract score sources (Rows 7-14)
                stats.ScoreSourceKickoutLong = ParseInt(sheet.Cells[7, teamCol + periodCol - 2].Value);
                stats.ScoreSourceKickoutShort = ParseInt(sheet.Cells[8, teamCol + periodCol - 2].Value);
                stats.ScoreSourceOppKickoutLong = ParseInt(sheet.Cells[9, teamCol + periodCol - 2].Value);
                stats.ScoreSourceOppKickoutShort = ParseInt(sheet.Cells[10, teamCol + periodCol - 2].Value);
                stats.ScoreSourceTurnover = ParseInt(sheet.Cells[11, teamCol + periodCol - 2].Value);
                stats.ScoreSourcePossessionLost = ParseInt(sheet.Cells[12, teamCol + periodCol - 2].Value);
                stats.ScoreSourceShotShort = ParseInt(sheet.Cells[13, teamCol + periodCol - 2].Value);
                stats.ScoreSourceThrowUpIn = ParseInt(sheet.Cells[14, teamCol + periodCol - 2].Value);

                // Extract shot sources (Rows 16-23)
                stats.ShotSourceKickoutLong = ParseInt(sheet.Cells[16, teamCol + periodCol - 2].Value);
                stats.ShotSourceKickoutShort = ParseInt(sheet.Cells[17, teamCol + periodCol - 2].Value);
                stats.ShotSourceOppKickoutLong = ParseInt(sheet.Cells[18, teamCol + periodCol - 2].Value);
                stats.ShotSourceOppKickoutShort = ParseInt(sheet.Cells[19, teamCol + periodCol - 2].Value);
                stats.ShotSourceTurnover = ParseInt(sheet.Cells[20, teamCol + periodCol - 2].Value);
                stats.ShotSourcePossessionLost = ParseInt(sheet.Cells[21, teamCol + periodCol - 2].Value);
                stats.ShotSourceShotShort = ParseInt(sheet.Cells[22, teamCol + periodCol - 2].Value);
                stats.ShotSourceThrowUpIn = ParseInt(sheet.Cells[23, teamCol + periodCol - 2].Value);

                matchData.TeamStatistics.Add(stats);
            }
        }

        _logger.LogDebug("Extracted {Count} team statistics records", matchData.TeamStatistics.Count);
    }

    /// <summary>
    /// Safely parses an integer value from Excel cell
    /// </summary>
    private int? ParseInt(object? value)
    {
        if (value == null) return null;

        if (value is double d) return (int)d;
        if (value is int i) return i;

        if (int.TryParse(value.ToString(), out var result))
            return result;

        return null;
    }

    /// <summary>
    /// Safely parses a decimal value from Excel cell
    /// </summary>
    private decimal? ParseDecimal(object? value)
    {
        if (value == null) return null;

        if (value is double d) return (decimal)d;
        if (value is decimal dec) return dec;

        if (decimal.TryParse(value.ToString(), out var result))
            return result;

        return null;
    }
}
