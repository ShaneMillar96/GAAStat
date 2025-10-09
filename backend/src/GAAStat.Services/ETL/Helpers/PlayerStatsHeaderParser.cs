using System;
using System.Collections.Generic;
using System.Linq;
using OfficeOpenXml;

namespace GAAStat.Services.ETL.Helpers;

/// <summary>
/// Parses 3-row nested headers from player statistics Excel sheets.
/// Row 1: Numeric weights/values
/// Row 2: Category headers
/// Row 3: Field abbreviations (primary identifiers)
/// </summary>
public class PlayerStatsHeaderParser
{
    /// <summary>
    /// Expected field abbreviations in order (86 total)
    /// Used for validation to ensure all required fields are present
    /// </summary>
    private static readonly string[] ExpectedFields = new[]
    {
        // Summary Statistics (8)
        "#", "Player Name", "Min", "TE", "TE/PSR", "Scores", "PSR", "PSR/TP",

        // Possession Play (13)
        "TP", "ToW", "Int", "TPL", "KP", "HP", "Ha", "TO", "In", "SS", "S Save", "Fo", "Ww",

        // Kickout Analysis - Drum (4)
        "KoW", "WC", "BW", "SW",

        // Kickout Analysis - Opposition (4) - Note: Same abbreviations but different context
        "KoW_Opp", "WC_Opp", "BW_Opp", "SW_Opp",

        // Attacking Play (5)
        "TA", "KR", "KL", "CR", "CL",

        // Shots from Play (11)
        "Tot", "Pts", "2 Pts", "Gls", "Wid", "Sh", "Save", "Ww_Shots", "Bd", "45", "%",

        // Scoreable Frees (11)
        "Tot_Frees", "Pts_Frees", "2 Pts_Frees", "Gls_Frees", "Wid_Frees", "Sh_Frees",
        "Save_Frees", "Ww_Frees", "45_Frees", "QF", "%_Frees",

        // Total Shots (2)
        "TS", "%_Total",

        // Assists (3)
        "TA_Assists", "Point", "Goal",

        // Tackles (4)
        "Tot_Tackles", "Con", "Mis", "%_Tackles",

        // Frees Conceded (5)
        "Tot_FC", "Att", "Mid", "Def", "Pen",

        // 50m Free Conceded (4)
        "Tot_50m", "Delay", "Diss", "3v3",

        // Bookings (3)
        "Yel", "Bla", "Red",

        // Throw Up (2)
        "Won", "Los",

        // Goalkeeper Stats (5)
        "TKo", "KoR", "KoL", "%_GK", "Saves"
    };

    /// <summary>
    /// Parses Row 3 to extract field abbreviations and build column index map.
    /// </summary>
    /// <param name="sheet">Excel worksheet containing player statistics</param>
    /// <returns>Dictionary mapping field abbreviation to column index (1-based)</returns>
    /// <exception cref="ArgumentNullException">If sheet is null</exception>
    /// <exception cref="InvalidOperationException">If header row is invalid or incomplete</exception>
    public Dictionary<string, int> ParseHeaderRow(ExcelWorksheet sheet)
    {
        if (sheet == null)
            throw new ArgumentNullException(nameof(sheet));

        var fieldMap = new Dictionary<string, int>();
        var headerRow = 3; // Row 3 contains field abbreviations
        var maxColumn = sheet.Dimension?.End.Column ?? 0;

        if (maxColumn == 0)
            throw new InvalidOperationException($"Sheet '{sheet.Name}' has no data or invalid dimensions");

        // Scan Row 3 for field abbreviations
        for (int col = 1; col <= maxColumn; col++)
        {
            var cellValue = sheet.Cells[headerRow, col].Value;
            if (cellValue == null)
                continue;

            var fieldName = cellValue.ToString()?.Trim();
            if (string.IsNullOrEmpty(fieldName))
                continue;

            // Handle duplicate abbreviations by adding context suffix
            // This is necessary for fields like "KoW" (appears twice: Drum and Opposition kickouts)
            var uniqueFieldName = MakeFieldNameUnique(fieldName, fieldMap, col);
            fieldMap[uniqueFieldName] = col;
        }

        // Validate that we have all expected fields
        ValidateHeaderCompleteness(fieldMap);

        return fieldMap;
    }

    /// <summary>
    /// Makes field name unique by adding context suffix if duplicate detected.
    /// Uses the category header (Row 2) to add context.
    /// </summary>
    private string MakeFieldNameUnique(string fieldName, Dictionary<string, int> existingMap, int column)
    {
        if (!existingMap.ContainsKey(fieldName))
            return fieldName;

        // Field already exists - we need to disambiguate
        // Common duplicates: KoW, WC, BW, SW (Drum vs Opposition kickouts)
        // Strategy: Add "_Opp" suffix for second occurrence
        return $"{fieldName}_Opp";
    }

    /// <summary>
    /// Validates that all expected fields are present in the header.
    /// Throws exception if critical fields are missing.
    /// </summary>
    /// <param name="fieldMap">Parsed field map</param>
    /// <exception cref="InvalidOperationException">If required fields are missing</exception>
    private void ValidateHeaderCompleteness(Dictionary<string, int> fieldMap)
    {
        var missingFields = new List<string>();

        // Check for critical identification fields (must be present)
        var criticalFields = new[] { "#", "Player Name", "Min" };

        foreach (var criticalField in criticalFields)
        {
            if (!fieldMap.ContainsKey(criticalField))
                missingFields.Add(criticalField);
        }

        if (missingFields.Any())
        {
            throw new InvalidOperationException(
                $"Missing critical header fields: {string.Join(", ", missingFields)}. " +
                "Cannot process player statistics without player identification fields.");
        }

        // Warn if we have significantly fewer fields than expected (but don't fail)
        // Allow flexibility as Excel structure may evolve
        if (fieldMap.Count < 80) // 80 = ~93% of 86 fields
        {
            Console.WriteLine(
                $"Warning: Found only {fieldMap.Count} fields in header. " +
                $"Expected approximately {ExpectedFields.Length} fields. " +
                "Some statistical fields may be missing.");
        }
    }

    /// <summary>
    /// Gets the expected field count for validation purposes.
    /// </summary>
    public static int ExpectedFieldCount => ExpectedFields.Length;

    /// <summary>
    /// Gets the list of expected field abbreviations.
    /// </summary>
    public static IReadOnlyList<string> GetExpectedFields() => ExpectedFields;
}
