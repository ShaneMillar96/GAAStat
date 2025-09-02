using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.src.GAAStat.Dal.Models.application;

/// <summary>
/// Player season statistics and averages
/// </summary>
[Table("season_player_totals")]
[Index("GamesPlayed", Name = "idx_season_player_totals_games_played")]
[Index("PlayerId", Name = "idx_season_player_totals_player_id")]
[Index("SeasonId", Name = "idx_season_player_totals_season_id")]
[Index("TotalScores", Name = "idx_season_player_totals_total_scores")]
[Index("PlayerId", "SeasonId", Name = "season_player_totals_player_id_season_id_key", IsUnique = true)]
public partial class SeasonPlayerTotal
{
    [Key]
    [Column("season_total_id")]
    public int SeasonTotalId { get; set; }

    [Column("player_id")]
    public int PlayerId { get; set; }

    [Column("season_id")]
    public int SeasonId { get; set; }

    /// <summary>
    /// Number of games played in the season
    /// </summary>
    [Column("games_played")]
    public int? GamesPlayed { get; set; }

    /// <summary>
    /// Total minutes played in the season
    /// </summary>
    [Column("total_minutes")]
    public int? TotalMinutes { get; set; }

    /// <summary>
    /// Average engagement efficiency across all games
    /// </summary>
    [Column("avg_engagement_efficiency")]
    [Precision(5, 4)]
    public decimal? AvgEngagementEfficiency { get; set; }

    [Column("avg_possession_success_rate")]
    [Precision(5, 4)]
    public decimal? AvgPossessionSuccessRate { get; set; }

    /// <summary>
    /// Total combined goals and points scored
    /// </summary>
    [Column("total_scores")]
    public int? TotalScores { get; set; }

    [Column("total_goals")]
    public int? TotalGoals { get; set; }

    [Column("total_points")]
    public int? TotalPoints { get; set; }

    [Column("avg_conversion_rate")]
    [Precision(5, 4)]
    public decimal? AvgConversionRate { get; set; }

    [Column("total_tackles")]
    public int? TotalTackles { get; set; }

    [Column("avg_tackle_success_rate")]
    [Precision(5, 4)]
    public decimal? AvgTackleSuccessRate { get; set; }

    [Column("total_turnovers_won")]
    public int? TotalTurnoversWon { get; set; }

    [Column("total_interceptions")]
    public int? TotalInterceptions { get; set; }

    [ForeignKey("PlayerId")]
    [InverseProperty("SeasonPlayerTotals")]
    public virtual Player Player { get; set; } = null!;

    [ForeignKey("SeasonId")]
    [InverseProperty("SeasonPlayerTotals")]
    public virtual Season Season { get; set; } = null!;
}
