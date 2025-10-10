using GAAStat.Dal.Interfaces;
using GAAStat.Dal.Models.Application;
using GAAStat.Services.Dashboard.Interfaces;
using GAAStat.Services.Dashboard.Models;
using GAAStat.Services.Dashboard.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GAAStat.Services.Dashboard.Services;

/// <summary>
/// Main dashboard service implementation with caching and aggregation logic
/// Follows patterns from MatchStatisticsEtlService with dashboard-specific concerns
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly IGAAStatDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DashboardService> _logger;
    private readonly ScoreCalculator _scoreCalculator;

    // Cache configuration
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix = "Dashboard_";

    public DashboardService(
        IGAAStatDbContext dbContext,
        IMemoryCache cache,
        ILogger<DashboardService> logger,
        ScoreCalculator scoreCalculator)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
        _scoreCalculator = scoreCalculator;
    }

    /// <inheritdoc/>
    public async Task<DashboardResult<TeamOverviewDto>> GetTeamOverviewAsync(
        int? seasonId = null,
        string? competitionType = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey("TeamOverview", seasonId, competitionType);

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            _logger.LogInformation(
                "Cache miss for TeamOverview. SeasonId: {SeasonId}, Competition: {Competition}",
                seasonId,
                competitionType);

            var result = DashboardResult<TeamOverviewDto>.CreateSuccess();
            result.StartTime = DateTime.UtcNow;

            try
            {
                // Get current season if not specified
                var targetSeasonId = seasonId ?? await GetCurrentSeasonIdAsync(cancellationToken);
                if (!targetSeasonId.HasValue)
                {
                    return result.WithError("NO_SEASON", "No season found");
                }

                // Get Drum team
                var drumTeam = await _dbContext.Teams
                    .FirstOrDefaultAsync(t => t.IsDrum, cancellationToken);

                if (drumTeam == null)
                {
                    return result.WithError("NO_DRUM_TEAM", "Drum team not found in database");
                }

                // Build query for matches
                var matchesQuery = _dbContext.Matches
                    .Include(m => m.Competition)
                    .Include(m => m.HomeTeam)
                    .Include(m => m.AwayTeam)
                    .Include(m => m.TeamStatistics)
                    .Where(m => m.Competition.SeasonId == targetSeasonId.Value);

                // Apply competition filter
                if (!string.IsNullOrWhiteSpace(competitionType))
                {
                    matchesQuery = matchesQuery.Where(m =>
                        m.Competition.Type == competitionType);
                }

                var matches = await matchesQuery
                    .OrderByDescending(m => m.MatchDate)
                    .ToListAsync(cancellationToken);

                if (!matches.Any())
                {
                    return result.WithWarning("NO_MATCHES", "No matches found for season");
                }

                // Calculate aggregated statistics
                var overview = CalculateTeamOverview(matches, drumTeam.TeamId);
                result.Data = overview;
                result.Success = true;
                result.EndTime = DateTime.UtcNow;

                _logger.LogInformation(
                    "Team overview calculated successfully. Matches: {Count}, Duration: {Duration}ms",
                    matches.Count,
                    result.Duration.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving team overview");
                return result.WithError("UNEXPECTED_ERROR", $"Error: {ex.Message}");
            }
        }) ?? DashboardResult<TeamOverviewDto>.CreateFailure("CACHE_ERROR", "Cache retrieval failed");
    }

    /// <inheritdoc/>
    public async Task<DashboardResult<List<TopPerformerDto>>> GetTopPerformersAsync(
        string metricType,
        int? seasonId = null,
        string? competitionType = null,
        int topCount = 10,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey($"TopPerformers_{metricType}_{topCount}",
            seasonId, competitionType);

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            _logger.LogInformation(
                "Cache miss for TopPerformers. Metric: {Metric}, Count: {Count}",
                metricType,
                topCount);

            var result = DashboardResult<List<TopPerformerDto>>.CreateSuccess();
            result.StartTime = DateTime.UtcNow;

            try
            {
                // Get current season if not specified
                var targetSeasonId = seasonId ?? await GetCurrentSeasonIdAsync(cancellationToken);
                if (!targetSeasonId.HasValue)
                {
                    return result.WithError("NO_SEASON", "No season found");
                }

                // Build player stats query
                var statsQuery = _dbContext.PlayerMatchStatistics
                    .Include(pms => pms.Player)
                        .ThenInclude(p => p.Position)
                    .Include(pms => pms.Match)
                        .ThenInclude(m => m.Competition)
                    .Where(pms => pms.Match.Competition.SeasonId == targetSeasonId.Value
                                  && pms.MinutesPlayed > 0); // Only include players who played

                // Apply competition filter
                if (!string.IsNullOrWhiteSpace(competitionType))
                {
                    statsQuery = statsQuery.Where(pms =>
                        pms.Match.Competition.Type == competitionType);
                }

                var playerStats = await statsQuery.ToListAsync(cancellationToken);

                if (!playerStats.Any())
                {
                    return result.WithWarning("NO_STATS", "No player statistics found");
                }

                // Group by player and calculate aggregates
                var topPerformers = CalculateTopPerformers(
                    playerStats,
                    metricType,
                    topCount);

                result.Data = topPerformers;
                result.Success = true;
                result.EndTime = DateTime.UtcNow;

                _logger.LogInformation(
                    "Top performers calculated. Metric: {Metric}, Players: {Count}, Duration: {Duration}ms",
                    metricType,
                    topPerformers.Count,
                    result.Duration.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving top performers");
                return result.WithError("UNEXPECTED_ERROR", $"Error: {ex.Message}");
            }
        }) ?? DashboardResult<List<TopPerformerDto>>.CreateFailure(
            "CACHE_ERROR", "Cache retrieval failed");
    }

    /// <inheritdoc/>
    public async Task<DashboardResult<List<RecentMatchDto>>> GetRecentMatchesAsync(
        int? seasonId = null,
        string? competitionType = null,
        int matchCount = 5,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey($"RecentMatches_{matchCount}",
            seasonId, competitionType);

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            _logger.LogInformation(
                "Cache miss for RecentMatches. Count: {Count}",
                matchCount);

            var result = DashboardResult<List<RecentMatchDto>>.CreateSuccess();
            result.StartTime = DateTime.UtcNow;

            try
            {
                // Get current season if not specified
                var targetSeasonId = seasonId ?? await GetCurrentSeasonIdAsync(cancellationToken);
                if (!targetSeasonId.HasValue)
                {
                    return result.WithError("NO_SEASON", "No season found");
                }

                // Build matches query
                var matchesQuery = _dbContext.Matches
                    .Include(m => m.Competition)
                    .Include(m => m.HomeTeam)
                    .Include(m => m.AwayTeam)
                    .Where(m => m.Competition.SeasonId == targetSeasonId.Value);

                // Apply competition filter
                if (!string.IsNullOrWhiteSpace(competitionType))
                {
                    matchesQuery = matchesQuery.Where(m =>
                        m.Competition.Type == competitionType);
                }

                var matches = await matchesQuery
                    .OrderByDescending(m => m.MatchDate)
                    .Take(matchCount)
                    .ToListAsync(cancellationToken);

                if (!matches.Any())
                {
                    return result.WithWarning("NO_MATCHES", "No recent matches found");
                }

                // Map to DTOs
                var recentMatches = matches.Select(m => MapToRecentMatchDto(m)).ToList();

                result.Data = recentMatches;
                result.Success = true;
                result.EndTime = DateTime.UtcNow;

                _logger.LogInformation(
                    "Recent matches retrieved. Count: {Count}, Duration: {Duration}ms",
                    recentMatches.Count,
                    result.Duration.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent matches");
                return result.WithError("UNEXPECTED_ERROR", $"Error: {ex.Message}");
            }
        }) ?? DashboardResult<List<RecentMatchDto>>.CreateFailure(
            "CACHE_ERROR", "Cache retrieval failed");
    }

    /// <inheritdoc/>
    public async Task<DashboardResult<List<PlayerSeasonStatsDto>>> GetPlayerSeasonStatisticsAsync(
        int? seasonId = null,
        string? competitionType = null,
        string? positionCode = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey($"PlayerSeasonStats_{positionCode}",
            seasonId, competitionType);

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            _logger.LogInformation(
                "Cache miss for PlayerSeasonStats. Position: {Position}",
                positionCode);

            var result = DashboardResult<List<PlayerSeasonStatsDto>>.CreateSuccess();
            result.StartTime = DateTime.UtcNow;

            try
            {
                // Get current season if not specified
                var targetSeasonId = seasonId ?? await GetCurrentSeasonIdAsync(cancellationToken);
                if (!targetSeasonId.HasValue)
                {
                    return result.WithError("NO_SEASON", "No season found");
                }

                // Build query
                var statsQuery = _dbContext.PlayerMatchStatistics
                    .Include(pms => pms.Player)
                        .ThenInclude(p => p.Position)
                    .Include(pms => pms.Match)
                        .ThenInclude(m => m.Competition)
                    .Where(pms => pms.Match.Competition.SeasonId == targetSeasonId.Value);

                // Apply filters
                if (!string.IsNullOrWhiteSpace(competitionType))
                {
                    statsQuery = statsQuery.Where(pms =>
                        pms.Match.Competition.Type == competitionType);
                }

                if (!string.IsNullOrWhiteSpace(positionCode))
                {
                    statsQuery = statsQuery.Where(pms =>
                        pms.Player.Position.Code == positionCode);
                }

                var playerStats = await statsQuery.ToListAsync(cancellationToken);

                if (!playerStats.Any())
                {
                    return result.WithWarning("NO_STATS", "No player statistics found");
                }

                // Group by player and aggregate
                var seasonStats = CalculatePlayerSeasonStats(playerStats);

                result.Data = seasonStats;
                result.Success = true;
                result.EndTime = DateTime.UtcNow;

                _logger.LogInformation(
                    "Player season stats calculated. Players: {Count}, Duration: {Duration}ms",
                    seasonStats.Count,
                    result.Duration.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving player season statistics");
                return result.WithError("UNEXPECTED_ERROR", $"Error: {ex.Message}");
            }
        }) ?? DashboardResult<List<PlayerSeasonStatsDto>>.CreateFailure(
            "CACHE_ERROR", "Cache retrieval failed");
    }

    /// <inheritdoc/>
    public async Task<DashboardResult<TeamFormDto>> GetTeamFormAsync(
        int? seasonId = null,
        string? competitionType = null,
        int matchCount = 5,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey($"TeamForm_{matchCount}",
            seasonId, competitionType);

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            _logger.LogInformation(
                "Cache miss for TeamForm. MatchCount: {Count}",
                matchCount);

            var result = DashboardResult<TeamFormDto>.CreateSuccess();
            result.StartTime = DateTime.UtcNow;

            try
            {
                // Get current season if not specified
                var targetSeasonId = seasonId ?? await GetCurrentSeasonIdAsync(cancellationToken);
                if (!targetSeasonId.HasValue)
                {
                    return result.WithError("NO_SEASON", "No season found");
                }

                // Get Drum team
                var drumTeam = await _dbContext.Teams
                    .FirstOrDefaultAsync(t => t.IsDrum, cancellationToken);

                if (drumTeam == null)
                {
                    return result.WithError("NO_DRUM_TEAM", "Drum team not found");
                }

                // Build query
                var matchesQuery = _dbContext.Matches
                    .Include(m => m.Competition)
                    .Include(m => m.HomeTeam)
                    .Include(m => m.AwayTeam)
                    .Where(m => m.Competition.SeasonId == targetSeasonId.Value);

                // Apply competition filter
                if (!string.IsNullOrWhiteSpace(competitionType))
                {
                    matchesQuery = matchesQuery.Where(m =>
                        m.Competition.Type == competitionType);
                }

                var matches = await matchesQuery
                    .OrderByDescending(m => m.MatchDate)
                    .Take(matchCount)
                    .ToListAsync(cancellationToken);

                if (!matches.Any())
                {
                    return result.WithWarning("NO_MATCHES", "No matches found for form calculation");
                }

                // Calculate form
                var teamForm = CalculateTeamForm(matches, drumTeam.TeamId);

                result.Data = teamForm;
                result.Success = true;
                result.EndTime = DateTime.UtcNow;

                _logger.LogInformation(
                    "Team form calculated. Matches: {Count}, Duration: {Duration}ms",
                    matches.Count,
                    result.Duration.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving team form");
                return result.WithError("UNEXPECTED_ERROR", $"Error: {ex.Message}");
            }
        }) ?? DashboardResult<TeamFormDto>.CreateFailure(
            "CACHE_ERROR", "Cache retrieval failed");
    }

    /// <inheritdoc/>
    public async Task InvalidateCacheAsync()
    {
        _logger.LogInformation("Invalidating all dashboard cache entries");

        // Note: IMemoryCache doesn't provide a built-in way to clear all entries
        // In production, consider using IDistributedCache with Redis for better cache management
        // For now, we'll rely on the 5-minute expiration

        await Task.CompletedTask;
    }

    // =========================================================================
    // Private Helper Methods
    // =========================================================================

    /// <summary>
    /// Gets the current season ID or null if not found
    /// </summary>
    private async Task<int?> GetCurrentSeasonIdAsync(CancellationToken cancellationToken)
    {
        var currentSeason = await _dbContext.Seasons
            .Where(s => s.IsCurrent)
            .FirstOrDefaultAsync(cancellationToken);

        return currentSeason?.SeasonId;
    }

    /// <summary>
    /// Builds a cache key from parameters
    /// </summary>
    private static string BuildCacheKey(string operation, int? seasonId, string? competitionType)
    {
        var key = $"{CacheKeyPrefix}{operation}_{seasonId ?? 0}";
        if (!string.IsNullOrWhiteSpace(competitionType))
        {
            key += $"_{competitionType}";
        }
        return key;
    }

    /// <summary>
    /// Calculates team overview statistics from matches
    /// </summary>
    private TeamOverviewDto CalculateTeamOverview(List<Match> matches, int drumTeamId)
    {
        var overview = new TeamOverviewDto
        {
            TotalMatches = matches.Count,
            Wins = 0,
            Losses = 0,
            Draws = 0
        };

        int totalScoreFor = 0;
        int totalScoreAgainst = 0;
        decimal totalPossession = 0;
        int matchesWithPossession = 0;

        foreach (var match in matches)
        {
            var isDrumHome = match.HomeTeamId == drumTeamId;
            var drumScore = isDrumHome
                ? _scoreCalculator.ParseGaaScore(match.HomeScoreFullTime)
                : _scoreCalculator.ParseGaaScore(match.AwayScoreFullTime);

            var oppScore = isDrumHome
                ? _scoreCalculator.ParseGaaScore(match.AwayScoreFullTime)
                : _scoreCalculator.ParseGaaScore(match.HomeScoreFullTime);

            totalScoreFor += drumScore;
            totalScoreAgainst += oppScore;

            // Determine result
            if (drumScore > oppScore) overview.Wins++;
            else if (drumScore < oppScore) overview.Losses++;
            else overview.Draws++;

            // Get possession from team statistics (Full period)
            var drumStats = match.TeamStatistics
                .FirstOrDefault(ts => ts.TeamId == drumTeamId && ts.Period == "Full");

            if (drumStats?.TotalPossession.HasValue == true)
            {
                totalPossession += drumStats.TotalPossession.Value;
                matchesWithPossession++;
            }
        }

        overview.TotalPointsScored = totalScoreFor;
        overview.TotalPointsConceded = totalScoreAgainst;
        overview.AveragePointsScored = matches.Count > 0
            ? (decimal)totalScoreFor / matches.Count
            : 0;
        overview.AveragePointsConceded = matches.Count > 0
            ? (decimal)totalScoreAgainst / matches.Count
            : 0;
        overview.AveragePossession = matchesWithPossession > 0
            ? totalPossession / matchesWithPossession
            : 0;
        overview.WinPercentage = matches.Count > 0
            ? (decimal)overview.Wins / matches.Count
            : 0;

        return overview;
    }

    /// <summary>
    /// Calculates top performers based on metric type
    /// </summary>
    private List<TopPerformerDto> CalculateTopPerformers(
        List<PlayerMatchStatistics> stats,
        string metricType,
        int topCount)
    {
        // Group by player
        var playerGroups = stats
            .GroupBy(s => s.PlayerId)
            .Select(g => new
            {
                PlayerId = g.Key,
                Player = g.First().Player,
                Stats = g.ToList()
            });

        // Calculate metric value based on type
        var performers = playerGroups.Select(pg =>
        {
            var dto = new TopPerformerDto
            {
                PlayerId = pg.PlayerId,
                PlayerName = pg.Player.FullName,
                JerseyNumber = pg.Player.JerseyNumber,
                Position = pg.Player.Position.Code,
                MatchesPlayed = pg.Stats.Count,
                TotalMinutesPlayed = pg.Stats.Sum(s => s.MinutesPlayed)
            };

            // Calculate metric value
            dto.MetricValue = metricType.ToLowerInvariant() switch
            {
                "scoring" => CalculateTotalPoints(pg.Stats),
                "psr" => pg.Stats.Sum(s => s.Psr),
                "tackles" => pg.Stats.Sum(s => s.TacklesTotal),
                "assists" => pg.Stats.Sum(s => s.AssistsTotal),
                "possession" => pg.Stats.Sum(s => s.Tp),
                "interceptions" => pg.Stats.Sum(s => s.Interceptions),
                _ => 0
            };

            dto.MetricType = metricType;

            return dto;
        });

        // Order and take top N
        return performers
            .OrderByDescending(p => p.MetricValue)
            .Take(topCount)
            .ToList();
    }

    /// <summary>
    /// Calculates total points from player statistics
    /// Parses scores notation and sums goals (3 pts) + points (1 pt)
    /// </summary>
    private decimal CalculateTotalPoints(List<PlayerMatchStatistics> stats)
    {
        decimal total = 0;

        foreach (var stat in stats)
        {
            // Use shots from play + frees
            total += (stat.ShotsPlayGoals * 3) + stat.ShotsPlayPoints + stat.ShotsPlay2Points;
            total += (stat.FreesGoals * 3) + stat.FreesPoints + stat.Frees2Points;
        }

        return total;
    }

    /// <summary>
    /// Maps Match entity to RecentMatchDto
    /// </summary>
    private RecentMatchDto MapToRecentMatchDto(Match match)
    {
        // Determine if Drum is home or away
        var isDrumHome = match.HomeTeam.IsDrum;

        var drumScore = isDrumHome
            ? match.HomeScoreFullTime
            : match.AwayScoreFullTime;

        var oppScore = isDrumHome
            ? match.AwayScoreFullTime
            : match.HomeScoreFullTime;

        var drumPoints = _scoreCalculator.ParseGaaScore(drumScore);
        var oppPoints = _scoreCalculator.ParseGaaScore(oppScore);

        return new RecentMatchDto
        {
            MatchId = match.MatchId,
            MatchDate = match.MatchDate,
            Competition = match.Competition.Name,
            CompetitionType = match.Competition.Type,
            Opponent = isDrumHome ? match.AwayTeam.Name : match.HomeTeam.Name,
            DrumScore = drumScore ?? "0-00",
            OpponentScore = oppScore ?? "0-00",
            DrumPoints = drumPoints,
            OpponentPoints = oppPoints,
            Result = drumPoints > oppPoints ? "Win" :
                     drumPoints < oppPoints ? "Loss" : "Draw",
            Venue = match.Venue
        };
    }

    /// <summary>
    /// Calculates aggregated season statistics by player
    /// </summary>
    private List<PlayerSeasonStatsDto> CalculatePlayerSeasonStats(
        List<PlayerMatchStatistics> stats)
    {
        return stats
            .GroupBy(s => s.PlayerId)
            .Select(g =>
            {
                var player = g.First().Player;
                var playerStats = g.ToList();

                return new PlayerSeasonStatsDto
                {
                    PlayerId = player.PlayerId,
                    PlayerName = player.FullName,
                    JerseyNumber = player.JerseyNumber,
                    Position = player.Position.Code,
                    MatchesPlayed = playerStats.Count,
                    TotalMinutesPlayed = playerStats.Sum(s => s.MinutesPlayed),
                    AverageMinutesPlayed = playerStats.Average(s => s.MinutesPlayed),

                    // Scoring
                    TotalPoints = CalculateTotalPoints(playerStats),
                    ShotsPlayTotal = playerStats.Sum(s => s.ShotsPlayTotal),
                    ShotsPlayPercentage = CalculateAveragePercentage(
                        playerStats.Select(s => s.ShotsPlayPercentage)),

                    // Possession
                    TotalPossessions = playerStats.Sum(s => s.Tp),
                    TotalPsr = playerStats.Sum(s => s.Psr),
                    AveragePsr = playerStats.Average(s => s.Psr),

                    // Defensive
                    TotalTackles = playerStats.Sum(s => s.TacklesTotal),
                    TacklesPercentage = CalculateAveragePercentage(
                        playerStats.Select(s => s.TacklesPercentage)),
                    TotalInterceptions = playerStats.Sum(s => s.Interceptions),

                    // Assists
                    TotalAssists = playerStats.Sum(s => s.AssistsTotal),

                    // Discipline
                    YellowCards = playerStats.Sum(s => s.YellowCards),
                    BlackCards = playerStats.Sum(s => s.BlackCards),
                    RedCards = playerStats.Sum(s => s.RedCards)
                };
            })
            .OrderByDescending(p => p.TotalPoints)
            .ToList();
    }

    /// <summary>
    /// Calculates average percentage, handling nulls
    /// </summary>
    private static decimal? CalculateAveragePercentage(IEnumerable<decimal?> percentages)
    {
        var values = percentages.Where(p => p.HasValue).Select(p => p!.Value).ToList();
        return values.Any() ? values.Average() : null;
    }

    /// <summary>
    /// Calculates team form from recent matches
    /// </summary>
    private TeamFormDto CalculateTeamForm(List<Match> matches, int drumTeamId)
    {
        var form = new TeamFormDto
        {
            LastNMatches = matches.Count,
            Wins = 0,
            Losses = 0,
            Draws = 0,
            FormString = ""
        };

        var formChars = new List<char>();

        foreach (var match in matches.OrderBy(m => m.MatchDate))
        {
            var isDrumHome = match.HomeTeamId == drumTeamId;
            var drumScore = isDrumHome
                ? _scoreCalculator.ParseGaaScore(match.HomeScoreFullTime)
                : _scoreCalculator.ParseGaaScore(match.AwayScoreFullTime);

            var oppScore = isDrumHome
                ? _scoreCalculator.ParseGaaScore(match.AwayScoreFullTime)
                : _scoreCalculator.ParseGaaScore(match.HomeScoreFullTime);

            if (drumScore > oppScore)
            {
                form.Wins++;
                formChars.Add('W');
            }
            else if (drumScore < oppScore)
            {
                form.Losses++;
                formChars.Add('L');
            }
            else
            {
                form.Draws++;
                formChars.Add('D');
            }
        }

        form.FormString = string.Join("", formChars);
        form.WinPercentage = matches.Count > 0
            ? (decimal)form.Wins / matches.Count
            : 0;

        return form;
    }
}
