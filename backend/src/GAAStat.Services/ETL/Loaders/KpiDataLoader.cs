using GAAStat.Dal.Contexts;
using GAAStat.Dal.Models.Application;
using GAAStat.Services.ETL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GAAStat.Services.ETL.Loaders;

/// <summary>
/// Loads KPI definitions from Excel into the database.
/// Handles deduplication, upsert logic, and transaction management.
/// </summary>
public class KpiDataLoader
{
    private readonly GAAStatDbContext _dbContext;
    private readonly ILogger<KpiDataLoader> _logger;

    public KpiDataLoader(GAAStatDbContext dbContext, ILogger<KpiDataLoader> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Loads all KPI definitions into the database
    /// Uses UPSERT strategy: Update if exists, Insert if new
    /// </summary>
    /// <param name="definitions">List of KPI definitions to load</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ETL result with statistics</returns>
    public async Task<KpiEtlResult> LoadKpiDefinitionsAsync(
        List<KpiDefinitionData> definitions,
        CancellationToken cancellationToken = default)
    {
        var result = new KpiEtlResult
        {
            Success = true,
            StartTime = DateTime.UtcNow,
            SheetName = "KPI Definitions"
        };

        int inserted = 0;
        int updated = 0;
        int skipped = 0;

        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            _logger.LogInformation("Loading {Count} KPI definitions into database", definitions.Count);

            // Load all existing KPI definitions for comparison
            var existingDefinitions = await _dbContext.KpiDefinitions
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Found {Count} existing KPI definitions in database", existingDefinitions.Count);

            foreach (var definition in definitions)
            {
                try
                {
                    // Check if definition already exists (based on unique key)
                    var existing = existingDefinitions.FirstOrDefault(e =>
                        e.EventNumber == definition.EventNumber &&
                        e.EventName.Equals(definition.EventName, StringComparison.OrdinalIgnoreCase) &&
                        e.Outcome.Equals(definition.Outcome, StringComparison.OrdinalIgnoreCase) &&
                        e.TeamAssignment.Equals(definition.TeamAssignment, StringComparison.OrdinalIgnoreCase));

                    if (existing != null)
                    {
                        // Update if PSR Value or Definition changed
                        if (existing.PsrValue != definition.PsrValue ||
                            existing.Definition != definition.Definition)
                        {
                            existing.PsrValue = definition.PsrValue;
                            existing.Definition = definition.Definition;

                            _dbContext.KpiDefinitions.Update(existing);
                            updated++;

                            _logger.LogDebug("Updated KPI: Event {EventNumber} - {EventName} - {Outcome} ({Team})",
                                definition.EventNumber, definition.EventName, definition.Outcome, definition.TeamAssignment);
                        }
                        else
                        {
                            skipped++;
                            _logger.LogTrace("Skipped unchanged KPI: Event {EventNumber} - {EventName} - {Outcome}",
                                definition.EventNumber, definition.EventName, definition.Outcome);
                        }
                    }
                    else
                    {
                        // Insert new definition
                        var kpiEntity = new KpiDefinition
                        {
                            EventNumber = definition.EventNumber,
                            EventName = definition.EventName,
                            Outcome = definition.Outcome,
                            TeamAssignment = definition.TeamAssignment,
                            PsrValue = definition.PsrValue,
                            Definition = definition.Definition,
                            CreatedAt = DateTime.UtcNow
                        };

                        await _dbContext.KpiDefinitions.AddAsync(kpiEntity, cancellationToken);
                        inserted++;

                        _logger.LogDebug("Inserted new KPI: Event {EventNumber} - {EventName} - {Outcome} ({Team})",
                            definition.EventNumber, definition.EventName, definition.Outcome, definition.TeamAssignment);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load KPI definition at row {Row}: Event {EventNumber} - {EventName} - {Outcome}",
                        definition.SourceRowNumber, definition.EventNumber, definition.EventName, definition.Outcome);
                    result.Errors.Add(new EtlError
                    {
                        Code = $"ROW_{definition.SourceRowNumber}",
                        Message = ex.Message,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }

            // Save all changes
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // Update result statistics
            result.KpiDefinitionsCreated = inserted;
            result.KpiDefinitionsUpdated = updated;
            result.KpiDefinitionsSkipped = skipped;
            result.Success = result.Errors.Count == 0;
            result.EndTime = DateTime.UtcNow;

            _logger.LogInformation(
                "KPI definitions loaded successfully. Inserted: {Inserted}, Updated: {Updated}, Skipped: {Skipped}",
                inserted, updated, skipped);

            return result;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to load KPI definitions: {Message}", ex.Message);
            result.Success = false;
            result.Errors.Add(new EtlError
            {
                Code = "LOAD_FAILED",
                Message = $"Database load failed: {ex.Message}",
                Timestamp = DateTime.UtcNow
            });
            result.EndTime = DateTime.UtcNow;
            return result;
        }
    }
}
