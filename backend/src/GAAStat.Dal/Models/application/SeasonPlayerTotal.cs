using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GAAStat.Dal.Models.Application;

[Table("season_player_totals")]
public partial class SeasonPlayerTotal
{
    [Key]
    [Column("season_total_id")]
    public int SeasonTotalId { get; set; }

    [Required]
    [Column("player_id")]
    public int PlayerId { get; set; }

    [Required]
    [Column("season_id")]
    public int SeasonId { get; set; }

    [Column("games_played")]
    public int GamesPlayed { get; set; } = 0;

    [Column("total_minutes")]
    public int TotalMinutes { get; set; } = 0;

    [Column("avg_engagement_efficiency", TypeName = "decimal(5,4)")]
    public decimal? AvgEngagementEfficiency { get; set; }

    [Column("avg_possession_success_rate", TypeName = "decimal(5,4)")]
    public decimal? AvgPossessionSuccessRate { get; set; }

    [Column("total_scores")]
    public int TotalScores { get; set; } = 0;

    [Column("total_goals")]
    public int TotalGoals { get; set; } = 0;

    [Column("total_points")]
    public int TotalPoints { get; set; } = 0;

    [Column("avg_conversion_rate", TypeName = "decimal(5,4)")]
    public decimal? AvgConversionRate { get; set; }

    [Column("total_tackles")]
    public int TotalTackles { get; set; } = 0;

    [Column("avg_tackle_success_rate", TypeName = "decimal(5,4)")]
    public decimal? AvgTackleSuccessRate { get; set; }

    [Column("total_turnovers_won")]
    public int TotalTurnoversWon { get; set; } = 0;

    [Column("total_interceptions")]
    public int TotalInterceptions { get; set; } = 0;

    [ForeignKey("PlayerId")]
    public virtual Player Player { get; set; } = null!;

    [ForeignKey("SeasonId")]
    public virtual Season Season { get; set; } = null!;
}