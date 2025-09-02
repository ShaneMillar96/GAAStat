using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GAAStat.Dal.Models.Application;

[Table("seasons")]
public partial class Season
{
    [Key]
    [Column("season_id")]
    public int SeasonId { get; set; }

    [Required]
    [Column("season_name")]
    [StringLength(50)]
    public string SeasonName { get; set; } = string.Empty;

    [Required]
    [Column("start_date")]
    public DateOnly StartDate { get; set; }

    [Required]
    [Column("end_date")]
    public DateOnly EndDate { get; set; }

    [Column("is_current")]
    public bool IsCurrent { get; set; } = false;

    public virtual ICollection<Match> Matches { get; set; } = new List<Match>();
    public virtual ICollection<SeasonPlayerTotal> SeasonPlayerTotals { get; set; } = new List<SeasonPlayerTotal>();
    public virtual ICollection<PositionAverage> PositionAverages { get; set; } = new List<PositionAverage>();
}