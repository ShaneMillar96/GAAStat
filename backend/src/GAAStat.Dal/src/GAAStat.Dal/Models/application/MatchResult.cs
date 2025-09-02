using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.src.GAAStat.Dal.Models.application;

/// <summary>
/// Match outcome types (Win, Loss, Draw)
/// </summary>
[Table("match_results")]
[Index("ResultCode", Name = "match_results_result_code_key", IsUnique = true)]
public partial class MatchResult
{
    [Key]
    [Column("match_result_id")]
    public int MatchResultId { get; set; }

    [Column("result_code")]
    [StringLength(10)]
    public string ResultCode { get; set; } = null!;

    [Column("result_description")]
    [StringLength(50)]
    public string ResultDescription { get; set; } = null!;

    [InverseProperty("MatchResult")]
    public virtual ICollection<Match> Matches { get; set; } = new List<Match>();
}
