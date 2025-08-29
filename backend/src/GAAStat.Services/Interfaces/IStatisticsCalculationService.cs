using GAAStat.Services.Models;

namespace GAAStat.Services.Interfaces;

/// <summary>
/// Service for calculating PSR values and derived performance metrics
/// </summary>
public interface IStatisticsCalculationService
{
    /// <summary>
    /// Calculates Performance Success Rate for a player in a specific match
    /// </summary>
    /// <param name="playerStatistics">Raw player statistics from Excel</param>
    /// <returns>Calculated PSR value in -3.0 to +3.0 range</returns>
    decimal CalculatePerformanceSuccessRate(PlayerStatisticsRow playerStatistics);
    
    /// <summary>
    /// Calculates comprehensive efficiency metrics for a player
    /// </summary>
    /// <param name="playerStatistics">Raw player statistics from Excel</param>
    /// <returns>Complete efficiency metrics including PSR, ratios, and percentages</returns>
    PlayerEfficiencyMetrics CalculateEfficiencyMetrics(PlayerStatisticsRow playerStatistics);
    
    /// <summary>
    /// Calculates team-level statistics from aggregated player data
    /// </summary>
    /// <param name="teamPlayerStats">All player statistics for a team in a match</param>
    /// <returns>Aggregated team performance metrics</returns>
    TeamEfficiencyMetrics CalculateTeamMetrics(IEnumerable<PlayerStatisticsRow> teamPlayerStats);
    
    /// <summary>
    /// Validates PSR calculation inputs and ranges
    /// </summary>
    /// <param name="playerStatistics">Player statistics to validate</param>
    /// <returns>Validation result with any errors</returns>
    ValidationResult ValidateStatisticsForCalculation(PlayerStatisticsRow playerStatistics);
    
    /// <summary>
    /// Gets PSR value for specific event types based on KPI definitions
    /// </summary>
    /// <param name="eventType">Event type code (1.0-16.0)</param>
    /// <param name="outcome">Event outcome (success/failure)</param>
    /// <returns>PSR value for the event outcome</returns>
    decimal GetEventPsrValue(decimal eventType, bool outcome);
}