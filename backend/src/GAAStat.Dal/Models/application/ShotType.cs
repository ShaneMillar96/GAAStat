using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GAAStat.Dal.Models.Application;

[Table("shot_types")]
public partial class ShotType
{
    [Key]
    [Column("shot_type_id")]
    public int ShotTypeId { get; set; }

    [Required]
    [Column("type_name")]
    [StringLength(50)]
    public string TypeName { get; set; } = string.Empty;

    [Column("description")]
    [StringLength(100)]
    public string? Description { get; set; }

    public virtual ICollection<ShotAnalysis> ShotAnalyses { get; set; } = new List<ShotAnalysis>();
}