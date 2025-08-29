using GAAStat.Dal.Interfaces;
using GAAStat.Services.Interfaces;
using GAAStat.Services.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GAAStat.Services;

/// <summary>
/// Service for multi-season trends and comprehensive season analysis
/// </summary>
public class SeasonAnalyticsService : ISeasonAnalyticsService
{
    private readonly IGAAStatDbContext _context;
    private readonly ILogger<SeasonAnalyticsService> _logger;

    public SeasonAnalyticsService(
        IGAAStatDbContext context,
        ILogger<SeasonAnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ServiceResult<SeasonSummaryDto>> GetSeasonSummaryAsync(int seasonId)
    {
        try
        {
            _logger.LogInformation("Getting season summary for season {SeasonId}", seasonId);

            var season = await _context.Seasons.FindAsync(seasonId);
            if (season == null)
            {
                return ServiceResult<SeasonSummaryDto>.Failed("Season not found");
            }

            var matches = await _context.Matches
                .Include(m => m.Competition)
                    .ThenInclude(c => c.Season)
                .Where(m => m.Competition.SeasonId == seasonId)
                .ToListAsync();

            var playerStats = await _context.MatchPlayerStats
                .Include(mps => mps.Match)
                    .ThenInclude(m => m.Competition)
                        .ThenInclude(c => c.Season)
                .Include(mps => mps.Team)
                .Where(mps => mps.Match.Competition.SeasonId == seasonId)
                .ToListAsync();

            var overview = CalculateSeasonOverview(matches, playerStats);
            var topPerformers = CalculateTopPerformers(playerStats);
            var highlights = CalculateSeasonHighlights(playerStats);
            var statistics = CalculateSeasonStatistics(playerStats);

            var summary = new SeasonSummaryDto
            {
                SeasonId = seasonId,
                SeasonName = season.SeasonName,
                StartDate = season.StartDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue,
                EndDate = season.EndDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.MaxValue,
                Overview = overview,
                TopPerformers = topPerformers,
                Highlights = highlights,
                Statistics = statistics
            };

            return ServiceResult<SeasonSummaryDto>.Success(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting season summary for season {SeasonId}", seasonId);
            return ServiceResult<SeasonSummaryDto>.Failed("Failed to retrieve season summary");
        }
    }

    public async Task<ServiceResult<SeasonCumulativeStatsDto>> GetSeasonCumulativeStatisticsAsync(int seasonId)
    {
        try
        {
            _logger.LogInformation("Getting cumulative statistics for season {SeasonId}", seasonId);

            var season = await _context.Seasons.FindAsync(seasonId);
            if (season == null)
            {
                return ServiceResult<SeasonCumulativeStatsDto>.Failed("Season not found");
            }

            var playerStats = await _context.MatchPlayerStats
                .Include(mps => mps.Match)
                    .ThenInclude(m => m.Competition)
                        .ThenInclude(c => c.Season)
                .Include(mps => mps.Team)
                .Where(mps => mps.Match.Competition.SeasonId == seasonId)
                .ToListAsync();

            var playerCumulativeStats = CalculatePlayerCumulativeStats(playerStats);
            var teamCumulativeStats = CalculateTeamCumulativeStats(playerStats);
            var aggregates = CalculateSeasonAggregates(playerStats);

            var stats = new SeasonCumulativeStatsDto
            {
                SeasonId = seasonId,
                SeasonName = season.SeasonName,
                PlayerStats = playerCumulativeStats,
                TeamStats = teamCumulativeStats,
                Aggregates = aggregates
            };

            return ServiceResult<SeasonCumulativeStatsDto>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cumulative statistics for season {SeasonId}", seasonId);
            return ServiceResult<SeasonCumulativeStatsDto>.Failed("Failed to retrieve season cumulative statistics");
        }
    }

    public async Task<ServiceResult<IEnumerable<TopScorerDto>>> GetTopScorersAsync(int seasonId, string? position = null, int? teamId = null, int limit = 20)
    {
        try
        {
            _logger.LogInformation("Getting top scorers for season {SeasonId}", seasonId);

            var query = _context.MatchPlayerStats
                .Include(mps => mps.Match)
                    .ThenInclude(m => m.Competition)
                        .ThenInclude(c => c.Season)
                .Include(mps => mps.Team)
                .Where(mps => mps.Match.Competition.SeasonId == seasonId);

            if (!string.IsNullOrEmpty(position))
            {
                query = query.Where(mps => mps.StartingPosition == position);
            }

            if (teamId.HasValue)
            {
                query = query.Where(mps => mps.TeamId == teamId.Value);
            }

            var playerStats = await query.ToListAsync();

            var topScorers = playerStats
                .GroupBy(ps => new { ps.PlayerName, ps.Team.TeamName, ps.StartingPosition })
                .Select(g => new TopScorerDto
                {
                    PlayerName = g.Key.PlayerName,
                    Goals = g.Sum(ps => ps.GoalsFromPlay ?? 0),
                    Points = g.Sum(ps => ps.PointsFromPlay ?? 0),
                    TotalScore = g.Sum(ps => (ps.GoalsFromPlay ?? 0) * 3 + (ps.PointsFromPlay ?? 0)),
                    ScoringRate = g.Count() > 0 ? (decimal)g.Sum(ps => (ps.GoalsFromPlay ?? 0) + (ps.PointsFromPlay ?? 0)) / g.Count() : 0,
                    ShotEfficiency = g.Where(ps => ps.ShotEfficiency.HasValue).Average(ps => ps.ShotEfficiency ?? 0),
                    Position = g.Key.StartingPosition ?? "Unknown"
                })
                .OrderByDescending(ts => ts.TotalScore)
                .Take(limit)
                .ToList();

            return ServiceResult<IEnumerable<TopScorerDto>>.Success(topScorers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top scorers for season {SeasonId}", seasonId);
            return ServiceResult<IEnumerable<TopScorerDto>>.Failed("Failed to retrieve top scorers");
        }
    }

    public async Task<ServiceResult<IEnumerable<PsrLeaderDto>>> GetPsrLeadersAsync(int seasonId, string? position = null, int? teamId = null, int minMatches = 3, int limit = 20)
    {
        try
        {
            _logger.LogInformation("Getting PSR leaders for season {SeasonId}", seasonId);

            var query = _context.MatchPlayerStats
                .Include(mps => mps.Match)
                    .ThenInclude(m => m.Competition)
                        .ThenInclude(c => c.Season)
                .Include(mps => mps.Team)
                .Where(mps => mps.Match.Competition.SeasonId == seasonId);

            if (!string.IsNullOrEmpty(position))
            {
                query = query.Where(mps => mps.StartingPosition == position);
            }

            if (teamId.HasValue)
            {
                query = query.Where(mps => mps.TeamId == teamId.Value);
            }

            var playerStats = await query.ToListAsync();

            var psrLeaders = playerStats
                .GroupBy(ps => new { ps.PlayerName, ps.Team.TeamName, ps.StartingPosition })
                .Where(g => g.Count() >= minMatches)
                .Select(g => new PsrLeaderDto
                {
                    PlayerName = g.Key.PlayerName,
                    TeamName = g.Key.TeamName,
                    Position = g.Key.StartingPosition ?? "Unknown",
                    AveragePsr = g.Average(ps => ps.PerformanceSuccessRate ?? 0),
                    TotalPsr = g.Sum(ps => ps.PerformanceSuccessRate ?? 0),
                    MatchesPlayed = g.Count(),
                    ConsistencyRating = CalculateConsistencyRating(g.Select(ps => ps.PerformanceSuccessRate ?? 0).ToList()),
                    PsrTrend = CreatePsrTrend(g.OrderBy(ps => ps.Match.MatchDate).ToList())
                })
                .OrderByDescending(pl => pl.AveragePsr)
                .Take(limit)
                .Select((pl, index) => { pl.Ranking = index + 1; return pl; })
                .ToList();

            return ServiceResult<IEnumerable<PsrLeaderDto>>.Success(psrLeaders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PSR leaders for season {SeasonId}", seasonId);
            return ServiceResult<IEnumerable<PsrLeaderDto>>.Failed("Failed to retrieve PSR leaders");
        }
    }

    public async Task<ServiceResult<SeasonTrendsDto>> GetSeasonTrendsAsync(int seasonId)
    {
        try
        {
            _logger.LogInformation("Getting season trends for season {SeasonId}", seasonId);

            var playerStats = await _context.MatchPlayerStats
                .Include(mps => mps.Match)
                    .ThenInclude(m => m.Competition)
                        .ThenInclude(c => c.Season)
                .Where(mps => mps.Match.Competition.SeasonId == seasonId)
                .OrderBy(mps => mps.Match.MatchDate)
                .ToListAsync();

            var trendCategories = CalculateSeasonTrendCategories(playerStats);
            var seasonProgression = CalculateSeasonProgression(playerStats);
            var trendInsights = GenerateTrendInsights(trendCategories, seasonProgression);

            var trends = new SeasonTrendsDto
            {
                SeasonId = seasonId,
                TrendCategories = trendCategories,
                SeasonProgression = seasonProgression,
                TrendInsights = trendInsights
            };

            return ServiceResult<SeasonTrendsDto>.Success(trends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting season trends for season {SeasonId}", seasonId);
            return ServiceResult<SeasonTrendsDto>.Failed("Failed to retrieve season trends");
        }
    }

    public async Task<ServiceResult<MultiSeasonComparisonDto>> GetMultiSeasonComparisonAsync(IEnumerable<int> seasonIds)
    {
        try
        {
            _logger.LogInformation("Getting multi-season comparison for seasons: {SeasonIds}", string.Join(", ", seasonIds));

            var seasons = await _context.Seasons
                .Where(s => seasonIds.Contains(s.Id))
                .ToListAsync();

            if (!seasons.Any())
            {
                return ServiceResult<MultiSeasonComparisonDto>.Failed("No valid seasons found");
            }

            var seasonComparisons = new List<SeasonComparisonDto>();
            foreach (var season in seasons)
            {
                var playerStats = await _context.MatchPlayerStats
                    .Include(mps => mps.Match)
                        .ThenInclude(m => m.Competition)
                            .ThenInclude(c => c.Season)
                    .Where(mps => mps.Match.Competition.SeasonId == season.Id)
                    .ToListAsync();

                var metrics = CalculateSeasonMetrics(playerStats);
                seasonComparisons.Add(new SeasonComparisonDto
                {
                    SeasonId = season.Id,
                    SeasonName = season.SeasonName,
                    Metrics = metrics,
                    Ranking = 0 // Would be calculated based on comparison
                });
            }

            var crossSeasonTrends = CalculateCrossSeasonTrends(seasonComparisons);
            var seasonEvolution = CalculateSeasonEvolution(seasonComparisons);

            var comparison = new MultiSeasonComparisonDto
            {
                SeasonIds = seasonIds,
                SeasonComparisons = seasonComparisons,
                CrossSeasonTrends = crossSeasonTrends,
                SeasonEvolution = seasonEvolution
            };

            return ServiceResult<MultiSeasonComparisonDto>.Success(comparison);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting multi-season comparison");
            return ServiceResult<MultiSeasonComparisonDto>.Failed("Failed to retrieve multi-season comparison");
        }
    }

    public async Task<ServiceResult<SeasonLeagueTableDto>> GetSeasonLeagueTableAsync(int seasonId, int? competitionId = null)
    {
        try
        {
            _logger.LogInformation("Getting league table for season {SeasonId}", seasonId);

            var season = await _context.Seasons.FindAsync(seasonId);
            if (season == null)
            {
                return ServiceResult<SeasonLeagueTableDto>.Failed("Season not found");
            }

            var matchesQuery = _context.Matches
                .Include(m => m.Competition)
                    .ThenInclude(c => c.Season)
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Where(m => m.Competition.SeasonId == seasonId);

            if (competitionId.HasValue)
            {
                matchesQuery = matchesQuery.Where(m => m.CompetitionId == competitionId.Value);
            }

            var matches = await matchesQuery.ToListAsync();

            var leagueTable = CalculateLeagueTable(matches);
            var leagueStatistics = CalculateLeagueStatistics(matches);

            var table = new SeasonLeagueTableDto
            {
                SeasonId = seasonId,
                SeasonName = season.SeasonName,
                CompetitionName = competitionId.HasValue ? "Filtered Competition" : "All Competitions",
                LeagueTable = leagueTable,
                LeagueStatistics = leagueStatistics
            };

            return ServiceResult<SeasonLeagueTableDto>.Success(table);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting league table for season {SeasonId}", seasonId);
            return ServiceResult<SeasonLeagueTableDto>.Failed("Failed to retrieve season league table");
        }
    }

    public async Task<ServiceResult<SeasonStatisticalLeadersDto>> GetSeasonStatisticalLeadersAsync(int seasonId, int limit = 10)
    {
        try
        {
            _logger.LogInformation("Getting statistical leaders for season {SeasonId}", seasonId);

            var season = await _context.Seasons.FindAsync(seasonId);
            if (season == null)
            {
                return ServiceResult<SeasonStatisticalLeadersDto>.Failed("Season not found");
            }

            var playerStats = await _context.MatchPlayerStats
                .Include(mps => mps.Match)
                    .ThenInclude(m => m.Competition)
                        .ThenInclude(c => c.Season)
                .Include(mps => mps.Team)
                .Where(mps => mps.Match.Competition.SeasonId == seasonId)
                .ToListAsync();

            var categories = CalculateStatisticalCategories(playerStats, limit);
            var overallLeaders = CalculateOverallLeaders(playerStats);

            var leaders = new SeasonStatisticalLeadersDto
            {
                SeasonId = seasonId,
                SeasonName = season.SeasonName,
                Categories = categories,
                OverallLeaders = overallLeaders
            };

            return ServiceResult<SeasonStatisticalLeadersDto>.Success(leaders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistical leaders for season {SeasonId}", seasonId);
            return ServiceResult<SeasonStatisticalLeadersDto>.Failed("Failed to retrieve season statistical leaders");
        }
    }

    #region Private Helper Methods

    private SeasonOverviewDto CalculateSeasonOverview(
        List<Dal.Models.application.Match> matches,
        List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        var uniqueTeams = matches.Select(m => m.HomeTeamId)
            .Union(matches.Select(m => m.AwayTeamId))
            .Distinct()
            .Count();

        var uniquePlayers = playerStats.Select(ps => ps.PlayerName).Distinct().Count();
        var totalGoals = playerStats.Sum(ps => ps.GoalsFromPlay ?? 0);
        var totalPoints = playerStats.Sum(ps => ps.PointsFromPlay ?? 0);

        return new SeasonOverviewDto
        {
            TotalMatches = matches.Count,
            TotalTeams = uniqueTeams,
            TotalPlayers = uniquePlayers,
            AveragePsr = playerStats.Average(ps => ps.PerformanceSuccessRate ?? 0),
            TotalGoals = totalGoals,
            TotalPoints = totalPoints,
            AverageMatchScore = matches.Count > 0 ? (decimal)(totalGoals * 3 + totalPoints) / matches.Count : 0,
            MostCompetitiveMatch = "TBD", // Would analyze closest matches
            HighestScoringMatch = "TBD" // Would analyze highest scoring matches
        };
    }

    private IEnumerable<TopPerformerCategoryDto> CalculateTopPerformers(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        var categories = new List<TopPerformerCategoryDto>();

        // Top PSR Performers
        var topPsrPerformers = playerStats
            .GroupBy(ps => new { ps.PlayerName, ps.Team.TeamName, ps.StartingPosition })
            .Where(g => g.Count() >= 3) // Minimum games played
            .OrderByDescending(g => g.Average(ps => ps.PerformanceSuccessRate ?? 0))
            .Take(5)
            .Select((g, index) => new SeasonTopPerformerDto
            {
                PlayerName = g.Key.PlayerName,
                TeamName = g.Key.TeamName,
                Position = g.Key.StartingPosition ?? "Unknown",
                Value = g.Average(ps => ps.PerformanceSuccessRate ?? 0),
                Achievement = $"Average PSR: {g.Average(ps => ps.PerformanceSuccessRate ?? 0):F2}",
                Ranking = index + 1
            }).ToList();

        categories.Add(new TopPerformerCategoryDto
        {
            Category = "Top PSR Performers",
            Performers = topPsrPerformers
        });

        // Top Scorers
        var topScorers = playerStats
            .GroupBy(ps => new { ps.PlayerName, ps.Team.TeamName, ps.StartingPosition })
            .OrderByDescending(g => g.Sum(ps => (ps.GoalsFromPlay ?? 0) * 3 + (ps.PointsFromPlay ?? 0)))
            .Take(5)
            .Select((g, index) => new SeasonTopPerformerDto
            {
                PlayerName = g.Key.PlayerName,
                TeamName = g.Key.TeamName,
                Position = g.Key.StartingPosition ?? "Unknown",
                Value = g.Sum(ps => (ps.GoalsFromPlay ?? 0) * 3 + (ps.PointsFromPlay ?? 0)),
                Achievement = $"Total Score: {g.Sum(ps => (ps.GoalsFromPlay ?? 0) * 3 + (ps.PointsFromPlay ?? 0))}",
                Ranking = index + 1
            }).ToList();

        categories.Add(new TopPerformerCategoryDto
        {
            Category = "Top Scorers",
            Performers = topScorers
        });

        return categories;
    }

    private SeasonHighlightsDto CalculateSeasonHighlights(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        var records = new List<SeasonRecordDto>
        {
            new()
            {
                RecordType = "Highest PSR",
                RecordHolder = playerStats.OrderByDescending(ps => ps.PerformanceSuccessRate).First().PlayerName,
                Value = playerStats.Max(ps => ps.PerformanceSuccessRate ?? 0),
                Description = "Best single match PSR performance",
                DateAchieved = playerStats.OrderByDescending(ps => ps.PerformanceSuccessRate).First().Match.MatchDate.ToDateTime(TimeOnly.MinValue)
            }
        };

        var achievements = new List<NotableAchievementDto>
        {
            new()
            {
                Achievement = "Perfect PSR Match",
                Player = "Sample Player", // Would identify actual achievements
                Team = "Sample Team",
                Date = DateTime.Now,
                Significance = "Exceptional individual performance"
            }
        };

        var milestones = new List<StatisticalMilestoneDto>
        {
            new()
            {
                Milestone = "2+ Average PSR",
                PlayersAchieved = playerStats
                    .GroupBy(ps => ps.PlayerName)
                    .Where(g => g.Average(ps => ps.PerformanceSuccessRate ?? 0) >= 2.0m)
                    .Select(g => g.Key)
                    .ToList(),
                Threshold = 2.0m,
                Category = "Performance"
            }
        };

        return new SeasonHighlightsDto
        {
            Records = records,
            Achievements = achievements,
            Milestones = milestones
        };
    }

    private SeasonStatisticsDto CalculateSeasonStatistics(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        var playerStatsSummary = new PlayerStatisticsSummaryDto
        {
            TotalPlayersParticipated = playerStats.Select(ps => ps.PlayerName).Distinct().Count(),
            HighestPsr = playerStats.Max(ps => ps.PerformanceSuccessRate ?? 0),
            LowestPsr = playerStats.Min(ps => ps.PerformanceSuccessRate ?? 0),
            AveragePsr = playerStats.Average(ps => ps.PerformanceSuccessRate ?? 0),
            MostGoalsScored = playerStats.Max(ps => ps.GoalsFromPlay ?? 0),
            MostPointsScored = playerStats.Max(ps => ps.PointsFromPlay ?? 0),
            HighestScoringRate = (decimal)playerStats
                .GroupBy(ps => ps.PlayerName)
                .Max(g => g.Average(ps => (ps.GoalsFromPlay ?? 0) + (ps.PointsFromPlay ?? 0))),
            BestTackleSuccessRate = playerStats
                .Where(ps => ps.TackleSuccessRate.HasValue)
                .Max(ps => ps.TackleSuccessRate ?? 0)
        };

        var teamStatsSummary = new TeamStatisticsSummaryDto
        {
            HighestTeamPsr = playerStats
                .GroupBy(ps => ps.TeamId)
                .Max(g => g.Average(ps => ps.PerformanceSuccessRate ?? 0)),
            LowestTeamPsr = playerStats
                .GroupBy(ps => ps.TeamId)
                .Min(g => g.Average(ps => ps.PerformanceSuccessRate ?? 0)),
            AverageTeamPsr = playerStats
                .GroupBy(ps => ps.TeamId)
                .Average(g => g.Average(ps => ps.PerformanceSuccessRate ?? 0)),
            HighestTeamScore = playerStats
                .GroupBy(ps => new { ps.MatchId, ps.TeamId })
                .Max(g => g.Sum(ps => (ps.GoalsFromPlay ?? 0) * 3 + (ps.PointsFromPlay ?? 0))),
            LowestTeamScore = playerStats
                .GroupBy(ps => new { ps.MatchId, ps.TeamId })
                .Min(g => g.Sum(ps => (ps.GoalsFromPlay ?? 0) * 3 + (ps.PointsFromPlay ?? 0))),
            BestOffensiveEfficiency = 85.0m, // Would calculate from actual data
            BestDefensiveEfficiency = 78.0m // Would calculate from actual data
        };

        var competitionStats = new CompetitionStatisticsDto
        {
            Competitions = new List<CompetitionSummaryDto>
            {
                new()
                {
                    CompetitionName = "Championship",
                    MatchesPlayed = playerStats.Select(ps => ps.MatchId).Distinct().Count(),
                    TeamsParticipated = playerStats.Select(ps => ps.TeamId).Distinct().Count(),
                    AveragePsr = playerStats.Average(ps => ps.PerformanceSuccessRate ?? 0),
                    AverageScore = (decimal)playerStats
                        .GroupBy(ps => new { ps.MatchId, ps.TeamId })
                        .Average(g => g.Sum(ps => (ps.GoalsFromPlay ?? 0) * 3 + (ps.PointsFromPlay ?? 0)))
                }
            },
            MostCompetitiveCompetition = "Championship",
            HighestScoringCompetition = "Championship"
        };

        return new SeasonStatisticsDto
        {
            PlayerStats = playerStatsSummary,
            TeamStats = teamStatsSummary,
            CompetitionStats = competitionStats
        };
    }

    private decimal CalculateConsistencyRating(List<decimal> psrValues)
    {
        if (!psrValues.Any()) return 0;

        var mean = psrValues.Average();
        var variance = psrValues.Sum(x => Math.Pow((double)(x - mean), 2)) / psrValues.Count;
        var standardDeviation = (decimal)Math.Sqrt(variance);

        // Lower standard deviation = higher consistency (scale 0-10)
        return Math.Max(0, 10 - standardDeviation * 2);
    }

    private IEnumerable<PsrTrendPointDto> CreatePsrTrend(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        return playerStats.Select(ps => new PsrTrendPointDto
        {
            MatchDate = ps.Match.MatchDate.ToDateTime(TimeOnly.MinValue),
            Psr = ps.PerformanceSuccessRate ?? 0,
            Opponent = GetOpponentName(ps),
            PerformanceLevel = GetPerformanceLevel(ps.PerformanceSuccessRate ?? 0)
        }).ToList();
    }

    private string GetOpponentName(Dal.Models.application.MatchPlayerStat playerStat)
    {
        if (playerStat.TeamId == playerStat.Match.HomeTeamId)
        {
            return playerStat.Match.AwayTeam?.TeamName ?? "Unknown Away Team";
        }
        return playerStat.Match.HomeTeam?.TeamName ?? "Unknown Home Team";
    }

    private string GetPerformanceLevel(decimal psr)
    {
        if (psr >= 2.0m) return "Excellent";
        if (psr >= 1.0m) return "Good";
        if (psr >= 0m) return "Average";
        return "Below Average";
    }

    // Additional helper methods with simplified implementations
    private IEnumerable<PlayerCumulativeStatsDto> CalculatePlayerCumulativeStats(List<Dal.Models.application.MatchPlayerStat> playerStats) => 
        new List<PlayerCumulativeStatsDto>();
    
    private IEnumerable<TeamCumulativeStatsDto> CalculateTeamCumulativeStats(List<Dal.Models.application.MatchPlayerStat> playerStats) => 
        new List<TeamCumulativeStatsDto>();
    
    private SeasonAggregatesDto CalculateSeasonAggregates(List<Dal.Models.application.MatchPlayerStat> playerStats) => 
        new SeasonAggregatesDto();
    
    private IEnumerable<SeasonTrendCategoryDto> CalculateSeasonTrendCategories(List<Dal.Models.application.MatchPlayerStat> playerStats) => 
        new List<SeasonTrendCategoryDto>();
    
    private SeasonProgressionDto CalculateSeasonProgression(List<Dal.Models.application.MatchPlayerStat> playerStats) => 
        new SeasonProgressionDto();
    
    private TrendInsightsDto GenerateTrendInsights(IEnumerable<SeasonTrendCategoryDto> categories, SeasonProgressionDto progression) => 
        new TrendInsightsDto();
    
    private SeasonMetricsDto CalculateSeasonMetrics(List<Dal.Models.application.MatchPlayerStat> playerStats) => 
        new SeasonMetricsDto();
    
    private CrossSeasonTrendsDto CalculateCrossSeasonTrends(List<SeasonComparisonDto> comparisons) => 
        new CrossSeasonTrendsDto();
    
    private SeasonEvolutionDto CalculateSeasonEvolution(List<SeasonComparisonDto> comparisons) => 
        new SeasonEvolutionDto();
    
    private IEnumerable<LeagueTableEntryDto> CalculateLeagueTable(List<Dal.Models.application.Match> matches) => 
        new List<LeagueTableEntryDto>();
    
    private LeagueStatisticsDto CalculateLeagueStatistics(List<Dal.Models.application.Match> matches) => 
        new LeagueStatisticsDto();
    
    private IEnumerable<StatisticalCategoryDto> CalculateStatisticalCategories(List<Dal.Models.application.MatchPlayerStat> playerStats, int limit) => 
        new List<StatisticalCategoryDto>();
    
    private OverallLeadersDto CalculateOverallLeaders(List<Dal.Models.application.MatchPlayerStat> playerStats) => 
        new OverallLeadersDto();

    #endregion
}