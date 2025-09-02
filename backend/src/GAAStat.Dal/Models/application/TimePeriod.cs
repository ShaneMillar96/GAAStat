using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GAAStat.Dal.Models.Application;

[Table("time_periods")]
public partial class TimePeriod
{
    [Key]
    [Column("time_period_id")]
    public int TimePeriodId { get; set; }

    [Required]
    [Column("period_name")]
    [StringLength(30)]
    public string PeriodName { get; set; } = string.Empty;

    [Column("description")]
    [StringLength(100)]
    public string? Description { get; set; }

    public virtual ICollection<KickoutAnalysis> KickoutAnalyses { get; set; } = new List<KickoutAnalysis>();
}