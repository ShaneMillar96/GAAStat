using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GAAStat.Dal.Contexts;
using GAAStat.Dal.Models.Application;
using GAAStat.Services.ETL.Helpers;
using GAAStat.Services.ETL.Interfaces;
using GAAStat.Services.ETL.Models;
using GAAStat.Services.ETL.Readers;
using GAAStat.Services.ETL.Services;
using GAAStat.Services.ETL.Transformers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;

namespace GAAStat.Services.ETL;

/// <summary>
/// Main orchestrator for player statistics ETL pipeline.
/// Coordinates Extract → Transform → Load operations for 86-field player statistics.
/// </summary>
public class PlayerStatisticsEtlService : IPlayerStatisticsEtlService
{
    private readonly GAAStatDbContext _dbContext;
    private readonly ExcelPlayerDataReader _excelReader;
    private readonly PlayerDataTransformer _transformer;
    private readonly PlayerDataLoader _dataLoader;
    private readonly PlayerRosterService _rosterService;
    private readonly PositionDetectionService _positionService;
    private readonly IExcelPositionSheetReader _positionReader;
    private readonly ILogger<PlayerStatisticsEtlService> _logger;

    public PlayerStatisticsEtlService(
        GAAStatDbContext dbContext,
        PlayerDataLoader dataLoader,
        PlayerRosterService rosterService,
        PositionDetectionService positionService,
        IExcelPositionSheetReader positionReader,
        ILogger<PlayerStatisticsEtlService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));
        _rosterService = rosterService ?? throw new ArgumentNullException(nameof(rosterService));
        _positionService = positionService ?? throw new ArgumentNullException(nameof(positionService));
        _positionReader = positionReader ?? throw new ArgumentNullException(nameof(positionReader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize ETL components
        _excelReader = new ExcelPlayerDataReader();
        _transformer = new PlayerDataTransformer();
    }

    /// <summary>
    /// Processes player statistics from Excel file.
    /// Orchestrates full ETL pipeline: Extract → Transform → Load.
    /// </summary>
    /// <param name="filePath">Absolute path to Excel file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ETL result with detailed statistics</returns>
    public async Task<PlayerEtlResult> ProcessPlayerStatisticsAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var result = new PlayerEtlResult
        {
            Success = false,
            StartTime = DateTime.UtcNow
        };

        var overallStopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting player statistics ETL for file: {FilePath}", filePath);

            // === PHASE 0: POSITION ENRICHMENT ===
            _logger.LogInformation("Phase 0: Reading position mappings from Excel...");
            PositionMappingResult positionMappings;

            using (var package = new OfficeOpenXml.ExcelPackage(new System.IO.FileInfo(filePath)))
            {
                positionMappings = await _positionReader.ReadPositionMappingsAsync(package);
            }

            _logger.LogInformation(
                "Position mappings loaded: {SheetsProcessed}/{ExpectedSheets} sheets, {PlayerCount} players, {DuplicateCount} duplicates, {ElapsedMs}ms",
                positionMappings.SheetsProcessed,
                4, // Expected sheet count
                positionMappings.Mappings.Count,
                positionMappings.DuplicatePlayerWarnings.Count,
                positionMappings.ProcessingTimeMs);

            // === PHASE 1: EXTRACT ===
            _logger.LogInformation("Phase 1: Extracting player data from Excel...");
            var sheets = await _excelReader.ReadPlayerStatsSheetsAsync(filePath, cancellationToken);

            if (sheets.Count == 0)
            {
                var errorMessage = "No player statistics sheets found in Excel file";
                result.AddError("NO_SHEETS", errorMessage);
                result.EndTime = DateTime.UtcNow;
                _logger.LogWarning(errorMessage);
                return result;
            }

            _logger.LogInformation("Found {SheetCount} player statistics sheets", sheets.Count);
            result.PlayerSheetsProcessed = sheets.Count;

            // === PHASE 1.5: POSITION ENRICHMENT ===
            _logger.LogInformation("Phase 1.5: Enriching player data with positions...");
            EnrichPlayerDataWithPositions(sheets, positionMappings.Mappings);

            // === PHASE 2: TRANSFORM ===
            _logger.LogInformation("Phase 2: Validating and transforming data...");
            var validationResults = _transformer.TransformAndValidateMultiple(sheets, throwOnError: false);

            // Log validation summary
            var validationSummary = _transformer.GetValidationSummary(validationResults);
            _logger.LogInformation("Validation completed:\n{Summary}", validationSummary);

            // Count validation errors/warnings
            result.ValidationErrorsTotal = validationResults.Sum(vr => vr.Value.Errors.Count);
            result.ValidationWarningsTotal = validationResults.Sum(vr => vr.Value.Warnings.Count);

            // Group errors by type
            result.ErrorsByType = CountErrorsByType(validationResults);

            // === PHASE 3: LOAD ===
            _logger.LogInformation("Phase 3: Loading data into database...");

            var sheetProcessingTimes = new List<TimeSpan>();
            int totalFieldsProcessed = 0;
            int totalPlayersCreated = 0;
            int totalPlayersUpdated = 0;
            int totalStatisticsCreated = 0;
            int totalPlayersSkipped = 0;

            foreach (var sheet in sheets)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var sheetStopwatch = Stopwatch.StartNew();

                try
                {
                    // Check for critical errors that would prevent loading
                    var sheetValidation = validationResults[sheet.SheetName];
                    if (_transformer.HasCriticalErrors(sheetValidation))
                    {
                        _logger.LogWarning(
                            "Skipping sheet '{SheetName}' due to critical validation errors",
                            sheet.SheetName);
                        totalPlayersSkipped += sheet.Players.Count;
                        continue;
                    }

                    // Find corresponding match in database
                    var match = await FindMatchAsync(sheet, cancellationToken);
                    if (match == null)
                    {
                        _logger.LogWarning(
                            "Could not find match for sheet '{SheetName}' (Match #{MatchNumber} vs {Opposition} on {MatchDate}). Skipping.",
                            sheet.SheetName, sheet.MatchNumber, sheet.Opposition, sheet.MatchDate);
                        totalPlayersSkipped += sheet.Players.Count;
                        continue;
                    }

                    // Check if statistics already exist for this match
                    if (await _dataLoader.StatisticsExistForMatchAsync(match.MatchId, cancellationToken))
                    {
                        _logger.LogInformation(
                            "Statistics already exist for match {MatchId} (vs {Opposition}). Skipping.",
                            match.MatchId, sheet.Opposition);
                        totalPlayersSkipped += sheet.Players.Count;
                        continue;
                    }

                    // Load player statistics for this match
                    var (created, updated, skipped) = await _dataLoader.LoadPlayerStatisticsAsync(
                        sheet,
                        match.MatchId,
                        cancellationToken);

                    totalPlayersCreated += created;
                    totalPlayersUpdated += updated;
                    totalStatisticsCreated += created;
                    totalPlayersSkipped += skipped;

                    // Count fields processed (86 fields per player)
                    totalFieldsProcessed += sheet.Players.Count * 86;

                    sheetStopwatch.Stop();
                    sheetProcessingTimes.Add(sheetStopwatch.Elapsed);

                    _logger.LogInformation(
                        "Processed sheet '{SheetName}' in {ElapsedMs}ms: {Created} stats created, {Skipped} skipped",
                        sheet.SheetName, sheetStopwatch.ElapsedMilliseconds, created, skipped);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing sheet '{SheetName}'", sheet.SheetName);
                    totalPlayersSkipped += sheet.Players.Count;

                    // Record error in result
                    result.ErrorsByType.TryGetValue("LoadError", out var count);
                    result.ErrorsByType["LoadError"] = count + 1;
                }
            }

            // === FINALIZE RESULT ===
            overallStopwatch.Stop();

            result.Success = totalStatisticsCreated > 0;
            result.PlayersCreated = totalPlayersCreated;
            result.PlayersUpdated = totalPlayersUpdated;
            result.PlayerStatisticsCreated = totalStatisticsCreated;
            result.PlayersSkipped = totalPlayersSkipped;
            result.FieldsProcessedTotal = totalFieldsProcessed;
            result.EndTime = DateTime.UtcNow;

            if (sheetProcessingTimes.Any())
            {
                result.AverageSheetProcessingTime = TimeSpan.FromMilliseconds(
                    sheetProcessingTimes.Average(t => t.TotalMilliseconds));
            }

            _logger.LogInformation(
                "Player statistics ETL completed in {ElapsedSeconds}s\n{DetailedSummary}",
                result.Duration.TotalSeconds,
                result.GetDetailedSummary());

            return result;
        }
        catch (Exception ex)
        {
            overallStopwatch.Stop();

            result.Success = false;
            result.EndTime = DateTime.UtcNow;
            result.AddError("ETL_EXCEPTION", $"ETL failed with exception: {ex.Message}");

            _logger.LogError(ex, "Player statistics ETL failed");

            return result;
        }
    }

    /// <summary>
    /// Finds match in database by match number, opposition, and date.
    /// Handles truncated sheet names where date is unknown (DateTime.MinValue).
    /// </summary>
    private async Task<Match?> FindMatchAsync(
        PlayerStatsSheetData sheet,
        CancellationToken cancellationToken)
    {
        // Handle truncated sheet names where date is unknown (DateTime.MinValue)
        if (sheet.MatchDate == DateTime.MinValue)
        {
            // Try match by number only (should be unique within season)
            var matchByNumber = await _dbContext.Matches
                .Include(m => m.AwayTeam)
                .Where(m => m.MatchNumber == sheet.MatchNumber)
                .FirstOrDefaultAsync(cancellationToken);

            if (matchByNumber != null)
            {
                _logger.LogInformation(
                    "Matched sheet '{SheetName}' to match #{MatchNumber} vs {Team} on {Date} (by number only)",
                    sheet.SheetName, matchByNumber.MatchNumber, matchByNumber.AwayTeam.Name, matchByNumber.MatchDate);
                return matchByNumber;
            }

            // If multiple matches with same number exist, try using partial opposition name
            if (!string.IsNullOrEmpty(sheet.Opposition) && sheet.Opposition != "Unknown")
            {
                var normalizedOpposition = sheet.Opposition.Trim().ToLower();
                var matchByNumberAndOpposition = await _dbContext.Matches
                    .Include(m => m.AwayTeam)
                    .Where(m => m.MatchNumber == sheet.MatchNumber &&
                               m.AwayTeam.Name.ToLower().Contains(normalizedOpposition))
                    .FirstOrDefaultAsync(cancellationToken);

                if (matchByNumberAndOpposition != null)
                    return matchByNumberAndOpposition;
            }

            _logger.LogWarning(
                "Could not find match for sheet '{SheetName}' with match number {MatchNumber} and opposition '{Opposition}'",
                sheet.SheetName, sheet.MatchNumber, sheet.Opposition);
            return null;
        }

        // Standard matching with full metadata
        // Try exact match on match number and date first
        var match = await _dbContext.Matches
            .Where(m => m.MatchNumber == sheet.MatchNumber &&
                       m.MatchDate.Date == sheet.MatchDate.Date)
            .FirstOrDefaultAsync(cancellationToken);

        if (match != null)
            return match;

        // Try match by date and opposition name (case-insensitive)
        var normalizedOpp = sheet.Opposition.Trim().ToLower();

        match = await _dbContext.Matches
            .Include(m => m.AwayTeam)
            .Where(m => m.MatchDate.Date == sheet.MatchDate.Date &&
                       m.AwayTeam.Name.ToLower().Contains(normalizedOpp))
            .FirstOrDefaultAsync(cancellationToken);

        return match;
    }

    /// <summary>
    /// Counts errors by type from validation results.
    /// </summary>
    private Dictionary<string, int> CountErrorsByType(Dictionary<string, ValidationResult> validationResults)
    {
        var errorsByType = new Dictionary<string, int>();

        foreach (var kvp in validationResults)
        {
            foreach (var error in kvp.Value.Errors)
            {
                // Categorize errors by keyword
                var errorType = CategorizeError(error);

                errorsByType.TryGetValue(errorType, out var count);
                errorsByType[errorType] = count + 1;
            }
        }

        return errorsByType;
    }

    /// <summary>
    /// Categorizes error message into type.
    /// </summary>
    private string CategorizeError(string errorMessage)
    {
        var lowerError = errorMessage.ToLower();

        if (lowerError.Contains("jersey") || lowerError.Contains("duplicate"))
            return "JerseyError";

        if (lowerError.Contains("name") || lowerError.Contains("player"))
            return "PlayerIdentificationError";

        if (lowerError.Contains("field") || lowerError.Contains("column"))
            return "FieldMapError";

        if (lowerError.Contains("percentage") || lowerError.Contains("negative"))
            return "DataTypeError";

        if (lowerError.Contains("total") || lowerError.Contains("sum") || lowerError.Contains("match"))
            return "CrossFieldError";

        if (lowerError.Contains("position"))
            return "PositionError";

        if (lowerError.Contains("booking") || lowerError.Contains("card"))
            return "BookingError";

        return "OtherError";
    }

    /// <summary>
    /// Enriches player statistics with position codes from position mappings.
    /// Falls back to goalkeeper inference if position not found in mappings.
    /// </summary>
    /// <param name="sheets">List of player statistics sheets to enrich.</param>
    /// <param name="positionMappings">Dictionary mapping normalized player names to position codes.</param>
    /// <remarks>
    /// <para><strong>Three-Tier Detection Strategy:</strong></para>
    /// <list type="number">
    ///   <item>
    ///     <term>Tier 1: Position Mapping Lookup (Primary)</term>
    ///     <description>
    ///       Lookup player in position mappings dictionary using normalized name (trimmed, lowercase).
    ///       This is the most reliable method as it comes directly from position sheets.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>Tier 2: Goalkeeper Inference (Fallback)</term>
    ///     <description>
    ///       If no position mapping found, check if player has goalkeeper statistics
    ///       (GkTotalKickouts > 0 OR GkSaves > 0). This handles cases where goalkeeper
    ///       is missing from Goalkeepers sheet.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>Tier 3: Position Unknown (Skip)</term>
    ///     <description>
    ///       If neither mapping nor inference succeeds, set PositionCode = empty string
    ///       and log warning. Player will be skipped during database loading.
    ///     </description>
    ///   </item>
    /// </list>
    ///
    /// <para><strong>Performance Impact:</strong></para>
    /// <para>
    /// Expected processing time: 10-50ms for ~169 players across 9 sheets.
    /// Dictionary lookup is O(1), iteration is O(n) where n = total player count.
    /// </para>
    /// </remarks>
    private void EnrichPlayerDataWithPositions(
        List<PlayerStatsSheetData> sheets,
        Dictionary<string, string> positionMappings)
    {
        var stopwatch = Stopwatch.StartNew();
        var enrichedCount = 0;
        var inferredCount = 0;
        var missingCount = 0;

        foreach (var sheet in sheets)
        {
            foreach (var player in sheet.Players)
            {
                var normalizedName = PlayerIdentifier.NormalizeName(player.PlayerName);

                // Tier 1: Try direct lookup from position sheets
                if (positionMappings.TryGetValue(normalizedName, out var positionCode))
                {
                    player.PositionCode = positionCode;
                    enrichedCount++;
                    _logger.LogTrace(
                        "Position from mapping: '{PlayerName}' → {PositionCode}",
                        player.PlayerName, positionCode);
                    continue;
                }

                // Tier 2: Try goalkeeper inference
                if (HasGoalkeeperStats(player))
                {
                    player.PositionCode = "GK";
                    inferredCount++;
                    _logger.LogDebug(
                        "Inferred goalkeeper position for #{JerseyNumber} '{PlayerName}' " +
                        "(GkTotalKickouts={Kickouts}, GkSaves={Saves})",
                        player.JerseyNumber, player.PlayerName,
                        player.GkTotalKickouts, player.GkSaves);
                    continue;
                }

                // Tier 3: Position unknown - log warning and mark as missing
                _logger.LogWarning(
                    "Position unknown for #{JerseyNumber} '{PlayerName}' in sheet '{SheetName}'. " +
                    "Player will be skipped during loading. " +
                    "Possible causes: (1) Player missing from position sheets, (2) Name mismatch between sheets.",
                    player.JerseyNumber, player.PlayerName, sheet.SheetName);

                player.PositionCode = string.Empty; // Mark as missing - will be skipped by loader
                missingCount++;
            }
        }

        stopwatch.Stop();

        _logger.LogInformation(
            "Position enrichment completed in {ElapsedMs}ms: " +
            "{Enriched} from mappings, {Inferred} inferred as GK, {Missing} missing (will be skipped)",
            stopwatch.ElapsedMilliseconds,
            enrichedCount,
            inferredCount,
            missingCount);
    }

    /// <summary>
    /// Determines if player has goalkeeper statistics.
    /// </summary>
    /// <param name="player">Player statistics data to check.</param>
    /// <returns>True if player has recorded kickouts or saves, false otherwise.</returns>
    /// <remarks>
    /// <para><strong>Inference Rule:</strong></para>
    /// <para>
    /// A player is considered a goalkeeper if:
    /// - GkTotalKickouts > 0 (player took kickouts) OR
    /// - GkSaves > 0 (player made saves)
    /// </para>
    ///
    /// <para><strong>Rationale:</strong></para>
    /// <para>
    /// Only goalkeepers take kickouts and make saves in GAA matches.
    /// This inference is already validated in PositionSpecificValidator.ValidateGoalkeeperFields().
    /// </para>
    ///
    /// <para><strong>Edge Cases:</strong></para>
    /// <list type="bullet">
    ///   <item>Player has 0 kickouts and 0 saves → Not a goalkeeper (returns false)</item>
    ///   <item>Player has kickouts but is missing from Goalkeepers sheet → Inferred as GK (returns true)</item>
    ///   <item>Data entry error (non-GK has GK stats) → Incorrectly inferred as GK (rare, logged for review)</item>
    /// </list>
    /// </remarks>
    private bool HasGoalkeeperStats(PlayerStatisticsData player)
    {
        // Goalkeeper indicators: Has recorded kickouts OR saves
        return player.GkTotalKickouts > 0 || player.GkSaves > 0;
    }
}
