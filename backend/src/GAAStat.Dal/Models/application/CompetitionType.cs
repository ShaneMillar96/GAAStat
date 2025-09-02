using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GAAStat.Dal.Models.Application;

[Table("competition_types")]
public partial class CompetitionType
{
    [Key]
    [Column("competition_type_id")]
    public int CompetitionTypeId { get; set; }

    [Required]
    [Column("type_name")]
    [StringLength(50)]
    public string TypeName { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    public virtual ICollection<Competition> Competitions { get; set; } = new List<Competition>();
}