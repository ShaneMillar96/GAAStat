using GAAStat.Services.Models;

namespace GAAStat.Services.Interfaces;

/// <summary>
/// Service for team-level statistics and comparisons
/// </summary>
public interface ITeamAnalyticsService
{
    /// <summary>
    /// Gets comprehensive team statistics for a season
    /// </summary>
    /// <param name="teamId">Team identifier</param>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Complete team performance metrics and averages</returns>
    Task<ServiceResult<TeamSeasonStatisticsDto>> GetTeamSeasonStatisticsAsync(int teamId, int seasonId);
    
    /// <summary>
    /// Gets team performance comparison against a specific opponent
    /// </summary>
    /// <param name="teamId">Team identifier</param>
    /// <param name="opponentId">Opponent team identifier</param>
    /// <param name="seasonId">Optional season filter</param>
    /// <returns>Head-to-head team performance analysis</returns>
    Task<ServiceResult<TeamComparisonDto>> GetTeamComparisonAsync(int teamId, int opponentId, int? seasonId = null);
    
    /// <summary>
    /// Gets team offensive efficiency metrics
    /// </summary>
    /// <param name="teamId">Team identifier</param>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Detailed offensive performance analysis</returns>
    Task<ServiceResult<TeamOffensiveStatsDto>> GetTeamOffensiveStatsAsync(int teamId, int seasonId);
    
    /// <summary>
    /// Gets team defensive efficiency metrics
    /// </summary>
    /// <param name="teamId">Team identifier</param>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Detailed defensive performance analysis</returns>
    Task<ServiceResult<TeamDefensiveStatsDto>> GetTeamDefensiveStatsAsync(int teamId, int seasonId);
    
    /// <summary>
    /// Gets team possession statistics and control metrics
    /// </summary>
    /// <param name="teamId">Team identifier</param>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Possession and ball control analysis</returns>
    Task<ServiceResult<TeamPossessionStatsDto>> GetTeamPossessionStatsAsync(int teamId, int seasonId);
    
    /// <summary>
    /// Gets team performance by venue (home vs away)
    /// </summary>
    /// <param name="teamId">Team identifier</param>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Home vs away performance breakdown</returns>
    Task<ServiceResult<TeamVenueAnalysisDto>> GetTeamVenueAnalysisAsync(int teamId, int seasonId);
    
    /// <summary>
    /// Gets team roster efficiency and player contribution analysis
    /// </summary>
    /// <param name="teamId">Team identifier</param>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Player contribution and roster depth analysis</returns>
    Task<ServiceResult<TeamRosterAnalysisDto>> GetTeamRosterAnalysisAsync(int teamId, int seasonId);
    
    /// <summary>
    /// Gets team performance trends throughout the season
    /// </summary>
    /// <param name="teamId">Team identifier</param>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Team performance progression and trends</returns>
    Task<ServiceResult<TeamTrendsDto>> GetTeamTrendsAsync(int teamId, int seasonId);
}