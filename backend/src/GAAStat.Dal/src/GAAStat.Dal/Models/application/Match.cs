using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.src.GAAStat.Dal.Models.application;

/// <summary>
/// Match information, results, and basic statistics
/// </summary>
[Table("matches")]
[Index("VenueId", "SeasonId", "MatchResultId", Name = "idx_match_results_venue_season")]
[Index("CompetitionId", Name = "idx_matches_competition_id")]
[Index("Date", Name = "idx_matches_date")]
[Index("OppositionId", Name = "idx_matches_opposition_id")]
[Index("SeasonId", "Date", Name = "idx_matches_season_date")]
[Index("SeasonId", Name = "idx_matches_season_id")]
[Index("CompetitionId", "SeasonId", "Date", Name = "idx_team_stats_comp_season")]
public partial class Match
{
    [Key]
    [Column("match_id")]
    public int MatchId { get; set; }

    /// <summary>
    /// Sequential match number for the season
    /// </summary>
    [Column("match_number")]
    public int? MatchNumber { get; set; }

    [Column("date")]
    public DateOnly Date { get; set; }

    /// <summary>
    /// Formatted score string for home team (e.g., &quot;2-12&quot;)
    /// </summary>
    [Column("drum_score")]
    [StringLength(20)]
    public string? DrumScore { get; set; }

    /// <summary>
    /// Formatted score string for opposition (e.g., &quot;1-08&quot;)
    /// </summary>
    [Column("opposition_score")]
    [StringLength(20)]
    public string? OppositionScore { get; set; }

    /// <summary>
    /// Number of goals scored by home team
    /// </summary>
    [Column("drum_goals")]
    public int? DrumGoals { get; set; }

    /// <summary>
    /// Number of points scored by home team
    /// </summary>
    [Column("drum_points")]
    public int? DrumPoints { get; set; }

    /// <summary>
    /// Number of goals scored by opposition
    /// </summary>
    [Column("opposition_goals")]
    public int? OppositionGoals { get; set; }

    /// <summary>
    /// Number of points scored by opposition
    /// </summary>
    [Column("opposition_points")]
    public int? OppositionPoints { get; set; }

    /// <summary>
    /// Total point difference (positive = home win)
    /// </summary>
    [Column("point_difference")]
    public int? PointDifference { get; set; }

    [Column("competition_id")]
    public int? CompetitionId { get; set; }

    [Column("season_id")]
    public int? SeasonId { get; set; }

    [Column("venue_id")]
    public int? VenueId { get; set; }

    [Column("match_result_id")]
    public int? MatchResultId { get; set; }

    [Column("opposition_id")]
    public int? OppositionId { get; set; }

    [ForeignKey("CompetitionId")]
    [InverseProperty("Matches")]
    public virtual Competition? Competition { get; set; }

    [InverseProperty("Match")]
    public virtual ICollection<KickoutAnalysis> KickoutAnalyses { get; set; } = new List<KickoutAnalysis>();

    [InverseProperty("Match")]
    public virtual ICollection<MatchPlayerStatistic> MatchPlayerStatistics { get; set; } = new List<MatchPlayerStatistic>();

    [ForeignKey("MatchResultId")]
    [InverseProperty("Matches")]
    public virtual MatchResult? MatchResult { get; set; }

    [InverseProperty("Match")]
    public virtual ICollection<MatchTeamStatistic> MatchTeamStatistics { get; set; } = new List<MatchTeamStatistic>();

    [ForeignKey("OppositionId")]
    [InverseProperty("Matches")]
    public virtual Team? Opposition { get; set; }

    [InverseProperty("Match")]
    public virtual ICollection<PositionalAnalysis> PositionalAnalyses { get; set; } = new List<PositionalAnalysis>();

    [InverseProperty("Match")]
    public virtual ICollection<ScoreableFreeAnalysis> ScoreableFreeAnalyses { get; set; } = new List<ScoreableFreeAnalysis>();

    [ForeignKey("SeasonId")]
    [InverseProperty("Matches")]
    public virtual Season? Season { get; set; }

    [InverseProperty("Match")]
    public virtual ICollection<ShotAnalysis> ShotAnalyses { get; set; } = new List<ShotAnalysis>();

    [ForeignKey("VenueId")]
    [InverseProperty("Matches")]
    public virtual Venue? Venue { get; set; }
}
