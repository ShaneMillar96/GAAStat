using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.src.GAAStat.Dal.Models.application;

/// <summary>
/// Free kick types (Standard, Quick)
/// </summary>
[Table("free_types")]
[Index("TypeName", Name = "free_types_type_name_key", IsUnique = true)]
public partial class FreeType
{
    [Key]
    [Column("free_type_id")]
    public int FreeTypeId { get; set; }

    [Column("type_name")]
    [StringLength(30)]
    public string TypeName { get; set; } = null!;

    [Column("description")]
    [StringLength(100)]
    public string? Description { get; set; }

    [InverseProperty("FreeType")]
    public virtual ICollection<ScoreableFreeAnalysis> ScoreableFreeAnalyses { get; set; } = new List<ScoreableFreeAnalysis>();
}
