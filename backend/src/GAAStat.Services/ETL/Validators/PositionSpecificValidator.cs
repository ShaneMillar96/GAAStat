using System;
using GAAStat.Services.ETL.Models;

namespace GAAStat.Services.ETL.Validators;

/// <summary>
/// Layer 5: Validates position-specific statistics.
/// Ensures goalkeeper stats are only for GKs, forwards have attacking stats, etc.
/// </summary>
public class PositionSpecificValidator
{
    /// <summary>
    /// Validates position-specific statistics for a player.
    /// Position code should be set before calling (GK, DEF, MID, FWD).
    /// </summary>
    /// <param name="player">Player data to validate</param>
    /// <returns>Validation result with warnings for unusual patterns</returns>
    public ValidationResult Validate(PlayerStatisticsData player)
    {
        var result = new ValidationResult { IsValid = true };

        if (player == null)
        {
            result.AddError("Player data is null");
            return result;
        }

        var playerContext = $"Player #{player.JerseyNumber} '{player.PlayerName}'";

        // If position is not set, skip position-specific validation
        if (string.IsNullOrEmpty(player.PositionCode))
        {
            result.AddWarning($"{playerContext}: Position code not set, skipping position-specific validation");
            return result;
        }

        switch (player.PositionCode.ToUpper())
        {
            case "GK":
                ValidateGoalkeeper(player, playerContext, result);
                break;
            case "DEF":
                ValidateDefender(player, playerContext, result);
                break;
            case "MID":
                ValidateMidfielder(player, playerContext, result);
                break;
            case "FWD":
                ValidateForward(player, playerContext, result);
                break;
            default:
                result.AddWarning($"{playerContext}: Unknown position code '{player.PositionCode}'");
                break;
        }

        return result;
    }

    /// <summary>
    /// Validates goalkeeper-specific statistics.
    /// GKs should have kickout stats and minimal attacking stats.
    /// </summary>
    private void ValidateGoalkeeper(PlayerStatisticsData player, string playerContext, ValidationResult result)
    {
        // GK should have kickout stats
        if (player.GkTotalKickouts == 0)
        {
            result.AddWarning($"{playerContext}: Goalkeeper has no kickouts recorded");
        }

        // GK should have minimal shots/scores
        if (player.TotalShots > 2)
        {
            result.AddWarning($"{playerContext}: Goalkeeper has {player.TotalShots} shots, which is unusually high");
        }

        if (player.ShotsPlayGoals > 0 || player.FreesGoals > 0)
        {
            result.AddWarning($"{playerContext}: Goalkeeper has goals recorded, which is unusual");
        }

        // GK should have minimal attacking play
        if (player.Ta > 5)
        {
            result.AddWarning($"{playerContext}: Goalkeeper has {player.Ta} total attacks, which is unusually high");
        }

        // GK typically has more defensive actions
        if (player.TacklesTotal == 0 && player.MinutesPlayed > 30)
        {
            result.AddWarning($"{playerContext}: Goalkeeper has no tackles despite significant playing time");
        }
    }

    /// <summary>
    /// Validates defender-specific statistics.
    /// Defenders should have high tackle counts and lower shot counts.
    /// </summary>
    private void ValidateDefender(PlayerStatisticsData player, string playerContext, ValidationResult result)
    {
        // Defenders shouldn't have GK stats
        if (player.GkTotalKickouts > 0)
        {
            result.AddWarning($"{playerContext}: Defender has {player.GkTotalKickouts} kickouts, which should only be for goalkeepers");
        }

        // Defenders should have tackles if they played significant time
        if (player.TacklesTotal == 0 && player.MinutesPlayed > 30)
        {
            result.AddWarning($"{playerContext}: Defender has no tackles despite {player.MinutesPlayed} minutes played");
        }

        // Defenders typically have fewer shots than forwards
        if (player.TotalShots > 10)
        {
            result.AddWarning($"{playerContext}: Defender has {player.TotalShots} shots, which is unusually high for a defender");
        }

        // Defenders should have reasonable defensive stats
        if (player.MinutesPlayed > 40 && player.TacklesTotal < 2 && player.Interceptions < 2)
        {
            result.AddWarning($"{playerContext}: Defender has minimal defensive actions (tackles: {player.TacklesTotal}, interceptions: {player.Interceptions}) despite significant playing time");
        }
    }

    /// <summary>
    /// Validates midfielder-specific statistics.
    /// Midfielders should have balanced stats across categories.
    /// </summary>
    private void ValidateMidfielder(PlayerStatisticsData player, string playerContext, ValidationResult result)
    {
        // Midfielders shouldn't have GK stats
        if (player.GkTotalKickouts > 0)
        {
            result.AddWarning($"{playerContext}: Midfielder has {player.GkTotalKickouts} kickouts, which should only be for goalkeepers");
        }

        // Midfielders should have reasonable possession stats if they played significant time
        if (player.MinutesPlayed > 40 && player.Tp < 5)
        {
            result.AddWarning($"{playerContext}: Midfielder has only {player.Tp} total possessions despite {player.MinutesPlayed} minutes played");
        }

        // Midfielders should participate in kickout contests
        if (player.MinutesPlayed > 40 && (player.KoDrumKow + player.KoOppKow) == 0)
        {
            result.AddWarning($"{playerContext}: Midfielder has no kickout wins despite significant playing time");
        }

        // Midfielders should have some tackles
        if (player.MinutesPlayed > 40 && player.TacklesTotal == 0)
        {
            result.AddWarning($"{playerContext}: Midfielder has no tackles despite {player.MinutesPlayed} minutes played");
        }
    }

    /// <summary>
    /// Validates forward-specific statistics.
    /// Forwards should have high shot counts and attacking stats.
    /// </summary>
    private void ValidateForward(PlayerStatisticsData player, string playerContext, ValidationResult result)
    {
        // Forwards shouldn't have GK stats
        if (player.GkTotalKickouts > 0)
        {
            result.AddWarning($"{playerContext}: Forward has {player.GkTotalKickouts} kickouts, which should only be for goalkeepers");
        }

        // Forwards should have shots if they played significant time
        if (player.MinutesPlayed > 40 && player.TotalShots == 0)
        {
            result.AddWarning($"{playerContext}: Forward has no shots despite {player.MinutesPlayed} minutes played");
        }

        // Forwards should have attacking play stats
        if (player.MinutesPlayed > 40 && player.Ta < 3)
        {
            result.AddWarning($"{playerContext}: Forward has only {player.Ta} total attacks despite {player.MinutesPlayed} minutes played");
        }

        // Forwards typically have fewer tackles than defenders
        if (player.TacklesTotal > 8)
        {
            result.AddWarning($"{playerContext}: Forward has {player.TacklesTotal} tackles, which is unusually high for a forward");
        }

        // Forwards should score or assist if they played most of the match
        if (player.MinutesPlayed > 50)
        {
            var totalScores = player.ShotsPlayPoints + player.ShotsPlayGoals + player.FreesPoints + player.FreesGoals;
            var totalContributions = totalScores + player.AssistsTotal;

            if (totalContributions == 0)
            {
                result.AddWarning($"{playerContext}: Forward played {player.MinutesPlayed} minutes but recorded no scores or assists");
            }
        }
    }
}
