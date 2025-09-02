using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GAAStat.Dal.Models.Application;

[Table("position_areas")]
public partial class PositionArea
{
    [Key]
    [Column("position_area_id")]
    public int PositionAreaId { get; set; }

    [Required]
    [Column("area_name")]
    [StringLength(50)]
    public string AreaName { get; set; } = string.Empty;

    [Column("description")]
    [StringLength(100)]
    public string? Description { get; set; }

    public virtual ICollection<ShotAnalysis> ShotAnalyses { get; set; } = new List<ShotAnalysis>();
}