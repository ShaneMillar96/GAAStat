using GAAStat.Services.Models;

namespace GAAStat.Services.Interfaces;

/// <summary>
/// Service for multi-season trends and analysis
/// </summary>
public interface ISeasonAnalyticsService
{
    /// <summary>
    /// Gets comprehensive season summary with key statistics
    /// </summary>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Complete season overview with top performers and trends</returns>
    Task<ServiceResult<SeasonSummaryDto>> GetSeasonSummaryAsync(int seasonId);
    
    /// <summary>
    /// Gets cumulative statistics for all players and teams in a season
    /// </summary>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Season-wide cumulative statistics</returns>
    Task<ServiceResult<SeasonCumulativeStatsDto>> GetSeasonCumulativeStatisticsAsync(int seasonId);
    
    /// <summary>
    /// Gets top scorers across the season with various filters
    /// </summary>
    /// <param name="seasonId">Season identifier</param>
    /// <param name="position">Optional position filter</param>
    /// <param name="teamId">Optional team filter</param>
    /// <param name="limit">Maximum number of players to return</param>
    /// <returns>Top scoring players with detailed statistics</returns>
    Task<ServiceResult<IEnumerable<TopScorerDto>>> GetTopScorersAsync(int seasonId, string? position = null, int? teamId = null, int limit = 20);
    
    /// <summary>
    /// Gets PSR (Performance Success Rate) leaders across the season
    /// </summary>
    /// <param name="seasonId">Season identifier</param>
    /// <param name="position">Optional position filter</param>
    /// <param name="teamId">Optional team filter</param>
    /// <param name="minMatches">Minimum matches played to qualify</param>
    /// <param name="limit">Maximum number of players to return</param>
    /// <returns>Top PSR performers with trend analysis</returns>
    Task<ServiceResult<IEnumerable<PsrLeaderDto>>> GetPsrLeadersAsync(int seasonId, string? position = null, int? teamId = null, int minMatches = 3, int limit = 20);
    
    /// <summary>
    /// Gets season-wide performance trends and patterns
    /// </summary>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Statistical trends and patterns analysis</returns>
    Task<ServiceResult<SeasonTrendsDto>> GetSeasonTrendsAsync(int seasonId);
    
    /// <summary>
    /// Gets comparative analysis across multiple seasons
    /// </summary>
    /// <param name="seasonIds">List of season identifiers to compare</param>
    /// <returns>Cross-season comparison and trend analysis</returns>
    Task<ServiceResult<MultiSeasonComparisonDto>> GetMultiSeasonComparisonAsync(IEnumerable<int> seasonIds);
    
    /// <summary>
    /// Gets team rankings and league table for a season
    /// </summary>
    /// <param name="seasonId">Season identifier</param>
    /// <param name="competitionId">Optional competition filter</param>
    /// <returns>Team rankings based on performance metrics</returns>
    Task<ServiceResult<SeasonLeagueTableDto>> GetSeasonLeagueTableAsync(int seasonId, int? competitionId = null);
    
    /// <summary>
    /// Gets statistical leaders across all categories for a season
    /// </summary>
    /// <param name="seasonId">Season identifier</param>
    /// <param name="limit">Number of leaders per category</param>
    /// <returns>Leaders in all major statistical categories</returns>
    Task<ServiceResult<SeasonStatisticalLeadersDto>> GetSeasonStatisticalLeadersAsync(int seasonId, int limit = 10);
}