using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GAAStat.Dal.Models.Application;

[Table("competitions")]
public partial class Competition
{
    [Key]
    [Column("competition_id")]
    public int CompetitionId { get; set; }

    [Required]
    [Column("competition_name")]
    [StringLength(100)]
    public string CompetitionName { get; set; } = string.Empty;

    [Required]
    [Column("season")]
    [StringLength(20)]
    public string Season { get; set; } = string.Empty;

    [Required]
    [Column("competition_type_id")]
    public int CompetitionTypeId { get; set; }

    [ForeignKey("CompetitionTypeId")]
    public virtual CompetitionType CompetitionType { get; set; } = null!;

    public virtual ICollection<Match> Matches { get; set; } = new List<Match>();
}