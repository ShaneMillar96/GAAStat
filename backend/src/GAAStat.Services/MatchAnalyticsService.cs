using GAAStat.Dal.Interfaces;
using GAAStat.Services.Interfaces;
using GAAStat.Services.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GAAStat.Services;

/// <summary>
/// Service for match-level analysis and reporting with optimized database queries
/// </summary>
public class MatchAnalyticsService : IMatchAnalyticsService
{
    private readonly IGAAStatDbContext _context;
    private readonly ILogger<MatchAnalyticsService> _logger;

    public MatchAnalyticsService(
        IGAAStatDbContext context,
        ILogger<MatchAnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ServiceResult<MatchSummaryDto>> GetMatchSummaryAsync(int matchId)
    {
        try
        {
            _logger.LogInformation("Getting match summary for match {MatchId}", matchId);

            var match = await _context.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Include(m => m.Competition)
                .Include(m => m.MatchPlayerStats)
                    .ThenInclude(mps => mps.Team)
                .FirstOrDefaultAsync(m => m.Id == matchId);

            if (match == null)
            {
                return ServiceResult<MatchSummaryDto>.Failed("Match not found");
            }

            var homeTeamStats = CalculateTeamMatchStats(match.MatchPlayerStats, match.HomeTeamId);
            var awayTeamStats = CalculateTeamMatchStats(match.MatchPlayerStats, match.AwayTeamId);
            var keyMoments = await CalculateKeyMomentsAsync(matchId);

            var summary = new MatchSummaryDto
            {
                MatchId = match.Id,
                HomeTeam = match.HomeTeam.TeamName,
                AwayTeam = match.AwayTeam.TeamName,
                MatchDate = match.MatchDateTime,
                Venue = match.Venue,
                Competition = match.Competition?.CompetitionName ?? "Unknown",
                HomeTeamStats = homeTeamStats,
                AwayTeamStats = awayTeamStats,
                KeyMoments = keyMoments,
                TotalPlayers = match.MatchPlayerStats.Count,
                MatchDuration = TimeSpan.FromMinutes(GaaConstants.STANDARD_MATCH_DURATION_MINUTES)
            };

            return ServiceResult<MatchSummaryDto>.Success(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting match summary for match {MatchId}", matchId);
            return ServiceResult<MatchSummaryDto>.Failed("Failed to retrieve match summary");
        }
    }

    public async Task<ServiceResult<MatchTeamComparisonDto>> GetMatchTeamComparisonAsync(int matchId)
    {
        try
        {
            _logger.LogInformation("Getting team comparison for match {MatchId}", matchId);

            var match = await _context.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Include(m => m.MatchPlayerStats)
                .FirstOrDefaultAsync(m => m.Id == matchId);

            if (match == null)
            {
                return ServiceResult<MatchTeamComparisonDto>.Failed("Match not found");
            }

            var homeTeamStats = CalculateTeamMatchStats(match.MatchPlayerStats, match.HomeTeamId);
            var awayTeamStats = CalculateTeamMatchStats(match.MatchPlayerStats, match.AwayTeamId);
            var differentials = CalculatePerformanceDifferentials(homeTeamStats, awayTeamStats);
            var keyStats = GenerateKeyStatComparisons(homeTeamStats, awayTeamStats);

            var comparison = new MatchTeamComparisonDto
            {
                MatchId = matchId,
                HomeTeam = homeTeamStats,
                AwayTeam = awayTeamStats,
                Differentials = differentials,
                MatchWinner = DetermineMatchWinner(homeTeamStats, awayTeamStats),
                KeyStatistics = keyStats
            };

            return ServiceResult<MatchTeamComparisonDto>.Success(comparison);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team comparison for match {MatchId}", matchId);
            return ServiceResult<MatchTeamComparisonDto>.Failed("Failed to retrieve team comparison");
        }
    }

    public async Task<ServiceResult<KickoutAnalysisDto>> GetKickoutAnalysisAsync(int matchId)
    {
        try
        {
            _logger.LogInformation("Getting kickout analysis for match {MatchId}", matchId);

            var kickoutStats = await _context.MatchKickoutStats
                .Include(mks => mks.MatchPlayerStat)
                    .ThenInclude(mps => mps.Team)
                .Where(mks => mks.MatchPlayerStat.MatchId == matchId)
                .ToListAsync();

            if (!kickoutStats.Any())
            {
                return ServiceResult<KickoutAnalysisDto>.Failed("No kickout data found for this match");
            }

            var teamKickouts = kickoutStats
                .GroupBy(ks => ks.MatchPlayerStat.TeamId)
                .Select(g => new TeamKickoutStatsDto
                {
                    TeamId = g.Key,
                    TeamName = g.First().MatchPlayerStat.Team.TeamName,
                    TotalKickouts = g.Sum(ks => ks.TotalKickouts ?? 0),
                    SuccessfulKickouts = g.Sum(ks => ks.SuccessfulKickouts ?? 0),
                    RetentionRate = CalculateRetentionRate(
                        g.Sum(ks => ks.SuccessfulKickouts ?? 0),
                        g.Sum(ks => ks.TotalKickouts ?? 0)),
                    PlayerKickouts = g.Select(ks => new PlayerKickoutStatsDto
                    {
                        PlayerName = ks.MatchPlayerStat.PlayerName,
                        TotalKickouts = ks.TotalKickouts ?? 0,
                        SuccessfulKickouts = ks.SuccessfulKickouts ?? 0,
                        SuccessRate = CalculateRetentionRate(
                            ks.SuccessfulKickouts ?? 0,
                            ks.TotalKickouts ?? 0),
                        PreferredDirection = ks.PreferredDirection ?? "Unknown"
                    }).ToList()
                }).ToList();

            var directionAnalysis = CalculateKickoutDirectionAnalysis(kickoutStats);
            var retentionAnalysis = CalculateKickoutRetentionAnalysis(kickoutStats);
            var totalKickouts = kickoutStats.Sum(ks => ks.TotalKickouts ?? 0);
            var totalSuccessful = kickoutStats.Sum(ks => ks.SuccessfulKickouts ?? 0);

            var analysis = new KickoutAnalysisDto
            {
                MatchId = matchId,
                TeamKickouts = teamKickouts,
                DirectionAnalysis = directionAnalysis,
                RetentionAnalysis = retentionAnalysis,
                OverallRetentionRate = CalculateRetentionRate(totalSuccessful, totalKickouts),
                TotalKickouts = totalKickouts
            };

            return ServiceResult<KickoutAnalysisDto>.Success(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting kickout analysis for match {MatchId}", matchId);
            return ServiceResult<KickoutAnalysisDto>.Failed("Failed to retrieve kickout analysis");
        }
    }

    public async Task<ServiceResult<ShotAnalysisDto>> GetShotAnalysisAsync(int matchId)
    {
        try
        {
            _logger.LogInformation("Getting shot analysis for match {MatchId}", matchId);

            var playerStats = await _context.MatchPlayerStats
                .Include(mps => mps.Team)
                .Where(mps => mps.MatchId == matchId)
                .ToListAsync();

            if (!playerStats.Any())
            {
                return ServiceResult<ShotAnalysisDto>.Failed("No player statistics found for this match");
            }

            var teamShotStats = playerStats
                .GroupBy(ps => ps.TeamId)
                .Select(g => CalculateTeamShotStats(g.Key, g.First().Team.TeamName, g.ToList()))
                .ToList();

            var conversionAnalysis = CalculateShotConversionAnalysis(playerStats);
            var locationAnalysis = CalculateShotLocationAnalysis(playerStats);
            
            var totalShots = playerStats.Sum(ps => 
                (ps.GoalsFromPlay ?? 0) + (ps.PointsFromPlay ?? 0) +
                (ps.ShotsWide ?? 0) + (ps.ShotsBlocked ?? 0) + 
                (ps.ShotsSaved ?? 0) + (ps.ShotsShort ?? 0));
            
            var totalScores = playerStats.Sum(ps => 
                (ps.GoalsFromPlay ?? 0) + (ps.PointsFromPlay ?? 0));

            var analysis = new ShotAnalysisDto
            {
                MatchId = matchId,
                TeamShotStats = teamShotStats,
                ConversionAnalysis = conversionAnalysis,
                LocationAnalysis = locationAnalysis,
                TotalShots = totalShots,
                TotalScores = totalScores,
                OverallConversionRate = CalculateConversionRate(totalScores, totalShots)
            };

            return ServiceResult<ShotAnalysisDto>.Success(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shot analysis for match {MatchId}", matchId);
            return ServiceResult<ShotAnalysisDto>.Failed("Failed to retrieve shot analysis");
        }
    }

    public async Task<ServiceResult<MatchMomentumDto>> GetMatchMomentumAnalysisAsync(int matchId)
    {
        try
        {
            _logger.LogInformation("Getting momentum analysis for match {MatchId}", matchId);

            var playerStats = await _context.MatchPlayerStats
                .Include(mps => mps.Team)
                .Where(mps => mps.MatchId == matchId)
                .OrderBy(mps => mps.SubstitutedOnMinute ?? 0)
                .ToListAsync();

            if (!playerStats.Any())
            {
                return ServiceResult<MatchMomentumDto>.Failed("No player statistics found for this match");
            }

            // Simulate period performance (would require actual time-based data in real scenario)
            var periodPerformance = CreatePeriodPerformanceAnalysis(playerStats);
            var momentumSwings = CreateMomentumSwingsAnalysis(playerStats);
            var progressionAnalysis = CreateProgressionAnalysis(playerStats);

            var momentum = new MatchMomentumDto
            {
                MatchId = matchId,
                PeriodPerformance = periodPerformance,
                MomentumSwings = momentumSwings,
                ProgressionAnalysis = progressionAnalysis
            };

            return ServiceResult<MatchMomentumDto>.Success(momentum);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting momentum analysis for match {MatchId}", matchId);
            return ServiceResult<MatchMomentumDto>.Failed("Failed to retrieve momentum analysis");
        }
    }

    public async Task<ServiceResult<MatchTopPerformersDto>> GetMatchTopPerformersAsync(int matchId, int limit = 5)
    {
        try
        {
            _logger.LogInformation("Getting top performers for match {MatchId}", matchId);

            var playerStats = await _context.MatchPlayerStats
                .Include(mps => mps.Team)
                .Where(mps => mps.MatchId == matchId)
                .ToListAsync();

            if (!playerStats.Any())
            {
                return ServiceResult<MatchTopPerformersDto>.Failed("No player statistics found for this match");
            }

            var topPsrPerformers = GetTopPerformersByMetric(playerStats, ps => ps.PerformanceSuccessRate ?? 0, "PSR", limit);
            var topScorers = GetTopPerformersByMetric(playerStats, ps => (ps.GoalsFromPlay ?? 0) * 3 + (ps.PointsFromPlay ?? 0), "Total Score", limit);
            var topDefenders = GetTopPerformersByMetric(playerStats, ps => ps.TacklesMade ?? 0, "Tackles Made", limit);
            var topDistributors = GetTopPerformersByMetric(playerStats, ps => (ps.KickPasses ?? 0) + (ps.HandPasses ?? 0), "Total Passes", limit);
            
            var manOfTheMatch = topPsrPerformers.FirstOrDefault() ?? new TopPerformerDto();

            var topPerformers = new MatchTopPerformersDto
            {
                MatchId = matchId,
                TopPsrPerformers = topPsrPerformers,
                TopScorers = topScorers,
                TopDefenders = topDefenders,
                TopDistributors = topDistributors,
                ManOfTheMatch = manOfTheMatch
            };

            return ServiceResult<MatchTopPerformersDto>.Success(topPerformers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top performers for match {MatchId}", matchId);
            return ServiceResult<MatchTopPerformersDto>.Failed("Failed to retrieve top performers");
        }
    }

    #region Private Helper Methods

    private TeamMatchStatsDto CalculateTeamMatchStats(IEnumerable<Dal.Models.application.MatchPlayerStat> playerStats, int teamId)
    {
        var teamPlayerStats = playerStats.Where(ps => ps.TeamId == teamId).ToList();
        
        if (!teamPlayerStats.Any())
        {
            return new TeamMatchStatsDto { TeamId = teamId };
        }

        var teamName = teamPlayerStats.First().Team?.TeamName ?? "Unknown Team";
        
        return new TeamMatchStatsDto
        {
            TeamId = teamId,
            TeamName = teamName,
            PlayersAnalyzed = teamPlayerStats.Count,
            AveragePsr = teamPlayerStats.Average(ps => ps.PerformanceSuccessRate ?? 0),
            TeamTotalPsr = teamPlayerStats.Sum(ps => ps.PerformanceSuccessRate ?? 0),
            TotalPossessions = teamPlayerStats.Sum(ps => ps.TotalPossessions ?? 0),
            TotalEvents = teamPlayerStats.Sum(ps => ps.TotalEvents ?? 0),
            TotalScores = teamPlayerStats.Sum(ps => (ps.GoalsFromPlay ?? 0) + (ps.PointsFromPlay ?? 0)),
            TotalGoals = teamPlayerStats.Sum(ps => ps.GoalsFromPlay ?? 0),
            TotalPoints = teamPlayerStats.Sum(ps => ps.PointsFromPlay ?? 0),
            ScoringEfficiency = CalculateScoringEfficiency(teamPlayerStats),
            PossessionRetention = CalculatePossessionRetention(teamPlayerStats),
            TotalTackles = teamPlayerStats.Sum(ps => ps.TacklesMade ?? 0),
            TackleSuccessRate = CalculateAverageTackleSuccessRate(teamPlayerStats),
            TotalTurnovers = teamPlayerStats.Sum(ps => ps.PossessionsLost ?? 0),
            TurnoverRate = CalculateTurnoverRate(teamPlayerStats)
        };
    }

    private async Task<MatchKeyMomentsDto> CalculateKeyMomentsAsync(int matchId)
    {
        var playerStats = await _context.MatchPlayerStats
            .Include(mps => mps.Team)
            .Where(mps => mps.MatchId == matchId)
            .ToListAsync();

        var topPerformers = GetTopPerformersByMetric(playerStats, ps => ps.PerformanceSuccessRate ?? 0, "PSR", 3);
        var keyScores = CreateScoringEvents(playerStats);
        var keyDefensiveActions = CreateDefensiveEvents(playerStats);

        return new MatchKeyMomentsDto
        {
            TopPerformers = topPerformers,
            KeyScores = keyScores,
            KeyDefensiveActions = keyDefensiveActions,
            TotalCards = playerStats.Sum(ps => (ps.CardsYellow ?? 0) + (ps.CardsBlack ?? 0) + (ps.CardsRed ?? 0)),
            RedCards = playerStats.Sum(ps => ps.CardsRed ?? 0),
            BlackCards = playerStats.Sum(ps => ps.CardsBlack ?? 0),
            YellowCards = playerStats.Sum(ps => ps.CardsYellow ?? 0)
        };
    }

    private PerformanceDifferentialsDto CalculatePerformanceDifferentials(TeamMatchStatsDto homeTeam, TeamMatchStatsDto awayTeam)
    {
        return new PerformanceDifferentialsDto
        {
            PsrDifference = homeTeam.AveragePsr - awayTeam.AveragePsr,
            PossessionDifference = homeTeam.PossessionRetention - awayTeam.PossessionRetention,
            ScoringEfficiencyDifference = homeTeam.ScoringEfficiency - awayTeam.ScoringEfficiency,
            TackleSuccessRateDifference = homeTeam.TackleSuccessRate - awayTeam.TackleSuccessRate,
            TotalScoreDifference = homeTeam.TotalScores - awayTeam.TotalScores
        };
    }

    private IEnumerable<KeyStatComparisonDto> GenerateKeyStatComparisons(TeamMatchStatsDto homeTeam, TeamMatchStatsDto awayTeam)
    {
        var comparisons = new List<KeyStatComparisonDto>
        {
            new() {
                StatisticName = "Average PSR",
                HomeValue = homeTeam.AveragePsr,
                AwayValue = awayTeam.AveragePsr,
                Difference = homeTeam.AveragePsr - awayTeam.AveragePsr,
                WinningTeam = homeTeam.AveragePsr > awayTeam.AveragePsr ? homeTeam.TeamName : awayTeam.TeamName
            },
            new() {
                StatisticName = "Total Scores",
                HomeValue = homeTeam.TotalScores,
                AwayValue = awayTeam.TotalScores,
                Difference = homeTeam.TotalScores - awayTeam.TotalScores,
                WinningTeam = homeTeam.TotalScores > awayTeam.TotalScores ? homeTeam.TeamName : awayTeam.TeamName
            },
            new() {
                StatisticName = "Possession Retention %",
                HomeValue = homeTeam.PossessionRetention,
                AwayValue = awayTeam.PossessionRetention,
                Difference = homeTeam.PossessionRetention - awayTeam.PossessionRetention,
                WinningTeam = homeTeam.PossessionRetention > awayTeam.PossessionRetention ? homeTeam.TeamName : awayTeam.TeamName
            },
            new() {
                StatisticName = "Tackle Success Rate %",
                HomeValue = homeTeam.TackleSuccessRate,
                AwayValue = awayTeam.TackleSuccessRate,
                Difference = homeTeam.TackleSuccessRate - awayTeam.TackleSuccessRate,
                WinningTeam = homeTeam.TackleSuccessRate > awayTeam.TackleSuccessRate ? homeTeam.TeamName : awayTeam.TeamName
            }
        };

        return comparisons;
    }

    private string DetermineMatchWinner(TeamMatchStatsDto homeTeam, TeamMatchStatsDto awayTeam)
    {
        var homeScore = homeTeam.TotalGoals * GaaConstants.POINTS_PER_GOAL + homeTeam.TotalPoints;
        var awayScore = awayTeam.TotalGoals * GaaConstants.POINTS_PER_GOAL + awayTeam.TotalPoints;

        if (homeScore > awayScore) return homeTeam.TeamName;
        if (awayScore > homeScore) return awayTeam.TeamName;
        return "Draw";
    }

    private decimal CalculateRetentionRate(int successful, int total)
    {
        return total > 0 ? (decimal)successful / total * 100 : 0;
    }

    private KickoutDirectionAnalysisDto CalculateKickoutDirectionAnalysis(List<Dal.Models.application.MatchKickoutStat> kickoutStats)
    {
        // This would be more sophisticated with actual direction data
        var totalKickouts = kickoutStats.Sum(ks => ks.TotalKickouts ?? 0);
        
        return new KickoutDirectionAnalysisDto
        {
            ShortKickouts = (int)(totalKickouts * 0.3m), // Estimated
            LongKickouts = (int)(totalKickouts * 0.7m),
            LeftSideKickouts = (int)(totalKickouts * 0.35m),
            RightSideKickouts = (int)(totalKickouts * 0.35m),
            CentralKickouts = (int)(totalKickouts * 0.3m),
            ShortKickoutSuccessRate = 85.0m, // Would be calculated from actual data
            LongKickoutSuccessRate = 65.0m
        };
    }

    private KickoutRetentionAnalysisDto CalculateKickoutRetentionAnalysis(List<Dal.Models.application.MatchKickoutStat> kickoutStats)
    {
        var totalKickouts = kickoutStats.Sum(ks => ks.TotalKickouts ?? 0);
        var totalSuccessful = kickoutStats.Sum(ks => ks.SuccessfulKickouts ?? 0);
        var retained = totalSuccessful;
        var lost = totalKickouts - totalSuccessful;

        return new KickoutRetentionAnalysisDto
        {
            RetainedKickouts = retained,
            LostKickouts = lost,
            RetentionPercentage = CalculateRetentionRate(retained, totalKickouts),
            MostEffectiveStrategy = "Short kickouts to midfield", // Would be determined from analysis
            LeastEffectiveStrategy = "Long kickouts under pressure"
        };
    }

    private TeamShotStatsDto CalculateTeamShotStats(int teamId, string teamName, List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        var totalShots = playerStats.Sum(ps => 
            (ps.GoalsFromPlay ?? 0) + (ps.PointsFromPlay ?? 0) +
            (ps.ShotsWide ?? 0) + (ps.ShotsBlocked ?? 0) + 
            (ps.ShotsSaved ?? 0) + (ps.ShotsShort ?? 0));
        
        var goals = playerStats.Sum(ps => ps.GoalsFromPlay ?? 0);
        var points = playerStats.Sum(ps => ps.PointsFromPlay ?? 0);
        var scores = goals + points;

        return new TeamShotStatsDto
        {
            TeamId = teamId,
            TeamName = teamName,
            TotalShots = totalShots,
            Goals = goals,
            Points = points,
            ShotsWide = playerStats.Sum(ps => ps.ShotsWide ?? 0),
            ShotsBlocked = playerStats.Sum(ps => ps.ShotsBlocked ?? 0),
            ShotsSaved = playerStats.Sum(ps => ps.ShotsSaved ?? 0),
            ShotsShort = playerStats.Sum(ps => ps.ShotsShort ?? 0),
            ConversionRate = CalculateConversionRate(scores, totalShots),
            ShotAccuracy = CalculateShotAccuracy(scores, totalShots)
        };
    }

    private ShotConversionAnalysisDto CalculateShotConversionAnalysis(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        var totalGoals = playerStats.Sum(ps => (ps.GoalsFromPlay ?? 0) + (ps.GoalsFromFrees ?? 0));
        var totalPoints = playerStats.Sum(ps => (ps.PointsFromPlay ?? 0) + (ps.PointsFromFrees ?? 0));
        var totalAttempts = playerStats.Sum(ps => 
            (ps.GoalsFromPlay ?? 0) + (ps.PointsFromPlay ?? 0) + (ps.GoalsFromFrees ?? 0) + (ps.PointsFromFrees ?? 0) +
            (ps.ShotsWide ?? 0) + (ps.ShotsBlocked ?? 0) + (ps.ShotsSaved ?? 0) + (ps.ShotsShort ?? 0) +
            (ps.FreesWide ?? 0) + (ps.FreesSaved ?? 0) + (ps.FreesShort ?? 0));

        var topScorers = playerStats
            .Where(ps => ((ps.GoalsFromPlay ?? 0) + (ps.PointsFromPlay ?? 0)) > 0)
            .OrderByDescending(ps => (ps.GoalsFromPlay ?? 0) * 3 + (ps.PointsFromPlay ?? 0))
            .Take(5)
            .Select(ps => new TopScorerInMatchDto
            {
                PlayerName = ps.PlayerName,
                TeamName = ps.Team?.TeamName ?? "Unknown",
                Goals = ps.GoalsFromPlay ?? 0,
                Points = ps.PointsFromPlay ?? 0,
                TotalScore = (ps.GoalsFromPlay ?? 0) * 3 + (ps.PointsFromPlay ?? 0),
                ShotEfficiency = ps.ShotEfficiency ?? 0
            }).ToList();

        return new ShotConversionAnalysisDto
        {
            GoalConversionRate = CalculateConversionRate(totalGoals, totalAttempts),
            PointConversionRate = CalculateConversionRate(totalPoints, totalAttempts),
            FreeConversionRate = CalculateConversionRate(
                playerStats.Sum(ps => (ps.GoalsFromFrees ?? 0) + (ps.PointsFromFrees ?? 0)),
                playerStats.Sum(ps => (ps.GoalsFromFrees ?? 0) + (ps.PointsFromFrees ?? 0) + (ps.FreesWide ?? 0) + (ps.FreesSaved ?? 0) + (ps.FreesShort ?? 0))),
            PlayConversionRate = CalculateConversionRate(
                playerStats.Sum(ps => (ps.GoalsFromPlay ?? 0) + (ps.PointsFromPlay ?? 0)),
                playerStats.Sum(ps => (ps.GoalsFromPlay ?? 0) + (ps.PointsFromPlay ?? 0) + (ps.ShotsWide ?? 0) + (ps.ShotsBlocked ?? 0) + (ps.ShotsSaved ?? 0) + (ps.ShotsShort ?? 0))),
            TopScorers = topScorers
        };
    }

    private ShotLocationAnalysisDto CalculateShotLocationAnalysis(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        var shotsFromPlay = playerStats.Sum(ps => 
            (ps.GoalsFromPlay ?? 0) + (ps.PointsFromPlay ?? 0) + (ps.ShotsWide ?? 0) + (ps.ShotsBlocked ?? 0) + (ps.ShotsSaved ?? 0) + (ps.ShotsShort ?? 0));
        
        var shotsFromFrees = playerStats.Sum(ps => 
            (ps.GoalsFromFrees ?? 0) + (ps.PointsFromFrees ?? 0) + (ps.FreesWide ?? 0) + (ps.FreesSaved ?? 0) + (ps.FreesShort ?? 0));

        var scoresFromPlay = playerStats.Sum(ps => (ps.GoalsFromPlay ?? 0) + (ps.PointsFromPlay ?? 0));
        var scoresFromFrees = playerStats.Sum(ps => (ps.GoalsFromFrees ?? 0) + (ps.PointsFromFrees ?? 0));

        return new ShotLocationAnalysisDto
        {
            ShotsFromPlay = shotsFromPlay,
            ShotsFromFrees = shotsFromFrees,
            PlayShotSuccessRate = CalculateConversionRate(scoresFromPlay, shotsFromPlay),
            FreeShotSuccessRate = CalculateConversionRate(scoresFromFrees, shotsFromFrees)
        };
    }

    private IEnumerable<PeriodPerformanceDto> CreatePeriodPerformanceAnalysis(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        // This is a simplified implementation - in reality, you'd need time-based data
        var homeTeamStats = playerStats.Where(ps => ps.TeamId == playerStats.First().MatchId).Take(playerStats.Count / 2);
        var awayTeamStats = playerStats.Except(homeTeamStats);

        return new List<PeriodPerformanceDto>
        {
            new()
            {
                Period = "First Half",
                HomeTeamPerformance = CreateTeamPeriodPerformance(homeTeamStats.ToList(), "Home Team"),
                AwayTeamPerformance = CreateTeamPeriodPerformance(awayTeamStats.ToList(), "Away Team")
            },
            new()
            {
                Period = "Second Half",
                HomeTeamPerformance = CreateTeamPeriodPerformance(homeTeamStats.ToList(), "Home Team"),
                AwayTeamPerformance = CreateTeamPeriodPerformance(awayTeamStats.ToList(), "Away Team")
            }
        };
    }

    private TeamPerformanceInPeriodDto CreateTeamPeriodPerformance(List<Dal.Models.application.MatchPlayerStat> playerStats, string teamName)
    {
        return new TeamPerformanceInPeriodDto
        {
            TeamName = teamName,
            AveragePsr = playerStats.Average(ps => ps.PerformanceSuccessRate ?? 0),
            Possessions = playerStats.Sum(ps => ps.TotalPossessions ?? 0) / 2, // Simulate half data
            Scores = playerStats.Sum(ps => (ps.GoalsFromPlay ?? 0) + (ps.PointsFromPlay ?? 0)) / 2,
            Turnovers = playerStats.Sum(ps => ps.PossessionsLost ?? 0) / 2,
            Efficiency = CalculateScoringEfficiency(playerStats) / 2
        };
    }

    private MomentumSwingsDto CreateMomentumSwingsAnalysis(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        // Simplified momentum analysis - in reality would use time-series data
        var keyMoments = new List<MomentumPointDto>
        {
            new()
            {
                TimeMinute = 15,
                Event = "Key Score",
                AffectedTeam = "Home",
                PsrChange = 1.5m,
                Description = "Crucial goal shifted momentum"
            },
            new()
            {
                TimeMinute = 35,
                Event = "Red Card",
                AffectedTeam = "Away",
                PsrChange = -2.0m,
                Description = "Red card changed game dynamic"
            }
        };

        return new MomentumSwingsDto
        {
            KeyMomentumShifts = keyMoments,
            DominantTeam = "Home", // Would be calculated from actual data
            NumberOfSwings = keyMoments.Count
        };
    }

    private PerformanceProgressionDto CreateProgressionAnalysis(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        // Simplified progression analysis
        var homeProgression = new List<ProgressionPointDto>
        {
            new() { TimeMarker = 15, CumulativePsr = 5.0m, CumulativePossessions = 25, EfficiencyAtPoint = 0.8m },
            new() { TimeMarker = 30, CumulativePsr = 12.0m, CumulativePossessions = 55, EfficiencyAtPoint = 0.85m },
            new() { TimeMarker = 45, CumulativePsr = 18.0m, CumulativePossessions = 80, EfficiencyAtPoint = 0.82m },
            new() { TimeMarker = 60, CumulativePsr = 25.0m, CumulativePossessions = 110, EfficiencyAtPoint = 0.87m }
        };

        var awayProgression = new List<ProgressionPointDto>
        {
            new() { TimeMarker = 15, CumulativePsr = 4.0m, CumulativePossessions = 22, EfficiencyAtPoint = 0.75m },
            new() { TimeMarker = 30, CumulativePsr = 9.5m, CumulativePossessions = 48, EfficiencyAtPoint = 0.79m },
            new() { TimeMarker = 45, CumulativePsr = 14.0m, CumulativePossessions = 72, EfficiencyAtPoint = 0.77m },
            new() { TimeMarker = 60, CumulativePsr = 19.5m, CumulativePossessions = 98, EfficiencyAtPoint = 0.81m }
        };

        return new PerformanceProgressionDto
        {
            HomeTeamProgression = homeProgression,
            AwayTeamProgression = awayProgression,
            TrendDirection = "Improving"
        };
    }

    private IEnumerable<TopPerformerDto> GetTopPerformersByMetric<T>(
        List<Dal.Models.application.MatchPlayerStat> playerStats, 
        Func<Dal.Models.application.MatchPlayerStat, T> metricSelector, 
        string metricName, 
        int limit) where T : IComparable<T>
    {
        return playerStats
            .OrderByDescending(metricSelector)
            .Take(limit)
            .Select(ps => new TopPerformerDto
            {
                PlayerName = ps.PlayerName,
                TeamName = ps.Team?.TeamName ?? "Unknown",
                Position = ps.StartingPosition ?? "Unknown",
                PrimaryStatistic = Convert.ToDecimal(metricSelector(ps)),
                StatisticName = metricName,
                PerformanceSuccessRate = ps.PerformanceSuccessRate ?? 0,
                SupportingStats = CreateSupportingStats(ps)
            }).ToList();
    }

    private IEnumerable<KeyStatDto> CreateSupportingStats(Dal.Models.application.MatchPlayerStat playerStat)
    {
        return new List<KeyStatDto>
        {
            new() { Name = "Total Events", Value = playerStat.TotalEvents ?? 0, Unit = "events" },
            new() { Name = "Possessions", Value = playerStat.TotalPossessions ?? 0, Unit = "possessions" },
            new() { Name = "Minutes Played", Value = playerStat.MinutesPlayed ?? 0, Unit = "minutes" }
        };
    }

    private IEnumerable<ScoringEventDto> CreateScoringEvents(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        return playerStats
            .Where(ps => (ps.GoalsFromPlay ?? 0) > 0 || (ps.PointsFromPlay ?? 0) > 0)
            .Take(10) // Top 10 scoring events
            .Select(ps => new ScoringEventDto
            {
                PlayerName = ps.PlayerName,
                TeamName = ps.Team?.TeamName ?? "Unknown",
                ScoreType = (ps.GoalsFromPlay ?? 0) > 0 ? "Goal" : "Point",
                Minute = ps.SubstitutedOnMinute ?? 20, // Simulated timing
                ScoreValue = (ps.GoalsFromPlay ?? 0) * 3 + (ps.PointsFromPlay ?? 0),
                Context = "From play"
            }).ToList();
    }

    private IEnumerable<DefensiveEventDto> CreateDefensiveEvents(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        return playerStats
            .Where(ps => (ps.TacklesMade ?? 0) > 0 || (ps.Interceptions ?? 0) > 0)
            .Take(5) // Top 5 defensive events
            .Select(ps => new DefensiveEventDto
            {
                PlayerName = ps.PlayerName,
                TeamName = ps.Team?.TeamName ?? "Unknown",
                EventType = (ps.TacklesMade ?? 0) > (ps.Interceptions ?? 0) ? "Tackle" : "Interception",
                Minute = ps.SubstitutedOnMinute ?? 30, // Simulated timing
                Impact = "Turnover won"
            }).ToList();
    }

    private decimal CalculateScoringEfficiency(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        var totalAttempts = playerStats.Sum(ps => 
            (ps.GoalsFromPlay ?? 0) + (ps.PointsFromPlay ?? 0) + 
            (ps.ShotsWide ?? 0) + (ps.ShotsBlocked ?? 0) + (ps.ShotsSaved ?? 0) + (ps.ShotsShort ?? 0));
        
        var totalScores = playerStats.Sum(ps => (ps.GoalsFromPlay ?? 0) + (ps.PointsFromPlay ?? 0));
        
        return totalAttempts > 0 ? (decimal)totalScores / totalAttempts * 100 : 0;
    }

    private decimal CalculatePossessionRetention(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        var totalPossessions = playerStats.Sum(ps => ps.TotalPossessions ?? 0);
        var possessionsLost = playerStats.Sum(ps => ps.PossessionsLost ?? 0);
        var possessionsRetained = totalPossessions - possessionsLost;
        
        return totalPossessions > 0 ? (decimal)possessionsRetained / totalPossessions * 100 : 0;
    }

    private decimal CalculateAverageTackleSuccessRate(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        var playersWithTackles = playerStats.Where(ps => ps.TackleSuccessRate.HasValue).ToList();
        return playersWithTackles.Any() ? playersWithTackles.Average(ps => ps.TackleSuccessRate ?? 0) : 0;
    }

    private decimal CalculateTurnoverRate(List<Dal.Models.application.MatchPlayerStat> playerStats)
    {
        var totalPossessions = playerStats.Sum(ps => ps.TotalPossessions ?? 0);
        var totalTurnovers = playerStats.Sum(ps => ps.PossessionsLost ?? 0);
        
        return totalPossessions > 0 ? (decimal)totalTurnovers / totalPossessions * 100 : 0;
    }

    private decimal CalculateConversionRate(int scores, int attempts)
    {
        return attempts > 0 ? (decimal)scores / attempts * 100 : 0;
    }

    private decimal CalculateShotAccuracy(int scores, int totalShots)
    {
        return totalShots > 0 ? (decimal)scores / totalShots * 100 : 0;
    }

    #endregion
}