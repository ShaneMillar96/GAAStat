using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GAAStat.Dal.Models.Application;

[Table("positional_analysis")]
public partial class PositionalAnalysis
{
    [Key]
    [Column("positional_analysis_id")]
    public int PositionalAnalysisId { get; set; }

    [Required]
    [Column("match_id")]
    public int MatchId { get; set; }

    [Required]
    [Column("position_id")]
    public int PositionId { get; set; }

    [Column("avg_engagement_efficiency", TypeName = "decimal(5,4)")]
    public decimal? AvgEngagementEfficiency { get; set; }

    [Column("avg_possession_success_rate", TypeName = "decimal(5,4)")]
    public decimal? AvgPossessionSuccessRate { get; set; }

    [Column("avg_conversion_rate", TypeName = "decimal(5,4)")]
    public decimal? AvgConversionRate { get; set; }

    [Column("avg_tackle_success_rate", TypeName = "decimal(5,4)")]
    public decimal? AvgTackleSuccessRate { get; set; }

    [Column("total_scores")]
    public int TotalScores { get; set; } = 0;

    [Column("total_possessions")]
    public int TotalPossessions { get; set; } = 0;

    [Column("total_tackles")]
    public int TotalTackles { get; set; } = 0;

    [ForeignKey("MatchId")]
    public virtual Match Match { get; set; } = null!;

    [ForeignKey("PositionId")]
    public virtual Position Position { get; set; } = null!;
}