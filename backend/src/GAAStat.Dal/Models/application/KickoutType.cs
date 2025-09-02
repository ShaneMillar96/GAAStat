using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GAAStat.Dal.Models.Application;

[Table("kickout_types")]
public partial class KickoutType
{
    [Key]
    [Column("kickout_type_id")]
    public int KickoutTypeId { get; set; }

    [Required]
    [Column("type_name")]
    [StringLength(30)]
    public string TypeName { get; set; } = string.Empty;

    [Column("description")]
    [StringLength(100)]
    public string? Description { get; set; }

    public virtual ICollection<KickoutAnalysis> KickoutAnalyses { get; set; } = new List<KickoutAnalysis>();
}