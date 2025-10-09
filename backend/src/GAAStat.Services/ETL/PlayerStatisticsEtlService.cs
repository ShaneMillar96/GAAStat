using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GAAStat.Dal.Contexts;
using GAAStat.Dal.Models.Application;
using GAAStat.Services.ETL.Helpers;
using GAAStat.Services.ETL.Interfaces;
using GAAStat.Services.ETL.Models;
using GAAStat.Services.ETL.Services;
using GAAStat.Services.ETL.Transformers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<PlayerStatisticsEtlService> _logger;

    public PlayerStatisticsEtlService(
        GAAStatDbContext dbContext,
        PlayerDataLoader dataLoader,
        PlayerRosterService rosterService,
        PositionDetectionService positionService,
        ILogger<PlayerStatisticsEtlService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));
        _rosterService = rosterService ?? throw new ArgumentNullException(nameof(rosterService));
        _positionService = positionService ?? throw new ArgumentNullException(nameof(positionService));
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
}
