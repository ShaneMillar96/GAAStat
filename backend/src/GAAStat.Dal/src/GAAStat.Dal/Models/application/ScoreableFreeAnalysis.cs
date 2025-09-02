using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.src.GAAStat.Dal.Models.application;

/// <summary>
/// Free kick performance with distance and success tracking
/// </summary>
[Table("scoreable_free_analysis")]
[Index("MatchId", Name = "idx_scoreable_free_match_id")]
[Index("PlayerId", Name = "idx_scoreable_free_player_id")]
[Index("Success", Name = "idx_scoreable_free_success")]
[Index("FreeTypeId", Name = "idx_scoreable_free_type")]
public partial class ScoreableFreeAnalysis
{
    [Key]
    [Column("scoreable_free_id")]
    public int ScoreableFreeId { get; set; }

    [Column("match_id")]
    public int MatchId { get; set; }

    [Column("player_id")]
    public int? PlayerId { get; set; }

    [Column("free_number")]
    public int? FreeNumber { get; set; }

    /// <summary>
    /// Distance description of the free kick
    /// </summary>
    [Column("distance")]
    [StringLength(20)]
    public string? Distance { get; set; }

    /// <summary>
    /// Whether the free kick was successful
    /// </summary>
    [Column("success")]
    public bool? Success { get; set; }

    [Column("free_type_id")]
    public int? FreeTypeId { get; set; }

    [Column("shot_outcome_id")]
    public int? ShotOutcomeId { get; set; }

    [ForeignKey("FreeTypeId")]
    [InverseProperty("ScoreableFreeAnalyses")]
    public virtual FreeType? FreeType { get; set; }

    [ForeignKey("MatchId")]
    [InverseProperty("ScoreableFreeAnalyses")]
    public virtual Match Match { get; set; } = null!;

    [ForeignKey("PlayerId")]
    [InverseProperty("ScoreableFreeAnalyses")]
    public virtual Player? Player { get; set; }

    [ForeignKey("ShotOutcomeId")]
    [InverseProperty("ScoreableFreeAnalyses")]
    public virtual ShotOutcome? ShotOutcome { get; set; }
}
