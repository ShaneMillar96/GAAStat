using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.src.GAAStat.Dal.Models.application;

/// <summary>
/// Detailed team-level performance metrics (235+ data points per match)
/// </summary>
[Table("match_team_statistics")]
[Index("MatchId", Name = "idx_match_team_stats_match_id")]
[Index("MatchId", "MetricDefinitionId", Name = "idx_match_team_stats_match_metric")]
[Index("MetricDefinitionId", Name = "idx_match_team_stats_metric_id")]
public partial class MatchTeamStatistic
{
    [Key]
    [Column("match_team_stat_id")]
    public int MatchTeamStatId { get; set; }

    [Column("match_id")]
    public int MatchId { get; set; }

    /// <summary>
    /// Home team metric value for first half
    /// </summary>
    [Column("drum_first_half")]
    [Precision(10, 4)]
    public decimal? DrumFirstHalf { get; set; }

    /// <summary>
    /// Home team metric value for second half
    /// </summary>
    [Column("drum_second_half")]
    [Precision(10, 4)]
    public decimal? DrumSecondHalf { get; set; }

    /// <summary>
    /// Home team metric value for full game
    /// </summary>
    [Column("drum_full_game")]
    [Precision(10, 4)]
    public decimal? DrumFullGame { get; set; }

    [Column("opposition_first_half")]
    [Precision(10, 4)]
    public decimal? OppositionFirstHalf { get; set; }

    [Column("opposition_second_half")]
    [Precision(10, 4)]
    public decimal? OppositionSecondHalf { get; set; }

    [Column("opposition_full_game")]
    [Precision(10, 4)]
    public decimal? OppositionFullGame { get; set; }

    [Column("metric_definition_id")]
    public int MetricDefinitionId { get; set; }

    [ForeignKey("MatchId")]
    [InverseProperty("MatchTeamStatistics")]
    public virtual Match Match { get; set; } = null!;

    [ForeignKey("MetricDefinitionId")]
    [InverseProperty("MatchTeamStatistics")]
    public virtual MetricDefinition MetricDefinition { get; set; } = null!;
}
