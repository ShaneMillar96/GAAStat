using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GAAStat.Dal.Models.Application;

[Table("shot_analysis")]
public partial class ShotAnalysis
{
    [Key]
    [Column("shot_analysis_id")]
    public int ShotAnalysisId { get; set; }

    [Required]
    [Column("match_id")]
    public int MatchId { get; set; }

    [Column("player_id")]
    public int? PlayerId { get; set; }

    [Column("shot_number")]
    public int? ShotNumber { get; set; }

    [Column("time_period")]
    [StringLength(20)]
    public string? TimePeriod { get; set; }

    [Column("shot_type_id")]
    public int? ShotTypeId { get; set; }

    [Column("shot_outcome_id")]
    public int? ShotOutcomeId { get; set; }

    [Column("position_area_id")]
    public int? PositionAreaId { get; set; }

    [ForeignKey("MatchId")]
    public virtual Match Match { get; set; } = null!;

    [ForeignKey("PlayerId")]
    public virtual Player? Player { get; set; }

    [ForeignKey("ShotTypeId")]
    public virtual ShotType? ShotType { get; set; }

    [ForeignKey("ShotOutcomeId")]
    public virtual ShotOutcome? ShotOutcome { get; set; }

    [ForeignKey("PositionAreaId")]
    public virtual PositionArea? PositionArea { get; set; }
}