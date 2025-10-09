using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GAAStat.Services.ETL.Models;
using OfficeOpenXml;

namespace GAAStat.Services.ETL.Helpers;

/// <summary>
/// Reads player statistics data from Excel sheets.
/// Handles sheet detection, metadata extraction, and 86-field player data parsing.
/// </summary>
public class ExcelPlayerDataReader
{
    private readonly PlayerStatsHeaderParser _headerParser;

    /// <summary>
    /// Regex pattern for player stats sheet names.
    /// Pattern: "[number]. Player [Ss]tats vs [Opposition] [date]"
    /// Example: "09. Player stats vs Slaughtmanus 26.09.25"
    /// </summary>
    private static readonly Regex PlayerStatsSheetPattern = new(
        @"^(\d+)\.\s+Player\s+[Ss]tats\s+vs\s+(.+?)\s+(\d{2})\.(\d{2})\.(\d{2})$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Regex pattern for truncated sheet names (31-char Excel limit).
    /// Detects player stats sheets even when date is truncated.
    /// </summary>
    private static readonly Regex TruncatedPlayerStatsPattern = new(
        @"^(\d+)\.\s+Player\s+[Ss]tats\s+vs\s+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public ExcelPlayerDataReader()
    {
        _headerParser = new PlayerStatsHeaderParser();
    }

    /// <summary>
    /// Reads all player statistics sheets from Excel file.
    /// </summary>
    /// <param name="filePath">Absolute path to Excel file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of player stats sheets with extracted data</returns>
    public async Task<List<PlayerStatsSheetData>> ReadPlayerStatsSheetsAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentNullException(nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Excel file not found: {filePath}", filePath);

        var sheets = new List<PlayerStatsSheetData>();

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        await Task.Run(() =>
        {
            using var package = new ExcelPackage(new FileInfo(filePath));

            foreach (var worksheet in package.Workbook.Worksheets)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (IsPlayerStatsSheet(worksheet.Name))
                {
                    var sheetData = ExtractPlayerStatsSheet(worksheet);
                    if (sheetData != null)
                    {
                        sheets.Add(sheetData);
                    }
                }
            }
        }, cancellationToken);

        return sheets;
    }

    /// <summary>
    /// Determines if sheet is a player statistics sheet based on name pattern.
    /// </summary>
    private bool IsPlayerStatsSheet(string sheetName)
    {
        if (string.IsNullOrEmpty(sheetName))
            return false;

        // Try full pattern match first
        if (PlayerStatsSheetPattern.IsMatch(sheetName))
            return true;

        // Try truncated pattern (handles 31-char Excel limit)
        return TruncatedPlayerStatsPattern.IsMatch(sheetName);
    }

    /// <summary>
    /// Extracts player statistics data from a single sheet.
    /// </summary>
    private PlayerStatsSheetData? ExtractPlayerStatsSheet(ExcelWorksheet sheet)
    {
        try
        {
            // Parse sheet metadata (match number, opposition, date)
            var (matchNumber, opposition, matchDate) = ParseSheetMetadata(sheet);

            // Parse Row 3 headers to build field map
            var fieldMap = _headerParser.ParseHeaderRow(sheet);

            // Create sheet data container
            var sheetData = new PlayerStatsSheetData
            {
                SheetName = sheet.Name,
                MatchNumber = matchNumber,
                Opposition = opposition,
                MatchDate = matchDate,
                FieldMap = fieldMap
            };

            // Extract players (starting from Row 4)
            ExtractPlayers(sheet, fieldMap, sheetData);

            return sheetData;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting sheet '{sheet.Name}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Parses sheet name to extract match metadata.
    /// Handles both full and truncated sheet names. For truncated names, extracts
    /// match number and partial opposition, using DateTime.MinValue as placeholder.
    /// </summary>
    private (int MatchNumber, string Opposition, DateTime MatchDate) ParseSheetMetadata(ExcelWorksheet sheet)
    {
        var sheetName = sheet.Name;
        var match = PlayerStatsSheetPattern.Match(sheetName);

        if (match.Success)
        {
            // Full pattern matched - extract all metadata
            var matchNumber = int.Parse(match.Groups[1].Value);
            var opposition = match.Groups[2].Value.Trim();
            var day = int.Parse(match.Groups[3].Value);
            var month = int.Parse(match.Groups[4].Value);
            var year = int.Parse(match.Groups[5].Value) + 2000; // Convert 25 → 2025

            var matchDate = new DateTime(year, month, day);

            return (matchNumber, opposition, matchDate);
        }

        // Sheet name is truncated - try to read metadata from Cell B1
        // Cell B1 format: "[number]. [competition] Drum vs [opponent] DD.MM.YY"
        var cellB1Value = sheet.Cells[1, 2].Value?.ToString()?.Trim();
        if (!string.IsNullOrEmpty(cellB1Value))
        {
            var cellB1Pattern = new Regex(
                @"^(\d+)\.\s+.+?\s+Drum\s+vs\s+(.+?)\s+(\d{2})\.(\d{2})\.(\d{2})$",
                RegexOptions.IgnoreCase);

            var cellB1Match = cellB1Pattern.Match(cellB1Value);
            if (cellB1Match.Success)
            {
                var matchNumber = int.Parse(cellB1Match.Groups[1].Value);
                var opposition = cellB1Match.Groups[2].Value.Trim();
                var day = int.Parse(cellB1Match.Groups[3].Value);
                var month = int.Parse(cellB1Match.Groups[4].Value);
                var year = int.Parse(cellB1Match.Groups[5].Value) + 2000;

                var matchDate = new DateTime(year, month, day);

                return (matchNumber, opposition, matchDate);
            }
        }

        // Cell B1 fallback failed - try truncated pattern to extract partial metadata
        // This handles Excel's 31-character sheet name limit
        var truncatedMatch = TruncatedPlayerStatsPattern.Match(sheetName);
        if (truncatedMatch.Success)
        {
            var matchNumber = int.Parse(truncatedMatch.Groups[1].Value);

            // Extract opposition from remainder of sheet name after "vs "
            var vsIndex = sheetName.IndexOf(" vs ", StringComparison.OrdinalIgnoreCase);
            var opposition = "Unknown";
            if (vsIndex >= 0)
            {
                var oppositionStart = vsIndex + 4; // Length of " vs "
                var remainingText = sheetName.Substring(oppositionStart).Trim();

                // Try to extract opposition, handling potential date fragments
                // Format could be: "Magilligan 17.08" or just "Magilligan "
                var datePattern = new Regex(@"\s+\d{1,2}\.\d{0,2}");
                var dateMatch = datePattern.Match(remainingText);

                opposition = dateMatch.Success
                    ? remainingText.Substring(0, dateMatch.Index).Trim()
                    : remainingText.Trim();
            }

            // Use DateTime.MinValue as placeholder - will be looked up from database
            // by match number in FindMatchAsync
            return (matchNumber, opposition, DateTime.MinValue);
        }

        throw new InvalidOperationException(
            $"Cannot parse metadata from sheet '{sheetName}'. " +
            "Sheet name pattern not recognized and Cell B1 fallback failed.");
    }

    /// <summary>
    /// Extracts all player rows from sheet starting at Row 4.
    /// Stops when empty row detected (no jersey number or player name).
    /// </summary>
    private void ExtractPlayers(
        ExcelWorksheet sheet,
        Dictionary<string, int> fieldMap,
        PlayerStatsSheetData sheetData)
    {
        const int startRow = 4; // Row 4 is first player data row
        var maxRow = sheet.Dimension?.End.Row ?? 0;

        for (int row = startRow; row <= maxRow; row++)
        {
            var playerData = ExtractPlayerRow(sheet, row, fieldMap);

            if (playerData == null)
            {
                // Empty row detected - stop processing
                break;
            }

            sheetData.Players.Add(playerData);
        }
    }

    /// <summary>
    /// Extracts a single player row using field map.
    /// Returns null if row is empty (stop condition).
    /// </summary>
    private PlayerStatisticsData? ExtractPlayerRow(
        ExcelWorksheet sheet,
        int row,
        Dictionary<string, int> fieldMap)
    {
        // Check for empty row (no jersey number or player name)
        if (!fieldMap.TryGetValue("#", out var jerseyCol) ||
            !fieldMap.TryGetValue("Player Name", out var nameCol))
        {
            throw new InvalidOperationException("Field map missing critical fields: # or Player Name");
        }

        var jerseyValue = sheet.Cells[row, jerseyCol].Value;
        var nameValue = sheet.Cells[row, nameCol].Value?.ToString()?.Trim();

        if (jerseyValue == null || string.IsNullOrEmpty(nameValue))
        {
            // Empty row - stop condition
            return null;
        }

        var jerseyNumber = ParseInt(jerseyValue);
        if (jerseyNumber == null || jerseyNumber <= 0)
        {
            // Invalid jersey number - skip row
            return null;
        }

        // Create player data object and populate all 86 fields
        var player = new PlayerStatisticsData
        {
            JerseyNumber = jerseyNumber.Value,
            PlayerName = nameValue
        };

        // Summary Statistics
        player.MinutesPlayed = ParseInt(GetCellValue(sheet, row, fieldMap, "Min")) ?? 0;
        player.TotalEngagements = ParseInt(GetCellValue(sheet, row, fieldMap, "TE")) ?? 0;
        player.TePerPsr = ParseDecimal(GetCellValue(sheet, row, fieldMap, "TE/PSR"));
        player.Scores = GetCellValue(sheet, row, fieldMap, "Scores")?.ToString();
        player.Psr = ParseInt(GetCellValue(sheet, row, fieldMap, "PSR")) ?? 0;
        player.PsrPerTp = ParseDecimal(GetCellValue(sheet, row, fieldMap, "PSR/TP"));

        // Possession Play
        player.Tp = ParseInt(GetCellValue(sheet, row, fieldMap, "TP")) ?? 0;
        player.Tow = ParseInt(GetCellValue(sheet, row, fieldMap, "ToW")) ?? 0;
        player.Interceptions = ParseInt(GetCellValue(sheet, row, fieldMap, "Int")) ?? 0;
        player.Tpl = ParseInt(GetCellValue(sheet, row, fieldMap, "TPL")) ?? 0;
        player.Kp = ParseInt(GetCellValue(sheet, row, fieldMap, "KP")) ?? 0;
        player.Hp = ParseInt(GetCellValue(sheet, row, fieldMap, "HP")) ?? 0;
        player.Ha = ParseInt(GetCellValue(sheet, row, fieldMap, "Ha")) ?? 0;
        player.Turnovers = ParseInt(GetCellValue(sheet, row, fieldMap, "TO")) ?? 0;
        player.Ineffective = ParseInt(GetCellValue(sheet, row, fieldMap, "In")) ?? 0;
        player.ShotShort = ParseInt(GetCellValue(sheet, row, fieldMap, "SS")) ?? 0;
        player.ShotSave = ParseInt(GetCellValue(sheet, row, fieldMap, "S Save")) ?? 0;
        player.Fouled = ParseInt(GetCellValue(sheet, row, fieldMap, "Fo")) ?? 0;
        player.Woodwork = ParseInt(GetCellValue(sheet, row, fieldMap, "Ww")) ?? 0;

        // Kickout Analysis - Drum
        player.KoDrumKow = ParseInt(GetCellValue(sheet, row, fieldMap, "KoW")) ?? 0;
        player.KoDrumWc = ParseInt(GetCellValue(sheet, row, fieldMap, "WC")) ?? 0;
        player.KoDrumBw = ParseInt(GetCellValue(sheet, row, fieldMap, "BW")) ?? 0;
        player.KoDrumSw = ParseInt(GetCellValue(sheet, row, fieldMap, "SW")) ?? 0;

        // Kickout Analysis - Opposition
        player.KoOppKow = ParseInt(GetCellValue(sheet, row, fieldMap, "KoW_Opp")) ?? 0;
        player.KoOppWc = ParseInt(GetCellValue(sheet, row, fieldMap, "WC_Opp")) ?? 0;
        player.KoOppBw = ParseInt(GetCellValue(sheet, row, fieldMap, "BW_Opp")) ?? 0;
        player.KoOppSw = ParseInt(GetCellValue(sheet, row, fieldMap, "SW_Opp")) ?? 0;

        // Attacking Play
        player.Ta = ParseInt(GetCellValue(sheet, row, fieldMap, "TA")) ?? 0;
        player.Kr = ParseInt(GetCellValue(sheet, row, fieldMap, "KR")) ?? 0;
        player.Kl = ParseInt(GetCellValue(sheet, row, fieldMap, "KL")) ?? 0;
        player.Cr = ParseInt(GetCellValue(sheet, row, fieldMap, "CR")) ?? 0;
        player.Cl = ParseInt(GetCellValue(sheet, row, fieldMap, "CL")) ?? 0;

        // Shots from Play
        player.ShotsPlayTotal = ParseInt(GetCellValue(sheet, row, fieldMap, "Tot")) ?? 0;
        player.ShotsPlayPoints = ParseInt(GetCellValue(sheet, row, fieldMap, "Pts")) ?? 0;
        player.ShotsPlay2Points = ParseInt(GetCellValue(sheet, row, fieldMap, "2 Pts")) ?? 0;
        player.ShotsPlayGoals = ParseInt(GetCellValue(sheet, row, fieldMap, "Gls")) ?? 0;
        player.ShotsPlayWide = ParseInt(GetCellValue(sheet, row, fieldMap, "Wid")) ?? 0;
        player.ShotsPlayShort = ParseInt(GetCellValue(sheet, row, fieldMap, "Sh")) ?? 0;
        player.ShotsPlaySave = ParseInt(GetCellValue(sheet, row, fieldMap, "Save")) ?? 0;
        player.ShotsPlayWoodwork = ParseInt(GetCellValue(sheet, row, fieldMap, "Ww_Shots")) ?? 0;
        player.ShotsPlayBlocked = ParseInt(GetCellValue(sheet, row, fieldMap, "Bd")) ?? 0;
        player.ShotsPlay45 = ParseInt(GetCellValue(sheet, row, fieldMap, "45")) ?? 0;
        player.ShotsPlayPercentage = ParseDecimal(GetCellValue(sheet, row, fieldMap, "%"));

        // Scoreable Frees
        player.FreesTotal = ParseInt(GetCellValue(sheet, row, fieldMap, "Tot_Frees")) ?? 0;
        player.FreesPoints = ParseInt(GetCellValue(sheet, row, fieldMap, "Pts_Frees")) ?? 0;
        player.Frees2Points = ParseInt(GetCellValue(sheet, row, fieldMap, "2 Pts_Frees")) ?? 0;
        player.FreesGoals = ParseInt(GetCellValue(sheet, row, fieldMap, "Gls_Frees")) ?? 0;
        player.FreesWide = ParseInt(GetCellValue(sheet, row, fieldMap, "Wid_Frees")) ?? 0;
        player.FreesShort = ParseInt(GetCellValue(sheet, row, fieldMap, "Sh_Frees")) ?? 0;
        player.FreesSave = ParseInt(GetCellValue(sheet, row, fieldMap, "Save_Frees")) ?? 0;
        player.FreesWoodwork = ParseInt(GetCellValue(sheet, row, fieldMap, "Ww_Frees")) ?? 0;
        player.Frees45 = ParseInt(GetCellValue(sheet, row, fieldMap, "45_Frees")) ?? 0;
        player.FreesQf = ParseInt(GetCellValue(sheet, row, fieldMap, "QF")) ?? 0;
        player.FreesPercentage = ParseDecimal(GetCellValue(sheet, row, fieldMap, "%_Frees"));

        // Total Shots
        player.TotalShots = ParseInt(GetCellValue(sheet, row, fieldMap, "TS")) ?? 0;
        player.TotalShotsPercentage = ParseDecimal(GetCellValue(sheet, row, fieldMap, "%_Total"));

        // Assists
        player.AssistsTotal = ParseInt(GetCellValue(sheet, row, fieldMap, "TA_Assists")) ?? 0;
        player.AssistsPoint = ParseInt(GetCellValue(sheet, row, fieldMap, "Point")) ?? 0;
        player.AssistsGoal = ParseInt(GetCellValue(sheet, row, fieldMap, "Goal")) ?? 0;

        // Tackles
        player.TacklesTotal = ParseInt(GetCellValue(sheet, row, fieldMap, "Tot_Tackles")) ?? 0;
        player.TacklesContested = ParseInt(GetCellValue(sheet, row, fieldMap, "Con")) ?? 0;
        player.TacklesMissed = ParseInt(GetCellValue(sheet, row, fieldMap, "Mis")) ?? 0;
        player.TacklesPercentage = ParseDecimal(GetCellValue(sheet, row, fieldMap, "%_Tackles"));

        // Frees Conceded
        player.FreesConcededTotal = ParseInt(GetCellValue(sheet, row, fieldMap, "Tot_FC")) ?? 0;
        player.FreesConcededAttack = ParseInt(GetCellValue(sheet, row, fieldMap, "Att")) ?? 0;
        player.FreesConcededMidfield = ParseInt(GetCellValue(sheet, row, fieldMap, "Mid")) ?? 0;
        player.FreesConcededDefense = ParseInt(GetCellValue(sheet, row, fieldMap, "Def")) ?? 0;
        player.FreesConcededPenalty = ParseInt(GetCellValue(sheet, row, fieldMap, "Pen")) ?? 0;

        // 50m Free Conceded
        player.Frees50mTotal = ParseInt(GetCellValue(sheet, row, fieldMap, "Tot_50m")) ?? 0;
        player.Frees50mDelay = ParseInt(GetCellValue(sheet, row, fieldMap, "Delay")) ?? 0;
        player.Frees50mDissent = ParseInt(GetCellValue(sheet, row, fieldMap, "Diss")) ?? 0;
        player.Frees50m3v3 = ParseInt(GetCellValue(sheet, row, fieldMap, "3v3")) ?? 0;

        // Bookings
        player.YellowCards = ParseInt(GetCellValue(sheet, row, fieldMap, "Yel")) ?? 0;
        player.BlackCards = ParseInt(GetCellValue(sheet, row, fieldMap, "Bla")) ?? 0;
        player.RedCards = ParseInt(GetCellValue(sheet, row, fieldMap, "Red")) ?? 0;

        // Throw Up
        player.ThrowUpWon = ParseInt(GetCellValue(sheet, row, fieldMap, "Won")) ?? 0;
        player.ThrowUpLost = ParseInt(GetCellValue(sheet, row, fieldMap, "Los")) ?? 0;

        // Goalkeeper Stats
        player.GkTotalKickouts = ParseInt(GetCellValue(sheet, row, fieldMap, "TKo")) ?? 0;
        player.GkKickoutRetained = ParseInt(GetCellValue(sheet, row, fieldMap, "KoR")) ?? 0;
        player.GkKickoutLost = ParseInt(GetCellValue(sheet, row, fieldMap, "KoL")) ?? 0;
        player.GkKickoutPercentage = ParseDecimal(GetCellValue(sheet, row, fieldMap, "%_GK"));
        player.GkSaves = ParseInt(GetCellValue(sheet, row, fieldMap, "Saves")) ?? 0;

        return player;
    }

    /// <summary>
    /// Gets cell value by field name using field map.
    /// Returns null if field not found in map.
    /// </summary>
    private object? GetCellValue(
        ExcelWorksheet sheet,
        int row,
        Dictionary<string, int> fieldMap,
        string fieldName)
    {
        if (!fieldMap.TryGetValue(fieldName, out var column))
        {
            return null;
        }

        return sheet.Cells[row, column].Value;
    }

    /// <summary>
    /// Safely parses integer value from Excel cell.
    /// Handles null, empty, and various numeric formats.
    /// </summary>
    private int? ParseInt(object? value)
    {
        if (value == null)
            return null;

        if (value is int intValue)
            return intValue;

        if (value is double doubleValue)
            return (int)Math.Round(doubleValue);

        if (value is decimal decimalValue)
            return (int)Math.Round(decimalValue);

        if (value is string stringValue && int.TryParse(stringValue, out var parsedInt))
            return parsedInt;

        return null;
    }

    /// <summary>
    /// Safely parses decimal value from Excel cell.
    /// Handles null, empty, and various numeric formats.
    /// </summary>
    private decimal? ParseDecimal(object? value)
    {
        if (value == null)
            return null;

        if (value is decimal decimalValue)
            return decimalValue;

        if (value is double doubleValue)
            return (decimal)doubleValue;

        if (value is int intValue)
            return intValue;

        if (value is string stringValue &&
            decimal.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedDecimal))
            return parsedDecimal;

        return null;
    }
}
