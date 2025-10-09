namespace GAAStat.Services.ETL.Models;

/// <summary>
/// Uniquely identifies a player by normalized name.
/// Used for name normalization and comparison during ETL processing.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>
/// During ETL, position information comes from separate Excel sheets (Goalkeepers, Defenders, etc.)
/// while player statistics come from "Player Stats" sheets. This record type provides
/// consistent name normalization for player identification.
/// </para>
///
/// <para><strong>Name Normalization:</strong></para>
/// <para>
/// Player names are normalized (trimmed, lowercased) to handle minor inconsistencies
/// between position sheets and stats sheets (e.g., "John Smith" vs "john smith").
/// </para>
///
/// <para><strong>Value Equality:</strong></para>
/// <para>
/// As a record type, PlayerIdentifier provides automatic value-based equality.
/// Two instances with the same normalized PlayerName are considered equal.
/// </para>
///
/// <para><strong>Example Usage:</strong></para>
/// <code>
/// var player1 = PlayerIdentifier.Create("Cahair O Kane");
/// var player2 = PlayerIdentifier.Create("CAHAIR O KANE");
///
/// // True - name normalization ensures case-insensitive matching
/// Assert.Equal(player1, player2);
///
/// var positionMap = new Dictionary&lt;string, string&gt;
/// {
///     [player1.PlayerName] = "GK"
/// };
///
/// // O(1) lookup using normalized name
/// string? position = positionMap.GetValueOrDefault(player2.PlayerName); // Returns "GK"
/// </code>
/// </remarks>
public record PlayerIdentifier
{
    /// <summary>
    /// The player's normalized full name (trimmed, lowercase).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Normalization rules:
    /// <list type="bullet">
    ///   <item>Whitespace trimmed from start and end</item>
    ///   <item>Converted to lowercase for case-insensitive comparison</item>
    ///   <item>Empty strings if name is null</item>
    /// </list>
    /// </para>
    ///
    /// <para><strong>Known Limitations:</strong></para>
    /// <para>
    /// - Does not handle apostrophes (e.g., "O'Brien" vs "OBrien")
    /// - Does not handle special characters (diacritics, hyphens)
    /// - Does not handle middle initials (e.g., "John A Smith" vs "John Smith")
    /// - Does not handle spelling variations
    /// </para>
    ///
    /// <para>
    /// For cases where name normalization fails, the ETL pipeline falls back to
    /// goalkeeper inference (if GK stats present) or leaves position empty.
    /// </para>
    /// </remarks>
    public string PlayerName { get; init; } = string.Empty;

    /// <summary>
    /// Creates a new PlayerIdentifier with normalized name.
    /// </summary>
    /// <param name="playerName">The player's full name (will be normalized).</param>
    /// <returns>A new PlayerIdentifier instance with normalized name.</returns>
    /// <remarks>
    /// This is the recommended way to create PlayerIdentifier instances, as it
    /// ensures consistent name normalization across the application.
    /// </remarks>
    /// <example>
    /// <code>
    /// var player = PlayerIdentifier.Create("  Shane Millar  ");
    /// // player.PlayerName will be "shane millar" (trimmed, lowercase)
    /// </code>
    /// </example>
    public static PlayerIdentifier Create(string playerName)
    {
        return new PlayerIdentifier
        {
            PlayerName = NormalizeName(playerName)
        };
    }

    /// <summary>
    /// Normalizes a player name for consistent comparison.
    /// </summary>
    /// <param name="name">The raw player name from Excel.</param>
    /// <returns>Trimmed, lowercase name; empty string if null.</returns>
    /// <remarks>
    /// <para><strong>Normalization Strategy:</strong></para>
    /// <para>
    /// Simple normalization balances reliability with performance:
    /// - Handles common whitespace issues (leading/trailing spaces)
    /// - Case-insensitive matching (most common inconsistency)
    /// - Fast execution (&lt;1μs per name)
    /// </para>
    ///
    /// <para>
    /// More aggressive normalization (apostrophes, diacritics) was considered but
    /// rejected due to:
    /// - Risk of false positives (different players normalized to same name)
    /// - Increased complexity and maintenance burden
    /// - Goalkeeper inference provides fallback for GK position
    /// </para>
    /// </remarks>
    public static string NormalizeName(string? name)
    {
        return name?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    /// <summary>
    /// Returns a string representation of this PlayerIdentifier.
    /// </summary>
    /// <returns>The normalized player name (e.g., "cahair o kane").</returns>
    /// <remarks>
    /// Useful for logging and debugging. Shows the normalized name as stored.
    /// </remarks>
    public override string ToString()
    {
        return PlayerName;
    }
}
