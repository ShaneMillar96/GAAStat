using GAAStat.Services.Dashboard.Models;

namespace GAAStat.Services.Dashboard.Interfaces;

/// <summary>
/// Service for retrieving dashboard statistics and aggregations
/// Provides cached access to performance metrics with 5-minute expiration
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Retrieves team performance overview with season totals and averages.
    /// Results are cached for 5 minutes to improve performance.
    /// </summary>
    /// <param name="seasonId">Season identifier (optional - uses current season if not provided)</param>
    /// <param name="competitionType">Filter by competition type (optional - includes all if not provided)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Team overview DTO with aggregated statistics</returns>
    Task<DashboardResult<TeamOverviewDto>> GetTeamOverviewAsync(
        int? seasonId = null,
        string? competitionType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves top performers by various metrics (scoring, possession, tackles, etc.).
    /// Results are cached for 5 minutes.
    /// </summary>
    /// <param name="metricType">Metric type to rank by (e.g., "scoring", "psr", "tackles")</param>
    /// <param name="seasonId">Season identifier (optional)</param>
    /// <param name="competitionType">Filter by competition type (optional)</param>
    /// <param name="topCount">Number of top performers to return (default: 10)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of top performers ordered by metric</returns>
    Task<DashboardResult<List<TopPerformerDto>>> GetTopPerformersAsync(
        string metricType,
        int? seasonId = null,
        string? competitionType = null,
        int topCount = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves recent match results with scores and key statistics.
    /// Results are cached for 5 minutes.
    /// </summary>
    /// <param name="seasonId">Season identifier (optional)</param>
    /// <param name="competitionType">Filter by competition type (optional)</param>
    /// <param name="matchCount">Number of recent matches to return (default: 5)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of recent matches ordered by date descending</returns>
    Task<DashboardResult<List<RecentMatchDto>>> GetRecentMatchesAsync(
        int? seasonId = null,
        string? competitionType = null,
        int matchCount = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves aggregated statistics for all players in the season.
    /// Results are cached for 5 minutes.
    /// </summary>
    /// <param name="seasonId">Season identifier (optional)</param>
    /// <param name="competitionType">Filter by competition type (optional)</param>
    /// <param name="positionCode">Filter by position code (GK, DEF, MID, FWD) (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of player statistics aggregated across all matches</returns>
    Task<DashboardResult<List<PlayerSeasonStatsDto>>> GetPlayerSeasonStatisticsAsync(
        int? seasonId = null,
        string? competitionType = null,
        string? positionCode = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves team form (last N matches) with win/loss/draw record.
    /// Results are cached for 5 minutes.
    /// </summary>
    /// <param name="seasonId">Season identifier (optional)</param>
    /// <param name="competitionType">Filter by competition type (optional)</param>
    /// <param name="matchCount">Number of recent matches for form calculation (default: 5)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Team form DTO with record and trend</returns>
    Task<DashboardResult<TeamFormDto>> GetTeamFormAsync(
        int? seasonId = null,
        string? competitionType = null,
        int matchCount = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates all cached dashboard data.
    /// Should be called after ETL operations complete successfully.
    /// </summary>
    Task InvalidateCacheAsync();
}
