using System;
using System.Linq;
using GAAStat.Services.ETL.Models;

namespace GAAStat.Services.ETL.Validators;

/// <summary>
/// Layer 6: Validates business rules and logical constraints.
/// Ensures data makes sense from a GAA gameplay perspective.
/// </summary>
public class BusinessRuleValidator
{
    /// <summary>
    /// Validates business rules for a player.
    /// </summary>
    /// <param name="player">Player data to validate</param>
    /// <returns>Validation result with errors/warnings for rule violations</returns>
    public ValidationResult Validate(PlayerStatisticsData player)
    {
        var result = new ValidationResult { IsValid = true };

        if (player == null)
        {
            result.AddError("Player data is null");
            return result;
        }

        var playerContext = $"Player #{player.JerseyNumber} '{player.PlayerName}'";

        // Validate booking rules (red card means player sent off)
        ValidateBookingRules(player, playerContext, result);

        // Validate activity rules (if no minutes, shouldn't have stats)
        ValidateActivityRules(player, playerContext, result);

        // Validate turnover rules (can't lose more than you win)
        ValidateTurnoverRules(player, playerContext, result);

        // Validate shooting efficiency rules
        ValidateShootingEfficiency(player, playerContext, result);

        // Validate possession engagement rules
        ValidatePossessionEngagement(player, playerContext, result);

        // Validate score notation format
        ValidateScoreNotation(player, playerContext, result);

        return result;
    }

    /// <summary>
    /// Validates team-level business rules across all players.
    /// </summary>
    /// <param name="sheet">Sheet data with all players</param>
    /// <returns>Validation result with team-level rule violations</returns>
    public ValidationResult ValidateTeamRules(PlayerStatsSheetData sheet)
    {
        var result = new ValidationResult { IsValid = true };

        if (sheet?.Players == null || sheet.Players.Count == 0)
        {
            return result;
        }

        // Validate team size (15 starters + subs)
        ValidateTeamSize(sheet, result);

        // Validate that at least one goalkeeper is present
        ValidateGoalkeeperPresence(sheet, result);

        // Validate total minutes played is reasonable
        ValidateTotalMinutes(sheet, result);

        // Validate that players who played have reasonable statistics
        ValidatePlayersWithMinutes(sheet, result);

        return result;
    }

    /// <summary>
    /// Validates booking rules (red card = send off, black card rules, etc.).
    /// </summary>
    private void ValidateBookingRules(PlayerStatisticsData player, string playerContext, ValidationResult result)
    {
        var hasRedCard = player.RedCards > 0;
        var hasBlackCard = player.BlackCards > 0;

        // Red card means player was sent off - shouldn't play full match
        if (hasRedCard && player.MinutesPlayed > 60)
        {
            result.AddWarning($"{playerContext}: Has red card but played {player.MinutesPlayed} minutes (expected early send-off)");
        }

        // Black card also means send-off in GAA
        if (hasBlackCard && player.MinutesPlayed > 60)
        {
            result.AddWarning($"{playerContext}: Has black card but played {player.MinutesPlayed} minutes (expected early send-off)");
        }

        // Multiple red cards is impossible (you're sent off after first)
        if (player.RedCards > 1)
        {
            result.AddError($"{playerContext}: Has {player.RedCards} red cards (maximum 1 per match)");
        }

        // Multiple black cards is impossible
        if (player.BlackCards > 1)
        {
            result.AddError($"{playerContext}: Has {player.BlackCards} black cards (maximum 1 per match)");
        }

        // Yellow + Black + Red shouldn't exceed 2 (usually can't get multiple)
        var totalCards = player.YellowCards + player.BlackCards + player.RedCards;
        if (totalCards > 2)
        {
            result.AddWarning($"{playerContext}: Has {totalCards} total cards, which is unusual");
        }
    }

    /// <summary>
    /// Validates activity rules (no minutes = no stats expected).
    /// </summary>
    private void ValidateActivityRules(PlayerStatisticsData player, string playerContext, ValidationResult result)
    {
        if (player.MinutesPlayed == 0)
        {
            // Player on bench - shouldn't have any significant statistics
            var hasStats = player.TotalEngagements > 0 ||
                          player.TotalShots > 0 ||
                          player.TacklesTotal > 0 ||
                          player.GkTotalKickouts > 0;

            if (hasStats)
            {
                result.AddWarning($"{playerContext}: Has 0 minutes played but has statistics recorded (may be data entry error)");
            }
        }
        else if (player.MinutesPlayed > 30)
        {
            // Player with significant time should have some engagement
            if (player.TotalEngagements == 0)
            {
                result.AddWarning($"{playerContext}: Played {player.MinutesPlayed} minutes but has 0 total engagements");
            }
        }
    }

    /// <summary>
    /// Validates turnover rules (turnovers won vs lost should be balanced).
    /// </summary>
    private void ValidateTurnoverRules(PlayerStatisticsData player, string playerContext, ValidationResult result)
    {
        var turnoversWon = player.Tow;
        var turnoversLost = player.Turnovers;

        // Can't lose dramatically more turnovers than you win (indicates poor play)
        if (turnoversLost > 0 && turnoversWon == 0 && turnoversLost > 5)
        {
            result.AddWarning($"{playerContext}: Has {turnoversLost} turnovers lost but 0 won (very poor possession retention)");
        }

        // Total possession lost should include turnovers
        if (player.Tpl > 0 && player.Turnovers > player.Tpl)
        {
            result.AddWarning($"{playerContext}: Turnovers ({player.Turnovers}) exceeds total possession lost ({player.Tpl})");
        }
    }

    /// <summary>
    /// Validates shooting efficiency is within reasonable bounds.
    /// </summary>
    private void ValidateShootingEfficiency(PlayerStatisticsData player, string playerContext, ValidationResult result)
    {
        // If player took many shots but scored nothing, flag it
        if (player.TotalShots > 5)
        {
            var totalScores = player.ShotsPlayPoints + player.ShotsPlayGoals +
                             player.FreesPoints + player.FreesGoals;

            if (totalScores == 0)
            {
                result.AddWarning($"{playerContext}: Took {player.TotalShots} shots but scored 0 points (0% accuracy)");
            }
            else
            {
                var accuracy = (decimal)totalScores / player.TotalShots;
                if (accuracy < 0.1m) // Less than 10% accuracy
                {
                    result.AddWarning($"{playerContext}: Very low shooting accuracy ({accuracy:P0}) from {player.TotalShots} shots");
                }
            }
        }

        // If player has perfect accuracy from many shots, verify
        if (player.TotalShots >= 5 && player.TotalShotsPercentage >= 1.0m)
        {
            result.AddWarning($"{playerContext}: Has 100% shooting accuracy from {player.TotalShots} shots (verify data)");
        }
    }

    /// <summary>
    /// Validates possession engagement relative to playing time.
    /// </summary>
    private void ValidatePossessionEngagement(PlayerStatisticsData player, string playerContext, ValidationResult result)
    {
        if (player.MinutesPlayed < 20)
        {
            return; // Skip validation for players with limited time
        }

        // Calculate engagements per minute
        var engagementsPerMinute = (decimal)player.TotalEngagements / player.MinutesPlayed;

        // Reasonable range: 0.3 to 2.0 engagements per minute
        if (engagementsPerMinute > 2.0m)
        {
            result.AddWarning($"{playerContext}: Very high engagement rate ({engagementsPerMinute:F1} per minute) - verify data");
        }
        else if (engagementsPerMinute < 0.2m && player.MinutesPlayed > 40)
        {
            result.AddWarning($"{playerContext}: Very low engagement rate ({engagementsPerMinute:F1} per minute) - player may have been injured or ineffective");
        }
    }

    /// <summary>
    /// Validates score notation format if present.
    /// Format: "G-PP(Ff)" where G=goals, PP=points, Ff=frees
    /// Example: "1-03(1f)" = 1 goal, 3 points, 1 from free
    /// </summary>
    private void ValidateScoreNotation(PlayerStatisticsData player, string playerContext, ValidationResult result)
    {
        if (string.IsNullOrEmpty(player.Scores))
        {
            return; // No score notation to validate
        }

        // Basic format check: should match pattern like "0-05" or "1-03(1f)"
        var scorePattern = @"^\d+-\d+(\(\d+f\))?$";
        if (!System.Text.RegularExpressions.Regex.IsMatch(player.Scores, scorePattern))
        {
            result.AddWarning($"{playerContext}: Score notation '{player.Scores}' doesn't match expected GAA format (e.g., '1-03' or '0-05(2f)')");
        }
    }

    /// <summary>
    /// Validates team has reasonable size (15 starters + subs).
    /// </summary>
    private void ValidateTeamSize(PlayerStatsSheetData sheet, ValidationResult result)
    {
        var playerCount = sheet.Players.Count;

        if (playerCount < 15)
        {
            result.AddWarning($"Match vs {sheet.Opposition}: Only {playerCount} players recorded (expected at least 15)");
        }
        else if (playerCount > 35)
        {
            result.AddWarning($"Match vs {sheet.Opposition}: {playerCount} players recorded (unusually high for GAA match)");
        }
    }

    /// <summary>
    /// Validates at least one goalkeeper is present.
    /// </summary>
    private void ValidateGoalkeeperPresence(PlayerStatsSheetData sheet, ValidationResult result)
    {
        var hasGoalkeeper = sheet.Players.Any(p => p.GkTotalKickouts > 0 || p.PositionCode == "GK");

        if (!hasGoalkeeper)
        {
            result.AddWarning($"Match vs {sheet.Opposition}: No goalkeeper detected (no kickout stats recorded)");
        }
    }

    /// <summary>
    /// Validates total minutes played is reasonable for team.
    /// GAA match is ~70 minutes, with 15 players = ~1050 player-minutes expected.
    /// </summary>
    private void ValidateTotalMinutes(PlayerStatsSheetData sheet, ValidationResult result)
    {
        var totalMinutes = sheet.Players.Sum(p => p.MinutesPlayed);

        // Expected: 70 minutes × 15 players = 1050 minutes (allowing ±300 for subs)
        if (totalMinutes < 700)
        {
            result.AddWarning($"Match vs {sheet.Opposition}: Total player-minutes ({totalMinutes}) is low (expected ~1050)");
        }
        else if (totalMinutes > 1400)
        {
            result.AddWarning($"Match vs {sheet.Opposition}: Total player-minutes ({totalMinutes}) is high (expected ~1050)");
        }
    }

    /// <summary>
    /// Validates players with significant minutes have reasonable stats.
    /// </summary>
    private void ValidatePlayersWithMinutes(PlayerStatsSheetData sheet, ValidationResult result)
    {
        var playersWithTime = sheet.Players.Where(p => p.MinutesPlayed > 40).ToList();

        if (playersWithTime.Count < 10)
        {
            result.AddWarning($"Match vs {sheet.Opposition}: Only {playersWithTime.Count} players with >40 minutes (expected ~15)");
        }

        // Check that players with time have some statistics
        var inactivePlayers = playersWithTime.Where(p => p.TotalEngagements == 0).ToList();
        if (inactivePlayers.Any())
        {
            var names = string.Join(", ", inactivePlayers.Select(p => $"#{p.JerseyNumber} {p.PlayerName}"));
            result.AddWarning($"Match vs {sheet.Opposition}: Players with >40 minutes but 0 engagements: {names}");
        }
    }
}
