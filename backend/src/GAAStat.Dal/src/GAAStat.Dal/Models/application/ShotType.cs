using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.src.GAAStat.Dal.Models.application;

/// <summary>
/// Shot type classifications (From Play, Free Kick, Penalty)
/// </summary>
[Table("shot_types")]
[Index("TypeName", Name = "shot_types_type_name_key", IsUnique = true)]
public partial class ShotType
{
    [Key]
    [Column("shot_type_id")]
    public int ShotTypeId { get; set; }

    [Column("type_name")]
    [StringLength(50)]
    public string TypeName { get; set; } = null!;

    [Column("description")]
    [StringLength(100)]
    public string? Description { get; set; }

    [InverseProperty("ShotType")]
    public virtual ICollection<ShotAnalysis> ShotAnalyses { get; set; } = new List<ShotAnalysis>();
}
