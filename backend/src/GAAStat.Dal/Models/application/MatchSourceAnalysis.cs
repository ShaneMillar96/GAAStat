using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.Models.application;

/// <summary>
/// Attack/shot source analysis for tactical insights
/// </summary>
[Table("match_source_analysis")]
[Index("MatchId", "TeamId", Name = "idx_source_analysis_match_team")]
[Index("AnalysisType", "SourceCategory", Name = "idx_source_analysis_type")]
[Index("MatchId", "TeamId", "AnalysisType", "SourceCategory", Name = "unique_match_team_analysis", IsUnique = true)]
public partial class MatchSourceAnalysis
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("match_id")]
    public int MatchId { get; set; }

    [Column("team_id")]
    public int TeamId { get; set; }

    [Column("analysis_type")]
    [StringLength(50)]
    public string AnalysisType { get; set; } = null!;

    [Column("source_category")]
    [StringLength(100)]
    public string SourceCategory { get; set; } = null!;

    [Column("total_count")]
    public int? TotalCount { get; set; }

    [Column("successful_count")]
    public int? SuccessfulCount { get; set; }

    [Column("success_rate")]
    [Precision(8, 4)]
    public decimal? SuccessRate { get; set; }

    [Column("imported_at", TypeName = "timestamp without time zone")]
    public DateTime ImportedAt { get; set; }

    [ForeignKey("MatchId")]
    [InverseProperty("MatchSourceAnalyses")]
    public virtual Match Match { get; set; } = null!;

    [ForeignKey("TeamId")]
    [InverseProperty("MatchSourceAnalyses")]
    public virtual Team Team { get; set; } = null!;
}
