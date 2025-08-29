using GAAStat.Services.Models;

namespace GAAStat.Services.Interfaces;

/// <summary>
/// Service for match-level analysis and reporting
/// </summary>
public interface IMatchAnalyticsService
{
    /// <summary>
    /// Gets comprehensive summary statistics for a specific match
    /// </summary>
    /// <param name="matchId">Match identifier</param>
    /// <returns>Complete match statistics including team and player summaries</returns>
    Task<ServiceResult<MatchSummaryDto>> GetMatchSummaryAsync(int matchId);
    
    /// <summary>
    /// Gets detailed team performance comparison for a match
    /// </summary>
    /// <param name="matchId">Match identifier</param>
    /// <returns>Side-by-side team performance comparison</returns>
    Task<ServiceResult<MatchTeamComparisonDto>> GetMatchTeamComparisonAsync(int matchId);
    
    /// <summary>
    /// Gets kickout analysis for a specific match
    /// </summary>
    /// <param name="matchId">Match identifier</param>
    /// <returns>Comprehensive kickout statistics and analysis</returns>
    Task<ServiceResult<KickoutAnalysisDto>> GetKickoutAnalysisAsync(int matchId);
    
    /// <summary>
    /// Gets shot analysis including conversion rates and efficiency
    /// </summary>
    /// <param name="matchId">Match identifier</param>
    /// <returns>Detailed shot analysis and scoring patterns</returns>
    Task<ServiceResult<ShotAnalysisDto>> GetShotAnalysisAsync(int matchId);
    
    /// <summary>
    /// Gets match momentum analysis showing performance trends
    /// </summary>
    /// <param name="matchId">Match identifier</param>
    /// <returns>Match momentum and performance trend data</returns>
    Task<ServiceResult<MatchMomentumDto>> GetMatchMomentumAnalysisAsync(int matchId);
    
    /// <summary>
    /// Gets top performers in a specific match across all categories
    /// </summary>
    /// <param name="matchId">Match identifier</param>
    /// <param name="limit">Maximum number of players to return per category</param>
    /// <returns>Top performers in various statistical categories</returns>
    Task<ServiceResult<MatchTopPerformersDto>> GetMatchTopPerformersAsync(int matchId, int limit = 5);
}