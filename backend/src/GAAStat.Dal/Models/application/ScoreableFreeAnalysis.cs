using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GAAStat.Dal.Models.Application;

[Table("scoreable_free_analysis")]
public partial class ScoreableFreeAnalysis
{
    [Key]
    [Column("scoreable_free_id")]
    public int ScoreableFreeId { get; set; }

    [Required]
    [Column("match_id")]
    public int MatchId { get; set; }

    [Column("player_id")]
    public int? PlayerId { get; set; }

    [Column("free_number")]
    public int? FreeNumber { get; set; }

    [Column("distance")]
    [StringLength(20)]
    public string? Distance { get; set; }

    [Column("success")]
    public bool? Success { get; set; }

    [Column("free_type_id")]
    public int? FreeTypeId { get; set; }

    [Column("shot_outcome_id")]
    public int? ShotOutcomeId { get; set; }

    [ForeignKey("MatchId")]
    public virtual Match Match { get; set; } = null!;

    [ForeignKey("PlayerId")]
    public virtual Player? Player { get; set; }

    [ForeignKey("FreeTypeId")]
    public virtual FreeType? FreeType { get; set; }

    [ForeignKey("ShotOutcomeId")]
    public virtual ShotOutcome? ShotOutcome { get; set; }
}