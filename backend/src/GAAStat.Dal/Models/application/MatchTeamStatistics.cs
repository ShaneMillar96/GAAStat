using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GAAStat.Dal.Models.Application;

[Table("match_team_statistics")]
public partial class MatchTeamStatistics
{
    [Key]
    [Column("match_team_stat_id")]
    public int MatchTeamStatId { get; set; }

    [Required]
    [Column("match_id")]
    public int MatchId { get; set; }

    [Column("drum_first_half", TypeName = "decimal(10,4)")]
    public decimal? DrumFirstHalf { get; set; }

    [Column("drum_second_half", TypeName = "decimal(10,4)")]
    public decimal? DrumSecondHalf { get; set; }

    [Column("drum_full_game", TypeName = "decimal(10,4)")]
    public decimal? DrumFullGame { get; set; }

    [Column("opposition_first_half", TypeName = "decimal(10,4)")]
    public decimal? OppositionFirstHalf { get; set; }

    [Column("opposition_second_half", TypeName = "decimal(10,4)")]
    public decimal? OppositionSecondHalf { get; set; }

    [Column("opposition_full_game", TypeName = "decimal(10,4)")]
    public decimal? OppositionFullGame { get; set; }

    [Required]
    [Column("metric_definition_id")]
    public int MetricDefinitionId { get; set; }

    [ForeignKey("MatchId")]
    public virtual Match Match { get; set; } = null!;

    [ForeignKey("MetricDefinitionId")]
    public virtual MetricDefinition MetricDefinition { get; set; } = null!;
}