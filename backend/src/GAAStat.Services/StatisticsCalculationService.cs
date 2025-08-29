using GAAStat.Services.Interfaces;
using GAAStat.Services.Models;
using Microsoft.Extensions.Logging;

namespace GAAStat.Services;

/// <summary>
/// Service for calculating PSR values and derived performance metrics
/// </summary>
public class StatisticsCalculationService : IStatisticsCalculationService
{
    private readonly ILogger<StatisticsCalculationService> _logger;

    public StatisticsCalculationService(ILogger<StatisticsCalculationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Calculates Performance Success Rate for a player based on Excel statistics
    /// </summary>
    public decimal CalculatePerformanceSuccessRate(PlayerStatisticsRow playerStatistics)
    {
        if (playerStatistics.PerformanceSuccessRate.HasValue)
        {
            // Use existing PSR value from Excel if available
            return Math.Max(GaaConstants.MIN_PSR_VALUE, Math.Min(GaaConstants.MAX_PSR_VALUE, playerStatistics.PerformanceSuccessRate.Value));
        }

        decimal totalPsr = 0m;

        // Scoring contributions (positive PSR)
        if (playerStatistics.Goals.HasValue)
            totalPsr += playerStatistics.Goals.Value * PsrValues.GOAL_SCORED;

        if (playerStatistics.TwoPointers.HasValue)
            totalPsr += playerStatistics.TwoPointers.Value * PsrValues.TWO_POINT_SCORE;

        if (playerStatistics.Points.HasValue)
            totalPsr += playerStatistics.Points.Value * PsrValues.POINT_SCORED;

        // Defensive contributions (positive PSR)
        if (playerStatistics.SuccessfulTackles.HasValue)
            totalPsr += playerStatistics.SuccessfulTackles.Value * PsrValues.SUCCESSFUL_TACKLE;

        if (playerStatistics.TurnoversWon.HasValue)
            totalPsr += playerStatistics.TurnoversWon.Value * PsrValues.BALL_WON_TACKLE;

        if (playerStatistics.Saves.HasValue || playerStatistics.GoalkeeperSaves.HasValue)
        {
            var saves = (playerStatistics.Saves ?? 0) + (playerStatistics.GoalkeeperSaves ?? 0);
            totalPsr += saves * PsrValues.GOALKEEPER_SAVE;
        }

        // Possession contributions (positive PSR)
        if (playerStatistics.TotalPossessions.HasValue)
            totalPsr += playerStatistics.TotalPossessions.Value * PsrValues.POSSESSION;

        // Negative contributions
        if (playerStatistics.RedCards.HasValue)
            totalPsr += playerStatistics.RedCards.Value * PsrValues.RED_CARD;

        if (playerStatistics.BlackCards.HasValue)
            totalPsr += playerStatistics.BlackCards.Value * PsrValues.BLACK_CARD;

        if (playerStatistics.YellowCards.HasValue)
            totalPsr += playerStatistics.YellowCards.Value * PsrValues.YELLOW_CARD;

        if (playerStatistics.TotalPossessionsLost.HasValue)
            totalPsr += playerStatistics.TotalPossessionsLost.Value * PsrValues.POSSESSION_LOST;

        if (playerStatistics.ShotsShort.HasValue)
            totalPsr += playerStatistics.ShotsShort.Value * PsrValues.SHORT_SHOT;

        // Ensure PSR stays within valid range
        return Math.Max(GaaConstants.MIN_PSR_VALUE, Math.Min(GaaConstants.MAX_PSR_VALUE, totalPsr));
    }

    /// <summary>
    /// Calculates comprehensive efficiency metrics for a player
    /// </summary>
    public PlayerEfficiencyMetrics CalculateEfficiencyMetrics(PlayerStatisticsRow playerStatistics)
    {
        var psr = CalculatePerformanceSuccessRate(playerStatistics);

        return new PlayerEfficiencyMetrics
        {
            PlayerName = playerStatistics.PlayerName,
            JerseyNumber = playerStatistics.JerseyNumber,
            PerformanceSuccessRate = psr,
            EventsPerPsrRatio = CalculateEventsPerPsrRatio(playerStatistics, psr),
            PsrPerPossessionRatio = CalculatePsrPerPossessionRatio(psr, playerStatistics.TotalPossessions),
            ShotEfficiency = CalculateShotEfficiency(playerStatistics),
            ScoreConversionRate = CalculateScoreConversionRate(playerStatistics),
            TackleSuccessRate = CalculateTackleSuccessRate(playerStatistics),
            PassCompletionRate = CalculatePassCompletionRate(playerStatistics),
            PossessionRetentionRate = CalculatePossessionRetentionRate(playerStatistics),
            DefensiveEfficiency = CalculateDefensiveEfficiency(playerStatistics),
            AttackingEfficiency = CalculateAttackingEfficiency(playerStatistics),
            OverallRating = CalculateOverallRating(psr, playerStatistics)
        };
    }

    /// <summary>
    /// Calculates team-level statistics from aggregated player data
    /// </summary>
    public TeamEfficiencyMetrics CalculateTeamMetrics(IEnumerable<PlayerStatisticsRow> teamPlayerStats)
    {
        var playerStatsList = teamPlayerStats.ToList();
        if (!playerStatsList.Any())
        {
            return new TeamEfficiencyMetrics();
        }

        var totalPsr = playerStatsList.Sum(p => CalculatePerformanceSuccessRate(p));
        var totalPossessions = playerStatsList.Sum(p => p.TotalPossessions ?? 0);
        var totalEvents = playerStatsList.Sum(p => p.TotalEvents ?? 0);
        var totalScores = playerStatsList.Sum(p => (p.Goals ?? 0) + (p.TwoPointers ?? 0) + (p.Points ?? 0));
        var totalAttempts = playerStatsList.Sum(p => p.TotalAttempts ?? 0);
        var totalTurnovers = playerStatsList.Sum(p => p.TotalPossessionsLost ?? 0);

        return new TeamEfficiencyMetrics
        {
            PlayersAnalyzed = playerStatsList.Count,
            AveragePerformanceSuccessRate = playerStatsList.Count > 0 ? totalPsr / playerStatsList.Count : 0,
            TeamTotalPsr = totalPsr,
            TotalPossessions = totalPossessions,
            TotalEvents = totalEvents,
            PossessionRetentionRate = totalPossessions > 0 ? ((totalPossessions - totalTurnovers) / (decimal)totalPossessions) * 100 : 0,
            ScoringEfficiency = totalAttempts > 0 ? (totalScores / (decimal)totalAttempts) * 100 : 0,
            TotalScores = totalScores,
            TotalAttempts = totalAttempts,
            TotalTurnovers = totalTurnovers,
            TurnoverRate = totalPossessions > 0 ? (totalTurnovers / (decimal)totalPossessions) * 100 : 0
        };
    }

    /// <summary>
    /// Validates PSR calculation inputs and ranges
    /// </summary>
    public ValidationResult ValidateStatisticsForCalculation(PlayerStatisticsRow playerStatistics)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Required fields validation
        if (string.IsNullOrWhiteSpace(playerStatistics.PlayerName))
            errors.Add("Player name is required");

        if (playerStatistics.JerseyNumber.HasValue)
        {
            if (playerStatistics.JerseyNumber < GaaConstants.MIN_JERSEY_NUMBER || 
                playerStatistics.JerseyNumber > GaaConstants.MAX_JERSEY_NUMBER)
            {
                errors.Add($"Jersey number must be between {GaaConstants.MIN_JERSEY_NUMBER} and {GaaConstants.MAX_JERSEY_NUMBER}");
            }
        }

        // Minutes played validation
        if (playerStatistics.MinutesPlayed.HasValue && playerStatistics.MinutesPlayed > 90)
        {
            warnings.Add($"Minutes played ({playerStatistics.MinutesPlayed}) exceeds typical match duration");
        }

        // PSR range validation
        if (playerStatistics.PerformanceSuccessRate.HasValue)
        {
            if (playerStatistics.PerformanceSuccessRate < GaaConstants.MIN_PSR_VALUE || 
                playerStatistics.PerformanceSuccessRate > GaaConstants.MAX_PSR_VALUE)
            {
                warnings.Add($"PSR value ({playerStatistics.PerformanceSuccessRate}) is outside expected range");
            }
        }

        // Statistical consistency checks
        if (playerStatistics.TotalEvents.HasValue && playerStatistics.TotalEvents < 0)
            errors.Add("Total events cannot be negative");

        if (playerStatistics.TotalPossessions.HasValue && playerStatistics.TotalPossessions < 0)
            errors.Add("Total possessions cannot be negative");

        // Card validation
        ValidateCardCounts(playerStatistics, errors);

        return errors.Any() ? ValidationResult.Failed(errors) : ValidationResult.Success();
    }

    /// <summary>
    /// Gets PSR value for specific event types based on KPI definitions
    /// </summary>
    public decimal GetEventPsrValue(decimal eventType, bool outcome)
    {
        return eventType switch
        {
            PsrEventTypes.KICKOUT => outcome ? 1.0m : -1.0m,
            PsrEventTypes.ATTACKS => outcome ? 1.0m : 0.0m,
            PsrEventTypes.SHOT_FROM_PLAY => outcome ? 3.0m : -1.0m,
            PsrEventTypes.SCOREABLE_FREE => outcome ? 3.0m : -1.0m,
            PsrEventTypes.SCORE_SOURCE => 0.0m,
            PsrEventTypes.TACKLE => outcome ? 1.0m : -1.0m,
            PsrEventTypes.FREE_CONCEDED => -2.0m,
            PsrEventTypes.POSSESSION_LOST => -1.0m,
            PsrEventTypes.BOOKINGS => -1.0m,
            PsrEventTypes.POSSESSIONS => 1.0m,
            PsrEventTypes.BALL_WON => 2.0m,
            PsrEventTypes.SCORE_ASSIST => outcome ? 2.0m : 1.0m,
            PsrEventTypes.GOALKEEPERS => outcome ? 3.0m : -1.0m,
            PsrEventTypes.ATTACK_SOURCE => 0.0m,
            PsrEventTypes.SHOT_SOURCE => 0.0m,
            PsrEventTypes.FIFTY_METER_FREE => -2.0m,
            _ => 0.0m
        };
    }

    #region Private Helper Methods

    private decimal? CalculateEventsPerPsrRatio(PlayerStatisticsRow stats, decimal psr)
    {
        if (!stats.TotalEvents.HasValue || stats.TotalEvents == 0 || psr == 0) return null;
        return stats.TotalEvents.Value / psr;
    }

    private decimal? CalculatePsrPerPossessionRatio(decimal psr, int? totalPossessions)
    {
        if (!totalPossessions.HasValue || totalPossessions == 0) return null;
        return psr / totalPossessions.Value;
    }

    private decimal? CalculateShotEfficiency(PlayerStatisticsRow stats)
    {
        var totalScores = (stats.Goals ?? 0) + (stats.TwoPointers ?? 0) + (stats.Points ?? 0);
        var totalAttempts = stats.TotalAttempts ?? 0;
        
        if (totalAttempts == 0) return null;
        return (totalScores / (decimal)totalAttempts) * 100;
    }

    private decimal? CalculateScoreConversionRate(PlayerStatisticsRow stats)
    {
        var totalScores = (stats.Goals ?? 0) + (stats.TwoPointers ?? 0) + (stats.Points ?? 0);
        var totalShots = totalScores + (stats.ShotsWide ?? 0) + (stats.ShotsSaved ?? 0) + (stats.ShotsShort ?? 0);
        
        if (totalShots == 0) return null;
        return (totalScores / (decimal)totalShots) * 100;
    }

    private decimal? CalculateTackleSuccessRate(PlayerStatisticsRow stats)
    {
        var successfulTackles = stats.SuccessfulTackles ?? 0;
        var totalTackles = successfulTackles + (stats.Turnovers ?? 0);
        
        if (totalTackles == 0) return null;
        return (successfulTackles / (decimal)totalTackles) * 100;
    }

    private decimal? CalculatePassCompletionRate(PlayerStatisticsRow stats)
    {
        var totalPasses = (stats.KickPasses ?? 0) + (stats.HandPasses ?? 0);
        if (totalPasses == 0) return null;
        
        // Assuming successful passes = total passes - handling errors (simplified)
        var errors = stats.HandlingErrors ?? 0;
        var successfulPasses = Math.Max(0, totalPasses - errors);
        
        return (successfulPasses / (decimal)totalPasses) * 100;
    }

    private decimal? CalculatePossessionRetentionRate(PlayerStatisticsRow stats)
    {
        var totalPossessions = stats.TotalPossessions ?? 0;
        var possessionsLost = stats.TotalPossessionsLost ?? 0;
        
        if (totalPossessions == 0) return null;
        return ((totalPossessions - possessionsLost) / (decimal)totalPossessions) * 100;
    }

    private decimal? CalculateDefensiveEfficiency(PlayerStatisticsRow stats)
    {
        var defensiveActions = (stats.SuccessfulTackles ?? 0) + (stats.Interceptions ?? 0) + (stats.TurnoversWon ?? 0);
        var defensiveEvents = defensiveActions + (stats.Turnovers ?? 0);
        
        if (defensiveEvents == 0) return null;
        return (defensiveActions / (decimal)defensiveEvents) * 100;
    }

    private decimal? CalculateAttackingEfficiency(PlayerStatisticsRow stats)
    {
        var scores = (stats.Goals ?? 0) + (stats.TwoPointers ?? 0) + (stats.Points ?? 0);
        var attempts = stats.TotalAttempts ?? 0;
        
        if (attempts == 0) return null;
        return (scores / (decimal)attempts) * 100;
    }

    private decimal CalculateOverallRating(decimal psr, PlayerStatisticsRow stats)
    {
        // Normalize PSR to 0-100 scale and factor in minutes played
        var normalizedPsr = ((psr - GaaConstants.MIN_PSR_VALUE) / (GaaConstants.MAX_PSR_VALUE - GaaConstants.MIN_PSR_VALUE)) * 100;
        
        // Apply minutes played factor (players with more minutes get slight boost)
        var minutesFactor = stats.MinutesPlayed.HasValue ? Math.Min(1.2m, (stats.MinutesPlayed.Value / 60m)) : 1.0m;
        
        return Math.Max(0, Math.Min(100, normalizedPsr * minutesFactor));
    }

    private void ValidateCardCounts(PlayerStatisticsRow stats, List<string> errors)
    {
        if (stats.RedCards.HasValue && stats.RedCards < 0)
            errors.Add("Red card count cannot be negative");
        
        if (stats.BlackCards.HasValue && stats.BlackCards < 0)
            errors.Add("Black card count cannot be negative");
        
        if (stats.YellowCards.HasValue && stats.YellowCards < 0)
            errors.Add("Yellow card count cannot be negative");

        // Logical card validation
        if (stats.RedCards.HasValue && stats.RedCards > 1)
            errors.Add("A player cannot receive more than one red card per match");
    }

    #endregion
}