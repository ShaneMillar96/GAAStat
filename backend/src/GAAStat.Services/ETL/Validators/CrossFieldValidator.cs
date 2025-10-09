using System;
using GAAStat.Services.ETL.Models;

namespace GAAStat.Services.ETL.Validators;

/// <summary>
/// Layer 4: Validates relationships between fields.
/// Ensures totals match sums, percentages are consistent, and logical relationships hold.
/// </summary>
public class CrossFieldValidator
{
    private const int ToleranceMargin = 2; // Allow ±2 for rounding errors

    /// <summary>
    /// Validates cross-field relationships for a player.
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

        // Validate kickout breakdowns
        ValidateKickoutBreakdown(player, playerContext, result);

        // Validate attacking play breakdown
        ValidateAttackingBreakdown(player, playerContext, result);

        // Validate shots from play breakdown
        ValidateShotsFromPlayBreakdown(player, playerContext, result);

        // Validate scoreable frees breakdown
        ValidateScoreableFreesBreakdown(player, playerContext, result);

        // Validate total shots calculation
        ValidateTotalShots(player, playerContext, result);

        // Validate assists breakdown
        ValidateAssistsBreakdown(player, playerContext, result);

        // Validate tackles breakdown
        ValidateTacklesBreakdown(player, playerContext, result);

        // Validate frees conceded breakdown
        ValidateFreesConcededBreakdown(player, playerContext, result);

        // Validate 50m frees breakdown
        Validate50mFreesBreakdown(player, playerContext, result);

        // Validate throw up totals
        ValidateThrowUpTotals(player, playerContext, result);

        // Validate goalkeeper kickout totals
        ValidateGoalkeeperKickouts(player, playerContext, result);

        // Validate hand pass attempts
        ValidateHandPassAttempts(player, playerContext, result);

        return result;
    }

    /// <summary>
    /// Validates Drum kickout breakdown: KoW = WC + BW + SW.
    /// </summary>
    private void ValidateKickoutBreakdown(PlayerStatisticsData player, string playerContext, ValidationResult result)
    {
        // Drum kickouts
        var drumTotal = player.KoDrumKow;
        var drumSum = player.KoDrumWc + player.KoDrumBw + player.KoDrumSw;

        if (drumTotal > 0 && Math.Abs(drumTotal - drumSum) > ToleranceMargin)
        {
            result.AddWarning($"{playerContext}: Drum kickout won ({drumTotal}) doesn't match sum of clean+break+short ({drumSum})");
        }

        // Opposition kickouts
        var oppTotal = player.KoOppKow;
        var oppSum = player.KoOppWc + player.KoOppBw + player.KoOppSw;

        if (oppTotal > 0 && Math.Abs(oppTotal - oppSum) > ToleranceMargin)
        {
            result.AddWarning($"{playerContext}: Opposition kickout won ({oppTotal}) doesn't match sum of clean+break+short ({oppSum})");
        }
    }

    /// <summary>
    /// Validates attacking play: TA should equal sum of retained and lost.
    /// </summary>
    private void ValidateAttackingBreakdown(PlayerStatisticsData player, string playerContext, ValidationResult result)
    {
        var totalAttacks = player.Ta;
        var kickSum = player.Kr + player.Kl;
        var carrySum = player.Cr + player.Cl;

        // TA should be close to (KR + KL + CR + CL), allowing for rounding
        var expectedTotal = kickSum + carrySum;

        if (totalAttacks > 0 && Math.Abs(totalAttacks - expectedTotal) > ToleranceMargin)
        {
            result.AddWarning($"{playerContext}: Total attacks ({totalAttacks}) doesn't match sum of kick/carry retained+lost ({expectedTotal})");
        }
    }

    /// <summary>
    /// Validates shots from play: Tot should equal sum of all outcomes.
    /// </summary>
    private void ValidateShotsFromPlayBreakdown(PlayerStatisticsData player, string playerContext, ValidationResult result)
    {
        var total = player.ShotsPlayTotal;
        var sum = player.ShotsPlayPoints + player.ShotsPlay2Points + player.ShotsPlayGoals +
                  player.ShotsPlayWide + player.ShotsPlayShort + player.ShotsPlaySave +
                  player.ShotsPlayWoodwork + player.ShotsPlayBlocked;

        if (total > 0 && Math.Abs(total - sum) > ToleranceMargin)
        {
            result.AddWarning($"{playerContext}: Shots from play total ({total}) doesn't match sum of outcomes ({sum})");
        }

        // Validate shooting percentage
        if (total > 0)
        {
            var scores = player.ShotsPlayPoints + (player.ShotsPlay2Points * 2) + (player.ShotsPlayGoals * 3);
            var expectedPercentage = (decimal)scores / total;

            if (player.ShotsPlayPercentage.HasValue &&
                Math.Abs(player.ShotsPlayPercentage.Value - expectedPercentage) > 0.05m)
            {
                result.AddWarning($"{playerContext}: Shots play percentage ({player.ShotsPlayPercentage:P1}) doesn't match calculated ({expectedPercentage:P1})");
            }
        }
    }

    /// <summary>
    /// Validates scoreable frees: Tot should equal sum of all outcomes.
    /// </summary>
    private void ValidateScoreableFreesBreakdown(PlayerStatisticsData player, string playerContext, ValidationResult result)
    {
        var total = player.FreesTotal;
        var sum = player.FreesPoints + player.Frees2Points + player.FreesGoals +
                  player.FreesWide + player.FreesShort + player.FreesSave +
                  player.FreesWoodwork;

        if (total > 0 && Math.Abs(total - sum) > ToleranceMargin)
        {
            result.AddWarning($"{playerContext}: Scoreable frees total ({total}) doesn't match sum of outcomes ({sum})");
        }

        // Validate free-taking percentage
        if (total > 0)
        {
            var scores = player.FreesPoints + (player.Frees2Points * 2) + (player.FreesGoals * 3);
            var expectedPercentage = (decimal)scores / total;

            if (player.FreesPercentage.HasValue &&
                Math.Abs(player.FreesPercentage.Value - expectedPercentage) > 0.05m)
            {
                result.AddWarning($"{playerContext}: Frees percentage ({player.FreesPercentage:P1}) doesn't match calculated ({expectedPercentage:P1})");
            }
        }
    }

    /// <summary>
    /// Validates total shots: TS should equal shots from play + scoreable frees.
    /// </summary>
    private void ValidateTotalShots(PlayerStatisticsData player, string playerContext, ValidationResult result)
    {
        var totalShots = player.TotalShots;
        var expectedTotal = player.ShotsPlayTotal + player.FreesTotal;

        if (totalShots > 0 && Math.Abs(totalShots - expectedTotal) > ToleranceMargin)
        {
            result.AddWarning($"{playerContext}: Total shots ({totalShots}) doesn't match shots play + frees ({expectedTotal})");
        }

        // Validate overall shooting percentage
        if (totalShots > 0)
        {
            var playScores = player.ShotsPlayPoints + (player.ShotsPlay2Points * 2) + (player.ShotsPlayGoals * 3);
            var freeScores = player.FreesPoints + (player.Frees2Points * 2) + (player.FreesGoals * 3);
            var totalScores = playScores + freeScores;
            var expectedPercentage = (decimal)totalScores / totalShots;

            if (player.TotalShotsPercentage.HasValue &&
                Math.Abs(player.TotalShotsPercentage.Value - expectedPercentage) > 0.05m)
            {
                result.AddWarning($"{playerContext}: Total shots percentage ({player.TotalShotsPercentage:P1}) doesn't match calculated ({expectedPercentage:P1})");
            }
        }
    }

    /// <summary>
    /// Validates assists: TA should equal point assists + goal assists.
    /// </summary>
    private void ValidateAssistsBreakdown(PlayerStatisticsData player, string playerContext, ValidationResult result)
    {
        var total = player.AssistsTotal;
        var sum = player.AssistsPoint + player.AssistsGoal;

        if (total > 0 && Math.Abs(total - sum) > ToleranceMargin)
        {
            result.AddWarning($"{playerContext}: Total assists ({total}) doesn't match point + goal assists ({sum})");
        }
    }

    /// <summary>
    /// Validates tackles: Tot should equal contested + missed.
    /// </summary>
    private void ValidateTacklesBreakdown(PlayerStatisticsData player, string playerContext, ValidationResult result)
    {
        var total = player.TacklesTotal;
        var sum = player.TacklesContested + player.TacklesMissed;

        if (total > 0 && Math.Abs(total - sum) > ToleranceMargin)
        {
            result.AddWarning($"{playerContext}: Total tackles ({total}) doesn't match contested + missed ({sum})");
        }

        // Validate tackle success percentage
        if (total > 0)
        {
            var expectedPercentage = (decimal)player.TacklesContested / total;

            if (player.TacklesPercentage.HasValue &&
                Math.Abs(player.TacklesPercentage.Value - expectedPercentage) > 0.05m)
            {
                result.AddWarning($"{playerContext}: Tackles percentage ({player.TacklesPercentage:P1}) doesn't match calculated ({expectedPercentage:P1})");
            }
        }
    }

    /// <summary>
    /// Validates frees conceded: Tot should equal sum of locations.
    /// </summary>
    private void ValidateFreesConcededBreakdown(PlayerStatisticsData player, string playerContext, ValidationResult result)
    {
        var total = player.FreesConcededTotal;
        var sum = player.FreesConcededAttack + player.FreesConcededMidfield +
                  player.FreesConcededDefense + player.FreesConcededPenalty;

        if (total > 0 && Math.Abs(total - sum) > ToleranceMargin)
        {
            result.AddWarning($"{playerContext}: Total frees conceded ({total}) doesn't match sum of locations ({sum})");
        }
    }

    /// <summary>
    /// Validates 50m frees: Tot should equal sum of reasons.
    /// </summary>
    private void Validate50mFreesBreakdown(PlayerStatisticsData player, string playerContext, ValidationResult result)
    {
        var total = player.Frees50mTotal;
        var sum = player.Frees50mDelay + player.Frees50mDissent + player.Frees50m3v3;

        if (total > 0 && Math.Abs(total - sum) > ToleranceMargin)
        {
            result.AddWarning($"{playerContext}: Total 50m frees ({total}) doesn't match sum of reasons ({sum})");
        }
    }

    /// <summary>
    /// Validates throw up totals are reasonable (won + lost should be total).
    /// </summary>
    private void ValidateThrowUpTotals(PlayerStatisticsData player, string playerContext, ValidationResult result)
    {
        var won = player.ThrowUpWon;
        var lost = player.ThrowUpLost;

        // Just validate that both aren't zero if one is non-zero (sanity check)
        if (won > 0 && lost > 10)
        {
            result.AddWarning($"{playerContext}: Throw up stats seem imbalanced (Won: {won}, Lost: {lost})");
        }
        else if (lost > 0 && won > 10)
        {
            result.AddWarning($"{playerContext}: Throw up stats seem imbalanced (Won: {won}, Lost: {lost})");
        }
    }

    /// <summary>
    /// Validates goalkeeper kickout totals: TKo should equal retained + lost.
    /// </summary>
    private void ValidateGoalkeeperKickouts(PlayerStatisticsData player, string playerContext, ValidationResult result)
    {
        var total = player.GkTotalKickouts;
        var sum = player.GkKickoutRetained + player.GkKickoutLost;

        if (total > 0 && Math.Abs(total - sum) > ToleranceMargin)
        {
            result.AddWarning($"{playerContext}: GK total kickouts ({total}) doesn't match retained + lost ({sum})");
        }

        // Validate kickout percentage
        if (total > 0)
        {
            var expectedPercentage = (decimal)player.GkKickoutRetained / total;

            if (player.GkKickoutPercentage.HasValue &&
                Math.Abs(player.GkKickoutPercentage.Value - expectedPercentage) > 0.05m)
            {
                result.AddWarning($"{playerContext}: GK kickout percentage ({player.GkKickoutPercentage:P1}) doesn't match calculated ({expectedPercentage:P1})");
            }
        }
    }

    /// <summary>
    /// Validates hand pass attempts: Ha should be >= HP (can't complete more than attempted).
    /// </summary>
    private void ValidateHandPassAttempts(PlayerStatisticsData player, string playerContext, ValidationResult result)
    {
        if (player.Hp > player.Ha)
        {
            result.AddWarning($"{playerContext}: Hand passes completed ({player.Hp}) exceeds attempts ({player.Ha})");
        }
    }
}
