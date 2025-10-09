namespace GAAStat.Services.ETL.Models;

/// <summary>
/// Result returned from reading position sheets during ETL Phase 0.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>
/// Encapsulates the outcome of reading 4 position sheets (Goalkeepers, Defenders,
/// Midfielders, Forwards) from the Excel file. Contains both the position mappings
/// (dictionary) and diagnostic information (sheet count, duplicates, timing).
/// </para>
///
/// <para><strong>Success vs Partial Success:</strong></para>
/// <para>
/// - Full Success: 4 sheets found, all players mapped, no duplicates
/// - Partial Success: Some sheets missing/corrupted, but at least 1 sheet read successfully
/// - Failure: No sheets found or all sheets failed to read
/// </para>
///
/// <para><strong>Example Usage:</strong></para>
/// <code>
/// var result = await _positionReader.ReadPositionMappingsAsync(package);
///
/// if (result.Mappings.Count == 0)
/// {
///     _logger.LogWarning("No position mappings found. Using goalkeeper inference only.");
/// }
///
/// foreach (var duplicate in result.DuplicatePlayerWarnings)
/// {
///     _logger.LogWarning("Duplicate player: {Warning}", duplicate);
/// }
/// </code>
/// </remarks>
public class PositionMappingResult
{
    /// <summary>
    /// Dictionary mapping normalized player names to position codes.
    /// </summary>
    /// <remarks>
    /// <para><strong>Key:</strong> Normalized player name (trimmed, lowercase)</para>
    /// <para><strong>Value:</strong> Position code ("GK", "DEF", "MID", "FWD")</para>
    ///
    /// <para><strong>Performance:</strong></para>
    /// <para>
    /// O(1) lookups during Phase 1.5 enrichment. Dictionary size typically:
    /// - Goalkeepers: ~3 players
    /// - Defenders: ~60 players
    /// - Midfielders: ~50 players
    /// - Forwards: ~56 players
    /// - Total: ~169 unique players
    /// </para>
    ///
    /// <para><strong>Duplicate Handling:</strong></para>
    /// <para>
    /// If a player appears in multiple position sheets (e.g., in both DEF and MID),
    /// the last occurrence wins. Duplicates are logged in DuplicatePlayerWarnings.
    /// </para>
    /// </remarks>
    public Dictionary<string, string> Mappings { get; init; } = new();

    /// <summary>
    /// Number of position sheets successfully read (0-4).
    /// </summary>
    /// <remarks>
    /// <para>Expected value: 4 (Goalkeepers, Defenders, Midfielders, Forwards)</para>
    ///
    /// <para><strong>Values Meaning:</strong></para>
    /// <list type="bullet">
    ///   <item>4 = Full success - all expected sheets found</item>
    ///   <item>3 = Partial success - one sheet missing or corrupted</item>
    ///   <item>1-2 = Degraded - significant data missing</item>
    ///   <item>0 = Failure - fall back to goalkeeper inference only</item>
    /// </list>
    ///
    /// <para>
    /// When SheetsProcessed &lt; 4, check logs for specific sheet errors
    /// (e.g., "Sheet 'Defenders' not found").
    /// </para>
    /// </remarks>
    public int SheetsProcessed { get; init; }

    /// <summary>
    /// List of warnings for players appearing in multiple position sheets.
    /// </summary>
    /// <remarks>
    /// <para><strong>Warning Format:</strong></para>
    /// <para>"Player 'PlayerName' appears in multiple position sheets. Previous: OldCode, Current: NewCode. Last occurrence wins."</para>
    ///
    /// <para><strong>Example:</strong></para>
    /// <para>"Player 'John Smith' appears in multiple position sheets. Previous: DEF, Current: MID. Last occurrence wins."</para>
    ///
    /// <para><strong>Action Required:</strong></para>
    /// <para>
    /// Log each warning at WARNING level. High duplicate count (>10%) may indicate:
    /// - Data entry errors in Excel file
    /// - Players moved between positions mid-season
    /// - Incorrect position sheet structure
    /// </para>
    ///
    /// <para>
    /// If duplicates &gt; 10% of total players, consider manual review of Excel file.
    /// </para>
    /// </remarks>
    public List<string> DuplicatePlayerWarnings { get; init; } = new();

    /// <summary>
    /// Time taken to read all position sheets (milliseconds).
    /// </summary>
    /// <remarks>
    /// <para><strong>Performance Targets:</strong></para>
    /// <list type="bullet">
    ///   <item>&lt;500ms = Excellent (typical for 169 players)</item>
    ///   <item>500-800ms = Acceptable (high player count or slow I/O)</item>
    ///   <item>&gt;800ms = Investigate (possible performance issue)</item>
    /// </list>
    ///
    /// <para>
    /// Processing time impacts overall ETL pipeline duration:
    /// - Total ETL: ~6-7 seconds
    /// - Position reading (Phase 0): ~500ms (7-8% of total)
    /// - Acceptable overhead for unblocking player loading
    /// </para>
    /// </remarks>
    public long ProcessingTimeMs { get; init; }

    /// <summary>
    /// Indicates whether position mappings were successfully read.
    /// </summary>
    /// <remarks>
    /// <para><strong>Success Criteria:</strong></para>
    /// <para>
    /// At least one position sheet was successfully read (SheetsProcessed &gt; 0).
    /// </para>
    ///
    /// <para><strong>Failure Handling:</strong></para>
    /// <para>
    /// If IsSuccess == false:
    /// - Log ERROR with details of sheet reading failures
    /// - Fall back to goalkeeper inference only (GK stats detection)
    /// - Continue ETL with reduced functionality (graceful degradation)
    /// - Players without GK stats will have empty position codes → skip during loading
    /// </para>
    /// </remarks>
    public bool IsSuccess => SheetsProcessed > 0;

    /// <summary>
    /// Creates a successful result with position mappings.
    /// </summary>
    /// <param name="mappings">Dictionary of normalized player names to position codes.</param>
    /// <param name="sheetsProcessed">Number of sheets successfully read.</param>
    /// <param name="duplicateWarnings">List of duplicate player warnings.</param>
    /// <param name="processingTimeMs">Time taken to read all sheets (ms).</param>
    /// <returns>A new PositionMappingResult instance.</returns>
    public static PositionMappingResult Success(
        Dictionary<string, string> mappings,
        int sheetsProcessed,
        List<string> duplicateWarnings,
        long processingTimeMs)
    {
        return new PositionMappingResult
        {
            Mappings = mappings,
            SheetsProcessed = sheetsProcessed,
            DuplicatePlayerWarnings = duplicateWarnings,
            ProcessingTimeMs = processingTimeMs
        };
    }

    /// <summary>
    /// Creates a failed result with empty mappings.
    /// </summary>
    /// <param name="processingTimeMs">Time taken before failure (ms).</param>
    /// <returns>A new PositionMappingResult instance indicating failure.</returns>
    /// <remarks>
    /// Used when all position sheets are missing or corrupted.
    /// ETL pipeline continues with goalkeeper inference only.
    /// </remarks>
    public static PositionMappingResult Failure(long processingTimeMs)
    {
        return new PositionMappingResult
        {
            Mappings = new Dictionary<string, string>(),
            SheetsProcessed = 0,
            DuplicatePlayerWarnings = new List<string>(),
            ProcessingTimeMs = processingTimeMs
        };
    }
}
