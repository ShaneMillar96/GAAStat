using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.src.GAAStat.Dal.Models.application;

/// <summary>
/// Season definitions and date ranges
/// </summary>
[Table("seasons")]
[Index("StartDate", "EndDate", Name = "idx_seasons_dates")]
[Index("SeasonName", Name = "seasons_season_name_key", IsUnique = true)]
public partial class Season
{
    [Key]
    [Column("season_id")]
    public int SeasonId { get; set; }

    [Column("season_name")]
    [StringLength(50)]
    public string SeasonName { get; set; } = null!;

    [Column("start_date")]
    public DateOnly StartDate { get; set; }

    [Column("end_date")]
    public DateOnly EndDate { get; set; }

    [Column("is_current")]
    public bool? IsCurrent { get; set; }

    [InverseProperty("Season")]
    public virtual ICollection<Match> Matches { get; set; } = new List<Match>();

    [InverseProperty("Season")]
    public virtual ICollection<PositionAverage> PositionAverages { get; set; } = new List<PositionAverage>();

    [InverseProperty("Season")]
    public virtual ICollection<SeasonPlayerTotal> SeasonPlayerTotals { get; set; } = new List<SeasonPlayerTotal>();
}
