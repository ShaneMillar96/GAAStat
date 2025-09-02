using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.src.GAAStat.Dal.Models.application;

/// <summary>
/// Playing position definitions and categories
/// </summary>
[Table("positions")]
[Index("PositionName", Name = "positions_position_name_key", IsUnique = true)]
public partial class Position
{
    [Key]
    [Column("position_id")]
    public int PositionId { get; set; }

    [Column("position_name")]
    [StringLength(50)]
    public string PositionName { get; set; } = null!;

    [Column("position_category")]
    [StringLength(20)]
    public string PositionCategory { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [InverseProperty("Position")]
    public virtual ICollection<Player> Players { get; set; } = new List<Player>();

    [InverseProperty("Position")]
    public virtual ICollection<PositionAverage> PositionAverages { get; set; } = new List<PositionAverage>();

    [InverseProperty("Position")]
    public virtual ICollection<PositionalAnalysis> PositionalAnalyses { get; set; } = new List<PositionalAnalysis>();
}
