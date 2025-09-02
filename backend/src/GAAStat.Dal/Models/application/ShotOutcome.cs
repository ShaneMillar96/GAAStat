using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GAAStat.Dal.Models.Application;

[Table("shot_outcomes")]
public partial class ShotOutcome
{
    [Key]
    [Column("shot_outcome_id")]
    public int ShotOutcomeId { get; set; }

    [Required]
    [Column("outcome_name")]
    [StringLength(30)]
    public string OutcomeName { get; set; } = string.Empty;

    [Column("description")]
    [StringLength(100)]
    public string? Description { get; set; }

    [Column("is_score")]
    public bool IsScore { get; set; } = false;

    public virtual ICollection<ShotAnalysis> ShotAnalyses { get; set; } = new List<ShotAnalysis>();
    public virtual ICollection<ScoreableFreeAnalysis> ScoreableFreeAnalyses { get; set; } = new List<ScoreableFreeAnalysis>();
}