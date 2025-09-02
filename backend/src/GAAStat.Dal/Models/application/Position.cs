using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GAAStat.Dal.Models.Application;

[Table("positions")]
public partial class Position
{
    [Key]
    [Column("position_id")]
    public int PositionId { get; set; }

    [Required]
    [Column("position_name")]
    [StringLength(50)]
    public string PositionName { get; set; } = string.Empty;

    [Required]
    [Column("position_category")]
    [StringLength(20)]
    public string PositionCategory { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    public virtual ICollection<Player> Players { get; set; } = new List<Player>();
    public virtual ICollection<PositionalAnalysis> PositionalAnalyses { get; set; } = new List<PositionalAnalysis>();
    public virtual ICollection<PositionAverage> PositionAverages { get; set; } = new List<PositionAverage>();
}