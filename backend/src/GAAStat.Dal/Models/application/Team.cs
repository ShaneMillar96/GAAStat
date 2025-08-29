using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.Models.application;

/// <summary>
/// GAA teams participating in matches - reference data not cleared on import
/// </summary>
[Table("teams")]
[Index("Name", Name = "idx_teams_name")]
public partial class Team
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Column("abbreviation")]
    [StringLength(10)]
    public string? Abbreviation { get; set; }

    [Column("county")]
    [StringLength(50)]
    public string? County { get; set; }

    [Column("division")]
    [StringLength(50)]
    public string? Division { get; set; }

    [Column("color_primary")]
    [StringLength(7)]
    public string? ColorPrimary { get; set; }

    [Column("color_secondary")]
    [StringLength(7)]
    public string? ColorSecondary { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Alias property for service compatibility - maps to Name
    /// </summary>
    [NotMapped]
    public string TeamName => Name;

    [InverseProperty("AwayTeam")]
    public virtual ICollection<Match> MatchAwayTeams { get; set; } = new List<Match>();

    [InverseProperty("HomeTeam")]
    public virtual ICollection<Match> MatchHomeTeams { get; set; } = new List<Match>();

    [InverseProperty("Team")]
    public virtual ICollection<MatchPlayerStat> MatchPlayerStats { get; set; } = new List<MatchPlayerStat>();

    [InverseProperty("Team")]
    public virtual ICollection<MatchSourceAnalysis> MatchSourceAnalyses { get; set; } = new List<MatchSourceAnalysis>();
}
