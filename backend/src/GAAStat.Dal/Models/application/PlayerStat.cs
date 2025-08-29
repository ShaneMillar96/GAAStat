using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GAAStat.Dal.Models.application;

public partial class PlayerStat
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int MatchId { get; set; }

    [Required]
    public int PlayerId { get; set; }

    public int MinutesPlayed { get; set; }

    public int PointsScored { get; set; }

    public int GoalsScored { get; set; }

    public int Assists { get; set; }

    public int Turnovers { get; set; }

    public int FoulsCommitted { get; set; }

    public int FoulsDrawn { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [ForeignKey("MatchId")]
    public virtual Match Match { get; set; } = null!;

    [ForeignKey("PlayerId")]
    public virtual Player Player { get; set; } = null!;
}