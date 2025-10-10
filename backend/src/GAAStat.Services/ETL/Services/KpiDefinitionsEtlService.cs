using GAAStat.Services.ETL.Interfaces;
using GAAStat.Services.ETL.Loaders;
using GAAStat.Services.ETL.Models;
using GAAStat.Services.ETL.Readers;
using GAAStat.Services.ETL.Transformers;
using Microsoft.Extensions.Logging;

namespace GAAStat.Services.ETL.Services;

/// <summary>
/// Main orchestrator service for the KPI definitions ETL pipeline.
/// Coordinates the Extract-Transform-Load process for KPI data from Excel to database.
/// </summary>
public class KpiDefinitionsEtlService : IKpiDefinitionsEtlService
{
    private readonly ExcelKpiDataReader _reader;
    private readonly KpiDataTransformer _transformer;
    private readonly KpiDataLoader _loader;
    private readonly ILogger<KpiDefinitionsEtlService> _logger;

    /// <summary>
    /// Initializes a new instance of KpiDefinitionsEtlService
    /// </summary>
    public KpiDefinitionsEtlService(
        ExcelKpiDataReader reader,
        KpiDataTransformer transformer,
        KpiDataLoader loader,
        ILogger<KpiDefinitionsEtlService> logger)
    {
        _reader = reader;
        _transformer = transformer;
        _loader = loader;
        _logger = logger;
    }

    /// <summary>
    /// Executes the complete ETL pipeline for KPI definitions.
    /// Orchestrates: Extract → Transform → Validate → Normalize → Load
    /// </summary>
    /// <param name="filePath">Absolute path to Excel file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ETL result with statistics and error details</returns>
    public async Task<KpiEtlResult> ProcessKpiDefinitionsAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var result = new KpiEtlResult
        {
            Success = true,
            StartTime = DateTime.UtcNow,
            SheetName = "KPI Definitions"
        };

        _logger.LogInformation("Starting KPI Definitions ETL pipeline for file: {FilePath}", filePath);

        try
        {
            // Phase 1: Extract - Read Excel sheet
            _logger.LogInformation("Phase 1: Extracting KPI definitions from Excel");
            var definitions = await _reader.ReadKpiDefinitionsAsync(filePath);

            if (definitions.Count == 0)
            {
                _logger.LogWarning("No KPI definitions found in file");
                result.Warnings.Add(new EtlWarning
                {
                    Code = "NO_DEFINITIONS",
                    Message = "No KPI definitions found in Excel sheet",
                    Timestamp = DateTime.UtcNow
                });
                result.EndTime = DateTime.UtcNow;
                return result;
            }

            _logger.LogInformation("Extracted {Count} KPI definitions", definitions.Count);

            // Phase 2: Normalize - Clean and standardize data (fix typos before validation)
            _logger.LogInformation("Phase 2: Normalizing KPI definitions");
            _transformer.NormalizeDefinitions(definitions);

            // Phase 2.5: Validate - Check data integrity after normalization
            _logger.LogInformation("Phase 2.5: Validating KPI definitions");
            var validationPassed = _transformer.ValidateKpiDefinitions(definitions);

            if (!validationPassed)
            {
                _logger.LogError("Validation failed for one or more KPI definitions");
                result.Success = false;
                result.Errors.Add(new EtlError
                {
                    Code = "VALIDATION_FAILED",
                    Message = "One or more KPI definitions failed validation. Check logs for details.",
                    Timestamp = DateTime.UtcNow
                });
                result.EndTime = DateTime.UtcNow;
                return result;
            }

            _logger.LogInformation("Validation passed for all {Count} definitions", definitions.Count);

            // Phase 3: Load - Insert/update in database
            _logger.LogInformation("Phase 3: Loading KPI definitions into database");
            var loadResult = await _loader.LoadKpiDefinitionsAsync(definitions, cancellationToken);

            // Merge load result into main result
            result.KpiDefinitionsCreated = loadResult.KpiDefinitionsCreated;
            result.KpiDefinitionsUpdated = loadResult.KpiDefinitionsUpdated;
            result.KpiDefinitionsSkipped = loadResult.KpiDefinitionsSkipped;
            result.Errors.AddRange(loadResult.Errors);
            result.Warnings.AddRange(loadResult.Warnings);

            result.Success = loadResult.Success && result.Errors.Count == 0;
            result.EndTime = DateTime.UtcNow;

            // Log final summary
            if (result.Success)
            {
                _logger.LogInformation(
                    "ETL pipeline completed successfully. " +
                    "Inserted: {Inserted}, Updated: {Updated}, Skipped: {Skipped}, Duration: {Duration}s",
                    result.KpiDefinitionsCreated,
                    result.KpiDefinitionsUpdated,
                    result.KpiDefinitionsSkipped,
                    result.Duration.TotalSeconds);
            }
            else
            {
                _logger.LogError(
                    "ETL pipeline completed with errors. " +
                    "Processed: {Count}, Errors: {Errors}, Duration: {Duration}s",
                    definitions.Count,
                    result.Errors.Count,
                    result.Duration.TotalSeconds);
            }

            return result;
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Excel file not found: {FilePath}", filePath);
            result.Success = false;
            result.Errors.Add(new EtlError
            {
                Code = "FILE_NOT_FOUND",
                Message = $"Excel file not found: {filePath}",
                Timestamp = DateTime.UtcNow
            });
            result.EndTime = DateTime.UtcNow;
            return result;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Sheet not found: {Message}", ex.Message);
            result.Success = false;
            result.Errors.Add(new EtlError
            {
                Code = "SHEET_NOT_FOUND",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
            result.EndTime = DateTime.UtcNow;
            return result;
        }
        catch (System.ComponentModel.DataAnnotations.ValidationException ex)
        {
            _logger.LogError(ex, "Validation error: {Message}", ex.Message);
            result.Success = false;
            result.Errors.Add(new EtlError
            {
                Code = "VALIDATION_ERROR",
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
            result.EndTime = DateTime.UtcNow;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ETL pipeline failed with unexpected error: {Message}", ex.Message);
            result.Success = false;
            result.Errors.Add(new EtlError
            {
                Code = "UNEXPECTED_ERROR",
                Message = $"Unexpected error: {ex.Message}",
                Timestamp = DateTime.UtcNow
            });
            result.EndTime = DateTime.UtcNow;
            return result;
        }
    }
}
