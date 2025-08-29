using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.Models.application;

/// <summary>
/// Comprehensive player statistics (85+ columns) cleared on import
/// </summary>
[Table("match_player_stats")]
[Index("MatchId", Name = "idx_player_stats_match")]
[Index("MatchId", "TeamId", Name = "idx_player_stats_match_team")]
[Index("PerformanceSuccessRate", Name = "idx_player_stats_performance", AllDescending = true)]
[Index("PlayerName", Name = "idx_player_stats_player_name")]
[Index("PointsFromPlay", "GoalsFromPlay", Name = "idx_player_stats_scoring", AllDescending = true)]
[Index("TeamId", "MatchId", "PerformanceSuccessRate", Name = "idx_player_stats_season_analysis")]
[Index("TeamId", Name = "idx_player_stats_team")]
[Index("MatchId", "PlayerName", "JerseyNumber", Name = "unique_match_player", IsUnique = true)]
public partial class MatchPlayerStat
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("match_id")]
    public int MatchId { get; set; }

    /// <summary>
    /// Player name stored exactly as appears in Excel import
    /// </summary>
    [Column("player_name")]
    [StringLength(100)]
    public string PlayerName { get; set; } = null!;

    [Column("jersey_number")]
    public int? JerseyNumber { get; set; }

    [Column("team_id")]
    public int TeamId { get; set; }

    [Column("minutes_played")]
    public int? MinutesPlayed { get; set; }

    [Column("total_events")]
    public int? TotalEvents { get; set; }

    /// <summary>
    /// PSR value calculation result (-3.0 to +3.0 range)
    /// </summary>
    [Column("performance_success_rate")]
    [Precision(8, 4)]
    public decimal? PerformanceSuccessRate { get; set; }

    [Column("total_possessions")]
    public int? TotalPossessions { get; set; }

    [Column("turnovers_won")]
    public int? TurnoversWon { get; set; }

    [Column("interceptions")]
    public int? Interceptions { get; set; }

    [Column("possessions_lost")]
    public int? PossessionsLost { get; set; }

    [Column("kick_passes")]
    public int? KickPasses { get; set; }

    [Column("hand_passes")]
    public int? HandPasses { get; set; }

    [Column("kick_pass_success_rate")]
    [Precision(8, 4)]
    public decimal? KickPassSuccessRate { get; set; }

    [Column("hand_pass_success_rate")]
    [Precision(8, 4)]
    public decimal? HandPassSuccessRate { get; set; }

    [Column("tackles_made")]
    public int? TacklesMade { get; set; }

    [Column("tackles_missed")]
    public int? TacklesMissed { get; set; }

    [Column("tackle_success_rate")]
    [Precision(8, 4)]
    public decimal? TackleSuccessRate { get; set; }

    [Column("frees_won")]
    public int? FreesWon { get; set; }

    [Column("frees_conceded")]
    public int? FreesConceded { get; set; }

    [Column("cards_yellow")]
    public int? CardsYellow { get; set; }

    [Column("cards_black")]
    public int? CardsBlack { get; set; }

    [Column("cards_red")]
    public int? CardsRed { get; set; }

    [Column("points_from_play")]
    public int? PointsFromPlay { get; set; }

    [Column("goals_from_play")]
    public int? GoalsFromPlay { get; set; }

    [Column("two_pointers_from_play")]
    public int? TwoPointersFromPlay { get; set; }

    [Column("shots_wide")]
    public int? ShotsWide { get; set; }

    [Column("shots_saved")]
    public int? ShotsSaved { get; set; }

    [Column("shots_short")]
    public int? ShotsShort { get; set; }

    [Column("shots_blocked")]
    public int? ShotsBlocked { get; set; }

    [Column("shots_woodwork")]
    public int? ShotsWoodwork { get; set; }

    [Column("points_from_frees")]
    public int? PointsFromFrees { get; set; }

    [Column("goals_from_frees")]
    public int? GoalsFromFrees { get; set; }

    [Column("frees_wide")]
    public int? FreesWide { get; set; }

    [Column("frees_saved")]
    public int? FreesSaved { get; set; }

    [Column("frees_short")]
    public int? FreesShort { get; set; }

    [Column("score_assists_points")]
    public int? ScoreAssistsPoints { get; set; }

    [Column("score_assists_goals")]
    public int? ScoreAssistsGoals { get; set; }

    [Column("shot_efficiency")]
    [Precision(8, 4)]
    public decimal? ShotEfficiency { get; set; }

    [Column("score_conversion_rate")]
    [Precision(8, 4)]
    public decimal? ScoreConversionRate { get; set; }

    [Column("defensive_actions")]
    public int? DefensiveActions { get; set; }

    [Column("attacking_plays")]
    public int? AttackingPlays { get; set; }

    [Column("possession_won_percentage")]
    [Precision(8, 4)]
    public decimal? PossessionWonPercentage { get; set; }

    [Column("distribution_accuracy")]
    [Precision(8, 4)]
    public decimal? DistributionAccuracy { get; set; }

    [Column("ground_ball_wins")]
    public int? GroundBallWins { get; set; }

    [Column("aerial_contests_won")]
    public int? AerialContestsWon { get; set; }

    [Column("clean_catches")]
    public int? CleanCatches { get; set; }

    [Column("fumbles")]
    public int? Fumbles { get; set; }

    [Column("overall_performance_rating")]
    [Precision(8, 4)]
    public decimal? OverallPerformanceRating { get; set; }

    [Column("attacking_rating")]
    [Precision(8, 4)]
    public decimal? AttackingRating { get; set; }

    [Column("defensive_rating")]
    [Precision(8, 4)]
    public decimal? DefensiveRating { get; set; }

    [Column("passing_rating")]
    [Precision(8, 4)]
    public decimal? PassingRating { get; set; }

    [Column("starting_position")]
    [StringLength(50)]
    public string? StartingPosition { get; set; }

    [Column("substituted_on_minute")]
    public int? SubstitutedOnMinute { get; set; }

    [Column("substituted_off_minute")]
    public int? SubstitutedOffMinute { get; set; }

    [Column("captain")]
    public bool? Captain { get; set; }

    [Column("imported_at", TypeName = "timestamp without time zone")]
    public DateTime ImportedAt { get; set; }

    [ForeignKey("MatchId")]
    [InverseProperty("MatchPlayerStats")]
    public virtual Match Match { get; set; } = null!;

    [InverseProperty("MatchPlayerStat")]
    public virtual ICollection<MatchKickoutStat> MatchKickoutStats { get; set; } = new List<MatchKickoutStat>();

    [ForeignKey("TeamId")]
    [InverseProperty("MatchPlayerStats")]
    public virtual Team Team { get; set; } = null!;
}
