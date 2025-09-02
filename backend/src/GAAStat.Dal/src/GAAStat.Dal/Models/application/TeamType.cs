using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.src.GAAStat.Dal.Models.application;

/// <summary>
/// Team type designations (Drum, Opposition)
/// </summary>
[Table("team_types")]
[Index("TypeName", Name = "team_types_type_name_key", IsUnique = true)]
public partial class TeamType
{
    [Key]
    [Column("team_type_id")]
    public int TeamTypeId { get; set; }

    [Column("type_name")]
    [StringLength(30)]
    public string TypeName { get; set; } = null!;

    [Column("description")]
    [StringLength(100)]
    public string? Description { get; set; }

    [InverseProperty("TeamType")]
    public virtual ICollection<KickoutAnalysis> KickoutAnalyses { get; set; } = new List<KickoutAnalysis>();
}
