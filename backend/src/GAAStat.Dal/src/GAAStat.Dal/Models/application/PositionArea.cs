using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.src.GAAStat.Dal.Models.application;

/// <summary>
/// Field position areas (Attacking Third, Middle Third, Defensive Third)
/// </summary>
[Table("position_areas")]
[Index("AreaName", Name = "position_areas_area_name_key", IsUnique = true)]
public partial class PositionArea
{
    [Key]
    [Column("position_area_id")]
    public int PositionAreaId { get; set; }

    [Column("area_name")]
    [StringLength(50)]
    public string AreaName { get; set; } = null!;

    [Column("description")]
    [StringLength(100)]
    public string? Description { get; set; }

    [InverseProperty("PositionArea")]
    public virtual ICollection<ShotAnalysis> ShotAnalyses { get; set; } = new List<ShotAnalysis>();
}
