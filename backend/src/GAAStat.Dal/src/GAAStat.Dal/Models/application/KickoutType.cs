using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.src.GAAStat.Dal.Models.application;

/// <summary>
/// Kickout classifications (Long, Short)
/// </summary>
[Table("kickout_types")]
[Index("TypeName", Name = "kickout_types_type_name_key", IsUnique = true)]
public partial class KickoutType
{
    [Key]
    [Column("kickout_type_id")]
    public int KickoutTypeId { get; set; }

    [Column("type_name")]
    [StringLength(30)]
    public string TypeName { get; set; } = null!;

    [Column("description")]
    [StringLength(100)]
    public string? Description { get; set; }

    [InverseProperty("KickoutType")]
    public virtual ICollection<KickoutAnalysis> KickoutAnalyses { get; set; } = new List<KickoutAnalysis>();
}
