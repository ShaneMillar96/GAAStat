using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.src.GAAStat.Dal.Models.application;

/// <summary>
/// Position-based aggregated statistics per match
/// </summary>
[Table("positional_analysis")]
[Index("MatchId", Name = "idx_positional_analysis_match_id")]
[Index("MatchId", "PositionId", Name = "idx_positional_analysis_match_position")]
[Index("PositionId", Name = "idx_positional_analysis_position_id")]
public partial class PositionalAnalysis
{
    [Key]
    [Column("positional_analysis_id")]
    public int PositionalAnalysisId { get; set; }

    [Column("match_id")]
    public int MatchId { get; set; }

    [Column("position_id")]
    public int PositionId { get; set; }

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

    [Column("total_scores")]
    public int? TotalScores { get; set; }

    [Column("total_possessions")]
    public int? TotalPossessions { get; set; }

    [Column("total_tackles")]
    public int? TotalTackles { get; set; }

    [ForeignKey("MatchId")]
    [InverseProperty("PositionalAnalyses")]
    public virtual Match Match { get; set; } = null!;

    [ForeignKey("PositionId")]
    [InverseProperty("PositionalAnalyses")]
    public virtual Position Position { get; set; } = null!;
}
