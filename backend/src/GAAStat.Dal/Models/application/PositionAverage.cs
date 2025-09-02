using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GAAStat.Dal.Models.Application;

[Table("position_averages")]
public partial class PositionAverage
{
    [Key]
    [Column("position_avg_id")]
    public int PositionAvgId { get; set; }

    [Required]
    [Column("position_id")]
    public int PositionId { get; set; }

    [Required]
    [Column("season_id")]
    public int SeasonId { get; set; }

    [Column("avg_engagement_efficiency", TypeName = "decimal(5,4)")]
    public decimal? AvgEngagementEfficiency { get; set; }

    [Column("avg_possession_success_rate", TypeName = "decimal(5,4)")]
    public decimal? AvgPossessionSuccessRate { get; set; }

    [Column("avg_conversion_rate", TypeName = "decimal(5,4)")]
    public decimal? AvgConversionRate { get; set; }

    [Column("avg_tackle_success_rate", TypeName = "decimal(5,4)")]
    public decimal? AvgTackleSuccessRate { get; set; }

    [Column("avg_scores_per_game", TypeName = "decimal(6,2)")]
    public decimal? AvgScoresPerGame { get; set; }

    [Column("avg_possessions_per_game", TypeName = "decimal(6,2)")]
    public decimal? AvgPossessionsPerGame { get; set; }

    [Column("avg_tackles_per_game", TypeName = "decimal(6,2)")]
    public decimal? AvgTacklesPerGame { get; set; }

    [ForeignKey("PositionId")]
    public virtual Position Position { get; set; } = null!;

    [ForeignKey("SeasonId")]
    public virtual Season Season { get; set; } = null!;
}