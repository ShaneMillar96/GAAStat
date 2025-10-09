using System;
using System.Collections.Generic;

namespace GAAStat.Dal.Models.Application;

/// <summary>
/// Represents a player position (GK, DEF, MID, FWD)
/// </summary>
public class Position
{
    /// <summary>
    /// Primary key
    /// </summary>
    public int PositionId { get; set; }

    /// <summary>
    /// Position name (e.g., "Goalkeeper", "Defender")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Position code (e.g., "GK", "DEF", "MID", "FWD")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Display order in UI (1-4)
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Record creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    /// <summary>
    /// Players in this position
    /// </summary>
    public virtual ICollection<Player> Players { get; set; } = new List<Player>();
}
