using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.src.GAAStat.Dal.Models.application;

/// <summary>
/// Opposition team information
/// </summary>
[Table("teams")]
[Index("TeamName", Name = "idx_teams_name")]
[Index("TeamName", Name = "teams_team_name_key", IsUnique = true)]
public partial class Team
{
    [Key]
    [Column("team_id")]
    public int TeamId { get; set; }

    [Column("team_name")]
    [StringLength(100)]
    public string TeamName { get; set; } = null!;

    [Column("home_venue")]
    [StringLength(100)]
    public string? HomeVenue { get; set; }

    [Column("county")]
    [StringLength(50)]
    public string? County { get; set; }

    [InverseProperty("Opposition")]
    public virtual ICollection<Match> Matches { get; set; } = new List<Match>();
}
