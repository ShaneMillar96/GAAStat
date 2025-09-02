using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.src.GAAStat.Dal.Models.application;

/// <summary>
/// Individual shot tracking with outcome and location data
/// </summary>
[Table("shot_analysis")]
[Index("MatchId", Name = "idx_shot_analysis_match_id")]
[Index("MatchId", "PlayerId", Name = "idx_shot_analysis_match_player")]
[Index("ShotOutcomeId", Name = "idx_shot_analysis_outcome")]
[Index("PlayerId", Name = "idx_shot_analysis_player_id")]
[Index("ShotTypeId", Name = "idx_shot_analysis_shot_type")]
public partial class ShotAnalysis
{
    [Key]
    [Column("shot_analysis_id")]
    public int ShotAnalysisId { get; set; }

    [Column("match_id")]
    public int MatchId { get; set; }

    [Column("player_id")]
    public int? PlayerId { get; set; }

    /// <summary>
    /// Sequential shot number within the match
    /// </summary>
    [Column("shot_number")]
    public int? ShotNumber { get; set; }

    /// <summary>
    /// Time period when shot was taken
    /// </summary>
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
    [InverseProperty("ShotAnalyses")]
    public virtual Match Match { get; set; } = null!;

    [ForeignKey("PlayerId")]
    [InverseProperty("ShotAnalyses")]
    public virtual Player? Player { get; set; }

    [ForeignKey("PositionAreaId")]
    [InverseProperty("ShotAnalyses")]
    public virtual PositionArea? PositionArea { get; set; }

    [ForeignKey("ShotOutcomeId")]
    [InverseProperty("ShotAnalyses")]
    public virtual ShotOutcome? ShotOutcome { get; set; }

    [ForeignKey("ShotTypeId")]
    [InverseProperty("ShotAnalyses")]
    public virtual ShotType? ShotType { get; set; }
}
