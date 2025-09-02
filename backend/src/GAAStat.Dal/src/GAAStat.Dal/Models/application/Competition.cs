using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.src.GAAStat.Dal.Models.application;

/// <summary>
/// Competition master data
/// </summary>
[Table("competitions")]
[Index("CompetitionName", Name = "idx_competitions_name")]
[Index("CompetitionTypeId", Name = "idx_competitions_type")]
public partial class Competition
{
    [Key]
    [Column("competition_id")]
    public int CompetitionId { get; set; }

    [Column("competition_name")]
    [StringLength(100)]
    public string CompetitionName { get; set; } = null!;

    [Column("season")]
    [StringLength(20)]
    public string Season { get; set; } = null!;

    [Column("competition_type_id")]
    public int CompetitionTypeId { get; set; }

    [ForeignKey("CompetitionTypeId")]
    [InverseProperty("Competitions")]
    public virtual CompetitionType CompetitionType { get; set; } = null!;

    [InverseProperty("Competition")]
    public virtual ICollection<Match> Matches { get; set; } = new List<Match>();
}
