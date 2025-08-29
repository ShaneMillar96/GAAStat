using GAAStat.Services.Models;

namespace GAAStat.Services.Interfaces;

/// <summary>
/// Service for individual player performance analysis
/// </summary>
public interface IPlayerAnalyticsService
{
    /// <summary>
    /// Gets comprehensive performance analysis for a player across a season
    /// </summary>
    /// <param name="playerName">Player name as stored in database</param>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Complete player performance metrics and trends</returns>
    Task<ServiceResult<PlayerPerformanceDto>> GetPlayerSeasonPerformanceAsync(string playerName, int seasonId);
    
    /// <summary>
    /// Gets player efficiency rating and detailed metrics
    /// </summary>
    /// <param name="playerId">Player identifier</param>
    /// <param name="seasonId">Optional season filter</param>
    /// <returns>Comprehensive efficiency metrics and ratings</returns>
    Task<ServiceResult<PlayerEfficiencyDto>> GetPlayerEfficiencyRatingAsync(string playerName, int? seasonId = null);
    
    /// <summary>
    /// Gets player performance comparison against team averages
    /// </summary>
    /// <param name="playerName">Player name</param>
    /// <param name="teamId">Team identifier</param>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Player vs team performance comparison</returns>
    Task<ServiceResult<PlayerTeamComparisonDto>> GetPlayerTeamComparisonAsync(string playerName, int teamId, int seasonId);
    
    /// <summary>
    /// Gets player performance trends over time
    /// </summary>
    /// <param name="playerName">Player name</param>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Performance trends and progression analysis</returns>
    Task<ServiceResult<PlayerTrendsDto>> GetPlayerTrendsAsync(string playerName, int seasonId);
    
    /// <summary>
    /// Gets player performance against different opposition teams
    /// </summary>
    /// <param name="playerName">Player name</param>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Performance breakdown by opposition</returns>
    Task<ServiceResult<PlayerOppositionAnalysisDto>> GetPlayerOppositionAnalysisAsync(string playerName, int seasonId);
    
    /// <summary>
    /// Gets player performance comparison between home and away matches
    /// </summary>
    /// <param name="playerName">Player name</param>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Home vs away performance analysis</returns>
    Task<ServiceResult<PlayerVenueAnalysisDto>> GetPlayerVenueAnalysisAsync(string playerName, int seasonId);
    
    /// <summary>
    /// Gets cumulative season statistics for a player
    /// </summary>
    /// <param name="playerName">Player name</param>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Cumulative statistics across all matches in season</returns>
    Task<ServiceResult<PlayerCumulativeStatsDto>> GetPlayerCumulativeStatsAsync(string playerName, int seasonId);
}