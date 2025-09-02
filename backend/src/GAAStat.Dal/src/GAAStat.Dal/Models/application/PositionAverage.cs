using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.src.GAAStat.Dal.Models.application;

/// <summary>
/// Position-based benchmark comparisons
/// </summary>
[Table("position_averages")]
[Index("PositionId", Name = "idx_position_averages_position_id")]
[Index("SeasonId", Name = "idx_position_averages_season_id")]
[Index("PositionId", "SeasonId", Name = "position_averages_position_id_season_id_key", IsUnique = true)]
public partial class PositionAverage
{
    [Key]
    [Column("position_avg_id")]
    public int PositionAvgId { get; set; }

    [Column("position_id")]
    public int PositionId { get; set; }

    [Column("season_id")]
    public int SeasonId { get; set; }

    [Column("avg_engagement_efficiency")]
    [Precision(5, 4)]
    public decimal? AvgEngagementEfficiency { get; set; }

    [Column("avg_possession_success_rate")]
    [Precision(5, 4)]
    public decimal? AvgPossessionSuccessRate { get; set; }

    [Column("avg_conversion_rate")]
    [Precision(5, 4)]
    public decimal? AvgConversionRate { get; set; }

    [Column("avg_tackle_success_rate")]
    [Precision(5, 4)]
    public decimal? AvgTackleSuccessRate { get; set; }

    /// <summary>
    /// Average scores per game for this position
    /// </summary>
    [Column("avg_scores_per_game")]
    [Precision(6, 2)]
    public decimal? AvgScoresPerGame { get; set; }

    /// <summary>
    /// Average possessions per game for this position
    /// </summary>
    [Column("avg_possessions_per_game")]
    [Precision(6, 2)]
    public decimal? AvgPossessionsPerGame { get; set; }

    /// <summary>
    /// Average tackles per game for this position
    /// </summary>
    [Column("avg_tackles_per_game")]
    [Precision(6, 2)]
    public decimal? AvgTacklesPerGame { get; set; }

    [ForeignKey("PositionId")]
    [InverseProperty("PositionAverages")]
    public virtual Position Position { get; set; } = null!;

    [ForeignKey("SeasonId")]
    [InverseProperty("PositionAverages")]
    public virtual Season Season { get; set; } = null!;
}
