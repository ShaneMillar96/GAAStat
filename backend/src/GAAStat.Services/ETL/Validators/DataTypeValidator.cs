using System;
using GAAStat.Services.ETL.Models;

namespace GAAStat.Services.ETL.Validators;

/// <summary>
/// Layer 3: Validates data types and value ranges.
/// Ensures numeric fields are within valid ranges and percentages are between 0-1.
/// </summary>
public class DataTypeValidator
{
    private const int MaxReasonableCount = 100; // Maximum reasonable value for count fields
    private const decimal MinPercentage = 0.0m;
    private const decimal MaxPercentage = 1.0m;

    /// <summary>
    /// Validates all numeric fields and percentages for a player.
    /// </summary>
    /// <param name="player">Player data to validate</param>
    /// <returns>Validation result with errors/warnings</returns>
    public ValidationResult Validate(PlayerStatisticsData player)
    {
        var result = new ValidationResult { IsValid = true };

        if (player == null)
        {
            result.AddError("Player data is null");
            return result;
        }

        var playerContext = $"Player #{player.JerseyNumber} '{player.PlayerName}'";

        // Validate Summary Statistics
        ValidateCountField(player.TotalEngagements, "Total Engagements", playerContext, result);
        ValidatePercentageField(player.TePerPsr, "TE/PSR", playerContext, result);
        ValidateCountField(player.Psr, "PSR", playerContext, result);
        ValidatePercentageField(player.PsrPerTp, "PSR/TP", playerContext, result);

        // Validate Possession Play
        ValidateCountField(player.Tp, "TP", playerContext, result);
        ValidateCountField(player.Tow, "ToW", playerContext, result);
        ValidateCountField(player.Interceptions, "Interceptions", playerContext, result);
        ValidateCountField(player.Tpl, "TPL", playerContext, result);
        ValidateCountField(player.Kp, "KP", playerContext, result);
        ValidateCountField(player.Hp, "HP", playerContext, result);
        ValidateCountField(player.Ha, "Ha", playerContext, result);
        ValidateCountField(player.Turnovers, "Turnovers", playerContext, result);
        ValidateCountField(player.Ineffective, "Ineffective", playerContext, result);
        ValidateCountField(player.ShotShort, "Shot Short", playerContext, result);
        ValidateCountField(player.ShotSave, "Shot Save", playerContext, result);
        ValidateCountField(player.Fouled, "Fouled", playerContext, result);
        ValidateCountField(player.Woodwork, "Woodwork", playerContext, result);

        // Validate Kickout Analysis - Drum
        ValidateCountField(player.KoDrumKow, "KoW (Drum)", playerContext, result);
        ValidateCountField(player.KoDrumWc, "WC (Drum)", playerContext, result);
        ValidateCountField(player.KoDrumBw, "BW (Drum)", playerContext, result);
        ValidateCountField(player.KoDrumSw, "SW (Drum)", playerContext, result);

        // Validate Kickout Analysis - Opposition
        ValidateCountField(player.KoOppKow, "KoW (Opp)", playerContext, result);
        ValidateCountField(player.KoOppWc, "WC (Opp)", playerContext, result);
        ValidateCountField(player.KoOppBw, "BW (Opp)", playerContext, result);
        ValidateCountField(player.KoOppSw, "SW (Opp)", playerContext, result);

        // Validate Attacking Play
        ValidateCountField(player.Ta, "TA", playerContext, result);
        ValidateCountField(player.Kr, "KR", playerContext, result);
        ValidateCountField(player.Kl, "KL", playerContext, result);
        ValidateCountField(player.Cr, "CR", playerContext, result);
        ValidateCountField(player.Cl, "CL", playerContext, result);

        // Validate Shots from Play
        ValidateCountField(player.ShotsPlayTotal, "Shots Play Total", playerContext, result);
        ValidateCountField(player.ShotsPlayPoints, "Shots Play Points", playerContext, result);
        ValidateCountField(player.ShotsPlay2Points, "Shots Play 2 Points", playerContext, result);
        ValidateCountField(player.ShotsPlayGoals, "Shots Play Goals", playerContext, result);
        ValidateCountField(player.ShotsPlayWide, "Shots Play Wide", playerContext, result);
        ValidateCountField(player.ShotsPlayShort, "Shots Play Short", playerContext, result);
        ValidateCountField(player.ShotsPlaySave, "Shots Play Save", playerContext, result);
        ValidateCountField(player.ShotsPlayWoodwork, "Shots Play Woodwork", playerContext, result);
        ValidateCountField(player.ShotsPlayBlocked, "Shots Play Blocked", playerContext, result);
        ValidateCountField(player.ShotsPlay45, "Shots Play 45", playerContext, result);
        ValidatePercentageField(player.ShotsPlayPercentage, "Shots Play %", playerContext, result);

        // Validate Scoreable Frees
        ValidateCountField(player.FreesTotal, "Frees Total", playerContext, result);
        ValidateCountField(player.FreesPoints, "Frees Points", playerContext, result);
        ValidateCountField(player.Frees2Points, "Frees 2 Points", playerContext, result);
        ValidateCountField(player.FreesGoals, "Frees Goals", playerContext, result);
        ValidateCountField(player.FreesWide, "Frees Wide", playerContext, result);
        ValidateCountField(player.FreesShort, "Frees Short", playerContext, result);
        ValidateCountField(player.FreesSave, "Frees Save", playerContext, result);
        ValidateCountField(player.FreesWoodwork, "Frees Woodwork", playerContext, result);
        ValidateCountField(player.Frees45, "Frees 45", playerContext, result);
        ValidateCountField(player.FreesQf, "Frees QF", playerContext, result);
        ValidatePercentageField(player.FreesPercentage, "Frees %", playerContext, result);

        // Validate Total Shots
        ValidateCountField(player.TotalShots, "Total Shots", playerContext, result);
        ValidatePercentageField(player.TotalShotsPercentage, "Total Shots %", playerContext, result);

        // Validate Assists
        ValidateCountField(player.AssistsTotal, "Assists Total", playerContext, result);
        ValidateCountField(player.AssistsPoint, "Assists Point", playerContext, result);
        ValidateCountField(player.AssistsGoal, "Assists Goal", playerContext, result);

        // Validate Tackles
        ValidateCountField(player.TacklesTotal, "Tackles Total", playerContext, result);
        ValidateCountField(player.TacklesContested, "Tackles Contested", playerContext, result);
        ValidateCountField(player.TacklesMissed, "Tackles Missed", playerContext, result);
        ValidatePercentageField(player.TacklesPercentage, "Tackles %", playerContext, result);

        // Validate Frees Conceded
        ValidateCountField(player.FreesConcededTotal, "Frees Conceded Total", playerContext, result);
        ValidateCountField(player.FreesConcededAttack, "Frees Conceded Attack", playerContext, result);
        ValidateCountField(player.FreesConcededMidfield, "Frees Conceded Midfield", playerContext, result);
        ValidateCountField(player.FreesConcededDefense, "Frees Conceded Defense", playerContext, result);
        ValidateCountField(player.FreesConcededPenalty, "Frees Conceded Penalty", playerContext, result);

        // Validate 50m Free Conceded
        ValidateCountField(player.Frees50mTotal, "50m Frees Total", playerContext, result);
        ValidateCountField(player.Frees50mDelay, "50m Frees Delay", playerContext, result);
        ValidateCountField(player.Frees50mDissent, "50m Frees Dissent", playerContext, result);
        ValidateCountField(player.Frees50m3v3, "50m Frees 3v3", playerContext, result);

        // Validate Bookings
        ValidateCountField(player.YellowCards, "Yellow Cards", playerContext, result, maxValue: 5);
        ValidateCountField(player.BlackCards, "Black Cards", playerContext, result, maxValue: 5);
        ValidateCountField(player.RedCards, "Red Cards", playerContext, result, maxValue: 2);

        // Validate Throw Up
        ValidateCountField(player.ThrowUpWon, "Throw Up Won", playerContext, result);
        ValidateCountField(player.ThrowUpLost, "Throw Up Lost", playerContext, result);

        // Validate Goalkeeper Stats
        ValidateCountField(player.GkTotalKickouts, "GK Total Kickouts", playerContext, result);
        ValidateCountField(player.GkKickoutRetained, "GK Kickout Retained", playerContext, result);
        ValidateCountField(player.GkKickoutLost, "GK Kickout Lost", playerContext, result);
        ValidatePercentageField(player.GkKickoutPercentage, "GK Kickout %", playerContext, result);
        ValidateCountField(player.GkSaves, "GK Saves", playerContext, result);

        return result;
    }

    /// <summary>
    /// Validates a count field (non-negative integer).
    /// </summary>
    private void ValidateCountField(
        int value,
        string fieldName,
        string playerContext,
        ValidationResult result,
        int maxValue = MaxReasonableCount)
    {
        if (value < 0)
        {
            result.AddError($"{playerContext}: {fieldName} cannot be negative (got {value})");
        }
        else if (value > maxValue)
        {
            result.AddWarning($"{playerContext}: {fieldName} is unusually high ({value}, max expected {maxValue})");
        }
    }

    /// <summary>
    /// Validates a percentage field (0.0 to 1.0 or null).
    /// </summary>
    private void ValidatePercentageField(
        decimal? value,
        string fieldName,
        string playerContext,
        ValidationResult result)
    {
        if (value == null)
        {
            // Null is acceptable for percentages (indicates no data or division by zero)
            return;
        }

        if (value < MinPercentage)
        {
            result.AddError($"{playerContext}: {fieldName} cannot be negative (got {value})");
        }
        else if (value > MaxPercentage)
        {
            // Allow slightly over 1.0 for rounding errors, but warn
            if (value <= 1.05m)
            {
                result.AddWarning($"{playerContext}: {fieldName} is slightly over 100% ({value:P2}), likely rounding error");
            }
            else
            {
                result.AddError($"{playerContext}: {fieldName} exceeds 100% ({value:P2})");
            }
        }
    }
}
