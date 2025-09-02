using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GAAStat.Dal.Models.Application;

[Table("matches")]
public partial class Match
{
    [Key]
    [Column("match_id")]
    public int MatchId { get; set; }

    [Column("match_number")]
    public int? MatchNumber { get; set; }

    [Required]
    [Column("date")]
    public DateOnly Date { get; set; }

    [Column("drum_score")]
    [StringLength(20)]
    public string? DrumScore { get; set; }

    [Column("opposition_score")]
    [StringLength(20)]
    public string? OppositionScore { get; set; }

    [Column("drum_goals")]
    public int DrumGoals { get; set; } = 0;

    [Column("drum_points")]
    public int DrumPoints { get; set; } = 0;

    [Column("opposition_goals")]
    public int OppositionGoals { get; set; } = 0;

    [Column("opposition_points")]
    public int OppositionPoints { get; set; } = 0;

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
    public virtual Competition? Competition { get; set; }

    [ForeignKey("SeasonId")]
    public virtual Season? Season { get; set; }

    [ForeignKey("VenueId")]
    public virtual Venue? Venue { get; set; }

    [ForeignKey("MatchResultId")]
    public virtual MatchResult? MatchResult { get; set; }

    [ForeignKey("OppositionId")]
    public virtual Team? Opposition { get; set; }

    public virtual ICollection<MatchTeamStatistics> MatchTeamStatistics { get; set; } = new List<MatchTeamStatistics>();
    public virtual ICollection<MatchPlayerStatistics> MatchPlayerStatistics { get; set; } = new List<MatchPlayerStatistics>();
    public virtual ICollection<KickoutAnalysis> KickoutAnalyses { get; set; } = new List<KickoutAnalysis>();
    public virtual ICollection<ShotAnalysis> ShotAnalyses { get; set; } = new List<ShotAnalysis>();
    public virtual ICollection<ScoreableFreeAnalysis> ScoreableFreeAnalyses { get; set; } = new List<ScoreableFreeAnalysis>();
    public virtual ICollection<PositionalAnalysis> PositionalAnalyses { get; set; } = new List<PositionalAnalysis>();
}