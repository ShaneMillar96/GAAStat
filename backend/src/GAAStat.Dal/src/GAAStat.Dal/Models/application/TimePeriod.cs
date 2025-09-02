using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.src.GAAStat.Dal.Models.application;

/// <summary>
/// Game period classifications (First Half, Second Half, Full Game)
/// </summary>
[Table("time_periods")]
[Index("PeriodName", Name = "time_periods_period_name_key", IsUnique = true)]
public partial class TimePeriod
{
    [Key]
    [Column("time_period_id")]
    public int TimePeriodId { get; set; }

    [Column("period_name")]
    [StringLength(30)]
    public string PeriodName { get; set; } = null!;

    [Column("description")]
    [StringLength(100)]
    public string? Description { get; set; }

    [InverseProperty("TimePeriod")]
    public virtual ICollection<KickoutAnalysis> KickoutAnalyses { get; set; } = new List<KickoutAnalysis>();
}
