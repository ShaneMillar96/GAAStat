using System;
using System.Collections.Generic;

namespace GAAStat.Dal.Models.Application;

/// <summary>
/// Represents a GAA player
/// </summary>
public class Player
{
    /// <summary>
    /// Primary key
    /// </summary>
    public int PlayerId { get; set; }

    /// <summary>
    /// Jersey/squad number (1-99)
    /// </summary>
    public int JerseyNumber { get; set; }

    /// <summary>
    /// Player's first name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Player's last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Player's full name
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to Position
    /// </summary>
    public int PositionId { get; set; }

    /// <summary>
    /// False for retired/inactive players
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Record creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Record last updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    /// <summary>
    /// The player's position
    /// </summary>
    public virtual Position Position { get; set; } = null!;

    /// <summary>
    /// Player's match statistics
    /// </summary>
    public virtual ICollection<PlayerMatchStatistics> MatchStatistics { get; set; } = new List<PlayerMatchStatistics>();
}
