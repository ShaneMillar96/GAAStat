using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.src.GAAStat.Dal.Models.application;

/// <summary>
/// Classification of competition types (League, Championship, Cup)
/// </summary>
[Table("competition_types")]
[Index("TypeName", Name = "competition_types_type_name_key", IsUnique = true)]
public partial class CompetitionType
{
    [Key]
    [Column("competition_type_id")]
    public int CompetitionTypeId { get; set; }

    [Column("type_name")]
    [StringLength(50)]
    public string TypeName { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [InverseProperty("CompetitionType")]
    public virtual ICollection<Competition> Competitions { get; set; } = new List<Competition>();
}
