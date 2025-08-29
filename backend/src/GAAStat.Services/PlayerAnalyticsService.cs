using GAAStat.Dal.Interfaces;
using GAAStat.Services.Interfaces;
using GAAStat.Services.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GAAStat.Services;

/// <summary>
/// Service for individual player performance analysis with efficiency calculations
/// </summary>
public class PlayerAnalyticsService : IPlayerAnalyticsService
{
    private readonly IGAAStatDbContext _context;
    private readonly IStatisticsCalculationService _statisticsService;
    private readonly ILogger<PlayerAnalyticsService> _logger;

    public PlayerAnalyticsService(
        IGAAStatDbContext context,
        IStatisticsCalculationService statisticsService,
        ILogger<PlayerAnalyticsService> logger)
    {
        _context = context;
        _statisticsService = statisticsService;
        _logger = logger;
    }

    public async Task<ServiceResult<PlayerPerformanceDto>> GetPlayerSeasonPerformanceAsync(string playerName, int seasonId)
    {
        try
        {
            _logger.LogInformation("Getting season performance for player {PlayerName} in season {SeasonId}", playerName, seasonId);

            var season = await _context.Seasons.FirstOrDefaultAsync(s => s.Id == seasonId);
            if (season == null)
            {
                return ServiceResult<PlayerPerformanceDto>.Failed("Season not found");
            }

            var playerStats = await _context.MatchPlayerStats
                .Include(mps => mps.Match)
                    .ThenInclude(m => m.Competition)
                        .ThenInclude(c => c.Season)
                .Include(mps => mps.Team)
                .Where(mps => mps.PlayerName == playerName && mps.Match.Competition.SeasonId == seasonId)
                .OrderBy(mps => mps.Match.MatchDate)
                .ToListAsync();

            if (!playerStats.Any())
            {
                return ServiceResult<PlayerPerformanceDto>.Failed($"No statistics found for player {playerName} in season {season.SeasonName}");
            }

            var firstStat = playerStats.First();
            var seasonStats = CalculateSeasonStats(playerStats);
            var trends = await CalculatePlayerTrends(playerStats);
            var comparisons = await CalculatePlayerComparisons(playerName, seasonId, playerStats);

            var performance = new PlayerPerformanceDto
            {
                PlayerName = playerName,
                SeasonId = seasonId,
                SeasonName = season.SeasonName,
                TeamName = firstStat.Team.TeamName,
                Position = firstStat.StartingPosition ?? "Unknown",
                MatchesPlayed = playerStats.Count(),
                TotalMinutesPlayed = playerStats.Sum(ps => ps.MinutesPlayed ?? 0),
                SeasonStats = seasonStats,
                Trends = trends,
                Comparisons = comparisons
            };

            return ServiceResult<PlayerPerformanceDto>.Success(performance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting season performance for player {PlayerName} in season {SeasonId}", playerName, seasonId);
            return ServiceResult<PlayerPerformanceDto>.Failed("Failed to retrieve player season performance");
        }
    }

    public async Task<ServiceResult<PlayerEfficiencyDto>> GetPlayerEfficiencyRatingAsync(string playerName, int? seasonId = null)
    {
        try
        {
            _logger.LogInformation("Getting efficiency rating for player {PlayerName}", playerName);

            var query = _context.MatchPlayerStats
                .Include(mps => mps.Match)
                    .ThenInclude(m => m.Competition)
                        .ThenInclude(c => c.Season)
                .Include(mps => mps.Team)
                .Where(mps => mps.PlayerName == playerName);

            if (seasonId.HasValue)
            {
                query = query.Where(mps => mps.Match.Competition.SeasonId == seasonId.Value);
            }

            var playerStats = await query
                .OrderBy(mps => mps.Match.MatchDate)
                .ToListAsync();

            if (!playerStats.Any())
            {
                return ServiceResult<PlayerEfficiencyDto>.Failed($"No statistics found for player {playerName}");
            }

            var firstStat = playerStats.First();
            var efficiencyBreakdown = CalculateEfficiencyBreakdown(playerStats);
            var efficiencyTrends = CalculateEfficiencyTrends(playerStats);
            
            var overallRating = CalculateOverallEfficiencyRating(playerStats);
            var attackingRating = CalculateAttackingRating(playerStats);
            var defensiveRating = CalculateDefensiveRating(playerStats);
            var passingRating = CalculatePassingRating(playerStats);

            var efficiency = new PlayerEfficiencyDto
            {
                PlayerName = playerName,
                TeamName = firstStat.Team.TeamName,
                Position = firstStat.StartingPosition ?? "Unknown",
                OverallEfficiencyRating = overallRating,
                AttackingRating = attackingRating,
                DefensiveRating = defensiveRating,
                PassingRating = passingRating,
                EfficiencyBreakdown = efficiencyBreakdown,
                EfficiencyTrends = efficiencyTrends
            };

            return ServiceResult<PlayerEfficiencyDto>.Success(efficiency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting efficiency rating for player {PlayerName}", playerName);
            return ServiceResult<PlayerEfficiencyDto>.Failed("Failed to retrieve player efficiency rating");
        }
    }

    public async Task<ServiceResult<PlayerTeamComparisonDto>> GetPlayerTeamComparisonAsync(string playerName, int teamId, int seasonId)
    {
        try
        {
            _logger.LogInformation("Getting team comparison for player {PlayerName}", playerName);

            var playerStats = await _context.MatchPlayerStats
                .Include(mps => mps.Match)
                    .ThenInclude(m => m.Competition)
                        .ThenInclude(c => c.Season)
                .Include(mps => mps.Team)
                .Where(mps => mps.PlayerName == playerName && mps.TeamId == teamId && mps.Match.Competition.SeasonId == seasonId)
                .ToListAsync();

            var teamStats = await _context.MatchPlayerStats
                .Include(mps => mps.Match)
                    .ThenInclude(m => m.Competition)
                        .ThenInclude(c => c.Season)
                .Where(mps => mps.TeamId == teamId && mps.Match.Competition.SeasonId == seasonId && mps.PlayerName != playerName)
                .ToListAsync();

            if (!playerStats.Any())
            {
                return ServiceResult<PlayerTeamComparisonDto>.Failed($"No statistics found for player {playerName}");
            }

            if (!teamStats.Any())
            {
                return ServiceResult<PlayerTeamComparisonDto>.Failed("No team statistics found for comparison");
            }

            var playerTeamStats = CalculatePlayerTeamStats(playerStats);
            var teamAverages = CalculateTeamAverageStats(teamStats);
            var comparisons = GenerateStatisticalComparisons(playerTeamStats, teamAverages);
            var assessment = GenerateOverallAssessment(comparisons);

            var comparison = new PlayerTeamComparisonDto
            {
                PlayerName = playerName,
                TeamName = playerStats.First().Team.TeamName,
                SeasonId = seasonId,
                PlayerStats = playerTeamStats,
                TeamAverages = teamAverages,
                StatisticalComparisons = comparisons,
                OverallAssessment = assessment
            };

            return ServiceResult<PlayerTeamComparisonDto>.Success(comparison);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team comparison for player {PlayerName}", playerName);
            return ServiceResult<PlayerTeamComparisonDto>.Failed("Failed to retrieve player team comparison");
        }
    }

    public async Task<ServiceResult<PlayerTrendsDto>> GetPlayerTrendsAsync(string playerName, int seasonId)
    {
        try
        {
            _logger.LogInformation("Getting trends for player {PlayerName} in season {SeasonId}", playerName, seasonId);

            var playerStats = await _context.MatchPlayerStats
                .Include(mps => mps.Match)
                    .ThenInclude(m => m.Competition)
                        .ThenInclude(c => c.Season)
                .Where(mps => mps.PlayerName == playerName && mps.Match.Competition.SeasonId == seasonId)
                .OrderBy(mps => mps.Match.MatchDate)
                .ToListAsync();

            if (!playerStats.Any())
            {
                return ServiceResult<PlayerTrendsDto>.Failed($"No statistics found for player {playerName}");
            }

            var trendAnalyses = GenerateTrendAnalyses(playerStats);
            var consistencyAnalysis = CalculateConsistencyAnalysis(playerStats);
            var seasonalPatterns = IdentifySeasonalPatterns(playerStats);

            var trends = new PlayerTrendsDto
            {
                PlayerName = playerName,
                SeasonId = seasonId,
                TrendAnalyses = trendAnalyses,
                ConsistencyAnalysis = consistencyAnalysis,
                SeasonalPatterns = seasonalPatterns
            };

            return ServiceResult<PlayerTrendsDto>.Success(trends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trends for player {PlayerName}", playerName);
            return ServiceResult<PlayerTrendsDto>.Failed("Failed to retrieve player trends");
        }
    }

    public async Task<ServiceResult<PlayerOppositionAnalysisDto>> GetPlayerOppositionAnalysisAsync(string playerName, int seasonId)
    {
        try
        {
            _logger.LogInformation("Getting opposition analysis for player {PlayerName}", playerName);

            var playerStats = await _context.MatchPlayerStats
                .Include(mps => mps.Match)
                    .ThenInclude(m => m.HomeTeam)
                .Include(mps => mps.Match)
                    .ThenInclude(m => m.AwayTeam)
                .Include(mps => mps.Team)
                .Where(mps => mps.PlayerName == playerName && mps.Match.Competition.SeasonId == seasonId)
                .ToListAsync();

            if (!playerStats.Any())
            {
                return ServiceResult<PlayerOppositionAnalysisDto>.Failed($"No statistics found for player {playerName}");
            }

            var oppositionPerformances = CalculateOppositionPerformances(playerStats);
            var oppositionSummary = GenerateOppositionSummary(oppositionPerformances);

            var analysis = new PlayerOppositionAnalysisDto
            {
                PlayerName = playerName,
                SeasonId = seasonId,
                OppositionPerformances = oppositionPerformances,
                OppositionSummary = oppositionSummary
            };

            return ServiceResult<PlayerOppositionAnalysisDto>.Success(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting opposition analysis for player {PlayerName}", playerName);
            return ServiceResult<PlayerOppositionAnalysisDto>.Failed("Failed to retrieve player opposition analysis");
        }
    }

    public async Task<ServiceResult<PlayerVenueAnalysisDto>> GetPlayerVenueAnalysisAsync(string playerName, int seasonId)
    {
        try
        {
            _logger.LogInformation("Getting venue analysis for player {PlayerName}", playerName);

            var playerStats = await _context.MatchPlayerStats
                .Include(mps => mps.Match)
                    .ThenInclude(m => m.Competition)
                        .ThenInclude(c => c.Season)
                .Include(mps => mps.Team)
                .Where(mps => mps.PlayerName == playerName && mps.Match.Competition.SeasonId == seasonId)
                .ToListAsync();

            if (!playerStats.Any())
            {
                return ServiceResult<PlayerVenueAnalysisDto>.Failed($"No statistics found for player {playerName}");
            }

            var homeStats = playerStats.Where(ps => ps.TeamId == ps.Match.HomeTeamId).ToList();
            var awayStats = playerStats.Where(ps => ps.TeamId == ps.Match.AwayTeamId).ToList();

            var homePerformance = CalculateVenuePerformance(homeStats, "Home");
            var awayPerformance = CalculateVenuePerformance(awayStats, "Away");
            var venueComparison = CompareVenuePerformances(homePerformance, awayPerformance);

            var analysis = new PlayerVenueAnalysisDto
            {
                PlayerName = playerName,
                SeasonId = seasonId,
                HomePerformance = homePerformance,
                AwayPerformance = awayPerformance,
                VenueComparison = venueComparison
            };

            return ServiceResult<PlayerVenueAnalysisDto>.Success(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting venue analysis for player {PlayerName}", playerName);
            return ServiceResult<PlayerVenueAnalysisDto>.Failed("Failed to retrieve player venue analysis");
        }
    }

    public async Task<ServiceResult<PlayerCumulativeStatsDto>> GetPlayerCumulativeStatsAsync(string playerName, int seasonId)
    {
        try
        {
            _logger.LogInformation("Getting cumulative stats for player {PlayerName} in season {SeasonId}", playerName, seasonId);

            var season = await _context.Seasons.FirstOrDefaultAsync(s => s.Id == seasonId);
            if (season == null)
            {
                return ServiceResult<PlayerCumulativeStatsDto>.Failed("Season not found");
            }

            var playerStats = await _context.MatchPlayerStats
                .Include(mps => mps.Match)
                    .ThenInclude(m => m.Competition)
                        .ThenInclude(c => c.Season)
                .Where(mps => mps.PlayerName == playerName && mps.Match.Competition.SeasonId == seasonId)
                .OrderBy(mps => mps.Match.MatchDate)
                .ToListAsync();

            if (!playerStats.Any())
            {
                return ServiceResult<PlayerCumulativeStatsDto>.Failed($"No statistics found for player {playerName}");
            }

            var cumulativeStats = CalculateCumulativeStats(playerStats);
            var matchContributions = CreateMatchContributions(playerStats);
            var rankings = await CalculatePlayerRankings(playerName, seasonId);

            var stats = new PlayerCumulativeStatsDto
            {
                PlayerName = playerName,
                SeasonId = seasonId,
                SeasonName = season.SeasonName,
                CumulativeStats = cumulativeStats,
                MatchContributions = matchContributions,
                Rankings = rankings
            };

            return ServiceResult<PlayerCumulativeStatsDto>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cumulative stats for player {PlayerName}", playerName);
            return ServiceResult<PlayerCumulativeStatsDto>.Failed("Failed to retrieve player cumulative statistics");
        }
    }

    #region Private Helper Methods

    private PlayerSeasonStatsDto CalculateSeasonStats(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        var totalMatches = playerStats.Count;
        var totalGoals = playerStats.Sum(ps => ps.GoalsFromPlay ?? 0);
        var totalPoints = playerStats.Sum(ps => ps.PointsFromPlay ?? 0);
        var totalTackles = playerStats.Sum(ps => ps.TacklesMade ?? 0);
        var successfulTackles = playerStats.Where(ps => ps.TackleSuccessRate.HasValue)
            .Sum(ps => (ps.TacklesMade ?? 0) * (ps.TackleSuccessRate ?? 0) / 100);

        return new PlayerSeasonStatsDto
        {
            AveragePsr = playerStats.Average(ps => ps.PerformanceSuccessRate ?? 0),
            TotalPsr = playerStats.Sum(ps => ps.PerformanceSuccessRate ?? 0),
            TotalPossessions = playerStats.Sum(ps => ps.TotalPossessions ?? 0),
            TotalEvents = playerStats.Sum(ps => ps.TotalEvents ?? 0),
            TotalGoals = totalGoals,
            TotalPoints = totalPoints,
            TotalScores = totalGoals + totalPoints,
            ScoringRate = totalMatches > 0 ? (decimal)(totalGoals + totalPoints) / totalMatches : 0,
            TotalTackles = totalTackles,
            TackleSuccessRate = totalTackles > 0 ? successfulTackles / totalTackles * 100 : 0,
            TotalInterceptions = playerStats.Sum(ps => ps.Interceptions ?? 0),
            TotalTurnoversWon = playerStats.Sum(ps => ps.TurnoversWon ?? 0),
            TotalPossessionsLost = playerStats.Sum(ps => ps.PossessionsLost ?? 0),
            PossessionRetentionRate = CalculatePossessionRetentionRate(playerStats),
            OverallEfficiencyRating = CalculateOverallEfficiencyRating(playerStats)
        };
    }

    private async Task<PlayerTrendDataDto> CalculatePlayerTrends(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        var psrTrend = CreatePerformanceDataPoints(playerStats, ps => ps.PerformanceSuccessRate ?? 0);
        var scoringTrend = CreatePerformanceDataPoints(playerStats, ps => (ps.GoalsFromPlay ?? 0) + (ps.PointsFromPlay ?? 0));
        var efficiencyTrend = CreatePerformanceDataPoints(playerStats, ps => ps.OverallPerformanceRating ?? 0);

        return new PlayerTrendDataDto
        {
            PsrTrend = psrTrend,
            ScoringTrend = scoringTrend,
            EfficiencyTrend = efficiencyTrend,
            OverallTrendDirection = DetermineTrendDirection(psrTrend),
            TrendSlope = CalculateTrendSlope(psrTrend)
        };
    }

    private async Task<PlayerComparisonDataDto> CalculatePlayerComparisons(string playerName, int seasonId, List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        var playerPsr = playerStats.Average(ps => ps.PerformanceSuccessRate ?? 0);
        
        // Get team averages
        var teamId = playerStats.First().TeamId;
        var teamStats = await _context.MatchPlayerStats
            .Include(mps => mps.Match)
                .ThenInclude(m => m.Competition)
                    .ThenInclude(c => c.Season)
            .Where(mps => mps.TeamId == teamId && mps.Match.Competition.SeasonId == seasonId && mps.PlayerName != playerName)
            .ToListAsync();

        var teamAveragePsr = teamStats.Any() ? teamStats.Average(ps => ps.PerformanceSuccessRate ?? 0) : 0;

        // Get position averages
        var position = playerStats.First().StartingPosition ?? "Unknown";
        var positionStats = await _context.MatchPlayerStats
            .Include(mps => mps.Match)
                .ThenInclude(m => m.Competition)
                    .ThenInclude(c => c.Season)
            .Where(mps => mps.StartingPosition == position && mps.Match.Competition.SeasonId == seasonId && mps.PlayerName != playerName)
            .ToListAsync();

        var positionAveragePsr = positionStats.Any() ? positionStats.Average(ps => ps.PerformanceSuccessRate ?? 0) : 0;

        // Get league averages
        var leagueStats = await _context.MatchPlayerStats
            .Include(mps => mps.Match)
                .ThenInclude(m => m.Competition)
                    .ThenInclude(c => c.Season)
            .Where(mps => mps.Match.Competition.SeasonId == seasonId && mps.PlayerName != playerName)
            .ToListAsync();

        var leagueAveragePsr = leagueStats.Any() ? leagueStats.Average(ps => ps.PerformanceSuccessRate ?? 0) : 0;

        var comparisonMetrics = GenerateComparisonMetrics(playerPsr, teamAveragePsr, positionAveragePsr, leagueAveragePsr);

        return new PlayerComparisonDataDto
        {
            TeamAveragePsr = teamAveragePsr,
            PositionAveragePsr = positionAveragePsr,
            LeagueAveragePsr = leagueAveragePsr,
            PerformanceLevel = DeterminePerformanceLevel(playerPsr, leagueAveragePsr),
            ComparisonMetrics = comparisonMetrics
        };
    }

    private PlayerEfficiencyBreakdownDto CalculateEfficiencyBreakdown(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        var scoringEfficiency = CalculateScoringEfficiency(playerStats);
        var passingEfficiency = CalculatePassingEfficiency(playerStats);
        var tacklingEfficiency = CalculateTacklingEfficiency(playerStats);
        var possessionEfficiency = CalculatePossessionEfficiency(playerStats);

        var strengths = IdentifyStrengths(playerStats);
        var improvements = IdentifyImprovementAreas(playerStats);

        return new PlayerEfficiencyBreakdownDto
        {
            ScoringEfficiency = scoringEfficiency,
            PassingEfficiency = passingEfficiency,
            TacklingEfficiency = tacklingEfficiency,
            PossessionEfficiency = possessionEfficiency,
            GameImpactRating = CalculateGameImpactRating(playerStats),
            ConsistencyRating = CalculateConsistencyRating(playerStats),
            Strengths = strengths,
            AreasForImprovement = improvements
        };
    }

    private IEnumerable<EfficiencyTrendPointDto> CalculateEfficiencyTrends(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        return playerStats.Select(ps => new EfficiencyTrendPointDto
        {
            Date = ps.Match.MatchDate.ToDateTime(TimeOnly.MinValue),
            EfficiencyRating = ps.OverallPerformanceRating ?? 0,
            PerformanceContext = GetPerformanceContext(ps)
        }).ToList();
    }

    private PlayerTeamStatsDto CalculatePlayerTeamStats(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        return new PlayerTeamStatsDto
        {
            AveragePsr = playerStats.Average(ps => ps.PerformanceSuccessRate ?? 0),
            ScoringRate = CalculatePlayerScoringRate(playerStats),
            TackleSuccessRate = CalculatePlayerTackleSuccessRate(playerStats),
            PassingAccuracy = CalculatePlayerPassingAccuracy(playerStats),
            PossessionRetention = CalculatePossessionRetentionRate(playerStats),
            GameImpact = CalculateGameImpactRating(playerStats)
        };
    }

    private PlayerTeamStatsDto CalculateTeamAverageStats(List<Dal.Models.application.MatchPlayerStat> teamStats)
    {
        return new PlayerTeamStatsDto
        {
            AveragePsr = teamStats.Average(ps => ps.PerformanceSuccessRate ?? 0),
            ScoringRate = CalculateAverageScoringRate(teamStats),
            TackleSuccessRate = CalculateAverageTackleSuccessRate(teamStats),
            PassingAccuracy = CalculateAveragePassingAccuracy(teamStats),
            PossessionRetention = CalculateAveragePossessionRetentionRate(teamStats),
            GameImpact = CalculateAverageGameImpactRating(teamStats)
        };
    }

    private IEnumerable<TrendAnalysisDto> GenerateTrendAnalyses(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        var trends = new List<TrendAnalysisDto>();

        // PSR Trend
        var psrPoints = CreatePerformanceDataPoints(playerStats, ps => ps.PerformanceSuccessRate ?? 0);
        trends.Add(new TrendAnalysisDto
        {
            MetricName = "Performance Success Rate",
            DataPoints = psrPoints,
            TrendDirection = DetermineTrendDirection(psrPoints),
            TrendStrength = CalculateTrendStrength(psrPoints),
            TrendDescription = GenerateTrendDescription("PSR", psrPoints)
        });

        // Scoring Trend
        var scoringPoints = CreatePerformanceDataPoints(playerStats, ps => (ps.GoalsFromPlay ?? 0) + (ps.PointsFromPlay ?? 0));
        trends.Add(new TrendAnalysisDto
        {
            MetricName = "Scoring Output",
            DataPoints = scoringPoints,
            TrendDirection = DetermineTrendDirection(scoringPoints),
            TrendStrength = CalculateTrendStrength(scoringPoints),
            TrendDescription = GenerateTrendDescription("Scoring", scoringPoints)
        });

        return trends;
    }

    private PerformanceConsistencyDto CalculateConsistencyAnalysis(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        var psrValues = playerStats.Select(ps => ps.PerformanceSuccessRate ?? 0).ToList();
        var mean = psrValues.Average();
        var variance = psrValues.Sum(x => Math.Pow((double)(x - mean), 2)) / psrValues.Count;
        var standardDeviation = (decimal)Math.Sqrt(variance);

        var consistentPerformances = psrValues.Count(psr => Math.Abs(psr - mean) <= standardDeviation);
        var outlierPerformances = psrValues.Count - consistentPerformances;

        return new PerformanceConsistencyDto
        {
            ConsistencyRating = CalculateConsistencyRating(playerStats),
            StandardDeviation = standardDeviation,
            PerformanceVariance = (decimal)variance,
            ConsistentPerformances = consistentPerformances,
            OutlierPerformances = outlierPerformances,
            ConsistencyAssessment = GenerateConsistencyAssessment(standardDeviation)
        };
    }

    private SeasonalPatternDto IdentifySeasonalPatterns(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        // Simple pattern identification - could be enhanced with more sophisticated analysis
        var firstHalf = playerStats.Take(playerStats.Count / 2).Average(ps => ps.PerformanceSuccessRate ?? 0);
        var secondHalf = playerStats.Skip(playerStats.Count / 2).Average(ps => ps.PerformanceSuccessRate ?? 0);

        var bestPeriod = firstHalf > secondHalf ? "Early Season" : "Late Season";
        var weakestPeriod = firstHalf < secondHalf ? "Early Season" : "Late Season";

        var patterns = new List<PatternObservationDto>
        {
            new()
            {
                PatternType = "Performance Progression",
                Description = secondHalf > firstHalf ? "Improving throughout season" : "Strong start, declined later",
                Confidence = 0.8m
            }
        };

        return new SeasonalPatternDto
        {
            BestPerformancePeriod = bestPeriod,
            WeakestPerformancePeriod = weakestPeriod,
            Patterns = patterns
        };
    }

    private IEnumerable<OppositionPerformanceDto> CalculateOppositionPerformances(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        return playerStats.GroupBy(ps => GetOpponentName(ps))
            .Select(g => new OppositionPerformanceDto
            {
                OpponentName = g.Key,
                MatchesPlayed = g.Count(),
                AveragePsr = g.Average(ps => ps.PerformanceSuccessRate ?? 0),
                ScoringRate = (decimal)g.Average(ps => (ps.GoalsFromPlay ?? 0) + (ps.PointsFromPlay ?? 0)),
                PerformanceRating = g.Average(ps => ps.OverallPerformanceRating ?? 0),
                BestPerformance = g.Max(ps => ps.PerformanceSuccessRate ?? 0).ToString("F2"),
                BestPerformanceDate = g.OrderByDescending(ps => ps.PerformanceSuccessRate).First().Match.MatchDate.ToDateTime(TimeOnly.MinValue)
            }).OrderByDescending(op => op.AveragePsr).ToList();
    }

    private OppositionSummaryDto GenerateOppositionSummary(IEnumerable<OppositionPerformanceDto> oppositionPerformances)
    {
        var performances = oppositionPerformances.ToList();
        
        return new OppositionSummaryDto
        {
            StrongestAgainst = performances.FirstOrDefault()?.OpponentName ?? "None",
            WeakestAgainst = performances.LastOrDefault()?.OpponentName ?? "None",
            AveragePerformanceRating = performances.Average(op => op.PerformanceRating),
            PerformancePattern = "Varies by opposition strength"
        };
    }

    private VenuePerformanceDto CalculateVenuePerformance(List<Dal.Models.application.MatchPlayerStat> stats, string venueType)
    {
        if (!stats.Any())
        {
            return new VenuePerformanceDto { VenueType = venueType };
        }

        return new VenuePerformanceDto
        {
            VenueType = venueType,
            MatchesPlayed = stats.Count,
            AveragePsr = stats.Average(ps => ps.PerformanceSuccessRate ?? 0),
            ScoringRate = (decimal)stats.Average(ps => (ps.GoalsFromPlay ?? 0) + (ps.PointsFromPlay ?? 0)),
            PerformanceRating = stats.Average(ps => ps.OverallPerformanceRating ?? 0),
            DetailedStats = CalculateSeasonStats(stats)
        };
    }

    private VenueComparisonDto CompareVenuePerformances(VenuePerformanceDto home, VenuePerformanceDto away)
    {
        var performanceDiff = home.AveragePsr - away.AveragePsr;
        var betterVenue = performanceDiff > 0 ? "Home" : "Away";
        
        return new VenueComparisonDto
        {
            PerformanceDifference = Math.Abs(performanceDiff),
            BetterVenue = betterVenue,
            DifferenceSignificance = Math.Abs(performanceDiff) > 0.5m ? 0.8m : 0.3m,
            Analysis = GenerateVenueAnalysis(performanceDiff)
        };
    }

    private CumulativeStatsDto CalculateCumulativeStats(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        return new CumulativeStatsDto
        {
            TotalMatches = playerStats.Count,
            TotalMinutes = playerStats.Sum(ps => ps.MinutesPlayed ?? 0),
            TotalPsr = playerStats.Sum(ps => ps.PerformanceSuccessRate ?? 0),
            AveragePsr = playerStats.Average(ps => ps.PerformanceSuccessRate ?? 0),
            TotalPossessions = playerStats.Sum(ps => ps.TotalPossessions ?? 0),
            TotalEvents = playerStats.Sum(ps => ps.TotalEvents ?? 0),
            TotalGoals = playerStats.Sum(ps => ps.GoalsFromPlay ?? 0),
            TotalPoints = playerStats.Sum(ps => ps.PointsFromPlay ?? 0),
            TotalTackles = playerStats.Sum(ps => ps.TacklesMade ?? 0),
            SuccessfulTackles = CalculateSuccessfulTackles(playerStats),
            TackleSuccessRate = CalculatePlayerTackleSuccessRate(playerStats),
            TotalInterceptions = playerStats.Sum(ps => ps.Interceptions ?? 0),
            TotalTurnovers = playerStats.Sum(ps => ps.PossessionsLost ?? 0),
            TotalAssists = playerStats.Sum(ps => (ps.ScoreAssistsGoals ?? 0) + (ps.ScoreAssistsPoints ?? 0)),
            TotalCards = playerStats.Sum(ps => (ps.CardsYellow ?? 0) + (ps.CardsBlack ?? 0) + (ps.CardsRed ?? 0))
        };
    }

    private IEnumerable<MatchContributionDto> CreateMatchContributions(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        return playerStats.Select(ps => new MatchContributionDto
        {
            MatchId = ps.MatchId,
            MatchDate = ps.Match.MatchDate.ToDateTime(TimeOnly.MinValue),
            Opponent = GetOpponentName(ps),
            Venue = ps.TeamId == ps.Match.HomeTeamId ? "Home" : "Away",
            Psr = ps.PerformanceSuccessRate ?? 0,
            Goals = ps.GoalsFromPlay ?? 0,
            Points = ps.PointsFromPlay ?? 0,
            PerformanceRating = ps.OverallPerformanceRating ?? 0,
            PerformanceLevel = GetPerformanceLevel(ps.PerformanceSuccessRate ?? 0)
        }).OrderByDescending(mc => mc.MatchDate).ToList();
    }

    private async Task<RankingsDto> CalculatePlayerRankings(string playerName, int seasonId)
    {
        // This would involve more complex ranking calculations
        // For now, return sample rankings
        return new RankingsDto
        {
            PsrRanking = 15, // Would calculate actual ranking
            ScoringRanking = 23,
            TacklingRanking = 8,
            OverallRanking = 12,
            PositionRanking = "3rd in position",
            TeamRanking = "2nd in team"
        };
    }

    #region Additional Helper Methods

    private IEnumerable<PerformanceDataPointDto> CreatePerformanceDataPoints<T>(
        List<Dal.Models.application.MatchPlayerStat> playerStats,
        Func<Dal.Models.application.MatchPlayerStat, T> valueSelector) where T : struct
    {
        return playerStats.Select(ps => new PerformanceDataPointDto
        {
            Date = ps.Match.MatchDate.ToDateTime(TimeOnly.MinValue),
            MatchId = ps.MatchId,
            Opponent = GetOpponentName(ps),
            Value = Convert.ToDecimal(valueSelector(ps)),
            Context = GetMatchContext(ps)
        }).ToList();
    }

    private string DetermineTrendDirection(IEnumerable<PerformanceDataPointDto> dataPoints)
    {
        var points = dataPoints.OrderBy(dp => dp.Date).ToList();
        if (points.Count < 2) return "Insufficient Data";

        var firstHalf = points.Take(points.Count / 2).Average(dp => dp.Value);
        var secondHalf = points.Skip(points.Count / 2).Average(dp => dp.Value);

        if (secondHalf > firstHalf * 1.1m) return "Improving";
        if (secondHalf < firstHalf * 0.9m) return "Declining";
        return "Stable";
    }

    private decimal CalculateTrendSlope(IEnumerable<PerformanceDataPointDto> dataPoints)
    {
        var points = dataPoints.OrderBy(dp => dp.Date).ToList();
        if (points.Count < 2) return 0;

        // Simple slope calculation
        var firstValue = points.First().Value;
        var lastValue = points.Last().Value;
        return (lastValue - firstValue) / points.Count;
    }

    private decimal CalculatePossessionRetentionRate(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        var totalPossessions = playerStats.Sum(ps => ps.TotalPossessions ?? 0);
        var possessionsLost = playerStats.Sum(ps => ps.PossessionsLost ?? 0);
        var retained = totalPossessions - possessionsLost;

        return totalPossessions > 0 ? (decimal)retained / totalPossessions * 100 : 0;
    }

    private decimal CalculateOverallEfficiencyRating(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        if (!playerStats.Any()) return 0;

        var ratings = playerStats.Where(ps => ps.OverallPerformanceRating.HasValue)
            .Select(ps => ps.OverallPerformanceRating.Value).ToList();

        return ratings.Any() ? ratings.Average() : 0;
    }

    private decimal CalculateAttackingRating(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        if (!playerStats.Any()) return 0;

        var ratings = playerStats.Where(ps => ps.AttackingRating.HasValue)
            .Select(ps => ps.AttackingRating.Value).ToList();

        return ratings.Any() ? ratings.Average() : 0;
    }

    private decimal CalculateDefensiveRating(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        if (!playerStats.Any()) return 0;

        var ratings = playerStats.Where(ps => ps.DefensiveRating.HasValue)
            .Select(ps => ps.DefensiveRating.Value).ToList();

        return ratings.Any() ? ratings.Average() : 0;
    }

    private decimal CalculatePassingRating(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        if (!playerStats.Any()) return 0;

        var ratings = playerStats.Where(ps => ps.PassingRating.HasValue)
            .Select(ps => ps.PassingRating.Value).ToList();

        return ratings.Any() ? ratings.Average() : 0;
    }

    private string GetOpponentName(Dal.Models.application.MatchPlayerStat playerStat)
    {
        if (playerStat.TeamId == playerStat.Match.HomeTeamId)
        {
            return playerStat.Match.AwayTeam?.TeamName ?? "Unknown Away Team";
        }
        return playerStat.Match.HomeTeam?.TeamName ?? "Unknown Home Team";
    }

    private string GetMatchContext(Dal.Models.application.MatchPlayerStat playerStat)
    {
        var venue = playerStat.TeamId == playerStat.Match.HomeTeamId ? "Home" : "Away";
        return $"{venue} vs {GetOpponentName(playerStat)}";
    }

    private string GetPerformanceContext(Dal.Models.application.MatchPlayerStat playerStat)
    {
        var psr = playerStat.PerformanceSuccessRate ?? 0;
        if (psr >= 2.0m) return "Excellent";
        if (psr >= 1.0m) return "Good";
        if (psr >= 0m) return "Average";
        return "Below Average";
    }

    private string GetPerformanceLevel(decimal psr)
    {
        if (psr >= 2.0m) return "Excellent";
        if (psr >= 1.0m) return "Good";
        if (psr >= 0m) return "Average";
        return "Below Average";
    }

    // Additional helper methods would be implemented here...
    private decimal CalculatePlayerScoringRate(List<Dal.Models.application.MatchPlayerStat> playerStats) => 
        playerStats.Count > 0 ? (decimal)playerStats.Sum(ps => (ps.GoalsFromPlay ?? 0) + (ps.PointsFromPlay ?? 0)) / playerStats.Count : 0;

    private decimal CalculatePlayerTackleSuccessRate(List<Dal.Models.application.MatchPlayerStat> playerStats) =>
        playerStats.Where(ps => ps.TackleSuccessRate.HasValue).Average(ps => ps.TackleSuccessRate ?? 0);

    private decimal CalculatePlayerPassingAccuracy(List<Dal.Models.application.MatchPlayerStat> playerStats) =>
        playerStats.Where(ps => ps.KickPassSuccessRate.HasValue && ps.HandPassSuccessRate.HasValue)
            .Average(ps => ((ps.KickPassSuccessRate ?? 0) + (ps.HandPassSuccessRate ?? 0)) / 2);

    private decimal CalculateGameImpactRating(List<Dal.Models.application.MatchPlayerStat> playerStats) =>
        playerStats.Average(ps => ps.OverallPerformanceRating ?? 0);

    private decimal CalculateConsistencyRating(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        var psrValues = playerStats.Select(ps => ps.PerformanceSuccessRate ?? 0).ToList();
        if (!psrValues.Any()) return 0;

        var mean = psrValues.Average();
        var variance = psrValues.Sum(x => Math.Pow((double)(x - mean), 2)) / psrValues.Count;
        var standardDeviation = (decimal)Math.Sqrt(variance);

        // Lower standard deviation = higher consistency
        return Math.Max(0, 10 - standardDeviation);
    }

    private int CalculateSuccessfulTackles(List<Dal.Models.application.MatchPlayerStat> playerStats) =>
        playerStats.Sum(ps => (int)((ps.TacklesMade ?? 0) * (ps.TackleSuccessRate ?? 0) / 100));

    private decimal CalculateScoringEfficiency(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        var totalAttempts = playerStats.Sum(ps => 
            (ps.GoalsFromPlay ?? 0) + (ps.PointsFromPlay ?? 0) + 
            (ps.ShotsWide ?? 0) + (ps.ShotsBlocked ?? 0) + (ps.ShotsSaved ?? 0) + (ps.ShotsShort ?? 0));
        var totalScores = playerStats.Sum(ps => (ps.GoalsFromPlay ?? 0) + (ps.PointsFromPlay ?? 0));
        return totalAttempts > 0 ? (decimal)totalScores / totalAttempts * 100 : 0;
    }

    private decimal CalculatePassingEfficiency(List<Dal.Models.application.MatchPlayerStat> playerStats) =>
        CalculatePlayerPassingAccuracy(playerStats);

    private decimal CalculateTacklingEfficiency(List<Dal.Models.application.MatchPlayerStat> playerStats) =>
        CalculatePlayerTackleSuccessRate(playerStats);

    private decimal CalculatePossessionEfficiency(List<Dal.Models.application.MatchPlayerStat> playerStats) =>
        CalculatePossessionRetentionRate(playerStats);

    private IEnumerable<StrengthWeaknessDto> IdentifyStrengths(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        var strengths = new List<StrengthWeaknessDto>();
        
        var avgPsr = playerStats.Average(ps => ps.PerformanceSuccessRate ?? 0);
        if (avgPsr > 1.5m)
        {
            strengths.Add(new StrengthWeaknessDto
            {
                Category = "Performance Success Rate",
                Rating = avgPsr,
                Description = "Consistently high PSR performance",
                ComparisonToAverage = avgPsr - 1.0m // Assume 1.0 is average
            });
        }

        return strengths;
    }

    private IEnumerable<StrengthWeaknessDto> IdentifyImprovementAreas(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        var improvements = new List<StrengthWeaknessDto>();
        
        var tackleRate = CalculatePlayerTackleSuccessRate(playerStats);
        if (tackleRate < 70m)
        {
            improvements.Add(new StrengthWeaknessDto
            {
                Category = "Tackling Success Rate",
                Rating = tackleRate,
                Description = "Could improve defensive tackling technique",
                ComparisonToAverage = tackleRate - 75m // Assume 75% is average
            });
        }

        return improvements;
    }

    private string DeterminePerformanceLevel(decimal playerPsr, decimal leaguePsr)
    {
        var ratio = playerPsr / Math.Max(leaguePsr, 0.1m);
        
        if (ratio >= 1.5m) return "Excellent";
        if (ratio >= 1.2m) return "Above Average";
        if (ratio >= 0.8m) return "Average";
        return "Below Average";
    }

    private IEnumerable<ComparisonMetricDto> GenerateComparisonMetrics(decimal playerPsr, decimal teamPsr, decimal positionPsr, decimal leaguePsr)
    {
        return new List<ComparisonMetricDto>
        {
            new()
            {
                MetricName = "PSR vs Team Average",
                PlayerValue = playerPsr,
                ComparisonValue = teamPsr,
                ComparisonType = "Team",
                PercentageDifference = teamPsr > 0 ? (playerPsr - teamPsr) / teamPsr * 100 : 0,
                PerformanceIndicator = playerPsr > teamPsr ? "Above" : "Below"
            },
            new()
            {
                MetricName = "PSR vs Position Average",
                PlayerValue = playerPsr,
                ComparisonValue = positionPsr,
                ComparisonType = "Position",
                PercentageDifference = positionPsr > 0 ? (playerPsr - positionPsr) / positionPsr * 100 : 0,
                PerformanceIndicator = playerPsr > positionPsr ? "Above" : "Below"
            },
            new()
            {
                MetricName = "PSR vs League Average",
                PlayerValue = playerPsr,
                ComparisonValue = leaguePsr,
                ComparisonType = "League",
                PercentageDifference = leaguePsr > 0 ? (playerPsr - leaguePsr) / leaguePsr * 100 : 0,
                PerformanceIndicator = playerPsr > leaguePsr ? "Above" : "Below"
            }
        };
    }

    private IEnumerable<ComparisonMetricDto> GenerateStatisticalComparisons(PlayerTeamStatsDto playerStats, PlayerTeamStatsDto teamStats)
    {
        return new List<ComparisonMetricDto>
        {
            new()
            {
                MetricName = "Average PSR",
                PlayerValue = playerStats.AveragePsr,
                ComparisonValue = teamStats.AveragePsr,
                ComparisonType = "Team",
                PercentageDifference = teamStats.AveragePsr > 0 ? (playerStats.AveragePsr - teamStats.AveragePsr) / teamStats.AveragePsr * 100 : 0,
                PerformanceIndicator = playerStats.AveragePsr > teamStats.AveragePsr ? "Above Team Average" : "Below Team Average"
            }
        };
    }

    private string GenerateOverallAssessment(IEnumerable<ComparisonMetricDto> comparisons)
    {
        var aboveAverageCount = comparisons.Count(c => c.PerformanceIndicator.Contains("Above"));
        var totalComparisons = comparisons.Count();
        
        if (aboveAverageCount >= totalComparisons * 0.75) return "Strong contributor to team";
        if (aboveAverageCount >= totalComparisons * 0.5) return "Solid team member";
        return "Development potential";
    }

    private decimal CalculateTrendStrength(IEnumerable<PerformanceDataPointDto> dataPoints)
    {
        // Simple trend strength calculation
        var points = dataPoints.OrderBy(dp => dp.Date).ToList();
        if (points.Count < 3) return 0;

        var firstThird = points.Take(points.Count / 3).Average(dp => dp.Value);
        var lastThird = points.Skip(points.Count * 2 / 3).Average(dp => dp.Value);
        
        return Math.Abs(lastThird - firstThird);
    }

    private string GenerateTrendDescription(string metricName, IEnumerable<PerformanceDataPointDto> dataPoints)
    {
        var direction = DetermineTrendDirection(dataPoints);
        var strength = CalculateTrendStrength(dataPoints);
        
        return $"{metricName} shows {direction.ToLower()} trend with {(strength > 1 ? "strong" : "moderate")} consistency";
    }

    private string GenerateConsistencyAssessment(decimal standardDeviation)
    {
        if (standardDeviation < 0.5m) return "Very Consistent";
        if (standardDeviation < 1.0m) return "Consistent";
        if (standardDeviation < 1.5m) return "Moderately Consistent";
        return "Inconsistent";
    }

    private string GenerateVenueAnalysis(decimal performanceDiff)
    {
        if (Math.Abs(performanceDiff) < 0.3m) return "Consistent performance regardless of venue";
        if (performanceDiff > 0) return "Performs better at home venue";
        return "Performs better in away matches";
    }

    // Simplified implementations for remaining helper methods
    private decimal CalculateAverageScoringRate(List<Dal.Models.application.MatchPlayerStat> stats) => CalculatePlayerScoringRate(stats);
    private decimal CalculateAverageTackleSuccessRate(List<Dal.Models.application.MatchPlayerStat> stats) => CalculatePlayerTackleSuccessRate(stats);
    private decimal CalculateAveragePassingAccuracy(List<Dal.Models.application.MatchPlayerStat> stats) => CalculatePlayerPassingAccuracy(stats);
    private decimal CalculateAveragePossessionRetentionRate(List<Dal.Models.application.MatchPlayerStat> stats) => CalculatePossessionRetentionRate(stats);
    private decimal CalculateAverageGameImpactRating(List<Dal.Models.application.MatchPlayerStat> stats) => CalculateGameImpactRating(stats);

    #endregion

    #endregion
}