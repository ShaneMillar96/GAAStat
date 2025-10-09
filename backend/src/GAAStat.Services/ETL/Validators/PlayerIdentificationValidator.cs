using System;
using System.Text.RegularExpressions;
using GAAStat.Services.ETL.Models;

namespace GAAStat.Services.ETL.Validators;

/// <summary>
/// Layer 2: Validates player identification fields.
/// Ensures jersey number, player name, and minutes are valid.
/// </summary>
public class PlayerIdentificationValidator
{
    /// <summary>
    /// Pattern for valid player names (letters, spaces, apostrophes, hyphens).
    /// Example: "Seamus O'Kane", "John-Paul Smith"
    /// </summary>
    private static readonly Regex ValidNamePattern = new(
        @"^[a-zA-Z\s'\-\.]+$",
        RegexOptions.Compiled);

    /// <summary>
    /// Validates player identification fields.
    /// </summary>
    /// <param name="player">Player data to validate</param>
    /// <param name="playerIndex">Player index in sheet (for error messages)</param>
    /// <returns>Validation result with errors/warnings</returns>
    public ValidationResult Validate(PlayerStatisticsData player, int playerIndex)
    {
        var result = new ValidationResult { IsValid = true };

        if (player == null)
        {
            result.AddError($"Player at index {playerIndex} is null");
            return result;
        }

        // Validate jersey number
        ValidateJerseyNumber(player, playerIndex, result);

        // Validate player name
        ValidatePlayerName(player, playerIndex, result);

        // Validate minutes played
        ValidateMinutesPlayed(player, playerIndex, result);

        return result;
    }

    /// <summary>
    /// Validates jersey number is within valid range for GAA.
    /// </summary>
    private void ValidateJerseyNumber(PlayerStatisticsData player, int playerIndex, ValidationResult result)
    {
        if (player.JerseyNumber <= 0)
        {
            result.AddError($"Player at index {playerIndex}: Jersey number must be positive (got {player.JerseyNumber})");
        }
        else if (player.JerseyNumber > 99)
        {
            result.AddError($"Player '{player.PlayerName}' (index {playerIndex}): Jersey number {player.JerseyNumber} exceeds maximum (99)");
        }
        else if (player.JerseyNumber > 40)
        {
            result.AddWarning($"Player '{player.PlayerName}' (index {playerIndex}): Jersey number {player.JerseyNumber} is unusually high");
        }
    }

    /// <summary>
    /// Validates player name is present and has valid format.
    /// </summary>
    private void ValidatePlayerName(PlayerStatisticsData player, int playerIndex, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(player.PlayerName))
        {
            result.AddError($"Player #{player.JerseyNumber} (index {playerIndex}): Player name is empty");
            return;
        }

        // Check minimum length (at least 2 characters)
        if (player.PlayerName.Length < 2)
        {
            result.AddError($"Player #{player.JerseyNumber} (index {playerIndex}): Player name '{player.PlayerName}' is too short (minimum 2 characters)");
            return;
        }

        // Check maximum length (reasonable limit)
        if (player.PlayerName.Length > 100)
        {
            result.AddError($"Player #{player.JerseyNumber} (index {playerIndex}): Player name is too long ({player.PlayerName.Length} characters)");
        }

        // Validate name contains only valid characters
        if (!ValidNamePattern.IsMatch(player.PlayerName))
        {
            result.AddWarning($"Player #{player.JerseyNumber} '{player.PlayerName}' (index {playerIndex}): Name contains unusual characters");
        }

        // Check for common data entry errors
        if (player.PlayerName.Contains("  ")) // Double spaces
        {
            result.AddWarning($"Player #{player.JerseyNumber} '{player.PlayerName}' (index {playerIndex}): Name contains double spaces");
        }

        if (player.PlayerName.StartsWith(" ") || player.PlayerName.EndsWith(" "))
        {
            result.AddWarning($"Player #{player.JerseyNumber} '{player.PlayerName}' (index {playerIndex}): Name has leading or trailing spaces");
        }

        // Check if name is all uppercase or all lowercase (potential data quality issue)
        if (player.PlayerName == player.PlayerName.ToUpper() && player.PlayerName.Length > 3)
        {
            result.AddWarning($"Player #{player.JerseyNumber} '{player.PlayerName}' (index {playerIndex}): Name is all uppercase");
        }
        else if (player.PlayerName == player.PlayerName.ToLower() && player.PlayerName.Length > 3)
        {
            result.AddWarning($"Player #{player.JerseyNumber} '{player.PlayerName}' (index {playerIndex}): Name is all lowercase");
        }
    }

    /// <summary>
    /// Validates minutes played is within reasonable range.
    /// GAA matches are typically 60-70 minutes (allowing for extra time).
    /// </summary>
    private void ValidateMinutesPlayed(PlayerStatisticsData player, int playerIndex, ValidationResult result)
    {
        const int MaxRegularMinutes = 70; // Standard match + extra time
        const int MaxExtraTimeMinutes = 90; // Match + significant extra time

        if (player.MinutesPlayed < 0)
        {
            result.AddError($"Player #{player.JerseyNumber} '{player.PlayerName}': Minutes played cannot be negative (got {player.MinutesPlayed})");
        }
        else if (player.MinutesPlayed == 0)
        {
            // Player was on bench - this is valid but worth noting
            result.AddWarning($"Player #{player.JerseyNumber} '{player.PlayerName}': Minutes played is 0 (player was on bench)");
        }
        else if (player.MinutesPlayed > MaxExtraTimeMinutes)
        {
            result.AddError($"Player #{player.JerseyNumber} '{player.PlayerName}': Minutes played ({player.MinutesPlayed}) exceeds maximum ({MaxExtraTimeMinutes})");
        }
        else if (player.MinutesPlayed > MaxRegularMinutes)
        {
            result.AddWarning($"Player #{player.JerseyNumber} '{player.PlayerName}': Minutes played ({player.MinutesPlayed}) is higher than standard match duration");
        }
    }

    /// <summary>
    /// Validates uniqueness of jersey numbers within a team.
    /// Call this method after validating all individual players.
    /// </summary>
    /// <param name="sheet">Sheet data with all players</param>
    /// <returns>Validation result with errors for duplicate jerseys</returns>
    public ValidationResult ValidateJerseyUniqueness(PlayerStatsSheetData sheet)
    {
        var result = new ValidationResult { IsValid = true };

        if (sheet?.Players == null || sheet.Players.Count == 0)
        {
            return result;
        }

        // Group players by jersey number and find duplicates
        var duplicateGroups = sheet.Players
            .GroupBy(p => p.JerseyNumber)
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var group in duplicateGroups)
        {
            var playerNames = string.Join(", ", group.Select(p => p.PlayerName));
            result.AddError($"Duplicate jersey number {group.Key} found for players: {playerNames}");
        }

        return result;
    }
}
