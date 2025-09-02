using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GAAStat.Dal.Models.Application;

[Table("players")]
public partial class Player
{
    [Key]
    [Column("player_id")]
    public int PlayerId { get; set; }

    [Required]
    [Column("player_name")]
    [StringLength(100)]
    public string PlayerName { get; set; } = string.Empty;

    [Column("jersey_number")]
    public int? JerseyNumber { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("position_id")]
    public int? PositionId { get; set; }

    [ForeignKey("PositionId")]
    public virtual Position? Position { get; set; }

    public virtual ICollection<MatchPlayerStatistics> MatchPlayerStatistics { get; set; } = new List<MatchPlayerStatistics>();
    public virtual ICollection<ShotAnalysis> ShotAnalyses { get; set; } = new List<ShotAnalysis>();
    public virtual ICollection<ScoreableFreeAnalysis> ScoreableFreeAnalyses { get; set; } = new List<ScoreableFreeAnalysis>();
    public virtual ICollection<SeasonPlayerTotal> SeasonPlayerTotals { get; set; } = new List<SeasonPlayerTotal>();
}