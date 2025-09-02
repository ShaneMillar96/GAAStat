using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.src.GAAStat.Dal.Models.application;

/// <summary>
/// Shot outcome types (Goal, Point, Wide, Save, etc.)
/// </summary>
[Table("shot_outcomes")]
[Index("OutcomeName", Name = "shot_outcomes_outcome_name_key", IsUnique = true)]
public partial class ShotOutcome
{
    [Key]
    [Column("shot_outcome_id")]
    public int ShotOutcomeId { get; set; }

    [Column("outcome_name")]
    [StringLength(30)]
    public string OutcomeName { get; set; } = null!;

    [Column("description")]
    [StringLength(100)]
    public string? Description { get; set; }

    [Column("is_score")]
    public bool? IsScore { get; set; }

    [InverseProperty("ShotOutcome")]
    public virtual ICollection<ScoreableFreeAnalysis> ScoreableFreeAnalyses { get; set; } = new List<ScoreableFreeAnalysis>();

    [InverseProperty("ShotOutcome")]
    public virtual ICollection<ShotAnalysis> ShotAnalyses { get; set; } = new List<ShotAnalysis>();
}
