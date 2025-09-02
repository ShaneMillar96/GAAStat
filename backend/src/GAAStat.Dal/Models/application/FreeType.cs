using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GAAStat.Dal.Models.Application;

[Table("free_types")]
public partial class FreeType
{
    [Key]
    [Column("free_type_id")]
    public int FreeTypeId { get; set; }

    [Required]
    [Column("type_name")]
    [StringLength(30)]
    public string TypeName { get; set; } = string.Empty;

    [Column("description")]
    [StringLength(100)]
    public string? Description { get; set; }

    public virtual ICollection<ScoreableFreeAnalysis> ScoreableFreeAnalyses { get; set; } = new List<ScoreableFreeAnalysis>();
}