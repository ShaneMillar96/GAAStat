using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.Models.application;

/// <summary>
/// Competitions within seasons (League, Championship, Cup formats)
/// </summary>
[Table("competitions")]
[Index("SeasonId", "Type", Name = "idx_competitions_season")]
public partial class Competition
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Column("type")]
    [StringLength(50)]
    public string Type { get; set; } = null!;

    [Column("season_id")]
    public int SeasonId { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Alias property for service compatibility - maps to Name
    /// </summary>
    [NotMapped]
    public string CompetitionName => Name;

    [InverseProperty("Competition")]
    public virtual ICollection<Match> Matches { get; set; } = new List<Match>();

    [ForeignKey("SeasonId")]
    [InverseProperty("Competitions")]
    public virtual Season Season { get; set; } = null!;
}
