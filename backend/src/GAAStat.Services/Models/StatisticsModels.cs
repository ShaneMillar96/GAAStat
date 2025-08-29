namespace GAAStat.Services.Models;

/// <summary>
/// Comprehensive efficiency metrics calculated for each player
/// </summary>
public class PlayerEfficiencyMetrics
{
    public string PlayerName { get; set; } = string.Empty;
    public int? JerseyNumber { get; set; }
    public decimal PerformanceSuccessRate { get; set; }
    public decimal? EventsPerPsrRatio { get; set; }
    public decimal? PsrPerPossessionRatio { get; set; }
    public decimal? ShotEfficiency { get; set; }
    public decimal? ScoreConversionRate { get; set; }
    public decimal? TackleSuccessRate { get; set; }
    public decimal? PassCompletionRate { get; set; }
    public decimal? PossessionRetentionRate { get; set; }
    public decimal? DefensiveEfficiency { get; set; }
    public decimal? AttackingEfficiency { get; set; }
    public decimal OverallRating { get; set; }
}

/// <summary>
/// Team-level aggregated performance metrics
/// </summary>
public class TeamEfficiencyMetrics
{
    public int MatchId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public int PlayersAnalyzed { get; set; }
    public decimal AveragePerformanceSuccessRate { get; set; }
    public decimal TeamTotalPsr { get; set; }
    public int TotalPossessions { get; set; }
    public int TotalEvents { get; set; }
    public decimal PossessionRetentionRate { get; set; }
    public decimal ScoringEfficiency { get; set; }
    public decimal DefensiveEfficiency { get; set; }
    public int TotalScores { get; set; }
    public int TotalAttempts { get; set; }
    public int TotalTurnovers { get; set; }
    public decimal TurnoverRate { get; set; }
}

/// <summary>
/// Validation result for statistics calculations
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public IEnumerable<string> Errors { get; set; } = new List<string>();
    public IEnumerable<string> Warnings { get; set; } = new List<string>();

    public static ValidationResult Success() => new() { IsValid = true };
    public static ValidationResult Failed(params string[] errors) => new() 
    { 
        IsValid = false, 
        Errors = errors 
    };
    public static ValidationResult Failed(IEnumerable<string> errors) => new() 
    { 
        IsValid = false, 
        Errors = errors 
    };
}

/// <summary>
/// PSR event type definitions based on KPI sheet
/// </summary>
public static class PsrEventTypes
{
    public const decimal KICKOUT = 1.0m;
    public const decimal ATTACKS = 2.0m;
    public const decimal SHOT_FROM_PLAY = 3.0m;
    public const decimal SCOREABLE_FREE = 4.0m;
    public const decimal SCORE_SOURCE = 5.0m;
    public const decimal TACKLE = 6.0m;
    public const decimal FREE_CONCEDED = 7.0m;
    public const decimal POSSESSION_LOST = 8.0m;
    public const decimal BOOKINGS = 9.0m;
    public const decimal POSSESSIONS = 10.0m;
    public const decimal BALL_WON = 11.0m;
    public const decimal SCORE_ASSIST = 12.0m;
    public const decimal GOALKEEPERS = 13.0m;
    public const decimal ATTACK_SOURCE = 14.0m;
    public const decimal SHOT_SOURCE = 15.0m;
    public const decimal FIFTY_METER_FREE = 16.0m;
}

/// <summary>
/// PSR value constants for different outcomes
/// </summary>
public static class PsrValues
{
    // Positive values
    public const decimal GOAL_SCORED = 3.0m;
    public const decimal GOALKEEPER_SAVE = 3.0m;
    public const decimal TWO_POINT_SCORE = 2.0m;
    public const decimal BALL_WON_TACKLE = 2.0m;
    public const decimal GOAL_ASSIST = 2.0m;
    public const decimal POINT_SCORED = 1.0m;
    public const decimal SUCCESSFUL_TACKLE = 1.0m;
    public const decimal POSSESSION = 1.0m;
    public const decimal SCORE_ASSIST = 1.0m;
    
    // Negative values
    public const decimal RED_CARD = -3.0m;
    public const decimal BLACK_CARD = -2.0m;
    public const decimal PENALTY_CONCEDED = -2.0m;
    public const decimal SERIOUS_FOUL = -2.0m;
    public const decimal MISSED_TACKLE = -1.0m;
    public const decimal POSSESSION_LOST = -1.0m;
    public const decimal YELLOW_CARD = -1.0m;
    public const decimal SHORT_SHOT = -1.0m;
    
    // Neutral values
    public const decimal TRACKING_EVENT = 0.0m;
    public const decimal WIDE_SHOT = 0.0m;
    public const decimal QUICK_FREE = 0.0m;
}

/// <summary>
/// Statistical constants for GAA football
/// </summary>
public static class GaaConstants
{
    public const int POINTS_PER_GOAL = 3;
    public const int STANDARD_MATCH_DURATION_MINUTES = 60;
    public const int MAX_JERSEY_NUMBER = 99;
    public const int MIN_JERSEY_NUMBER = 1;
    public const decimal MAX_PSR_VALUE = 3.0m;
    public const decimal MIN_PSR_VALUE = -3.0m;
    public const int EXPECTED_PLAYER_STATS_COLUMNS = 85;
}