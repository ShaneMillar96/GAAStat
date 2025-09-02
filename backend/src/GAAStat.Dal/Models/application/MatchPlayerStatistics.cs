using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GAAStat.Dal.Models.Application;

[Table("match_player_statistics")]
public partial class MatchPlayerStatistics
{
    [Key]
    [Column("match_player_stat_id")]
    public int MatchPlayerStatId { get; set; }

    [Required]
    [Column("match_id")]
    public int MatchId { get; set; }

    [Required]
    [Column("player_id")]
    public int PlayerId { get; set; }

    [Column("minutes_played")]
    public int MinutesPlayed { get; set; } = 0;

    [Column("total_engagements")]
    public int TotalEngagements { get; set; } = 0;

    [Column("engagement_efficiency", TypeName = "decimal(5,4)")]
    public decimal? EngagementEfficiency { get; set; }

    [Column("scores")]
    [StringLength(20)]
    public string? Scores { get; set; }

    [Column("possession_success_rate", TypeName = "decimal(5,4)")]
    public decimal? PossessionSuccessRate { get; set; }

    [Column("possessions_per_te", TypeName = "decimal(10,4)")]
    public decimal? PossessionsPerTe { get; set; }

    [Column("total_possessions")]
    public int TotalPossessions { get; set; } = 0;

    [Column("turnovers_won")]
    public int TurnoversWon { get; set; } = 0;

    [Column("interceptions")]
    public int Interceptions { get; set; } = 0;

    [Column("total_attacks")]
    public int TotalAttacks { get; set; } = 0;

    [Column("kick_retained")]
    public int KickRetained { get; set; } = 0;

    [Column("kick_lost")]
    public int KickLost { get; set; } = 0;

    [Column("carry_retained")]
    public int CarryRetained { get; set; } = 0;

    [Column("carry_lost")]
    public int CarryLost { get; set; } = 0;

    [Column("shots_total")]
    public int ShotsTotal { get; set; } = 0;

    [Column("goals")]
    public int Goals { get; set; } = 0;

    [Column("points")]
    public int Points { get; set; } = 0;

    [Column("wides")]
    public int Wides { get; set; } = 0;

    [Column("conversion_rate", TypeName = "decimal(5,4)")]
    public decimal? ConversionRate { get; set; }

    [Column("tackles_total")]
    public int TacklesTotal { get; set; } = 0;

    [Column("tackles_contact")]
    public int TacklesContact { get; set; } = 0;

    [Column("tackles_missed")]
    public int TacklesMissed { get; set; } = 0;

    [Column("tackle_percentage", TypeName = "decimal(5,4)")]
    public decimal? TacklePercentage { get; set; }

    [Column("frees_conceded_total")]
    public int FreesConcededTotal { get; set; } = 0;

    [Column("yellow_cards")]
    public int YellowCards { get; set; } = 0;

    [Column("black_cards")]
    public int BlackCards { get; set; } = 0;

    [Column("red_cards")]
    public int RedCards { get; set; } = 0;

    [Column("kickouts_total")]
    public int KickoutsTotal { get; set; } = 0;

    [Column("kickouts_retained")]
    public int KickoutsRetained { get; set; } = 0;

    [Column("kickouts_lost")]
    public int KickoutsLost { get; set; } = 0;

    [Column("kickout_percentage", TypeName = "decimal(5,4)")]
    public decimal? KickoutPercentage { get; set; }

    [Column("saves")]
    public int Saves { get; set; } = 0;

    [ForeignKey("MatchId")]
    public virtual Match Match { get; set; } = null!;

    [ForeignKey("PlayerId")]
    public virtual Player Player { get; set; } = null!;
}