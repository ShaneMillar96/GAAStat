using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.src.GAAStat.Dal.Models.application;

/// <summary>
/// Individual player performance data (80+ fields per player per match)
/// </summary>
[Table("match_player_statistics")]
[Index("MatchId", Name = "idx_match_player_stats_match_id")]
[Index("MatchId", "PlayerId", Name = "idx_match_player_stats_match_player")]
[Index("MinutesPlayed", Name = "idx_match_player_stats_minutes")]
[Index("PlayerId", Name = "idx_match_player_stats_player_id")]
[Index("PlayerId", "MatchId", Name = "idx_player_season_performance")]
[Index("PlayerId", "MatchId", Name = "idx_seasonal_analysis")]
public partial class MatchPlayerStatistic
{
    [Key]
    [Column("match_player_stat_id")]
    public int MatchPlayerStatId { get; set; }

    [Column("match_id")]
    public int MatchId { get; set; }

    [Column("player_id")]
    public int PlayerId { get; set; }

    [Column("minutes_played")]
    public int? MinutesPlayed { get; set; }

    [Column("total_engagements")]
    public int? TotalEngagements { get; set; }

    /// <summary>
    /// Player engagement efficiency rate (0-1)
    /// </summary>
    [Column("engagement_efficiency")]
    [Precision(5, 4)]
    public decimal? EngagementEfficiency { get; set; }

    [Column("scores")]
    [StringLength(20)]
    public string? Scores { get; set; }

    /// <summary>
    /// Success rate for possessions (0-1)
    /// </summary>
    [Column("possession_success_rate")]
    [Precision(5, 4)]
    public decimal? PossessionSuccessRate { get; set; }

    /// <summary>
    /// Possessions per total engagement
    /// </summary>
    [Column("possessions_per_te")]
    [Precision(10, 4)]
    public decimal? PossessionsPerTe { get; set; }

    [Column("total_possessions")]
    public int? TotalPossessions { get; set; }

    [Column("turnovers_won")]
    public int? TurnoversWon { get; set; }

    [Column("interceptions")]
    public int? Interceptions { get; set; }

    [Column("total_attacks")]
    public int? TotalAttacks { get; set; }

    [Column("kick_retained")]
    public int? KickRetained { get; set; }

    [Column("kick_lost")]
    public int? KickLost { get; set; }

    [Column("carry_retained")]
    public int? CarryRetained { get; set; }

    [Column("carry_lost")]
    public int? CarryLost { get; set; }

    [Column("shots_total")]
    public int? ShotsTotal { get; set; }

    [Column("goals")]
    public int? Goals { get; set; }

    [Column("points")]
    public int? Points { get; set; }

    [Column("wides")]
    public int? Wides { get; set; }

    [Column("conversion_rate")]
    [Precision(5, 4)]
    public decimal? ConversionRate { get; set; }

    [Column("tackles_total")]
    public int? TacklesTotal { get; set; }

    [Column("tackles_contact")]
    public int? TacklesContact { get; set; }

    [Column("tackles_missed")]
    public int? TacklesMissed { get; set; }

    [Column("tackle_percentage")]
    [Precision(5, 4)]
    public decimal? TacklePercentage { get; set; }

    [Column("frees_conceded_total")]
    public int? FreesConcededTotal { get; set; }

    [Column("yellow_cards")]
    public int? YellowCards { get; set; }

    [Column("black_cards")]
    public int? BlackCards { get; set; }

    [Column("red_cards")]
    public int? RedCards { get; set; }

    [Column("kickouts_total")]
    public int? KickoutsTotal { get; set; }

    [Column("kickouts_retained")]
    public int? KickoutsRetained { get; set; }

    [Column("kickouts_lost")]
    public int? KickoutsLost { get; set; }

    [Column("kickout_percentage")]
    [Precision(5, 4)]
    public decimal? KickoutPercentage { get; set; }

    [Column("saves")]
    public int? Saves { get; set; }

    [ForeignKey("MatchId")]
    [InverseProperty("MatchPlayerStatistics")]
    public virtual Match Match { get; set; } = null!;

    [ForeignKey("PlayerId")]
    [InverseProperty("MatchPlayerStatistics")]
    public virtual Player Player { get; set; } = null!;
}
