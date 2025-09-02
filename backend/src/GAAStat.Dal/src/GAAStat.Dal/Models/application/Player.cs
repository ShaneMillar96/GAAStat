using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.src.GAAStat.Dal.Models.application;

/// <summary>
/// Player master data and position information
/// </summary>
[Table("players")]
[Index("JerseyNumber", Name = "idx_players_jersey_number")]
[Index("PlayerName", Name = "idx_players_name")]
[Index("PositionId", Name = "idx_players_position_id")]
public partial class Player
{
    [Key]
    [Column("player_id")]
    public int PlayerId { get; set; }

    /// <summary>
    /// Full name of the player
    /// </summary>
    [Column("player_name")]
    [StringLength(100)]
    public string PlayerName { get; set; } = null!;

    /// <summary>
    /// Jersey number (1-99)
    /// </summary>
    [Column("jersey_number")]
    public int? JerseyNumber { get; set; }

    /// <summary>
    /// Whether the player is currently active
    /// </summary>
    [Column("is_active")]
    public bool? IsActive { get; set; }

    [Column("position_id")]
    public int? PositionId { get; set; }

    [InverseProperty("Player")]
    public virtual ICollection<MatchPlayerStatistic> MatchPlayerStatistics { get; set; } = new List<MatchPlayerStatistic>();

    [ForeignKey("PositionId")]
    [InverseProperty("Players")]
    public virtual Position? Position { get; set; }

    [InverseProperty("Player")]
    public virtual ICollection<ScoreableFreeAnalysis> ScoreableFreeAnalyses { get; set; } = new List<ScoreableFreeAnalysis>();

    [InverseProperty("Player")]
    public virtual ICollection<SeasonPlayerTotal> SeasonPlayerTotals { get; set; } = new List<SeasonPlayerTotal>();

    [InverseProperty("Player")]
    public virtual ICollection<ShotAnalysis> ShotAnalyses { get; set; } = new List<ShotAnalysis>();
}
