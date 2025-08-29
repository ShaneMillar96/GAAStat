using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.Models.application;

/// <summary>
/// Specialized kickout analysis data for goalkeepers and field players
/// </summary>
[Table("match_kickout_stats")]
[Index("KickoutRetentionRate", Name = "idx_kickout_retention", AllDescending = true)]
[Index("MatchPlayerStatId", Name = "idx_kickout_stats_player")]
public partial class MatchKickoutStat
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("match_player_stat_id")]
    public int MatchPlayerStatId { get; set; }

    [Column("kickouts_taken")]
    public int? KickoutsTaken { get; set; }

    [Column("kickouts_won_clean")]
    public int? KickoutsWonClean { get; set; }

    [Column("kickouts_lost_clean")]
    public int? KickoutsLostClean { get; set; }

    [Column("kickouts_won_break")]
    public int? KickoutsWonBreak { get; set; }

    [Column("kickouts_lost_break")]
    public int? KickoutsLostBreak { get; set; }

    [Column("kickouts_won_short")]
    public int? KickoutsWonShort { get; set; }

    [Column("kickouts_lost_short")]
    public int? KickoutsLostShort { get; set; }

    [Column("kickouts_to_right")]
    public int? KickoutsToRight { get; set; }

    [Column("kickouts_to_left")]
    public int? KickoutsToLeft { get; set; }

    [Column("kickouts_down_middle")]
    public int? KickoutsDownMiddle { get; set; }

    [Column("kickout_retention_rate")]
    [Precision(8, 4)]
    public decimal? KickoutRetentionRate { get; set; }

    [Column("saves")]
    public int? Saves { get; set; }

    [Column("save_percentage")]
    [Precision(8, 4)]
    public decimal? SavePercentage { get; set; }

    [Column("imported_at", TypeName = "timestamp without time zone")]
    public DateTime ImportedAt { get; set; }

    [ForeignKey("MatchPlayerStatId")]
    [InverseProperty("MatchKickoutStats")]
    public virtual MatchPlayerStat MatchPlayerStat { get; set; } = null!;

    /// <summary>
    /// Calculated property for total kickouts taken
    /// </summary>
    [NotMapped]
    public int? TotalKickouts => KickoutsTaken;

    /// <summary>
    /// Calculated property for successful kickouts (clean wins + break wins + short wins)
    /// </summary>
    [NotMapped]
    public int? SuccessfulKickouts => 
        (KickoutsWonClean ?? 0) + (KickoutsWonBreak ?? 0) + (KickoutsWonShort ?? 0);

    /// <summary>
    /// Calculated property for preferred kickout direction based on highest count
    /// </summary>
    [NotMapped]
    public string? PreferredDirection
    {
        get
        {
            var right = KickoutsToRight ?? 0;
            var left = KickoutsToLeft ?? 0;
            var middle = KickoutsDownMiddle ?? 0;

            if (right >= left && right >= middle && right > 0)
                return "Right";
            if (left >= right && left >= middle && left > 0)
                return "Left";
            if (middle > 0)
                return "Middle";
            
            return "Unknown";
        }
    }
}
