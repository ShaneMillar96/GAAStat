using System;
using System.Collections.Generic;
using System.Linq;
using GAAStat.Services.ETL.Exceptions;
using GAAStat.Services.ETL.Models;
using GAAStat.Services.ETL.Validators;

namespace GAAStat.Services.ETL.Transformers;

/// <summary>
/// Orchestrates 6-layer validation pipeline and transforms player statistics data.
/// Validates and enriches player data before database loading.
/// </summary>
public class PlayerDataTransformer
{
    private readonly SheetStructureValidator _sheetStructureValidator;
    private readonly PlayerIdentificationValidator _playerIdentificationValidator;
    private readonly DataTypeValidator _dataTypeValidator;
    private readonly CrossFieldValidator _crossFieldValidator;
    private readonly PositionSpecificValidator _positionSpecificValidator;
    private readonly BusinessRuleValidator _businessRuleValidator;

    public PlayerDataTransformer()
    {
        _sheetStructureValidator = new SheetStructureValidator();
        _playerIdentificationValidator = new PlayerIdentificationValidator();
        _dataTypeValidator = new DataTypeValidator();
        _crossFieldValidator = new CrossFieldValidator();
        _positionSpecificValidator = new PositionSpecificValidator();
        _businessRuleValidator = new BusinessRuleValidator();
    }

    /// <summary>
    /// Transforms and validates player statistics sheet.
    /// Applies 6-layer validation pipeline and enriches data.
    /// </summary>
    /// <param name="sheet">Raw player stats sheet data</param>
    /// <returns>Validation result with all errors and warnings</returns>
    /// <exception cref="ValidationException">If critical validation errors found</exception>
    public ValidationResult TransformAndValidate(PlayerStatsSheetData sheet)
    {
        var combinedResult = new ValidationResult { IsValid = true };

        // Layer 1: Sheet Structure Validation
        var layer1Result = _sheetStructureValidator.Validate(sheet);
        MergeValidationResults(combinedResult, layer1Result);

        // If sheet structure is critically broken, stop here
        if (!layer1Result.IsValid)
        {
            return combinedResult;
        }

        // Detect positions for all players (needed for layer 5)
        DetectPlayerPositions(sheet);

        // Layer 2-6: Validate each player
        for (int i = 0; i < sheet.Players.Count; i++)
        {
            var player = sheet.Players[i];

            // Layer 2: Player Identification
            var layer2Result = _playerIdentificationValidator.Validate(player, i);
            MergeValidationResults(combinedResult, layer2Result);

            // Skip further validation for this player if identification failed
            if (!layer2Result.IsValid)
            {
                continue;
            }

            // Layer 3: Data Type Validation
            var layer3Result = _dataTypeValidator.Validate(player);
            MergeValidationResults(combinedResult, layer3Result);

            // Layer 4: Cross-Field Validation
            var layer4Result = _crossFieldValidator.Validate(player);
            MergeValidationResults(combinedResult, layer4Result);

            // Layer 5: Position-Specific Validation
            var layer5Result = _positionSpecificValidator.Validate(player);
            MergeValidationResults(combinedResult, layer5Result);

            // Layer 6: Business Rule Validation (per player)
            var layer6Result = _businessRuleValidator.Validate(player);
            MergeValidationResults(combinedResult, layer6Result);
        }

        // Layer 2: Jersey Uniqueness (team-level check)
        var jerseyUniquenessResult = _playerIdentificationValidator.ValidateJerseyUniqueness(sheet);
        MergeValidationResults(combinedResult, jerseyUniquenessResult);

        // Layer 6: Business Rules (team-level checks)
        var teamRulesResult = _businessRuleValidator.ValidateTeamRules(sheet);
        MergeValidationResults(combinedResult, teamRulesResult);

        return combinedResult;
    }

    /// <summary>
    /// Transforms and validates multiple sheets.
    /// Returns aggregated validation results.
    /// </summary>
    /// <param name="sheets">List of player stats sheets</param>
    /// <param name="throwOnError">If true, throws ValidationException on first critical error</param>
    /// <returns>Dictionary mapping sheet name to validation result</returns>
    public Dictionary<string, ValidationResult> TransformAndValidateMultiple(
        List<PlayerStatsSheetData> sheets,
        bool throwOnError = false)
    {
        var results = new Dictionary<string, ValidationResult>();

        foreach (var sheet in sheets)
        {
            var result = TransformAndValidate(sheet);
            results[sheet.SheetName] = result;

            if (throwOnError && !result.IsValid)
            {
                var errorSummary = string.Join("; ", result.Errors);
                throw new ValidationException(
                    $"Validation failed for sheet '{sheet.SheetName}': {errorSummary}");
            }
        }

        return results;
    }

    /// <summary>
    /// Gets validation summary across all sheets.
    /// </summary>
    /// <param name="validationResults">Dictionary of sheet validation results</param>
    /// <returns>Summary string with error/warning counts</returns>
    public string GetValidationSummary(Dictionary<string, ValidationResult> validationResults)
    {
        var totalErrors = validationResults.Sum(kvp => kvp.Value.Errors.Count);
        var totalWarnings = validationResults.Sum(kvp => kvp.Value.Warnings.Count);
        var sheetsWithErrors = validationResults.Count(kvp => !kvp.Value.IsValid);

        var summary = $"Validation Summary:\n";
        summary += $"  Total Sheets: {validationResults.Count}\n";
        summary += $"  Sheets with Errors: {sheetsWithErrors}\n";
        summary += $"  Total Errors: {totalErrors}\n";
        summary += $"  Total Warnings: {totalWarnings}\n";

        if (sheetsWithErrors > 0)
        {
            summary += "\nSheets with errors:\n";
            foreach (var kvp in validationResults.Where(kvp => !kvp.Value.IsValid))
            {
                summary += $"  - {kvp.Key}: {kvp.Value.Errors.Count} errors\n";
            }
        }

        return summary;
    }

    /// <summary>
    /// Detects positions for all players based on statistics heuristics.
    /// Sets PositionCode property on each player.
    /// </summary>
    private void DetectPlayerPositions(PlayerStatsSheetData sheet)
    {
        foreach (var player in sheet.Players)
        {
            player.PositionCode = DetectPosition(player);
        }
    }

    /// <summary>
    /// Detects player position using heuristic rules.
    /// GK: Has kickout stats
    /// FWD: High shots (>5) or high attacking play (>10)
    /// DEF: High tackles (>3) and low shots (≤2)
    /// MID: Default (balanced stats)
    /// </summary>
    private string DetectPosition(PlayerStatisticsData player)
    {
        // Goalkeeper: Has kickout statistics
        if (player.GkTotalKickouts > 0)
        {
            return "GK";
        }

        // Forward: High shooting or attacking activity
        if (player.TotalShots > 5 || player.Ta > 10)
        {
            return "FWD";
        }

        // Defender: High tackles and low shooting
        if (player.TacklesTotal > 3 && player.TotalShots <= 2)
        {
            return "DEF";
        }

        // Default: Midfielder (balanced stats)
        return "MID";
    }

    /// <summary>
    /// Merges validation result into combined result.
    /// </summary>
    private void MergeValidationResults(ValidationResult target, ValidationResult source)
    {
        if (source == null)
            return;

        // Merge errors
        foreach (var error in source.Errors)
        {
            target.AddError(error);
        }

        // Merge warnings
        foreach (var warning in source.Warnings)
        {
            target.AddWarning(warning);
        }
    }

    /// <summary>
    /// Filters out players that should be skipped due to critical validation errors.
    /// Returns list of valid players and count of skipped players.
    /// </summary>
    /// <param name="sheet">Player stats sheet</param>
    /// <param name="validationResult">Validation result for sheet</param>
    /// <returns>Tuple of (valid players list, skipped player count)</returns>
    public (List<PlayerStatisticsData> ValidPlayers, int SkippedCount) FilterValidPlayers(
        PlayerStatsSheetData sheet,
        ValidationResult validationResult)
    {
        if (sheet?.Players == null)
        {
            return (new List<PlayerStatisticsData>(), 0);
        }

        // For now, include all players (validation is for warnings/errors, not filtering)
        // In future, could filter out players with critical identification errors
        var validPlayers = sheet.Players.ToList();
        var skippedCount = 0;

        return (validPlayers, skippedCount);
    }

    /// <summary>
    /// Checks if validation result has critical errors that should stop processing.
    /// </summary>
    /// <param name="validationResult">Validation result to check</param>
    /// <returns>True if has critical errors</returns>
    public bool HasCriticalErrors(ValidationResult validationResult)
    {
        if (validationResult == null || validationResult.IsValid)
        {
            return false;
        }

        // Define critical error patterns
        var criticalErrorPatterns = new[]
        {
            "Field map is empty",
            "Player list is empty",
            "Critical field",
            "Match number",
            "Opposition team name is empty",
            "Player list is null"
        };

        // Check if any error matches critical patterns
        return validationResult.Errors.Any(error =>
            criticalErrorPatterns.Any(pattern =>
                error.Contains(pattern, StringComparison.OrdinalIgnoreCase)));
    }
}
