using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GAAStat.Dal.Models.Application;

[Table("teams")]
public partial class Team
{
    [Key]
    [Column("team_id")]
    public int TeamId { get; set; }

    [Required]
    [Column("team_name")]
    [StringLength(100)]
    public string TeamName { get; set; } = string.Empty;

    [Column("home_venue")]
    [StringLength(100)]
    public string? HomeVenue { get; set; }

    [Column("county")]
    [StringLength(50)]
    public string? County { get; set; }

    public virtual ICollection<Match> Matches { get; set; } = new List<Match>();
}