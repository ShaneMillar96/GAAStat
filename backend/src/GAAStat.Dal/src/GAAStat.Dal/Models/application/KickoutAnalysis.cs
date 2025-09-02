using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.src.GAAStat.Dal.Models.application;

/// <summary>
/// Detailed kickout performance tracking by type and period
/// </summary>
[Table("kickout_analysis")]
[Index("KickoutTypeId", Name = "idx_kickout_analysis_kickout_type")]
[Index("MatchId", Name = "idx_kickout_analysis_match_id")]
[Index("TeamTypeId", Name = "idx_kickout_analysis_team_type")]
[Index("TimePeriodId", Name = "idx_kickout_analysis_time_period")]
public partial class KickoutAnalysis
{
    [Key]
    [Column("kickout_analysis_id")]
    public int KickoutAnalysisId { get; set; }

    [Column("match_id")]
    public int MatchId { get; set; }

    [Column("total_attempts")]
    public int? TotalAttempts { get; set; }

    [Column("successful")]
    public int? Successful { get; set; }

    [Column("success_rate")]
    [Precision(5, 4)]
    public decimal? SuccessRate { get; set; }

    /// <summary>
    /// JSONB object containing detailed outcome statistics
    /// </summary>
    [Column("outcome_breakdown", TypeName = "jsonb")]
    public string? OutcomeBreakdown { get; set; }

    [Column("time_period_id")]
    public int? TimePeriodId { get; set; }

    [Column("kickout_type_id")]
    public int? KickoutTypeId { get; set; }

    [Column("team_type_id")]
    public int? TeamTypeId { get; set; }

    [ForeignKey("KickoutTypeId")]
    [InverseProperty("KickoutAnalyses")]
    public virtual KickoutType? KickoutType { get; set; }

    [ForeignKey("MatchId")]
    [InverseProperty("KickoutAnalyses")]
    public virtual Match Match { get; set; } = null!;

    [ForeignKey("TeamTypeId")]
    [InverseProperty("KickoutAnalyses")]
    public virtual TeamType? TeamType { get; set; }

    [ForeignKey("TimePeriodId")]
    [InverseProperty("KickoutAnalyses")]
    public virtual TimePeriod? TimePeriod { get; set; }
}
