namespace GAAStat.Services.Models;

#region Match Analytics DTOs

/// <summary>
/// Comprehensive match summary with team and player statistics
/// </summary>
public class MatchSummaryDto
{
    public int MatchId { get; set; }
    public string HomeTeam { get; set; } = string.Empty;
    public string AwayTeam { get; set; } = string.Empty;
    public DateTime MatchDate { get; set; }
    public string? Venue { get; set; }
    public string Competition { get; set; } = string.Empty;
    public TeamMatchStatsDto HomeTeamStats { get; set; } = new();
    public TeamMatchStatsDto AwayTeamStats { get; set; } = new();
    public MatchKeyMomentsDto KeyMoments { get; set; } = new();
    public int TotalPlayers { get; set; }
    public TimeSpan MatchDuration { get; set; }
}

/// <summary>
/// Team statistics within a match context
/// </summary>
public class TeamMatchStatsDto
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public int PlayersAnalyzed { get; set; }
    public decimal AveragePsr { get; set; }
    public decimal TeamTotalPsr { get; set; }
    public int TotalPossessions { get; set; }
    public int TotalEvents { get; set; }
    public int TotalScores { get; set; }
    public int TotalGoals { get; set; }
    public int TotalPoints { get; set; }
    public decimal ScoringEfficiency { get; set; }
    public decimal PossessionRetention { get; set; }
    public int TotalTackles { get; set; }
    public decimal TackleSuccessRate { get; set; }
    public int TotalTurnovers { get; set; }
    public decimal TurnoverRate { get; set; }
}

/// <summary>
/// Match team comparison side-by-side
/// </summary>
public class MatchTeamComparisonDto
{
    public int MatchId { get; set; }
    public TeamMatchStatsDto HomeTeam { get; set; } = new();
    public TeamMatchStatsDto AwayTeam { get; set; } = new();
    public PerformanceDifferentialsDto Differentials { get; set; } = new();
    public string MatchWinner { get; set; } = string.Empty;
    public IEnumerable<KeyStatComparisonDto> KeyStatistics { get; set; } = [];
}

/// <summary>
/// Performance differentials between teams
/// </summary>
public class PerformanceDifferentialsDto
{
    public decimal PsrDifference { get; set; }
    public decimal PossessionDifference { get; set; }
    public decimal ScoringEfficiencyDifference { get; set; }
    public decimal TackleSuccessRateDifference { get; set; }
    public int TotalScoreDifference { get; set; }
}

/// <summary>
/// Key statistic comparison between teams
/// </summary>
public class KeyStatComparisonDto
{
    public string StatisticName { get; set; } = string.Empty;
    public decimal HomeValue { get; set; }
    public decimal AwayValue { get; set; }
    public decimal Difference { get; set; }
    public string WinningTeam { get; set; } = string.Empty;
}

/// <summary>
/// Key moments and highlights from a match
/// </summary>
public class MatchKeyMomentsDto
{
    public IEnumerable<TopPerformerDto> TopPerformers { get; set; } = [];
    public IEnumerable<ScoringEventDto> KeyScores { get; set; } = [];
    public IEnumerable<DefensiveEventDto> KeyDefensiveActions { get; set; } = [];
    public int TotalCards { get; set; }
    public int RedCards { get; set; }
    public int BlackCards { get; set; }
    public int YellowCards { get; set; }
}

/// <summary>
/// Kickout analysis for a match
/// </summary>
public class KickoutAnalysisDto
{
    public int MatchId { get; set; }
    public IEnumerable<TeamKickoutStatsDto> TeamKickouts { get; set; } = [];
    public KickoutDirectionAnalysisDto DirectionAnalysis { get; set; } = new();
    public KickoutRetentionAnalysisDto RetentionAnalysis { get; set; } = new();
    public decimal OverallRetentionRate { get; set; }
    public int TotalKickouts { get; set; }
}

/// <summary>
/// Team-specific kickout statistics
/// </summary>
public class TeamKickoutStatsDto
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public int TotalKickouts { get; set; }
    public int SuccessfulKickouts { get; set; }
    public decimal RetentionRate { get; set; }
    public IEnumerable<PlayerKickoutStatsDto> PlayerKickouts { get; set; } = [];
}

/// <summary>
/// Individual player kickout statistics
/// </summary>
public class PlayerKickoutStatsDto
{
    public string PlayerName { get; set; } = string.Empty;
    public int TotalKickouts { get; set; }
    public int SuccessfulKickouts { get; set; }
    public decimal SuccessRate { get; set; }
    public string PreferredDirection { get; set; } = string.Empty;
}

/// <summary>
/// Kickout direction analysis
/// </summary>
public class KickoutDirectionAnalysisDto
{
    public int ShortKickouts { get; set; }
    public int LongKickouts { get; set; }
    public int LeftSideKickouts { get; set; }
    public int RightSideKickouts { get; set; }
    public int CentralKickouts { get; set; }
    public decimal ShortKickoutSuccessRate { get; set; }
    public decimal LongKickoutSuccessRate { get; set; }
}

/// <summary>
/// Kickout retention analysis
/// </summary>
public class KickoutRetentionAnalysisDto
{
    public int RetainedKickouts { get; set; }
    public int LostKickouts { get; set; }
    public decimal RetentionPercentage { get; set; }
    public string MostEffectiveStrategy { get; set; } = string.Empty;
    public string LeastEffectiveStrategy { get; set; } = string.Empty;
}

/// <summary>
/// Shot analysis for a match
/// </summary>
public class ShotAnalysisDto
{
    public int MatchId { get; set; }
    public IEnumerable<TeamShotStatsDto> TeamShotStats { get; set; } = [];
    public ShotConversionAnalysisDto ConversionAnalysis { get; set; } = new();
    public ShotLocationAnalysisDto LocationAnalysis { get; set; } = new();
    public int TotalShots { get; set; }
    public int TotalScores { get; set; }
    public decimal OverallConversionRate { get; set; }
}

/// <summary>
/// Team shot statistics
/// </summary>
public class TeamShotStatsDto
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public int TotalShots { get; set; }
    public int Goals { get; set; }
    public int Points { get; set; }
    public int ShotsWide { get; set; }
    public int ShotsBlocked { get; set; }
    public int ShotsSaved { get; set; }
    public int ShotsShort { get; set; }
    public decimal ConversionRate { get; set; }
    public decimal ShotAccuracy { get; set; }
}

/// <summary>
/// Shot conversion analysis
/// </summary>
public class ShotConversionAnalysisDto
{
    public decimal GoalConversionRate { get; set; }
    public decimal PointConversionRate { get; set; }
    public decimal FreeConversionRate { get; set; }
    public decimal PlayConversionRate { get; set; }
    public IEnumerable<TopScorerInMatchDto> TopScorers { get; set; } = [];
}

/// <summary>
/// Top scorer in a specific match
/// </summary>
public class TopScorerInMatchDto
{
    public string PlayerName { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public int Goals { get; set; }
    public int Points { get; set; }
    public int TotalScore { get; set; }
    public decimal ShotEfficiency { get; set; }
}

/// <summary>
/// Shot location analysis
/// </summary>
public class ShotLocationAnalysisDto
{
    public int ShotsFromPlay { get; set; }
    public int ShotsFromFrees { get; set; }
    public decimal PlayShotSuccessRate { get; set; }
    public decimal FreeShotSuccessRate { get; set; }
}

/// <summary>
/// Match momentum analysis
/// </summary>
public class MatchMomentumDto
{
    public int MatchId { get; set; }
    public IEnumerable<PeriodPerformanceDto> PeriodPerformance { get; set; } = [];
    public MomentumSwingsDto MomentumSwings { get; set; } = new();
    public PerformanceProgressionDto ProgressionAnalysis { get; set; } = new();
}

/// <summary>
/// Performance by time period
/// </summary>
public class PeriodPerformanceDto
{
    public string Period { get; set; } = string.Empty; // "First Half", "Second Half", etc.
    public TeamPerformanceInPeriodDto HomeTeamPerformance { get; set; } = new();
    public TeamPerformanceInPeriodDto AwayTeamPerformance { get; set; } = new();
}

/// <summary>
/// Team performance in a specific period
/// </summary>
public class TeamPerformanceInPeriodDto
{
    public string TeamName { get; set; } = string.Empty;
    public decimal AveragePsr { get; set; }
    public int Possessions { get; set; }
    public int Scores { get; set; }
    public int Turnovers { get; set; }
    public decimal Efficiency { get; set; }
}

/// <summary>
/// Momentum swings during the match
/// </summary>
public class MomentumSwingsDto
{
    public IEnumerable<MomentumPointDto> KeyMomentumShifts { get; set; } = [];
    public string DominantTeam { get; set; } = string.Empty;
    public int NumberOfSwings { get; set; }
}

/// <summary>
/// Specific momentum shift point
/// </summary>
public class MomentumPointDto
{
    public int TimeMinute { get; set; }
    public string Event { get; set; } = string.Empty;
    public string AffectedTeam { get; set; } = string.Empty;
    public decimal PsrChange { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Performance progression throughout match
/// </summary>
public class PerformanceProgressionDto
{
    public IEnumerable<ProgressionPointDto> HomeTeamProgression { get; set; } = [];
    public IEnumerable<ProgressionPointDto> AwayTeamProgression { get; set; } = [];
    public string TrendDirection { get; set; } = string.Empty;
}

/// <summary>
/// Performance progression point
/// </summary>
public class ProgressionPointDto
{
    public int TimeMarker { get; set; }
    public decimal CumulativePsr { get; set; }
    public int CumulativePossessions { get; set; }
    public decimal EfficiencyAtPoint { get; set; }
}

/// <summary>
/// Top performers in a match
/// </summary>
public class MatchTopPerformersDto
{
    public int MatchId { get; set; }
    public IEnumerable<TopPerformerDto> TopPsrPerformers { get; set; } = [];
    public IEnumerable<TopPerformerDto> TopScorers { get; set; } = [];
    public IEnumerable<TopPerformerDto> TopDefenders { get; set; } = [];
    public IEnumerable<TopPerformerDto> TopDistributors { get; set; } = [];
    public TopPerformerDto ManOfTheMatch { get; set; } = new();
}

/// <summary>
/// Individual top performer details
/// </summary>
public class TopPerformerDto
{
    public string PlayerName { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public decimal PrimaryStatistic { get; set; }
    public string StatisticName { get; set; } = string.Empty;
    public decimal PerformanceSuccessRate { get; set; }
    public IEnumerable<KeyStatDto> SupportingStats { get; set; } = [];
}

/// <summary>
/// Key supporting statistic
/// </summary>
public class KeyStatDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Unit { get; set; } = string.Empty;
}

#endregion

#region Player Analytics DTOs

/// <summary>
/// Comprehensive player performance across a season
/// </summary>
public class PlayerPerformanceDto
{
    public string PlayerName { get; set; } = string.Empty;
    public int SeasonId { get; set; }
    public string SeasonName { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public int MatchesPlayed { get; set; }
    public int TotalMinutesPlayed { get; set; }
    public PlayerSeasonStatsDto SeasonStats { get; set; } = new();
    public PlayerTrendDataDto Trends { get; set; } = new();
    public PlayerComparisonDataDto Comparisons { get; set; } = new();
}

/// <summary>
/// Player season statistics summary
/// </summary>
public class PlayerSeasonStatsDto
{
    public decimal AveragePsr { get; set; }
    public decimal TotalPsr { get; set; }
    public int TotalPossessions { get; set; }
    public int TotalEvents { get; set; }
    public int TotalGoals { get; set; }
    public int TotalPoints { get; set; }
    public int TotalScores { get; set; }
    public decimal ScoringRate { get; set; }
    public int TotalTackles { get; set; }
    public decimal TackleSuccessRate { get; set; }
    public int TotalInterceptions { get; set; }
    public int TotalTurnoversWon { get; set; }
    public int TotalPossessionsLost { get; set; }
    public decimal PossessionRetentionRate { get; set; }
    public decimal OverallEfficiencyRating { get; set; }
}

/// <summary>
/// Player trend data over time
/// </summary>
public class PlayerTrendDataDto
{
    public IEnumerable<PerformanceDataPointDto> PsrTrend { get; set; } = [];
    public IEnumerable<PerformanceDataPointDto> ScoringTrend { get; set; } = [];
    public IEnumerable<PerformanceDataPointDto> EfficiencyTrend { get; set; } = [];
    public string OverallTrendDirection { get; set; } = string.Empty;
    public decimal TrendSlope { get; set; }
}

/// <summary>
/// Performance data point for trending
/// </summary>
public class PerformanceDataPointDto
{
    public DateTime Date { get; set; }
    public int MatchId { get; set; }
    public string Opponent { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Context { get; set; } = string.Empty;
}

/// <summary>
/// Player comparison data
/// </summary>
public class PlayerComparisonDataDto
{
    public decimal TeamAveragePsr { get; set; }
    public decimal PositionAveragePsr { get; set; }
    public decimal LeagueAveragePsr { get; set; }
    public string PerformanceLevel { get; set; } = string.Empty; // "Excellent", "Above Average", etc.
    public IEnumerable<ComparisonMetricDto> ComparisonMetrics { get; set; } = [];
}

/// <summary>
/// Individual comparison metric
/// </summary>
public class ComparisonMetricDto
{
    public string MetricName { get; set; } = string.Empty;
    public decimal PlayerValue { get; set; }
    public decimal ComparisonValue { get; set; }
    public string ComparisonType { get; set; } = string.Empty; // "Team", "Position", "League"
    public decimal PercentageDifference { get; set; }
    public string PerformanceIndicator { get; set; } = string.Empty;
}

/// <summary>
/// Player efficiency rating and metrics
/// </summary>
public class PlayerEfficiencyDto
{
    public string PlayerName { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public decimal OverallEfficiencyRating { get; set; }
    public decimal AttackingRating { get; set; }
    public decimal DefensiveRating { get; set; }
    public decimal PassingRating { get; set; }
    public PlayerEfficiencyBreakdownDto EfficiencyBreakdown { get; set; } = new();
    public IEnumerable<EfficiencyTrendPointDto> EfficiencyTrends { get; set; } = [];
}

/// <summary>
/// Detailed efficiency breakdown
/// </summary>
public class PlayerEfficiencyBreakdownDto
{
    public decimal ScoringEfficiency { get; set; }
    public decimal PassingEfficiency { get; set; }
    public decimal TacklingEfficiency { get; set; }
    public decimal PossessionEfficiency { get; set; }
    public decimal GameImpactRating { get; set; }
    public decimal ConsistencyRating { get; set; }
    public IEnumerable<StrengthWeaknessDto> Strengths { get; set; } = [];
    public IEnumerable<StrengthWeaknessDto> AreasForImprovement { get; set; } = [];
}

/// <summary>
/// Player strength or weakness
/// </summary>
public class StrengthWeaknessDto
{
    public string Category { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal ComparisonToAverage { get; set; }
}

/// <summary>
/// Efficiency trend point
/// </summary>
public class EfficiencyTrendPointDto
{
    public DateTime Date { get; set; }
    public decimal EfficiencyRating { get; set; }
    public string PerformanceContext { get; set; } = string.Empty;
}

/// <summary>
/// Player vs team comparison
/// </summary>
public class PlayerTeamComparisonDto
{
    public string PlayerName { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public int SeasonId { get; set; }
    public PlayerTeamStatsDto PlayerStats { get; set; } = new();
    public PlayerTeamStatsDto TeamAverages { get; set; } = new();
    public IEnumerable<ComparisonMetricDto> StatisticalComparisons { get; set; } = [];
    public string OverallAssessment { get; set; } = string.Empty;
}

/// <summary>
/// Player vs team statistics
/// </summary>
public class PlayerTeamStatsDto
{
    public decimal AveragePsr { get; set; }
    public decimal ScoringRate { get; set; }
    public decimal TackleSuccessRate { get; set; }
    public decimal PassingAccuracy { get; set; }
    public decimal PossessionRetention { get; set; }
    public decimal GameImpact { get; set; }
}

/// <summary>
/// Player performance trends
/// </summary>
public class PlayerTrendsDto
{
    public string PlayerName { get; set; } = string.Empty;
    public int SeasonId { get; set; }
    public IEnumerable<TrendAnalysisDto> TrendAnalyses { get; set; } = [];
    public PerformanceConsistencyDto ConsistencyAnalysis { get; set; } = new();
    public SeasonalPatternDto SeasonalPatterns { get; set; } = new();
}

/// <summary>
/// Trend analysis for specific metric
/// </summary>
public class TrendAnalysisDto
{
    public string MetricName { get; set; } = string.Empty;
    public IEnumerable<PerformanceDataPointDto> DataPoints { get; set; } = [];
    public string TrendDirection { get; set; } = string.Empty;
    public decimal TrendStrength { get; set; }
    public string TrendDescription { get; set; } = string.Empty;
}

/// <summary>
/// Performance consistency analysis
/// </summary>
public class PerformanceConsistencyDto
{
    public decimal ConsistencyRating { get; set; }
    public decimal StandardDeviation { get; set; }
    public decimal PerformanceVariance { get; set; }
    public int ConsistentPerformances { get; set; }
    public int OutlierPerformances { get; set; }
    public string ConsistencyAssessment { get; set; } = string.Empty;
}

/// <summary>
/// Seasonal performance patterns
/// </summary>
public class SeasonalPatternDto
{
    public string BestPerformancePeriod { get; set; } = string.Empty;
    public string WeakestPerformancePeriod { get; set; } = string.Empty;
    public IEnumerable<PatternObservationDto> Patterns { get; set; } = [];
}

/// <summary>
/// Pattern observation
/// </summary>
public class PatternObservationDto
{
    public string PatternType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
}

/// <summary>
/// Player opposition analysis
/// </summary>
public class PlayerOppositionAnalysisDto
{
    public string PlayerName { get; set; } = string.Empty;
    public int SeasonId { get; set; }
    public IEnumerable<OppositionPerformanceDto> OppositionPerformances { get; set; } = [];
    public OppositionSummaryDto OppositionSummary { get; set; } = new();
}

/// <summary>
/// Performance against specific opposition
/// </summary>
public class OppositionPerformanceDto
{
    public string OpponentName { get; set; } = string.Empty;
    public int MatchesPlayed { get; set; }
    public decimal AveragePsr { get; set; }
    public decimal ScoringRate { get; set; }
    public decimal PerformanceRating { get; set; }
    public string BestPerformance { get; set; } = string.Empty;
    public DateTime BestPerformanceDate { get; set; }
}

/// <summary>
/// Opposition performance summary
/// </summary>
public class OppositionSummaryDto
{
    public string StrongestAgainst { get; set; } = string.Empty;
    public string WeakestAgainst { get; set; } = string.Empty;
    public decimal AveragePerformanceRating { get; set; }
    public string PerformancePattern { get; set; } = string.Empty;
}

/// <summary>
/// Player venue analysis (home vs away)
/// </summary>
public class PlayerVenueAnalysisDto
{
    public string PlayerName { get; set; } = string.Empty;
    public int SeasonId { get; set; }
    public VenuePerformanceDto HomePerformance { get; set; } = new();
    public VenuePerformanceDto AwayPerformance { get; set; } = new();
    public VenueComparisonDto VenueComparison { get; set; } = new();
}

/// <summary>
/// Performance at specific venue type
/// </summary>
public class VenuePerformanceDto
{
    public string VenueType { get; set; } = string.Empty; // "Home", "Away"
    public int MatchesPlayed { get; set; }
    public decimal AveragePsr { get; set; }
    public decimal ScoringRate { get; set; }
    public decimal PerformanceRating { get; set; }
    public PlayerSeasonStatsDto DetailedStats { get; set; } = new();
}

/// <summary>
/// Venue performance comparison
/// </summary>
public class VenueComparisonDto
{
    public decimal PerformanceDifference { get; set; }
    public string BetterVenue { get; set; } = string.Empty;
    public decimal DifferenceSignificance { get; set; }
    public string Analysis { get; set; } = string.Empty;
}

/// <summary>
/// Player cumulative season statistics
/// </summary>
public class PlayerCumulativeStatsDto
{
    public string PlayerName { get; set; } = string.Empty;
    public int SeasonId { get; set; }
    public string SeasonName { get; set; } = string.Empty;
    public CumulativeStatsDto CumulativeStats { get; set; } = new();
    public IEnumerable<MatchContributionDto> MatchContributions { get; set; } = [];
    public RankingsDto Rankings { get; set; } = new();
}

/// <summary>
/// Cumulative statistics totals
/// </summary>
public class CumulativeStatsDto
{
    public int TotalMatches { get; set; }
    public int TotalMinutes { get; set; }
    public decimal TotalPsr { get; set; }
    public decimal AveragePsr { get; set; }
    public int TotalPossessions { get; set; }
    public int TotalEvents { get; set; }
    public int TotalGoals { get; set; }
    public int TotalPoints { get; set; }
    public int TotalTackles { get; set; }
    public int SuccessfulTackles { get; set; }
    public decimal TackleSuccessRate { get; set; }
    public int TotalInterceptions { get; set; }
    public int TotalTurnovers { get; set; }
    public int TotalAssists { get; set; }
    public int TotalCards { get; set; }
}

/// <summary>
/// Individual match contribution
/// </summary>
public class MatchContributionDto
{
    public int MatchId { get; set; }
    public DateTime MatchDate { get; set; }
    public string Opponent { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;
    public decimal Psr { get; set; }
    public int Goals { get; set; }
    public int Points { get; set; }
    public decimal PerformanceRating { get; set; }
    public string PerformanceLevel { get; set; } = string.Empty;
}

/// <summary>
/// Player rankings in various categories
/// </summary>
public class RankingsDto
{
    public int PsrRanking { get; set; }
    public int ScoringRanking { get; set; }
    public int TacklingRanking { get; set; }
    public int OverallRanking { get; set; }
    public string PositionRanking { get; set; } = string.Empty;
    public string TeamRanking { get; set; } = string.Empty;
}

#endregion

#region Team Analytics DTOs

/// <summary>
/// Comprehensive team season statistics
/// </summary>
public class TeamSeasonStatisticsDto
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public int SeasonId { get; set; }
    public string SeasonName { get; set; } = string.Empty;
    public TeamOverallStatsDto OverallStats { get; set; } = new();
    public TeamPerformanceMetricsDto PerformanceMetrics { get; set; } = new();
    public TeamRosterStatsDto RosterStats { get; set; } = new();
    public TeamTrendAnalysisDto TrendAnalysis { get; set; } = new();
}

/// <summary>
/// Team overall statistics
/// </summary>
public class TeamOverallStatsDto
{
    public int MatchesPlayed { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Draws { get; set; }
    public decimal WinPercentage { get; set; }
    public decimal AveragePsr { get; set; }
    public decimal TotalPsr { get; set; }
    public int TotalGoals { get; set; }
    public int TotalPoints { get; set; }
    public int TotalScores { get; set; }
    public decimal ScoringAverage { get; set; }
    public int GoalsAgainst { get; set; }
    public int PointsAgainst { get; set; }
    public decimal DefensiveAverage { get; set; }
}

/// <summary>
/// Team performance metrics
/// </summary>
public class TeamPerformanceMetricsDto
{
    public decimal OffensiveEfficiency { get; set; }
    public decimal DefensiveEfficiency { get; set; }
    public decimal PossessionRetentionRate { get; set; }
    public decimal TurnoverRate { get; set; }
    public decimal ScoringEfficiency { get; set; }
    public decimal TackleSuccessRate { get; set; }
    public decimal OverallTeamRating { get; set; }
    public string PerformanceGrade { get; set; } = string.Empty;
}

/// <summary>
/// Team roster statistics
/// </summary>
public class TeamRosterStatsDto
{
    public int TotalPlayers { get; set; }
    public int RegularStarters { get; set; }
    public int PlayersUsed { get; set; }
    public decimal AveragePlayerPsr { get; set; }
    public IEnumerable<PlayerContributionDto> TopContributors { get; set; } = [];
    public RosterDepthAnalysisDto DepthAnalysis { get; set; } = new();
}

/// <summary>
/// Individual player contribution to team
/// </summary>
public class PlayerContributionDto
{
    public string PlayerName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public int MatchesPlayed { get; set; }
    public decimal ContributionPercentage { get; set; }
    public decimal AveragePsr { get; set; }
    public int TotalScores { get; set; }
    public string Role { get; set; } = string.Empty; // "Key Player", "Regular", "Squad Player"
}

/// <summary>
/// Roster depth analysis
/// </summary>
public class RosterDepthAnalysisDto
{
    public string DepthRating { get; set; } = string.Empty;
    public IEnumerable<PositionalDepthDto> PositionalDepth { get; set; } = [];
    public string DepthConcerns { get; set; } = string.Empty;
    public string DepthStrengths { get; set; } = string.Empty;
}

/// <summary>
/// Positional depth information
/// </summary>
public class PositionalDepthDto
{
    public string Position { get; set; } = string.Empty;
    public int PlayersAvailable { get; set; }
    public string DepthLevel { get; set; } = string.Empty; // "Strong", "Adequate", "Weak"
    public decimal AverageQuality { get; set; }
}

/// <summary>
/// Team trend analysis
/// </summary>
public class TeamTrendAnalysisDto
{
    public IEnumerable<TeamPerformanceTrendDto> PerformanceTrends { get; set; } = [];
    public string OverallTrendDirection { get; set; } = string.Empty;
    public decimal TrendStrength { get; set; }
    public string TrendAnalysis { get; set; } = string.Empty;
}

/// <summary>
/// Team performance trend data
/// </summary>
public class TeamPerformanceTrendDto
{
    public string MetricName { get; set; } = string.Empty;
    public IEnumerable<TeamDataPointDto> DataPoints { get; set; } = [];
    public string TrendDirection { get; set; } = string.Empty;
    public decimal TrendSlope { get; set; }
}

/// <summary>
/// Team data point for trending
/// </summary>
public class TeamDataPointDto
{
    public DateTime Date { get; set; }
    public int MatchId { get; set; }
    public string Opponent { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Result { get; set; } = string.Empty;
}

/// <summary>
/// Team comparison against specific opponent
/// </summary>
public class TeamComparisonDto
{
    public int TeamId { get; set; }
    public int OpponentId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string OpponentName { get; set; } = string.Empty;
    public HeadToHeadStatsDto HeadToHeadStats { get; set; } = new();
    public TeamComparisonMetricsDto ComparisonMetrics { get; set; } = new();
    public IEnumerable<HistoricalMatchupDto> HistoricalMatchups { get; set; } = [];
}

/// <summary>
/// Head-to-head statistics
/// </summary>
public class HeadToHeadStatsDto
{
    public int TotalMeetings { get; set; }
    public int TeamWins { get; set; }
    public int OpponentWins { get; set; }
    public int Draws { get; set; }
    public decimal TeamWinPercentage { get; set; }
    public int TeamTotalScores { get; set; }
    public int OpponentTotalScores { get; set; }
    public decimal AverageMargin { get; set; }
    public string RecentForm { get; set; } = string.Empty;
}

/// <summary>
/// Team comparison metrics
/// </summary>
public class TeamComparisonMetricsDto
{
    public decimal TeamAveragePsr { get; set; }
    public decimal OpponentAveragePsr { get; set; }
    public decimal PsrAdvantage { get; set; }
    public decimal TeamScoringAverage { get; set; }
    public decimal OpponentScoringAverage { get; set; }
    public decimal ScoringAdvantage { get; set; }
    public string OverallAdvantage { get; set; } = string.Empty;
    public IEnumerable<ComparisonMetricDto> DetailedComparisons { get; set; } = [];
}

/// <summary>
/// Historical matchup details
/// </summary>
public class HistoricalMatchupDto
{
    public int MatchId { get; set; }
    public DateTime MatchDate { get; set; }
    public string Venue { get; set; } = string.Empty;
    public int TeamScore { get; set; }
    public int OpponentScore { get; set; }
    public string Result { get; set; } = string.Empty;
    public decimal TeamPsr { get; set; }
    public decimal OpponentPsr { get; set; }
    public string KeyPoints { get; set; } = string.Empty;
}

/// <summary>
/// Team offensive statistics
/// </summary>
public class TeamOffensiveStatsDto
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public int SeasonId { get; set; }
    public OffensiveMetricsDto OffensiveMetrics { get; set; } = new();
    public ScoringAnalysisDto ScoringAnalysis { get; set; } = new();
    public AttackingPlayAnalysisDto AttackingPlayAnalysis { get; set; } = new();
}

/// <summary>
/// Offensive performance metrics
/// </summary>
public class OffensiveMetricsDto
{
    public decimal ScoringEfficiency { get; set; }
    public decimal ShotConversionRate { get; set; }
    public decimal AttackingPsrAverage { get; set; }
    public int TotalAttackingPlays { get; set; }
    public int SuccessfulAttacks { get; set; }
    public decimal AttackSuccessRate { get; set; }
    public decimal GoalConversionRate { get; set; }
    public decimal PointConversionRate { get; set; }
}

/// <summary>
/// Detailed scoring analysis
/// </summary>
public class ScoringAnalysisDto
{
    public int TotalGoals { get; set; }
    public int TotalPoints { get; set; }
    public int GoalsFromPlay { get; set; }
    public int GoalsFromFrees { get; set; }
    public int PointsFromPlay { get; set; }
    public int PointsFromFrees { get; set; }
    public decimal GoalsPerMatch { get; set; }
    public decimal PointsPerMatch { get; set; }
    public IEnumerable<TopScorerDto> TopScorers { get; set; } = [];
}

/// <summary>
/// Top scorer information
/// </summary>
public class TopScorerDto
{
    public string PlayerName { get; set; } = string.Empty;
    public int Goals { get; set; }
    public int Points { get; set; }
    public int TotalScore { get; set; }
    public decimal ScoringRate { get; set; }
    public decimal ShotEfficiency { get; set; }
    public string Position { get; set; } = string.Empty;
}

/// <summary>
/// Attacking play analysis
/// </summary>
public class AttackingPlayAnalysisDto
{
    public decimal PossessionRetentionInAttack { get; set; }
    public decimal AttackingPassAccuracy { get; set; }
    public int TotalAttackingPossessions { get; set; }
    public int ScoringPossessions { get; set; }
    public decimal PossessionToScoreRatio { get; set; }
    public string AttackingStyle { get; set; } = string.Empty;
    public IEnumerable<AttackPatternDto> AttackPatterns { get; set; } = [];
}

/// <summary>
/// Attack pattern analysis
/// </summary>
public class AttackPatternDto
{
    public string PatternType { get; set; } = string.Empty;
    public int Frequency { get; set; }
    public decimal SuccessRate { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Team defensive statistics
/// </summary>
public class TeamDefensiveStatsDto
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public int SeasonId { get; set; }
    public DefensiveMetricsDto DefensiveMetrics { get; set; } = new();
    public TacklingAnalysisDto TacklingAnalysis { get; set; } = new();
    public TurnoverAnalysisDto TurnoverAnalysis { get; set; } = new();
}

/// <summary>
/// Defensive performance metrics
/// </summary>
public class DefensiveMetricsDto
{
    public decimal DefensiveEfficiency { get; set; }
    public decimal TackleSuccessRate { get; set; }
    public decimal DefensivePsrAverage { get; set; }
    public int TotalDefensiveActions { get; set; }
    public int SuccessfulDefensiveActions { get; set; }
    public decimal DefensiveActionSuccessRate { get; set; }
    public decimal GoalsConcededPerMatch { get; set; }
    public decimal PointsConcededPerMatch { get; set; }
}

/// <summary>
/// Tackling analysis
/// </summary>
public class TacklingAnalysisDto
{
    public int TotalTackles { get; set; }
    public int SuccessfulTackles { get; set; }
    public decimal TackleSuccessRate { get; set; }
    public int TacklesWon { get; set; }
    public int TacklesMissed { get; set; }
    public IEnumerable<TopTacklerDto> TopTacklers { get; set; } = [];
    public PositionalTacklingDto PositionalTackling { get; set; } = new();
}

/// <summary>
/// Top tackler information
/// </summary>
public class TopTacklerDto
{
    public string PlayerName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public int TotalTackles { get; set; }
    public decimal TackleSuccessRate { get; set; }
    public int TacklesWon { get; set; }
    public decimal TacklingEfficiency { get; set; }
}

/// <summary>
/// Positional tackling breakdown
/// </summary>
public class PositionalTacklingDto
{
    public IEnumerable<PositionTackleStatsDto> PositionStats { get; set; } = [];
    public string MostEffectivePosition { get; set; } = string.Empty;
    public string LeastEffectivePosition { get; set; } = string.Empty;
}

/// <summary>
/// Position-specific tackle statistics
/// </summary>
public class PositionTackleStatsDto
{
    public string Position { get; set; } = string.Empty;
    public int TotalTackles { get; set; }
    public decimal SuccessRate { get; set; }
    public decimal EfficiencyRating { get; set; }
}

/// <summary>
/// Turnover analysis
/// </summary>
public class TurnoverAnalysisDto
{
    public int TurnoversForced { get; set; }
    public int TurnoversConceeded { get; set; }
    public decimal TurnoverDifferential { get; set; }
    public decimal TurnoverRatio { get; set; }
    public IEnumerable<TurnoverSourceDto> TurnoverSources { get; set; } = [];
}

/// <summary>
/// Source of turnovers
/// </summary>
public class TurnoverSourceDto
{
    public string Source { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// Team possession statistics
/// </summary>
public class TeamPossessionStatsDto
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public int SeasonId { get; set; }
    public PossessionMetricsDto PossessionMetrics { get; set; } = new();
    public DistributionAnalysisDto DistributionAnalysis { get; set; } = new();
    public BallRetentionDto BallRetention { get; set; } = new();
}

/// <summary>
/// Possession performance metrics
/// </summary>
public class PossessionMetricsDto
{
    public int TotalPossessions { get; set; }
    public decimal PossessionRetentionRate { get; set; }
    public decimal AveragePossessionLength { get; set; }
    public decimal PossessionEfficiency { get; set; }
    public int QualityPossessions { get; set; }
    public decimal QualityPossessionPercentage { get; set; }
}

/// <summary>
/// Distribution analysis
/// </summary>
public class DistributionAnalysisDto
{
    public int TotalPasses { get; set; }
    public int SuccessfulPasses { get; set; }
    public decimal PassCompletionRate { get; set; }
    public int KickPasses { get; set; }
    public int HandPasses { get; set; }
    public decimal KickPassSuccessRate { get; set; }
    public decimal HandPassSuccessRate { get; set; }
    public IEnumerable<TopDistributorDto> TopDistributors { get; set; } = [];
}

/// <summary>
/// Top distributor information
/// </summary>
public class TopDistributorDto
{
    public string PlayerName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public int TotalPasses { get; set; }
    public decimal PassCompletionRate { get; set; }
    public decimal DistributionEfficiency { get; set; }
    public string DistributionStyle { get; set; } = string.Empty;
}

/// <summary>
/// Ball retention analysis
/// </summary>
public class BallRetentionDto
{
    public decimal RetentionRate { get; set; }
    public int PossessionsLost { get; set; }
    public IEnumerable<RetentionLossReasonDto> LossReasons { get; set; } = [];
    public string RetentionStrength { get; set; } = string.Empty;
}

/// <summary>
/// Reason for possession loss
/// </summary>
public class RetentionLossReasonDto
{
    public string Reason { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// Team venue analysis
/// </summary>
public class TeamVenueAnalysisDto
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public int SeasonId { get; set; }
    public VenuePerformanceDto HomePerformance { get; set; } = new();
    public VenuePerformanceDto AwayPerformance { get; set; } = new();
    public VenueComparisonDto VenueComparison { get; set; } = new();
    public VenueAdvantageAnalysisDto VenueAdvantage { get; set; } = new();
}

/// <summary>
/// Venue advantage analysis
/// </summary>
public class VenueAdvantageAnalysisDto
{
    public decimal HomeAdvantageRating { get; set; }
    public string VenueStrength { get; set; } = string.Empty;
    public IEnumerable<VenueFactorDto> AdvantageFactors { get; set; } = [];
    public string RecommendedStrategy { get; set; } = string.Empty;
}

/// <summary>
/// Factor contributing to venue advantage
/// </summary>
public class VenueFactorDto
{
    public string Factor { get; set; } = string.Empty;
    public decimal Impact { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Team roster analysis
/// </summary>
public class TeamRosterAnalysisDto
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public int SeasonId { get; set; }
    public RosterCompositionDto RosterComposition { get; set; } = new();
    public PlayerUtilizationDto PlayerUtilization { get; set; } = new();
    public IEnumerable<PlayerContributionAnalysisDto> PlayerContributions { get; set; } = [];
}

/// <summary>
/// Roster composition analysis
/// </summary>
public class RosterCompositionDto
{
    public int TotalPlayers { get; set; }
    public IEnumerable<PositionCountDto> PositionCounts { get; set; } = [];
    public decimal AverageAge { get; set; }
    public decimal AverageExperience { get; set; }
    public string RosterBalance { get; set; } = string.Empty;
}

/// <summary>
/// Count of players by position
/// </summary>
public class PositionCountDto
{
    public string Position { get; set; } = string.Empty;
    public int PlayerCount { get; set; }
    public decimal AverageRating { get; set; }
}

/// <summary>
/// Player utilization analysis
/// </summary>
public class PlayerUtilizationDto
{
    public int RegularStarters { get; set; }
    public int RotationPlayers { get; set; }
    public int SquadPlayers { get; set; }
    public decimal AverageUtilizationRate { get; set; }
    public string UtilizationStrategy { get; set; } = string.Empty;
}

/// <summary>
/// Individual player contribution analysis
/// </summary>
public class PlayerContributionAnalysisDto
{
    public string PlayerName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public decimal ContributionRating { get; set; }
    public decimal ImportanceRating { get; set; }
    public decimal ReliabilityRating { get; set; }
    public string ContributionCategory { get; set; } = string.Empty; // "Star", "Key Player", "Regular", "Squad Player"
    public IEnumerable<ContributionAreaDto> ContributionAreas { get; set; } = [];
}

/// <summary>
/// Area of player contribution
/// </summary>
public class ContributionAreaDto
{
    public string Area { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public string Impact { get; set; } = string.Empty;
}

/// <summary>
/// Team performance trends
/// </summary>
public class TeamTrendsDto
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public int SeasonId { get; set; }
    public IEnumerable<TrendAnalysisDto> PerformanceTrends { get; set; } = [];
    public TrendSummaryDto TrendSummary { get; set; } = new();
    public FuturePredictionDto Predictions { get; set; } = new();
}

/// <summary>
/// Trend summary
/// </summary>
public class TrendSummaryDto
{
    public string OverallDirection { get; set; } = string.Empty;
    public decimal TrendStrength { get; set; }
    public string KeyTrendInsights { get; set; } = string.Empty;
    public IEnumerable<string> ImprovementAreas { get; set; } = [];
    public IEnumerable<string> StrengthAreas { get; set; } = [];
}

/// <summary>
/// Future performance predictions
/// </summary>
public class FuturePredictionDto
{
    public decimal PredictedPerformanceLevel { get; set; }
    public string ConfidenceLevel { get; set; } = string.Empty;
    public IEnumerable<PredictionFactorDto> PredictionFactors { get; set; } = [];
}

/// <summary>
/// Factor affecting future prediction
/// </summary>
public class PredictionFactorDto
{
    public string Factor { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public string Impact { get; set; } = string.Empty;
}

#endregion

#region Season Analytics DTOs

/// <summary>
/// Comprehensive season summary
/// </summary>
public class SeasonSummaryDto
{
    public int SeasonId { get; set; }
    public string SeasonName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public SeasonOverviewDto Overview { get; set; } = new();
    public IEnumerable<TopPerformerCategoryDto> TopPerformers { get; set; } = [];
    public SeasonHighlightsDto Highlights { get; set; } = new();
    public SeasonStatisticsDto Statistics { get; set; } = new();
}

/// <summary>
/// Season overview statistics
/// </summary>
public class SeasonOverviewDto
{
    public int TotalMatches { get; set; }
    public int TotalTeams { get; set; }
    public int TotalPlayers { get; set; }
    public decimal AveragePsr { get; set; }
    public int TotalGoals { get; set; }
    public int TotalPoints { get; set; }
    public decimal AverageMatchScore { get; set; }
    public string MostCompetitiveMatch { get; set; } = string.Empty;
    public string HighestScoringMatch { get; set; } = string.Empty;
}

/// <summary>
/// Top performer category
/// </summary>
public class TopPerformerCategoryDto
{
    public string Category { get; set; } = string.Empty;
    public IEnumerable<SeasonTopPerformerDto> Performers { get; set; } = [];
}

/// <summary>
/// Season top performer
/// </summary>
public class SeasonTopPerformerDto
{
    public string PlayerName { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Achievement { get; set; } = string.Empty;
    public int Ranking { get; set; }
}

/// <summary>
/// Season highlights
/// </summary>
public class SeasonHighlightsDto
{
    public IEnumerable<SeasonRecordDto> Records { get; set; } = [];
    public IEnumerable<NotableAchievementDto> Achievements { get; set; } = [];
    public IEnumerable<StatisticalMilestoneDto> Milestones { get; set; } = [];
}

/// <summary>
/// Season record information
/// </summary>
public class SeasonRecordDto
{
    public string RecordType { get; set; } = string.Empty;
    public string RecordHolder { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime DateAchieved { get; set; }
}

/// <summary>
/// Notable achievement
/// </summary>
public class NotableAchievementDto
{
    public string Achievement { get; set; } = string.Empty;
    public string Player { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Significance { get; set; } = string.Empty;
}

/// <summary>
/// Statistical milestone
/// </summary>
public class StatisticalMilestoneDto
{
    public string Milestone { get; set; } = string.Empty;
    public IEnumerable<string> PlayersAchieved { get; set; } = [];
    public decimal Threshold { get; set; }
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// Season statistics summary
/// </summary>
public class SeasonStatisticsDto
{
    public PlayerStatisticsSummaryDto PlayerStats { get; set; } = new();
    public TeamStatisticsSummaryDto TeamStats { get; set; } = new();
    public CompetitionStatisticsDto CompetitionStats { get; set; } = new();
}

/// <summary>
/// Player statistics summary for season
/// </summary>
public class PlayerStatisticsSummaryDto
{
    public int TotalPlayersParticipated { get; set; }
    public decimal HighestPsr { get; set; }
    public decimal LowestPsr { get; set; }
    public decimal AveragePsr { get; set; }
    public int MostGoalsScored { get; set; }
    public int MostPointsScored { get; set; }
    public decimal HighestScoringRate { get; set; }
    public decimal BestTackleSuccessRate { get; set; }
}

/// <summary>
/// Team statistics summary for season
/// </summary>
public class TeamStatisticsSummaryDto
{
    public decimal HighestTeamPsr { get; set; }
    public decimal LowestTeamPsr { get; set; }
    public decimal AverageTeamPsr { get; set; }
    public int HighestTeamScore { get; set; }
    public int LowestTeamScore { get; set; }
    public decimal BestOffensiveEfficiency { get; set; }
    public decimal BestDefensiveEfficiency { get; set; }
}

/// <summary>
/// Competition statistics
/// </summary>
public class CompetitionStatisticsDto
{
    public IEnumerable<CompetitionSummaryDto> Competitions { get; set; } = [];
    public string MostCompetitiveCompetition { get; set; } = string.Empty;
    public string HighestScoringCompetition { get; set; } = string.Empty;
}

/// <summary>
/// Individual competition summary
/// </summary>
public class CompetitionSummaryDto
{
    public string CompetitionName { get; set; } = string.Empty;
    public int MatchesPlayed { get; set; }
    public int TeamsParticipated { get; set; }
    public decimal AveragePsr { get; set; }
    public decimal AverageScore { get; set; }
}

/// <summary>
/// Season cumulative statistics
/// </summary>
public class SeasonCumulativeStatsDto
{
    public int SeasonId { get; set; }
    public string SeasonName { get; set; } = string.Empty;
    public IEnumerable<PlayerCumulativeStatsDto> PlayerStats { get; set; } = [];
    public IEnumerable<TeamCumulativeStatsDto> TeamStats { get; set; } = [];
    public SeasonAggregatesDto Aggregates { get; set; } = new();
}

/// <summary>
/// Team cumulative statistics for season
/// </summary>
public class TeamCumulativeStatsDto
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public CumulativeStatsDto SeasonTotals { get; set; } = new();
    public TeamSeasonMetricsDto SeasonMetrics { get; set; } = new();
    public int FinalRanking { get; set; }
}

/// <summary>
/// Team season metrics
/// </summary>
public class TeamSeasonMetricsDto
{
    public decimal SeasonPsrAverage { get; set; }
    public decimal WinPercentage { get; set; }
    public decimal OffensiveRating { get; set; }
    public decimal DefensiveRating { get; set; }
    public decimal OverallRating { get; set; }
}

/// <summary>
/// Season aggregates
/// </summary>
public class SeasonAggregatesDto
{
    public int TotalMatches { get; set; }
    public int TotalGoals { get; set; }
    public int TotalPoints { get; set; }
    public int TotalTackles { get; set; }
    public int TotalPossessions { get; set; }
    public decimal AverageMatchPsr { get; set; }
    public decimal TotalSeasonPsr { get; set; }
}

/// <summary>
/// PSR leader information
/// </summary>
public class PsrLeaderDto
{
    public string PlayerName { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public decimal AveragePsr { get; set; }
    public decimal TotalPsr { get; set; }
    public int MatchesPlayed { get; set; }
    public int Ranking { get; set; }
    public decimal ConsistencyRating { get; set; }
    public IEnumerable<PsrTrendPointDto> PsrTrend { get; set; } = [];
}

/// <summary>
/// PSR trend data point
/// </summary>
public class PsrTrendPointDto
{
    public DateTime MatchDate { get; set; }
    public decimal Psr { get; set; }
    public string Opponent { get; set; } = string.Empty;
    public string PerformanceLevel { get; set; } = string.Empty;
}

/// <summary>
/// Season trends analysis
/// </summary>
public class SeasonTrendsDto
{
    public int SeasonId { get; set; }
    public IEnumerable<SeasonTrendCategoryDto> TrendCategories { get; set; } = [];
    public SeasonProgressionDto SeasonProgression { get; set; } = new();
    public TrendInsightsDto TrendInsights { get; set; } = new();
}

/// <summary>
/// Season trend category
/// </summary>
public class SeasonTrendCategoryDto
{
    public string Category { get; set; } = string.Empty;
    public IEnumerable<TrendDataPointDto> TrendPoints { get; set; } = [];
    public string TrendDirection { get; set; } = string.Empty;
    public decimal TrendStrength { get; set; }
    public string TrendDescription { get; set; } = string.Empty;
}

/// <summary>
/// Generic trend data point
/// </summary>
public class TrendDataPointDto
{
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
    public string Context { get; set; } = string.Empty;
}

/// <summary>
/// Season progression analysis
/// </summary>
public class SeasonProgressionDto
{
    public IEnumerable<SeasonPhaseDto> SeasonPhases { get; set; } = [];
    public string OverallProgression { get; set; } = string.Empty;
    public IEnumerable<string> KeyProgressionPoints { get; set; } = [];
}

/// <summary>
/// Season phase information
/// </summary>
public class SeasonPhaseDto
{
    public string Phase { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal AveragePerformance { get; set; }
    public string CharacteristicTrends { get; set; } = string.Empty;
}

/// <summary>
/// Trend insights and analysis
/// </summary>
public class TrendInsightsDto
{
    public IEnumerable<string> KeyInsights { get; set; } = [];
    public IEnumerable<string> EmergingTrends { get; set; } = [];
    public IEnumerable<string> Surprises { get; set; } = [];
    public string OverallAssessment { get; set; } = string.Empty;
}

/// <summary>
/// Multi-season comparison
/// </summary>
public class MultiSeasonComparisonDto
{
    public IEnumerable<int> SeasonIds { get; set; } = [];
    public IEnumerable<SeasonComparisonDto> SeasonComparisons { get; set; } = [];
    public CrossSeasonTrendsDto CrossSeasonTrends { get; set; } = new();
    public SeasonEvolutionDto SeasonEvolution { get; set; } = new();
}

/// <summary>
/// Individual season comparison data
/// </summary>
public class SeasonComparisonDto
{
    public int SeasonId { get; set; }
    public string SeasonName { get; set; } = string.Empty;
    public SeasonMetricsDto Metrics { get; set; } = new();
    public int Ranking { get; set; }
}

/// <summary>
/// Season metrics for comparison
/// </summary>
public class SeasonMetricsDto
{
    public decimal AveragePsr { get; set; }
    public decimal AverageScore { get; set; }
    public decimal CompetitiveBalance { get; set; }
    public int TotalMatches { get; set; }
    public string QualityRating { get; set; } = string.Empty;
}

/// <summary>
/// Cross-season trends
/// </summary>
public class CrossSeasonTrendsDto
{
    public IEnumerable<LongTermTrendDto> LongTermTrends { get; set; } = [];
    public string OverallEvolution { get; set; } = string.Empty;
    public IEnumerable<string> ConsistentPatterns { get; set; } = [];
}

/// <summary>
/// Long-term trend across seasons
/// </summary>
public class LongTermTrendDto
{
    public string TrendCategory { get; set; } = string.Empty;
    public IEnumerable<SeasonTrendPointDto> TrendPoints { get; set; } = [];
    public string TrendDirection { get; set; } = string.Empty;
    public decimal TrendRate { get; set; }
}

/// <summary>
/// Season trend point for cross-season analysis
/// </summary>
public class SeasonTrendPointDto
{
    public int SeasonId { get; set; }
    public string SeasonName { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

/// <summary>
/// Season evolution analysis
/// </summary>
public class SeasonEvolutionDto
{
    public IEnumerable<EvolutionAreaDto> EvolutionAreas { get; set; } = [];
    public string OverallEvolutionSummary { get; set; } = string.Empty;
    public IEnumerable<string> FutureProjections { get; set; } = [];
}

/// <summary>
/// Area of evolution between seasons
/// </summary>
public class EvolutionAreaDto
{
    public string Area { get; set; } = string.Empty;
    public decimal EvolutionRate { get; set; }
    public string EvolutionDescription { get; set; } = string.Empty;
    public string ImpactAssessment { get; set; } = string.Empty;
}

/// <summary>
/// Season league table
/// </summary>
public class SeasonLeagueTableDto
{
    public int SeasonId { get; set; }
    public string SeasonName { get; set; } = string.Empty;
    public string? CompetitionName { get; set; }
    public IEnumerable<LeagueTableEntryDto> LeagueTable { get; set; } = [];
    public LeagueStatisticsDto LeagueStatistics { get; set; } = new();
}

/// <summary>
/// League table entry
/// </summary>
public class LeagueTableEntryDto
{
    public int Position { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public int MatchesPlayed { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Draws { get; set; }
    public int GoalsFor { get; set; }
    public int GoalsAgainst { get; set; }
    public int PointsFor { get; set; }
    public int PointsAgainst { get; set; }
    public int ScoreDifference { get; set; }
    public decimal AveragePsr { get; set; }
    public int Points { get; set; }
    public decimal WinPercentage { get; set; }
}

/// <summary>
/// League statistics
/// </summary>
public class LeagueStatisticsDto
{
    public int TotalTeams { get; set; }
    public int TotalMatches { get; set; }
    public decimal AverageMatchScore { get; set; }
    public decimal CompetitiveBalance { get; set; }
    public string MostCompetitiveMatch { get; set; } = string.Empty;
    public string ChampionTeam { get; set; } = string.Empty;
    public IEnumerable<string> TopPerformingTeams { get; set; } = [];
}

/// <summary>
/// Season statistical leaders
/// </summary>
public class SeasonStatisticalLeadersDto
{
    public int SeasonId { get; set; }
    public string SeasonName { get; set; } = string.Empty;
    public IEnumerable<StatisticalCategoryDto> Categories { get; set; } = [];
    public OverallLeadersDto OverallLeaders { get; set; } = new();
}

/// <summary>
/// Statistical category leaders
/// </summary>
public class StatisticalCategoryDto
{
    public string CategoryName { get; set; } = string.Empty;
    public IEnumerable<StatisticalLeaderDto> Leaders { get; set; } = [];
    public string CategoryDescription { get; set; } = string.Empty;
}

/// <summary>
/// Individual statistical leader
/// </summary>
public class StatisticalLeaderDto
{
    public int Rank { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Achievement { get; set; } = string.Empty;
    public int MatchesPlayed { get; set; }
}

/// <summary>
/// Overall season leaders
/// </summary>
public class OverallLeadersDto
{
    public StatisticalLeaderDto MostValuablePlayer { get; set; } = new();
    public StatisticalLeaderDto TopScorer { get; set; } = new();
    public StatisticalLeaderDto BestDefender { get; set; } = new();
    public StatisticalLeaderDto MostConsistent { get; set; } = new();
    public StatisticalLeaderDto BestNewcomer { get; set; } = new();
}

#endregion

#region Positional Analysis DTOs

/// <summary>
/// Positional performance analysis
/// </summary>
public class PositionalPerformanceDto
{
    public string Position { get; set; } = string.Empty;
    public int SeasonId { get; set; }
    public string SeasonName { get; set; } = string.Empty;
    public PositionalStatisticsDto Statistics { get; set; } = new();
    public IEnumerable<PositionalPlayerDto> Players { get; set; } = [];
    public PositionalBenchmarksDto Benchmarks { get; set; } = new();
    public PositionalTrendsDto Trends { get; set; } = new();
}

/// <summary>
/// Positional statistics summary
/// </summary>
public class PositionalStatisticsDto
{
    public int TotalPlayers { get; set; }
    public decimal AveragePsr { get; set; }
    public decimal HighestPsr { get; set; }
    public decimal LowestPsr { get; set; }
    public decimal StandardDeviation { get; set; }
    public decimal AverageScoringRate { get; set; }
    public decimal AverageTackleSuccessRate { get; set; }
    public decimal AveragePassingAccuracy { get; set; }
    public IEnumerable<KeyPositionalMetricDto> KeyMetrics { get; set; } = [];
}

/// <summary>
/// Key positional metric
/// </summary>
public class KeyPositionalMetricDto
{
    public string MetricName { get; set; } = string.Empty;
    public decimal AverageValue { get; set; }
    public decimal BestValue { get; set; }
    public string BestPlayer { get; set; } = string.Empty;
    public decimal PositionImportance { get; set; }
}

/// <summary>
/// Positional player information
/// </summary>
public class PositionalPlayerDto
{
    public string PlayerName { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public decimal PerformanceRating { get; set; }
    public decimal Psr { get; set; }
    public int PositionRanking { get; set; }
    public IEnumerable<PlayerPositionalStrengthDto> Strengths { get; set; } = [];
    public PlayerPositionalStatsDto PositionalStats { get; set; } = new();
}

/// <summary>
/// Player positional strength
/// </summary>
public class PlayerPositionalStrengthDto
{
    public string Strength { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public string Impact { get; set; } = string.Empty;
}

/// <summary>
/// Player positional statistics
/// </summary>
public class PlayerPositionalStatsDto
{
    public int MatchesPlayed { get; set; }
    public decimal AveragePsr { get; set; }
    public decimal ScoringRate { get; set; }
    public decimal DefensiveRating { get; set; }
    public decimal DistributionRating { get; set; }
    public decimal PositionSpecificRating { get; set; }
}

/// <summary>
/// Positional benchmarks
/// </summary>
public class PositionalBenchmarksDto
{
    public decimal ExcellentThreshold { get; set; }
    public decimal GoodThreshold { get; set; }
    public decimal AverageThreshold { get; set; }
    public decimal BelowAverageThreshold { get; set; }
    public IEnumerable<BenchmarkMetricDto> BenchmarkMetrics { get; set; } = [];
}

/// <summary>
/// Benchmark metric
/// </summary>
public class BenchmarkMetricDto
{
    public string MetricName { get; set; } = string.Empty;
    public decimal Excellent { get; set; }
    public decimal Good { get; set; }
    public decimal Average { get; set; }
    public decimal BelowAverage { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Positional trends
/// </summary>
public class PositionalTrendsDto
{
    public IEnumerable<PositionalTrendDto> Trends { get; set; } = [];
    public string OverallPositionTrend { get; set; } = string.Empty;
    public IEnumerable<string> EmergingPatterns { get; set; } = [];
}

/// <summary>
/// Individual positional trend
/// </summary>
public class PositionalTrendDto
{
    public string TrendName { get; set; } = string.Empty;
    public IEnumerable<TrendDataPointDto> TrendData { get; set; } = [];
    public string TrendDirection { get; set; } = string.Empty;
    public string TrendSignificance { get; set; } = string.Empty;
}

/// <summary>
/// Positional comparison across all positions
/// </summary>
public class PositionalComparisonDto
{
    public int SeasonId { get; set; }
    public string SeasonName { get; set; } = string.Empty;
    public IEnumerable<PositionComparisonDataDto> PositionComparisons { get; set; } = [];
    public PositionRankingDto PositionRankings { get; set; } = new();
    public CrossPositionalInsightsDto CrossPositionalInsights { get; set; } = new();
}

/// <summary>
/// Position comparison data
/// </summary>
public class PositionComparisonDataDto
{
    public string Position { get; set; } = string.Empty;
    public int PlayerCount { get; set; }
    public decimal AveragePsr { get; set; }
    public decimal AverageImpactRating { get; set; }
    public decimal PerformanceConsistency { get; set; }
    public IEnumerable<PositionMetricComparisonDto> MetricComparisons { get; set; } = [];
}

/// <summary>
/// Position metric comparison
/// </summary>
public class PositionMetricComparisonDto
{
    public string MetricName { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public int Ranking { get; set; }
    public decimal RelativeStrength { get; set; }
}

/// <summary>
/// Position rankings
/// </summary>
public class PositionRankingDto
{
    public IEnumerable<RankedPositionDto> PsrRankings { get; set; } = [];
    public IEnumerable<RankedPositionDto> ImpactRankings { get; set; } = [];
    public IEnumerable<RankedPositionDto> ConsistencyRankings { get; set; } = [];
}

/// <summary>
/// Ranked position information
/// </summary>
public class RankedPositionDto
{
    public int Rank { get; set; }
    public string Position { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string PerformanceLevel { get; set; } = string.Empty;
}

/// <summary>
/// Cross-positional insights
/// </summary>
public class CrossPositionalInsightsDto
{
    public IEnumerable<string> KeyFindings { get; set; } = [];
    public string MostInfluentialPosition { get; set; } = string.Empty;
    public string MostConsistentPosition { get; set; } = string.Empty;
    public string MostImprovedPosition { get; set; } = string.Empty;
    public IEnumerable<PositionalRelationshipDto> PositionalRelationships { get; set; } = [];
}

/// <summary>
/// Relationship between positions
/// </summary>
public class PositionalRelationshipDto
{
    public string Position1 { get; set; } = string.Empty;
    public string Position2 { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public decimal CorrelationStrength { get; set; }
    public string Impact { get; set; } = string.Empty;
}

/// <summary>
/// Goalkeeper-specific analysis
/// </summary>
public class GoalkeeperAnalysisDto
{
    public int SeasonId { get; set; }
    public int? TeamId { get; set; }
    public IEnumerable<GoalkeeperPerformanceDto> Goalkeepers { get; set; } = [];
    public GoalkeeperBenchmarksDto Benchmarks { get; set; } = new();
    public GoalkeeperSpecialMetricsDto SpecialMetrics { get; set; } = new();
}

/// <summary>
/// Individual goalkeeper performance
/// </summary>
public class GoalkeeperPerformanceDto
{
    public string PlayerName { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public int MatchesPlayed { get; set; }
    public decimal AveragePsr { get; set; }
    public int SavesMade { get; set; }
    public decimal SavePercentage { get; set; }
    public int KickoutsMade { get; set; }
    public decimal KickoutSuccessRate { get; set; }
    public decimal DistributionAccuracy { get; set; }
    public int CleanSheets { get; set; }
    public decimal GoalsConcededPerMatch { get; set; }
    public decimal OverallRating { get; set; }
}

/// <summary>
/// Goalkeeper benchmarks
/// </summary>
public class GoalkeeperBenchmarksDto
{
    public decimal AverageSavePercentage { get; set; }
    public decimal AverageKickoutSuccess { get; set; }
    public decimal AverageDistributionAccuracy { get; set; }
    public decimal EliteThreshold { get; set; }
    public decimal GoodThreshold { get; set; }
}

/// <summary>
/// Goalkeeper special metrics
/// </summary>
public class GoalkeeperSpecialMetricsDto
{
    public string MostReliableGoalkeeper { get; set; } = string.Empty;
    public string BestDistributor { get; set; } = string.Empty;
    public string MostActive { get; set; } = string.Empty;
    public decimal AverageActionPerMatch { get; set; }
    public IEnumerable<GoalkeeperSpecialtyDto> Specialties { get; set; } = [];
}

/// <summary>
/// Goalkeeper specialty area
/// </summary>
public class GoalkeeperSpecialtyDto
{
    public string Specialty { get; set; } = string.Empty;
    public string BestPerformer { get; set; } = string.Empty;
    public decimal BestValue { get; set; }
    public decimal AverageValue { get; set; }
}

/// <summary>
/// Defender-specific analysis
/// </summary>
public class DefenderAnalysisDto
{
    public int SeasonId { get; set; }
    public int? TeamId { get; set; }
    public IEnumerable<DefenderPerformanceDto> Defenders { get; set; } = [];
    public DefenderBenchmarksDto Benchmarks { get; set; } = new();
    public DefensiveMetricsOverviewDto MetricsOverview { get; set; } = new();
}

/// <summary>
/// Individual defender performance
/// </summary>
public class DefenderPerformanceDto
{
    public string PlayerName { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string SpecificPosition { get; set; } = string.Empty;
    public int MatchesPlayed { get; set; }
    public decimal AveragePsr { get; set; }
    public int TotalTackles { get; set; }
    public decimal TackleSuccessRate { get; set; }
    public int InterceptionsMade { get; set; }
    public int TurnoversWon { get; set; }
    public decimal DefensiveActionsPerMatch { get; set; }
    public decimal DefensiveEfficiencyRating { get; set; }
}

/// <summary>
/// Defender benchmarks
/// </summary>
public class DefenderBenchmarksDto
{
    public decimal AverageTackleSuccessRate { get; set; }
    public decimal AverageInterceptionsPerMatch { get; set; }
    public decimal AverageTurnoversWonPerMatch { get; set; }
    public decimal EliteDefenderThreshold { get; set; }
}

/// <summary>
/// Defensive metrics overview
/// </summary>
public class DefensiveMetricsOverviewDto
{
    public string BestTackler { get; set; } = string.Empty;
    public string MostInterceptions { get; set; } = string.Empty;
    public string BestTurnoverWinner { get; set; } = string.Empty;
    public decimal AverageDefensiveRating { get; set; }
    public IEnumerable<DefensiveSpecialtyDto> DefensiveSpecialties { get; set; } = [];
}

/// <summary>
/// Defensive specialty area
/// </summary>
public class DefensiveSpecialtyDto
{
    public string Specialty { get; set; } = string.Empty;
    public string TopPerformer { get; set; } = string.Empty;
    public decimal TopValue { get; set; }
    public decimal BenchmarkValue { get; set; }
}

/// <summary>
/// Midfielder-specific analysis
/// </summary>
public class MidfielderAnalysisDto
{
    public int SeasonId { get; set; }
    public int? TeamId { get; set; }
    public IEnumerable<MidfielderPerformanceDto> Midfielders { get; set; } = [];
    public MidfielderBenchmarksDto Benchmarks { get; set; } = new();
    public MidfielderMetricsOverviewDto MetricsOverview { get; set; } = new();
}

/// <summary>
/// Individual midfielder performance
/// </summary>
public class MidfielderPerformanceDto
{
    public string PlayerName { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string SpecificPosition { get; set; } = string.Empty;
    public int MatchesPlayed { get; set; }
    public decimal AveragePsr { get; set; }
    public int TotalPossessions { get; set; }
    public decimal PossessionRetentionRate { get; set; }
    public decimal PassCompletionRate { get; set; }
    public decimal DistributionAccuracy { get; set; }
    public int ScoreAssists { get; set; }
    public decimal PlaymakingRating { get; set; }
    public decimal BoxToBoxRating { get; set; }
}

/// <summary>
/// Midfielder benchmarks
/// </summary>
public class MidfielderBenchmarksDto
{
    public decimal AveragePossessionRetention { get; set; }
    public decimal AveragePassCompletion { get; set; }
    public decimal AverageDistributionAccuracy { get; set; }
    public decimal EliteMidfielderThreshold { get; set; }
}

/// <summary>
/// Midfielder metrics overview
/// </summary>
public class MidfielderMetricsOverviewDto
{
    public string BestPlaymaker { get; set; } = string.Empty;
    public string BestDistributor { get; set; } = string.Empty;
    public string MostPossessions { get; set; } = string.Empty;
    public string BestBoxToBox { get; set; } = string.Empty;
    public decimal AveragePlaymakingRating { get; set; }
    public IEnumerable<MidfielderSpecialtyDto> MidfielderSpecialties { get; set; } = [];
}

/// <summary>
/// Midfielder specialty area
/// </summary>
public class MidfielderSpecialtyDto
{
    public string Specialty { get; set; } = string.Empty;
    public string TopPerformer { get; set; } = string.Empty;
    public decimal TopValue { get; set; }
    public decimal BenchmarkValue { get; set; }
}

/// <summary>
/// Forward-specific analysis
/// </summary>
public class ForwardAnalysisDto
{
    public int SeasonId { get; set; }
    public int? TeamId { get; set; }
    public IEnumerable<ForwardPerformanceDto> Forwards { get; set; } = [];
    public ForwardBenchmarksDto Benchmarks { get; set; } = new();
    public ForwardMetricsOverviewDto MetricsOverview { get; set; } = new();
}

/// <summary>
/// Individual forward performance
/// </summary>
public class ForwardPerformanceDto
{
    public string PlayerName { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string SpecificPosition { get; set; } = string.Empty;
    public int MatchesPlayed { get; set; }
    public decimal AveragePsr { get; set; }
    public int TotalGoals { get; set; }
    public int TotalPoints { get; set; }
    public int TotalScores { get; set; }
    public decimal ScoringRate { get; set; }
    public decimal ShotConversionRate { get; set; }
    public decimal AttackingEfficiencyRating { get; set; }
    public int ScoreAssists { get; set; }
    public decimal FinishingRating { get; set; }
}

/// <summary>
/// Forward benchmarks
/// </summary>
public class ForwardBenchmarksDto
{
    public decimal AverageScoringRate { get; set; }
    public decimal AverageShotConversion { get; set; }
    public decimal AverageAttackingEfficiency { get; set; }
    public decimal EliteForwardThreshold { get; set; }
}

/// <summary>
/// Forward metrics overview
/// </summary>
public class ForwardMetricsOverviewDto
{
    public string TopScorer { get; set; } = string.Empty;
    public string BestFinisher { get; set; } = string.Empty;
    public string MostEfficient { get; set; } = string.Empty;
    public string BestAssistProvider { get; set; } = string.Empty;
    public decimal AverageAttackingRating { get; set; }
    public IEnumerable<ForwardSpecialtyDto> ForwardSpecialties { get; set; } = [];
}

/// <summary>
/// Forward specialty area
/// </summary>
public class ForwardSpecialtyDto
{
    public string Specialty { get; set; } = string.Empty;
    public string TopPerformer { get; set; } = string.Empty;
    public decimal TopValue { get; set; }
    public decimal BenchmarkValue { get; set; }
}

/// <summary>
/// Positional PSR benchmarks
/// </summary>
public class PositionalPsrBenchmarksDto
{
    public int SeasonId { get; set; }
    public IEnumerable<PositionPsrBenchmarkDto> PositionBenchmarks { get; set; } = [];
    public OverallBenchmarksDto OverallBenchmarks { get; set; } = new();
}

/// <summary>
/// PSR benchmark for specific position
/// </summary>
public class PositionPsrBenchmarkDto
{
    public string Position { get; set; } = string.Empty;
    public decimal AveragePsr { get; set; }
    public decimal EliteThreshold { get; set; }
    public decimal GoodThreshold { get; set; }
    public decimal AverageThreshold { get; set; }
    public decimal BelowAverageThreshold { get; set; }
    public decimal StandardDeviation { get; set; }
    public int SampleSize { get; set; }
}

/// <summary>
/// Overall PSR benchmarks
/// </summary>
public class OverallBenchmarksDto
{
    public decimal OverallAveragePsr { get; set; }
    public string HighestPerformingPosition { get; set; } = string.Empty;
    public string LowestPerformingPosition { get; set; } = string.Empty;
    public decimal PositionVariance { get; set; }
    public IEnumerable<string> BenchmarkInsights { get; set; } = [];
}

/// <summary>
/// Formation analysis
/// </summary>
public class FormationAnalysisDto
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public int SeasonId { get; set; }
    public IEnumerable<FormationRecommendationDto> FormationRecommendations { get; set; } = [];
    public CurrentFormationAnalysisDto CurrentFormationAnalysis { get; set; } = new();
    public PlayerPositionOptimizationDto PlayerOptimization { get; set; } = new();
}

/// <summary>
/// Formation recommendation
/// </summary>
public class FormationRecommendationDto
{
    public string FormationName { get; set; } = string.Empty;
    public decimal SuitabilityScore { get; set; }
    public IEnumerable<FormationPlayerDto> RecommendedPlayers { get; set; } = [];
    public string Rationale { get; set; } = string.Empty;
    public IEnumerable<string> Advantages { get; set; } = [];
    public IEnumerable<string> Considerations { get; set; } = [];
}

/// <summary>
/// Player recommendation for formation
/// </summary>
public class FormationPlayerDto
{
    public string PlayerName { get; set; } = string.Empty;
    public string RecommendedPosition { get; set; } = string.Empty;
    public decimal PositionSuitability { get; set; }
    public string Reasoning { get; set; } = string.Empty;
}

/// <summary>
/// Current formation analysis
/// </summary>
public class CurrentFormationAnalysisDto
{
    public string CurrentFormation { get; set; } = string.Empty;
    public decimal FormationEffectiveness { get; set; }
    public IEnumerable<FormationStrengthDto> Strengths { get; set; } = [];
    public IEnumerable<FormationWeaknessDto> Weaknesses { get; set; } = [];
    public decimal OptimizationPotential { get; set; }
}

/// <summary>
/// Formation strength
/// </summary>
public class FormationStrengthDto
{
    public string Area { get; set; } = string.Empty;
    public decimal StrengthRating { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Formation weakness
/// </summary>
public class FormationWeaknessDto
{
    public string Area { get; set; } = string.Empty;
    public decimal WeaknessImpact { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ImprovementSuggestion { get; set; } = string.Empty;
}

/// <summary>
/// Player position optimization
/// </summary>
public class PlayerPositionOptimizationDto
{
    public IEnumerable<PlayerPositionRecommendationDto> PositionRecommendations { get; set; } = [];
    public decimal OverallOptimizationGain { get; set; }
    public string OptimizationSummary { get; set; } = string.Empty;
}

/// <summary>
/// Player position recommendation
/// </summary>
public class PlayerPositionRecommendationDto
{
    public string PlayerName { get; set; } = string.Empty;
    public string CurrentPosition { get; set; } = string.Empty;
    public string RecommendedPosition { get; set; } = string.Empty;
    public decimal CurrentPositionRating { get; set; }
    public decimal RecommendedPositionRating { get; set; }
    public decimal ImprovementPotential { get; set; }
    public string Justification { get; set; } = string.Empty;
}

#endregion

#region Scoring Events DTOs

/// <summary>
/// Scoring event details
/// </summary>
public class ScoringEventDto
{
    public string PlayerName { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string ScoreType { get; set; } = string.Empty; // "Goal", "Point", "Free"
    public int Minute { get; set; }
    public int ScoreValue { get; set; }
    public string Context { get; set; } = string.Empty;
}

/// <summary>
/// Defensive event details
/// </summary>
public class DefensiveEventDto
{
    public string PlayerName { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty; // "Tackle", "Interception", "Block"
    public int Minute { get; set; }
    public string Impact { get; set; } = string.Empty;
}

#endregion