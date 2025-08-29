using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.Models.application;

/// <summary>
/// Season definitions for multi-year data management
/// </summary>
[Table("seasons")]
[Index("IsCurrent", "Year", Name = "idx_seasons_current", IsDescending = new[] { false, true })]
[Index("Year", Name = "seasons_year_key", IsUnique = true)]
public partial class Season
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("year")]
    public int Year { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Column("start_date")]
    public DateOnly? StartDate { get; set; }

    [Column("end_date")]
    public DateOnly? EndDate { get; set; }

    [Column("is_current")]
    public bool? IsCurrent { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Alias property for service compatibility - maps to Name
    /// </summary>
    [NotMapped]
    public string SeasonName => Name;

    [InverseProperty("Season")]
    public virtual ICollection<Competition> Competitions { get; set; } = new List<Competition>();
}
