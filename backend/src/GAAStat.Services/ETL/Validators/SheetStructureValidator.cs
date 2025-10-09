using System;
using System.Linq;
using GAAStat.Services.ETL.Models;

namespace GAAStat.Services.ETL.Validators;

/// <summary>
/// Layer 1: Validates basic sheet structure and metadata.
/// Ensures sheet has valid field map, match metadata, and player list.
/// </summary>
public class SheetStructureValidator
{
    /// <summary>
    /// Minimum required fields for valid player statistics sheet.
    /// Must include identification fields and at least some statistical fields.
    /// </summary>
    private const int MinimumRequiredFields = 10;

    /// <summary>
    /// Validates sheet structure and metadata.
    /// </summary>
    /// <param name="sheet">Player stats sheet data to validate</param>
    /// <returns>Validation result with errors/warnings</returns>
    public ValidationResult Validate(PlayerStatsSheetData sheet)
    {
        var result = new ValidationResult { IsValid = true };

        if (sheet == null)
        {
            result.AddError("Sheet data is null");
            return result;
        }

        // Validate sheet name
        ValidateSheetName(sheet, result);

        // Validate match metadata
        ValidateMatchMetadata(sheet, result);

        // Validate field map
        ValidateFieldMap(sheet, result);

        // Validate player list
        ValidatePlayerList(sheet, result);

        return result;
    }

    /// <summary>
    /// Validates sheet name is present and reasonable.
    /// </summary>
    private void ValidateSheetName(PlayerStatsSheetData sheet, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(sheet.SheetName))
        {
            result.AddError("Sheet name is empty");
        }
        else if (sheet.SheetName.Length > 50)
        {
            result.AddWarning($"Sheet name is unusually long ({sheet.SheetName.Length} characters): '{sheet.SheetName}'");
        }
    }

    /// <summary>
    /// Validates match metadata (number, opposition, date).
    /// </summary>
    private void ValidateMatchMetadata(PlayerStatsSheetData sheet, ValidationResult result)
    {
        // Validate match number
        if (sheet.MatchNumber <= 0)
        {
            result.AddError($"Invalid match number: {sheet.MatchNumber}. Must be positive integer.");
        }
        else if (sheet.MatchNumber > 100)
        {
            result.AddWarning($"Unusually high match number: {sheet.MatchNumber}");
        }

        // Validate opposition
        if (string.IsNullOrWhiteSpace(sheet.Opposition))
        {
            result.AddError("Opposition team name is empty");
        }
        else if (sheet.Opposition.Length < 2)
        {
            result.AddWarning($"Opposition name is very short: '{sheet.Opposition}'");
        }

        // Validate match date
        var minDate = new DateTime(2020, 1, 1);
        var maxDate = DateTime.Today.AddYears(1);

        if (sheet.MatchDate < minDate)
        {
            result.AddError($"Match date {sheet.MatchDate:yyyy-MM-dd} is too far in the past (before {minDate:yyyy-MM-dd})");
        }
        else if (sheet.MatchDate > maxDate)
        {
            result.AddError($"Match date {sheet.MatchDate:yyyy-MM-dd} is too far in the future (after {maxDate:yyyy-MM-dd})");
        }
    }

    /// <summary>
    /// Validates field map has required fields and reasonable structure.
    /// </summary>
    private void ValidateFieldMap(PlayerStatsSheetData sheet, ValidationResult result)
    {
        if (sheet.FieldMap == null || sheet.FieldMap.Count == 0)
        {
            result.AddError("Field map is empty or null");
            return;
        }

        // Check for minimum required fields
        if (sheet.FieldMap.Count < MinimumRequiredFields)
        {
            result.AddError($"Field map has only {sheet.FieldMap.Count} fields. Minimum {MinimumRequiredFields} required.");
        }

        // Validate critical identification fields are present
        var criticalFields = new[] { "#", "Player Name", "Min" };
        foreach (var criticalField in criticalFields)
        {
            if (!sheet.FieldMap.ContainsKey(criticalField))
            {
                result.AddError($"Critical field '{criticalField}' is missing from field map");
            }
        }

        // Check for reasonable field count (should be around 86)
        if (sheet.FieldMap.Count < 70)
        {
            result.AddWarning($"Field map has only {sheet.FieldMap.Count} fields. Expected approximately 86 fields. Some statistics may be missing.");
        }

        // Validate column indices are positive
        foreach (var kvp in sheet.FieldMap)
        {
            if (kvp.Value <= 0)
            {
                result.AddError($"Field '{kvp.Key}' has invalid column index: {kvp.Value}");
            }
        }

        // Check for duplicate column indices (multiple fields mapped to same column)
        var duplicateColumns = sheet.FieldMap
            .GroupBy(kvp => kvp.Value)
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicateColumns.Any())
        {
            foreach (var dup in duplicateColumns)
            {
                var fieldNames = string.Join(", ", dup.Select(kvp => kvp.Key));
                result.AddWarning($"Multiple fields mapped to column {dup.Key}: {fieldNames}");
            }
        }
    }

    /// <summary>
    /// Validates player list is present and has reasonable size.
    /// </summary>
    private void ValidatePlayerList(PlayerStatsSheetData sheet, ValidationResult result)
    {
        if (sheet.Players == null)
        {
            result.AddError("Player list is null");
            return;
        }

        if (sheet.Players.Count == 0)
        {
            result.AddError("Player list is empty. No players found in sheet.");
            return;
        }

        // Check for reasonable player count (GAA teams have 15-30 players typically)
        if (sheet.Players.Count < 10)
        {
            result.AddWarning($"Only {sheet.Players.Count} players found. Expected at least 10 for a GAA match.");
        }
        else if (sheet.Players.Count > 40)
        {
            result.AddWarning($"Found {sheet.Players.Count} players. This is unusually high for a GAA match.");
        }

        // Check for null players in list
        var nullPlayerCount = sheet.Players.Count(p => p == null);
        if (nullPlayerCount > 0)
        {
            result.AddError($"Player list contains {nullPlayerCount} null entries");
        }
    }
}
