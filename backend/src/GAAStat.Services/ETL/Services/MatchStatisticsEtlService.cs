using GAAStat.Services.ETL.Interfaces;
using GAAStat.Services.ETL.Loaders;
using GAAStat.Services.ETL.Models;
using GAAStat.Services.ETL.Readers;
using GAAStat.Services.ETL.Transformers;
using Microsoft.Extensions.Logging;

namespace GAAStat.Services.ETL.Services;

/// <summary>
/// Main orchestrator service for the match statistics ETL pipeline.
/// Coordinates the Extract-Transform-Load process for match data from Excel to database.
/// </summary>
public class MatchStatisticsEtlService : IMatchStatisticsEtlService
{
    private readonly ExcelMatchDataReader _reader;
    private readonly MatchDataTransformer _transformer;
    private readonly MatchDataLoader _loader;
    private readonly ILogger<MatchStatisticsEtlService> _logger;

    /// <summary>
    /// Initializes a new instance of MatchStatisticsEtlService
    /// </summary>
    public MatchStatisticsEtlService(
        ExcelMatchDataReader reader,
        MatchDataTransformer transformer,
        MatchDataLoader loader,
        ILogger<MatchStatisticsEtlService> logger)
    {
        _reader = reader;
        _transformer = transformer;
        _loader = loader;
        _logger = logger;
    }

    /// <summary>
    /// Executes the complete ETL pipeline for match statistics.
    /// Orchestrates: Extract → Transform → Validate → Load
    /// </summary>
    /// <param name="filePath">Absolute path to Excel file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ETL result with statistics and error details</returns>
    public async Task<EtlResult> ProcessMatchStatisticsAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var result = EtlResult.CreateSuccess();
        result.StartTime = DateTime.UtcNow;

        _logger.LogInformation("Starting ETL pipeline for file: {FilePath}", filePath);

        try
        {
            // Phase 1: Extract - Read Excel sheets
            _logger.LogInformation("Phase 1: Extracting match data from Excel");
            var matchSheets = await _reader.ReadMatchSheetsAsync(filePath);

            if (matchSheets.Count == 0)
            {
                _logger.LogWarning("No match sheets found in file");
                result.AddWarning("NO_MATCHES", "No match sheets found in Excel file");
                result.EndTime = DateTime.UtcNow;
                return result;
            }

            _logger.LogInformation("Extracted {Count} match sheets", matchSheets.Count);

            // Phase 2: Transform & Validate - Parse and validate data
            _logger.LogInformation("Phase 2: Validating match data");
            var validationPassed = _transformer.ValidateMatchData(matchSheets);

            if (!validationPassed)
            {
                _logger.LogError("Validation failed for one or more matches");
                result.AddError("VALIDATION_FAILED", "One or more matches failed validation. Check logs for details.");
                result.EndTime = DateTime.UtcNow;
                return result;
            }

            _logger.LogInformation("Validation passed for all {Count} matches", matchSheets.Count);

            // Phase 3: Load - Insert into database
            _logger.LogInformation("Phase 3: Loading match data into database");
            var loadResult = await _loader.LoadMatchDataAsync(matchSheets, cancellationToken);

            // Merge load result into main result
            result.MatchesProcessed = loadResult.MatchesProcessed;
            result.TeamStatisticsCreated = loadResult.TeamStatisticsCreated;
            result.Errors.AddRange(loadResult.Errors);
            result.Warnings.AddRange(loadResult.Warnings);

            result.Success = loadResult.Success && result.Errors.Count == 0;
            result.EndTime = DateTime.UtcNow;

            // Log final summary
            if (result.Success)
            {
                _logger.LogInformation(
                    "ETL pipeline completed successfully. " +
                    "Matches: {Matches}, Team Stats: {TeamStats}, Duration: {Duration}s",
                    result.MatchesProcessed,
                    result.TeamStatisticsCreated,
                    result.Duration.TotalSeconds);
            }
            else
            {
                _logger.LogError(
                    "ETL pipeline completed with errors. " +
                    "Matches: {Matches}, Errors: {Errors}, Duration: {Duration}s",
                    result.MatchesProcessed,
                    result.Errors.Count,
                    result.Duration.TotalSeconds);
            }

            return result;
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Excel file not found: {FilePath}", filePath);
            result.AddError("FILE_NOT_FOUND", $"Excel file not found: {filePath}");
            result.EndTime = DateTime.UtcNow;
            return result;
        }
        catch (ValidationException ex)
        {
            _logger.LogError(ex, "Validation error: {Message}", ex.Message);
            result.AddError("VALIDATION_ERROR", ex.Message);
            result.EndTime = DateTime.UtcNow;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ETL pipeline failed with unexpected error: {Message}", ex.Message);
            result.AddError("UNEXPECTED_ERROR", $"Unexpected error: {ex.Message}");
            result.EndTime = DateTime.UtcNow;
            return result;
        }
    }
}
