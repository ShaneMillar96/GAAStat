using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GAAStat.Dal.Models.Application;

[Table("match_results")]
public partial class MatchResult
{
    [Key]
    [Column("match_result_id")]
    public int MatchResultId { get; set; }

    [Required]
    [Column("result_code")]
    [StringLength(10)]
    public string ResultCode { get; set; } = string.Empty;

    [Required]
    [Column("result_description")]
    [StringLength(50)]
    public string ResultDescription { get; set; } = string.Empty;

    public virtual ICollection<Match> Matches { get; set; } = new List<Match>();
}