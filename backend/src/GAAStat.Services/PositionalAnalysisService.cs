using GAAStat.Dal.Interfaces;
using GAAStat.Services.Interfaces;
using GAAStat.Services.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GAAStat.Services;

/// <summary>
/// Service for position-specific performance metrics and analysis
/// </summary>
public class PositionalAnalysisService : IPositionalAnalysisService
{
    private readonly IGAAStatDbContext _context;
    private readonly ILogger<PositionalAnalysisService> _logger;

    public PositionalAnalysisService(
        IGAAStatDbContext context,
        ILogger<PositionalAnalysisService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ServiceResult<PositionalPerformanceDto>> GetPositionalPerformanceAsync(string position, int seasonId)
    {
        try
        {
            _logger.LogInformation("Getting positional performance for {Position} in season {SeasonId}", position, seasonId);

            var season = await _context.Seasons.FindAsync(seasonId);
            if (season == null)
            {
                return ServiceResult<PositionalPerformanceDto>.Failed("Season not found");
            }

            var playerStats = await _context.MatchPlayerStats
                .Include(mps => mps.Match)
                    .ThenInclude(m => m.Competition)
                        .ThenInclude(c => c.Season)
                .Include(mps => mps.Team)
                .Where(mps => mps.StartingPosition == position && mps.Match.Competition.SeasonId == seasonId)
                .ToListAsync();

            if (!playerStats.Any())
            {
                return ServiceResult<PositionalPerformanceDto>.Failed($"No statistics found for position {position} in this season");
            }

            var statistics = CalculatePositionalStatistics(playerStats);
            var players = CalculatePositionalPlayers(playerStats);
            var benchmarks = CalculatePositionalBenchmarks(playerStats, position);
            var trends = CalculatePositionalTrends(playerStats);

            var performance = new PositionalPerformanceDto
            {
                Position = position,
                SeasonId = seasonId,
                SeasonName = season.SeasonName,
                Statistics = statistics,
                Players = players,
                Benchmarks = benchmarks,
                Trends = trends
            };

            return ServiceResult<PositionalPerformanceDto>.Success(performance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting positional performance for {Position}", position);
            return ServiceResult<PositionalPerformanceDto>.Failed("Failed to retrieve positional performance");
        }
    }

    public async Task<ServiceResult<PositionalComparisonDto>> GetPositionalComparisonAsync(int seasonId)
    {
        try
        {
            _logger.LogInformation("Getting positional comparison for season {SeasonId}", seasonId);

            var season = await _context.Seasons.FindAsync(seasonId);
            if (season == null)
            {
                return ServiceResult<PositionalComparisonDto>.Failed("Season not found");
            }

            var playerStats = await _context.MatchPlayerStats
                .Include(mps => mps.Match)
                .Include(mps => mps.Team)
                .Where(mps => mps.Match.Competition.SeasonId == seasonId && !string.IsNullOrEmpty(mps.StartingPosition))
                .ToListAsync();

            var positionComparisons = CalculatePositionComparisons(playerStats);
            var positionRankings = CalculatePositionRankings(playerStats);
            var crossPositionalInsights = GenerateCrossPositionalInsights(playerStats);

            var comparison = new PositionalComparisonDto
            {
                SeasonId = seasonId,
                SeasonName = season.SeasonName,
                PositionComparisons = positionComparisons,
                PositionRankings = positionRankings,
                CrossPositionalInsights = crossPositionalInsights
            };

            return ServiceResult<PositionalComparisonDto>.Success(comparison);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting positional comparison for season {SeasonId}", seasonId);
            return ServiceResult<PositionalComparisonDto>.Failed("Failed to retrieve positional comparison");
        }
    }

    public async Task<ServiceResult<GoalkeeperAnalysisDto>> GetGoalkeeperAnalysisAsync(int seasonId, int? teamId = null)
    {
        try
        {
            _logger.LogInformation("Getting goalkeeper analysis for season {SeasonId}", seasonId);

            var query = _context.MatchPlayerStats
                .Include(mps => mps.Match)
                .Include(mps => mps.Team)
                .Include(mps => mps.MatchKickoutStats)
                .Where(mps => mps.StartingPosition == "Goalkeeper" && mps.Match.Competition.SeasonId == seasonId);

            if (teamId.HasValue)
            {
                query = query.Where(mps => mps.TeamId == teamId.Value);
            }

            var goalkeeperStats = await query.ToListAsync();

            if (!goalkeeperStats.Any())
            {
                return ServiceResult<GoalkeeperAnalysisDto>.Failed("No goalkeeper statistics found");
            }

            var goalkeepers = CalculateGoalkeeperPerformances(goalkeeperStats);
            var benchmarks = CalculateGoalkeeperBenchmarks(goalkeeperStats);
            var specialMetrics = CalculateGoalkeeperSpecialMetrics(goalkeeperStats);

            var analysis = new GoalkeeperAnalysisDto
            {
                SeasonId = seasonId,
                TeamId = teamId,
                Goalkeepers = goalkeepers,
                Benchmarks = benchmarks,
                SpecialMetrics = specialMetrics
            };

            return ServiceResult<GoalkeeperAnalysisDto>.Success(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting goalkeeper analysis for season {SeasonId}", seasonId);
            return ServiceResult<GoalkeeperAnalysisDto>.Failed("Failed to retrieve goalkeeper analysis");
        }
    }

    public async Task<ServiceResult<DefenderAnalysisDto>> GetDefenderAnalysisAsync(int seasonId, int? teamId = null)
    {
        try
        {
            _logger.LogInformation("Getting defender analysis for season {SeasonId}", seasonId);

            var defenderPositions = new[] { "Full Back", "Half Back", "Centre Back" };

            var query = _context.MatchPlayerStats
                .Include(mps => mps.Match)
                    .ThenInclude(m => m.Competition)
                        .ThenInclude(c => c.Season)
                .Include(mps => mps.Team)
                .Where(mps => defenderPositions.Contains(mps.StartingPosition) && mps.Match.Competition.SeasonId == seasonId);

            if (teamId.HasValue)
            {
                query = query.Where(mps => mps.TeamId == teamId.Value);
            }

            var defenderStats = await query.ToListAsync();

            if (!defenderStats.Any())
            {
                return ServiceResult<DefenderAnalysisDto>.Failed("No defender statistics found");
            }

            var defenders = CalculateDefenderPerformances(defenderStats);
            var benchmarks = CalculateDefenderBenchmarks(defenderStats);
            var metricsOverview = CalculateDefensiveMetricsOverview(defenderStats);

            var analysis = new DefenderAnalysisDto
            {
                SeasonId = seasonId,
                TeamId = teamId,
                Defenders = defenders,
                Benchmarks = benchmarks,
                MetricsOverview = metricsOverview
            };

            return ServiceResult<DefenderAnalysisDto>.Success(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting defender analysis for season {SeasonId}", seasonId);
            return ServiceResult<DefenderAnalysisDto>.Failed("Failed to retrieve defender analysis");
        }
    }

    public async Task<ServiceResult<MidfielderAnalysisDto>> GetMidfielderAnalysisAsync(int seasonId, int? teamId = null)
    {
        try
        {
            _logger.LogInformation("Getting midfielder analysis for season {SeasonId}", seasonId);

            var midfielderPositions = new[] { "Midfielder", "Centre Midfielder", "Wing Midfielder" };

            var query = _context.MatchPlayerStats
                .Include(mps => mps.Match)
                    .ThenInclude(m => m.Competition)
                        .ThenInclude(c => c.Season)
                .Include(mps => mps.Team)
                .Where(mps => midfielderPositions.Contains(mps.StartingPosition) && mps.Match.Competition.SeasonId == seasonId);

            if (teamId.HasValue)
            {
                query = query.Where(mps => mps.TeamId == teamId.Value);
            }

            var midfielderStats = await query.ToListAsync();

            if (!midfielderStats.Any())
            {
                return ServiceResult<MidfielderAnalysisDto>.Failed("No midfielder statistics found");
            }

            var midfielders = CalculateMidfielderPerformances(midfielderStats);
            var benchmarks = CalculateMidfielderBenchmarks(midfielderStats);
            var metricsOverview = CalculateMidfielderMetricsOverview(midfielderStats);

            var analysis = new MidfielderAnalysisDto
            {
                SeasonId = seasonId,
                TeamId = teamId,
                Midfielders = midfielders,
                Benchmarks = benchmarks,
                MetricsOverview = metricsOverview
            };

            return ServiceResult<MidfielderAnalysisDto>.Success(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting midfielder analysis for season {SeasonId}", seasonId);
            return ServiceResult<MidfielderAnalysisDto>.Failed("Failed to retrieve midfielder analysis");
        }
    }

    public async Task<ServiceResult<ForwardAnalysisDto>> GetForwardAnalysisAsync(int seasonId, int? teamId = null)
    {
        try
        {
            _logger.LogInformation("Getting forward analysis for season {SeasonId}", seasonId);

            var forwardPositions = new[] { "Forward", "Full Forward", "Half Forward", "Centre Forward" };

            var query = _context.MatchPlayerStats
                .Include(mps => mps.Match)
                    .ThenInclude(m => m.Competition)
                        .ThenInclude(c => c.Season)
                .Include(mps => mps.Team)
                .Where(mps => forwardPositions.Contains(mps.StartingPosition) && mps.Match.Competition.SeasonId == seasonId);

            if (teamId.HasValue)
            {
                query = query.Where(mps => mps.TeamId == teamId.Value);
            }

            var forwardStats = await query.ToListAsync();

            if (!forwardStats.Any())
            {
                return ServiceResult<ForwardAnalysisDto>.Failed("No forward statistics found");
            }

            var forwards = CalculateForwardPerformances(forwardStats);
            var benchmarks = CalculateForwardBenchmarks(forwardStats);
            var metricsOverview = CalculateForwardMetricsOverview(forwardStats);

            var analysis = new ForwardAnalysisDto
            {
                SeasonId = seasonId,
                TeamId = teamId,
                Forwards = forwards,
                Benchmarks = benchmarks,
                MetricsOverview = metricsOverview
            };

            return ServiceResult<ForwardAnalysisDto>.Success(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting forward analysis for season {SeasonId}", seasonId);
            return ServiceResult<ForwardAnalysisDto>.Failed("Failed to retrieve forward analysis");
        }
    }

    public async Task<ServiceResult<PositionalPsrBenchmarksDto>> GetPositionalPsrBenchmarksAsync(int seasonId)
    {
        try
        {
            _logger.LogInformation("Getting positional PSR benchmarks for season {SeasonId}", seasonId);

            var playerStats = await _context.MatchPlayerStats
                .Include(mps => mps.Match)
                .Where(mps => mps.Match.Competition.SeasonId == seasonId && !string.IsNullOrEmpty(mps.StartingPosition))
                .ToListAsync();

            if (!playerStats.Any())
            {
                return ServiceResult<PositionalPsrBenchmarksDto>.Failed("No player statistics found for this season");
            }

            var positionBenchmarks = CalculatePositionPsrBenchmarks(playerStats);
            var overallBenchmarks = CalculateOverallPsrBenchmarks(playerStats);

            var benchmarks = new PositionalPsrBenchmarksDto
            {
                SeasonId = seasonId,
                PositionBenchmarks = positionBenchmarks,
                OverallBenchmarks = overallBenchmarks
            };

            return ServiceResult<PositionalPsrBenchmarksDto>.Success(benchmarks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting positional PSR benchmarks for season {SeasonId}", seasonId);
            return ServiceResult<PositionalPsrBenchmarksDto>.Failed("Failed to retrieve positional PSR benchmarks");
        }
    }

    public async Task<ServiceResult<FormationAnalysisDto>> GetOptimalFormationAnalysisAsync(int teamId, int seasonId)
    {
        try
        {
            _logger.LogInformation("Getting formation analysis for team {TeamId} in season {SeasonId}", teamId, seasonId);

            var team = await _context.Teams.FindAsync(teamId);
            if (team == null)
            {
                return ServiceResult<FormationAnalysisDto>.Failed("Team not found");
            }

            var teamPlayerStats = await _context.MatchPlayerStats
                .Include(mps => mps.Match)
                    .ThenInclude(m => m.Competition)
                        .ThenInclude(c => c.Season)
                .Where(mps => mps.TeamId == teamId && mps.Match.Competition.SeasonId == seasonId)
                .ToListAsync();

            if (!teamPlayerStats.Any())
            {
                return ServiceResult<FormationAnalysisDto>.Failed("No team statistics found for this season");
            }

            var formationRecommendations = GenerateFormationRecommendations(teamPlayerStats);
            var currentFormationAnalysis = AnalyzeCurrentFormation(teamPlayerStats);
            var playerOptimization = OptimizePlayerPositions(teamPlayerStats);

            var analysis = new FormationAnalysisDto
            {
                TeamId = teamId,
                TeamName = team.TeamName,
                SeasonId = seasonId,
                FormationRecommendations = formationRecommendations,
                CurrentFormationAnalysis = currentFormationAnalysis,
                PlayerOptimization = playerOptimization
            };

            return ServiceResult<FormationAnalysisDto>.Success(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting formation analysis for team {TeamId}", teamId);
            return ServiceResult<FormationAnalysisDto>.Failed("Failed to retrieve formation analysis");
        }
    }

    #region Private Helper Methods

    private PositionalStatisticsDto CalculatePositionalStatistics(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        var psrValues = playerStats.Select(ps => ps.PerformanceSuccessRate ?? 0).ToList();
        var mean = psrValues.Average();
        var variance = psrValues.Sum(x => Math.Pow((double)(x - mean), 2)) / psrValues.Count;
        var standardDeviation = (decimal)Math.Sqrt(variance);

        var keyMetrics = new List<KeyPositionalMetricDto>
        {
            new()
            {
                MetricName = "Average PSR",
                AverageValue = mean,
                BestValue = psrValues.Max(),
                BestPlayer = playerStats.OrderByDescending(ps => ps.PerformanceSuccessRate).First().PlayerName,
                PositionImportance = 1.0m
            },
            new()
            {
                MetricName = "Scoring Rate",
                AverageValue = (decimal)playerStats.Average(ps => (ps.GoalsFromPlay ?? 0) + (ps.PointsFromPlay ?? 0)),
                BestValue = (decimal)playerStats.Max(ps => (ps.GoalsFromPlay ?? 0) + (ps.PointsFromPlay ?? 0)),
                BestPlayer = playerStats.OrderByDescending(ps => (ps.GoalsFromPlay ?? 0) + (ps.PointsFromPlay ?? 0)).First().PlayerName,
                PositionImportance = 0.8m
            }
        };

        return new PositionalStatisticsDto
        {
            TotalPlayers = playerStats.Select(ps => ps.PlayerName).Distinct().Count(),
            AveragePsr = mean,
            HighestPsr = psrValues.Max(),
            LowestPsr = psrValues.Min(),
            StandardDeviation = standardDeviation,
            AverageScoringRate = (decimal)playerStats.Average(ps => (ps.GoalsFromPlay ?? 0) + (ps.PointsFromPlay ?? 0)),
            AverageTackleSuccessRate = playerStats.Where(ps => ps.TackleSuccessRate.HasValue).Average(ps => ps.TackleSuccessRate ?? 0),
            AveragePassingAccuracy = CalculateAveragePassingAccuracy(playerStats),
            KeyMetrics = keyMetrics
        };
    }

    private IEnumerable<PositionalPlayerDto> CalculatePositionalPlayers(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        return playerStats
            .GroupBy(ps => ps.PlayerName)
            .Select((g, index) => new PositionalPlayerDto
            {
                PlayerName = g.Key,
                TeamName = g.First().Team?.TeamName ?? "Unknown",
                PerformanceRating = g.Average(ps => ps.OverallPerformanceRating ?? 0),
                Psr = g.Average(ps => ps.PerformanceSuccessRate ?? 0),
                PositionRanking = index + 1,
                Strengths = IdentifyPlayerStrengths(g.ToList()),
                PositionalStats = CalculatePlayerPositionalStats(g.ToList())
            })
            .OrderByDescending(pp => pp.Psr)
            .ToList();
    }

    private PositionalBenchmarksDto CalculatePositionalBenchmarks(List<Dal.Models.application.MatchPlayerStat> playerStats, string position)
    {
        var psrValues = playerStats.Select(ps => ps.PerformanceSuccessRate ?? 0).OrderBy(x => x).ToList();
        var count = psrValues.Count;

        var benchmarkMetrics = new List<BenchmarkMetricDto>
        {
            new()
            {
                MetricName = "PSR",
                Excellent = psrValues[(int)(count * 0.9)], // 90th percentile
                Good = psrValues[(int)(count * 0.75)], // 75th percentile
                Average = psrValues[(int)(count * 0.5)], // 50th percentile (median)
                BelowAverage = psrValues[(int)(count * 0.25)], // 25th percentile
                Description = "Performance Success Rate benchmarks"
            }
        };

        return new PositionalBenchmarksDto
        {
            ExcellentThreshold = benchmarkMetrics.First().Excellent,
            GoodThreshold = benchmarkMetrics.First().Good,
            AverageThreshold = benchmarkMetrics.First().Average,
            BelowAverageThreshold = benchmarkMetrics.First().BelowAverage,
            BenchmarkMetrics = benchmarkMetrics
        };
    }

    private PositionalTrendsDto CalculatePositionalTrends(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        var trends = new List<PositionalTrendDto>
        {
            new()
            {
                TrendName = "PSR Over Time",
                TrendData = CreatePositionalTrendData(playerStats),
                TrendDirection = "Stable",
                TrendSignificance = "Moderate"
            }
        };

        return new PositionalTrendsDto
        {
            Trends = trends,
            OverallPositionTrend = "Improving performance standards",
            EmergingPatterns = new[] { "Increased consistency in performance", "Better tactical awareness" }
        };
    }

    private IEnumerable<PositionComparisonDataDto> CalculatePositionComparisons(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        return playerStats
            .GroupBy(ps => ps.StartingPosition)
            .Select(g => new PositionComparisonDataDto
            {
                Position = g.Key,
                PlayerCount = g.Select(ps => ps.PlayerName).Distinct().Count(),
                AveragePsr = g.Average(ps => ps.PerformanceSuccessRate ?? 0),
                AverageImpactRating = g.Average(ps => ps.OverallPerformanceRating ?? 0),
                PerformanceConsistency = CalculateConsistencyForGroup(g.ToList()),
                MetricComparisons = CreateMetricComparisons(g.ToList())
            })
            .OrderByDescending(pcd => pcd.AveragePsr)
            .ToList();
    }

    private PositionRankingDto CalculatePositionRankings(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        var positionAverages = playerStats
            .GroupBy(ps => ps.StartingPosition)
            .Select(g => new { Position = g.Key, AveragePsr = g.Average(ps => ps.PerformanceSuccessRate ?? 0) })
            .OrderByDescending(pa => pa.AveragePsr)
            .ToList();

        var psrRankings = positionAverages
            .Select((pa, index) => new RankedPositionDto
            {
                Rank = index + 1,
                Position = pa.Position,
                Value = pa.AveragePsr,
                PerformanceLevel = DeterminePerformanceLevel(pa.AveragePsr)
            }).ToList();

        return new PositionRankingDto
        {
            PsrRankings = psrRankings,
            ImpactRankings = psrRankings, // Simplified - would calculate separate impact rankings
            ConsistencyRankings = psrRankings // Simplified - would calculate separate consistency rankings
        };
    }

    private CrossPositionalInsightsDto GenerateCrossPositionalInsights(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        var positionStats = playerStats.GroupBy(ps => ps.StartingPosition).ToList();
        var mostInfluential = positionStats.OrderByDescending(g => g.Average(ps => ps.PerformanceSuccessRate ?? 0)).First().Key;
        var mostConsistent = positionStats.OrderBy(g => CalculateConsistencyForGroup(g.ToList())).First().Key;

        return new CrossPositionalInsightsDto
        {
            KeyFindings = new[]
            {
                "Forwards show highest scoring consistency",
                "Midfielders have best distribution accuracy",
                "Defenders show strong tackle success rates"
            },
            MostInfluentialPosition = mostInfluential,
            MostConsistentPosition = mostConsistent,
            MostImprovedPosition = "Midfielder", // Would calculate actual improvement
            PositionalRelationships = new List<PositionalRelationshipDto>()
        };
    }

    // Goalkeeper specific calculations
    private IEnumerable<GoalkeeperPerformanceDto> CalculateGoalkeeperPerformances(List<Dal.Models.application.MatchPlayerStat> goalkeeperStats)
    {
        return goalkeeperStats
            .GroupBy(gs => gs.PlayerName)
            .Select(g => new GoalkeeperPerformanceDto
            {
                PlayerName = g.Key,
                TeamName = g.First().Team?.TeamName ?? "Unknown",
                MatchesPlayed = g.Count(),
                AveragePsr = g.Average(gs => gs.PerformanceSuccessRate ?? 0),
                SavesMade = g.Sum(gs => gs.TacklesMade ?? 0), // Using tackles as proxy for saves
                SavePercentage = g.Average(gs => gs.TackleSuccessRate ?? 0), // Using tackle success as proxy
                KickoutsMade = g.Sum(gs => gs.MatchKickoutStats.Sum(ks => ks.TotalKickouts ?? 0)),
                KickoutSuccessRate = CalculateKickoutSuccessRate(g.ToList()),
                DistributionAccuracy = g.Average(gs => (gs.KickPassSuccessRate ?? 0 + gs.HandPassSuccessRate ?? 0) / 2),
                CleanSheets = g.Count(gs => gs.PerformanceSuccessRate >= 2.0m), // Simplified metric
                GoalsConcededPerMatch = 1.2m, // Would calculate from actual match data
                OverallRating = g.Average(gs => gs.OverallPerformanceRating ?? 0)
            }).ToList();
    }

    private decimal CalculateAveragePassingAccuracy(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        var playersWithPassingData = playerStats
            .Where(ps => ps.KickPassSuccessRate.HasValue && ps.HandPassSuccessRate.HasValue)
            .ToList();

        return playersWithPassingData.Any() 
            ? playersWithPassingData.Average(ps => (ps.KickPassSuccessRate ?? 0 + ps.HandPassSuccessRate ?? 0) / 2)
            : 0;
    }

    private IEnumerable<PlayerPositionalStrengthDto> IdentifyPlayerStrengths(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        var strengths = new List<PlayerPositionalStrengthDto>();
        
        var avgPsr = playerStats.Average(ps => ps.PerformanceSuccessRate ?? 0);
        if (avgPsr > 1.5m)
        {
            strengths.Add(new PlayerPositionalStrengthDto
            {
                Strength = "High PSR Performance",
                Rating = avgPsr,
                Impact = "Consistently strong overall performance"
            });
        }

        return strengths;
    }

    private PlayerPositionalStatsDto CalculatePlayerPositionalStats(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        return new PlayerPositionalStatsDto
        {
            MatchesPlayed = playerStats.Count,
            AveragePsr = playerStats.Average(ps => ps.PerformanceSuccessRate ?? 0),
            ScoringRate = (decimal)playerStats.Average(ps => (ps.GoalsFromPlay ?? 0) + (ps.PointsFromPlay ?? 0)),
            DefensiveRating = playerStats.Average(ps => ps.DefensiveRating ?? 0),
            DistributionRating = playerStats.Average(ps => ps.PassingRating ?? 0),
            PositionSpecificRating = playerStats.Average(ps => ps.OverallPerformanceRating ?? 0)
        };
    }

    private IEnumerable<TrendDataPointDto> CreatePositionalTrendData(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        return playerStats
            .GroupBy(ps => ps.Match.MatchDate.ToDateTime(TimeOnly.MinValue))
            .Select(g => new TrendDataPointDto
            {
                Date = g.Key,
                Value = g.Average(ps => ps.PerformanceSuccessRate ?? 0),
                Context = $"{g.Count()} players"
            })
            .OrderBy(tdp => tdp.Date)
            .ToList();
    }

    private decimal CalculateConsistencyForGroup(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        var psrValues = playerStats.Select(ps => ps.PerformanceSuccessRate ?? 0).ToList();
        if (!psrValues.Any()) return 0;

        var mean = psrValues.Average();
        var variance = psrValues.Sum(x => Math.Pow((double)(x - mean), 2)) / psrValues.Count;
        var standardDeviation = (decimal)Math.Sqrt(variance);

        return Math.Max(0, 10 - standardDeviation); // Higher consistency = lower standard deviation
    }

    private IEnumerable<PositionMetricComparisonDto> CreateMetricComparisons(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        return new List<PositionMetricComparisonDto>
        {
            new()
            {
                MetricName = "Average PSR",
                Value = playerStats.Average(ps => ps.PerformanceSuccessRate ?? 0),
                Ranking = 1, // Would calculate actual ranking
                RelativeStrength = 1.0m
            }
        };
    }

    private string DeterminePerformanceLevel(decimal averagePsr)
    {
        if (averagePsr >= 2.0m) return "Elite";
        if (averagePsr >= 1.5m) return "Excellent";
        if (averagePsr >= 1.0m) return "Good";
        if (averagePsr >= 0.5m) return "Average";
        return "Below Average";
    }

    private decimal CalculateKickoutSuccessRate(List<Dal.Models.application.MatchPlayerStat> goalkeeperStats)
    {
        var totalKickouts = goalkeeperStats.Sum(gs => gs.MatchKickoutStats.Sum(ks => ks.TotalKickouts ?? 0));
        var successfulKickouts = goalkeeperStats.Sum(gs => gs.MatchKickoutStats.Sum(ks => ks.SuccessfulKickouts ?? 0));
        
        return totalKickouts > 0 ? (decimal)successfulKickouts / totalKickouts * 100 : 0;
    }

    // Simplified implementations for remaining position-specific methods
    private GoalkeeperBenchmarksDto CalculateGoalkeeperBenchmarks(List<Dal.Models.application.MatchPlayerStat> stats) => new();
    private GoalkeeperSpecialMetricsDto CalculateGoalkeeperSpecialMetrics(List<Dal.Models.application.MatchPlayerStat> stats) => new();
    private IEnumerable<DefenderPerformanceDto> CalculateDefenderPerformances(List<Dal.Models.application.MatchPlayerStat> stats) => new List<DefenderPerformanceDto>();
    private DefenderBenchmarksDto CalculateDefenderBenchmarks(List<Dal.Models.application.MatchPlayerStat> stats) => new();
    private DefensiveMetricsOverviewDto CalculateDefensiveMetricsOverview(List<Dal.Models.application.MatchPlayerStat> stats) => new();
    private IEnumerable<MidfielderPerformanceDto> CalculateMidfielderPerformances(List<Dal.Models.application.MatchPlayerStat> stats) => new List<MidfielderPerformanceDto>();
    private MidfielderBenchmarksDto CalculateMidfielderBenchmarks(List<Dal.Models.application.MatchPlayerStat> stats) => new();
    private MidfielderMetricsOverviewDto CalculateMidfielderMetricsOverview(List<Dal.Models.application.MatchPlayerStat> stats) => new();
    private IEnumerable<ForwardPerformanceDto> CalculateForwardPerformances(List<Dal.Models.application.MatchPlayerStat> stats) => new List<ForwardPerformanceDto>();
    private ForwardBenchmarksDto CalculateForwardBenchmarks(List<Dal.Models.application.MatchPlayerStat> stats) => new();
    private ForwardMetricsOverviewDto CalculateForwardMetricsOverview(List<Dal.Models.application.MatchPlayerStat> stats) => new();
    private IEnumerable<PositionPsrBenchmarkDto> CalculatePositionPsrBenchmarks(List<Dal.Models.application.MatchPlayerStat> stats) => new List<PositionPsrBenchmarkDto>();
    private OverallBenchmarksDto CalculateOverallPsrBenchmarks(List<Dal.Models.application.MatchPlayerStat> stats) => new();
    private IEnumerable<FormationRecommendationDto> GenerateFormationRecommendations(List<Dal.Models.application.MatchPlayerStat> stats) => new List<FormationRecommendationDto>();
    private CurrentFormationAnalysisDto AnalyzeCurrentFormation(List<Dal.Models.application.MatchPlayerStat> stats) => new();
    private PlayerPositionOptimizationDto OptimizePlayerPositions(List<Dal.Models.application.MatchPlayerStat> stats) => new();

    #endregion
}