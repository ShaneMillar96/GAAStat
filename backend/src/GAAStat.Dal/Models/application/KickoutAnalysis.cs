using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace GAAStat.Dal.Models.Application;

[Table("kickout_analysis")]
public partial class KickoutAnalysis
{
    [Key]
    [Column("kickout_analysis_id")]
    public int KickoutAnalysisId { get; set; }

    [Required]
    [Column("match_id")]
    public int MatchId { get; set; }

    [Column("total_attempts")]
    public int TotalAttempts { get; set; } = 0;

    [Column("successful")]
    public int Successful { get; set; } = 0;

    [Column("success_rate", TypeName = "decimal(5,4)")]
    public decimal? SuccessRate { get; set; }

    [Column("outcome_breakdown", TypeName = "json")]
    public JsonDocument? OutcomeBreakdown { get; set; }

    [Column("time_period_id")]
    public int? TimePeriodId { get; set; }

    [Column("kickout_type_id")]
    public int? KickoutTypeId { get; set; }

    [Column("team_type_id")]
    public int? TeamTypeId { get; set; }

    [ForeignKey("MatchId")]
    public virtual Match Match { get; set; } = null!;

    [ForeignKey("TimePeriodId")]
    public virtual TimePeriod? TimePeriod { get; set; }

    [ForeignKey("KickoutTypeId")]
    public virtual KickoutType? KickoutType { get; set; }

    [ForeignKey("TeamTypeId")]
    public virtual TeamType? TeamType { get; set; }
}