using System.Diagnostics;
using GAAStat.Services.ETL.Models;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;

namespace GAAStat.Services.ETL.Readers;

/// <summary>
/// Reads player position information from Excel position sheets.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>
/// Reads 4 position-specific sheets (Goalkeepers, Defenders, Midfielders, Forwards)
/// to create a mapping of players to their positions. This mapping is used during
/// ETL Phase 1.5 to enrich player statistics with position codes before database loading.
/// </para>
///
/// <para><strong>Sheet Detection Strategy:</strong></para>
/// <para>
/// Uses case-insensitive dictionary lookup to find position sheets by name.
/// Supports both plural forms (e.g., "Goalkeepers") and handles Excel's sheet name
/// truncation (31-character limit).
/// </para>
///
/// <para><strong>Error Handling:</strong></para>
/// <para>
/// - Missing sheets: Logged as WARNING, continue with other sheets
/// - Corrupted sheets: Logged as ERROR, skip sheet
/// - No sheets found: Returns Failure result
/// - Duplicate players: Logged as WARNING, last position wins
/// </para>
/// </remarks>
public class ExcelPositionSheetReader : IExcelPositionSheetReader
{
    private readonly ILogger<ExcelPositionSheetReader> _logger;

    /// <summary>
    /// Maps expected position sheet names to their corresponding position codes.
    /// Uses case-insensitive comparison to handle naming variations.
    /// </summary>
    private static readonly Dictionary<string, string> PositionSheetPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Goalkeepers", "GK" },
        { "Defenders", "DEF" },
        { "Midfielders", "MID" },
        { "Forwards", "FWD" }
    };

    /// <summary>
    /// First row containing player data (row 4 contains first player).
    /// </summary>
    private const int DataStartRow = 4;

    /// <summary>
    /// Column containing player names (Column B).
    /// Position sheets contain ONLY names, no jersey numbers.
    /// </summary>
    private const int PlayerNameColumn = 2;

    /// <summary>
    /// Number of rows between players in position sheets.
    /// Players are listed at rows 4, 32, 60, 88, etc. (every 28 rows).
    /// </summary>
    private const int RowIncrement = 28;

    public ExcelPositionSheetReader(ILogger<ExcelPositionSheetReader> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<PositionMappingResult> ReadPositionMappingsAsync(ExcelPackage package)
    {
        ArgumentNullException.ThrowIfNull(package);

        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting position sheet reading (Phase 0)...");

        var mappings = new Dictionary<string, string>();
        var duplicateWarnings = new List<string>();
        int sheetsProcessed = 0;

        // Process each expected position sheet
        foreach (var (sheetName, positionCode) in PositionSheetPatterns)
        {
            try
            {
                var sheet = FindPositionSheet(package, sheetName);
                if (sheet == null)
                {
                    _logger.LogWarning(
                        "Position sheet '{SheetName}' not found. Position detection for {PositionCode} will rely on inference.",
                        sheetName,
                        positionCode);
                    continue;
                }

                _logger.LogDebug("Reading position sheet: {SheetName} → {PositionCode}", sheet.Name, positionCode);

                int playersRead = await ReadPlayersFromSheetAsync(
                    sheet,
                    positionCode,
                    mappings,
                    duplicateWarnings);

                _logger.LogInformation(
                    "Position sheet '{SheetName}' processed: {PlayerCount} players mapped to {PositionCode}",
                    sheet.Name,
                    playersRead,
                    positionCode);

                sheetsProcessed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to read position sheet '{SheetName}'. Position detection for {PositionCode} will rely on inference.",
                    sheetName,
                    positionCode);
            }
        }

        stopwatch.Stop();

        // Log duplicate warnings
        foreach (var warning in duplicateWarnings)
        {
            _logger.LogWarning("Duplicate player detected: {Warning}", warning);
        }

        // Summary logging
        if (sheetsProcessed == 0)
        {
            _logger.LogError(
                "No position sheets found. Position detection will rely entirely on goalkeeper inference. " +
                "This may result in many players with unknown positions.");
            return PositionMappingResult.Failure(stopwatch.ElapsedMilliseconds);
        }

        _logger.LogInformation(
            "Position sheet reading completed: {SheetCount}/{ExpectedCount} sheets processed, " +
            "{PlayerCount} unique players mapped, {DuplicateCount} duplicates found, {ElapsedMs}ms elapsed",
            sheetsProcessed,
            PositionSheetPatterns.Count,
            mappings.Count,
            duplicateWarnings.Count,
            stopwatch.ElapsedMilliseconds);

        return PositionMappingResult.Success(
            mappings,
            sheetsProcessed,
            duplicateWarnings,
            stopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// Finds a position sheet by name using case-insensitive matching.
    /// </summary>
    /// <param name="package">The Excel package to search.</param>
    /// <param name="sheetName">The expected sheet name (e.g., "Goalkeepers").</param>
    /// <returns>The matching worksheet, or null if not found.</returns>
    /// <remarks>
    /// Handles case variations (e.g., "goalkeepers", "GOALKEEPERS", "Goalkeepers").
    /// Also handles potential truncation from Excel's 31-character sheet name limit.
    /// </remarks>
    private ExcelWorksheet? FindPositionSheet(ExcelPackage package, string sheetName)
    {
        // Try exact case-insensitive match first
        var sheet = package.Workbook.Worksheets
            .FirstOrDefault(ws => ws.Name.Equals(sheetName, StringComparison.OrdinalIgnoreCase));

        if (sheet != null)
        {
            return sheet;
        }

        // Try prefix match for truncated names (31-char limit)
        // Example: "Goalkeepers" might be truncated if combined with other text
        sheet = package.Workbook.Worksheets
            .FirstOrDefault(ws => ws.Name.StartsWith(sheetName[..Math.Min(sheetName.Length, 25)],
                                                     StringComparison.OrdinalIgnoreCase));

        return sheet;
    }

    /// <summary>
    /// Reads all players from a position sheet and adds them to the mappings dictionary.
    /// </summary>
    /// <param name="sheet">The Excel worksheet to read.</param>
    /// <param name="positionCode">The position code to assign (e.g., "GK", "DEF").</param>
    /// <param name="mappings">The dictionary to populate with player mappings.</param>
    /// <param name="duplicateWarnings">List to collect duplicate player warnings.</param>
    /// <returns>The number of players read from the sheet.</returns>
    /// <remarks>
    /// <para><strong>Sheet Structure:</strong></para>
    /// <para>
    /// - Rows 1-3: Headers (ignored)
    /// - Row 4, 32, 60, 88... (every 28 rows): Player data
    /// - Column B: Player name (string only, NO jersey numbers in position sheets)
    /// </para>
    ///
    /// <para><strong>Empty Row Detection:</strong></para>
    /// <para>
    /// Reading stops when:
    /// - Player name column (B) is empty
    /// - End of sheet reached
    /// </para>
    ///
    /// <para><strong>Duplicate Handling:</strong></para>
    /// <para>
    /// If a player appears multiple times (same name):
    /// - Last occurrence wins
    /// - Warning logged with format: "Player 'Name' appears in multiple position sheets. Previous: CODE1, Current: CODE2. Last occurrence wins."
    /// </para>
    /// </remarks>
    private Task<int> ReadPlayersFromSheetAsync(
        ExcelWorksheet sheet,
        string positionCode,
        Dictionary<string, string> mappings,
        List<string> duplicateWarnings)
    {
        int playersRead = 0;
        int currentRow = DataStartRow;

        // Read until empty row encountered (rows 4, 32, 60, 88...)
        while (currentRow <= sheet.Dimension?.End.Row)
        {
            try
            {
                // Extract player name (Column B only - no jersey numbers in position sheets)
                var nameCell = sheet.Cells[currentRow, PlayerNameColumn];
                var playerName = nameCell.Text?.Trim();

                if (string.IsNullOrWhiteSpace(playerName))
                {
                    _logger.LogDebug(
                        "Empty player name at row {Row} in sheet '{SheetName}'. Stopping player extraction.",
                        currentRow,
                        sheet.Name);
                    break;
                }

                // Normalize player name for identification
                var normalizedName = PlayerIdentifier.NormalizeName(playerName);

                // Check for duplicate
                if (mappings.ContainsKey(normalizedName))
                {
                    var previousPosition = mappings[normalizedName];
                    duplicateWarnings.Add(
                        $"Player '{playerName}' appears in multiple position sheets. " +
                        $"Previous: {previousPosition}, Current: {positionCode}. Last occurrence wins.");
                }

                // Add/update mapping (last occurrence wins)
                mappings[normalizedName] = positionCode;
                playersRead++;

                _logger.LogTrace(
                    "Mapped player: '{PlayerName}' → {PositionCode}",
                    playerName,
                    positionCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error reading row {Row} in sheet '{SheetName}'. Skipping row.",
                    currentRow,
                    sheet.Name);
            }

            // Jump to next player (every 28 rows)
            currentRow += RowIncrement;
        }

        return Task.FromResult(playersRead);
    }
}
