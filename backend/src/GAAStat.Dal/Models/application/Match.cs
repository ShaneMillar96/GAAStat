using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GAAStat.Dal.Models.application;

public partial class Match
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int HomeTeamId { get; set; }

    [Required]
    public int AwayTeamId { get; set; }

    public DateTime MatchDate { get; set; }

    [StringLength(100)]
    public string? Venue { get; set; }

    [StringLength(100)]
    public string? Competition { get; set; }

    public int HomeScore { get; set; }

    public int AwayScore { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = "scheduled";

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [ForeignKey("HomeTeamId")]
    public virtual Team HomeTeam { get; set; } = null!;

    [ForeignKey("AwayTeamId")]
    public virtual Team AwayTeam { get; set; } = null!;

    public virtual ICollection<PlayerStat> PlayerStats { get; set; } = new List<PlayerStat>();
}