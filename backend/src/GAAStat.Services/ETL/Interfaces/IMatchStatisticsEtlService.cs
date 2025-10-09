using GAAStat.Services.ETL.Models;

namespace GAAStat.Services.ETL.Interfaces;

/// <summary>
/// Main orchestrator service for the match statistics ETL pipeline.
/// Coordinates Excel parsing, validation, transformation, and database loading.
/// </summary>
public interface IMatchStatisticsEtlService
{
    /// <summary>
    /// Executes the complete ETL pipeline for a given Excel file.
    /// Reads match sheets, validates data, and loads into the database.
    /// </summary>
    /// <param name="filePath">Absolute path to the Excel file (e.g., "Drum Analysis 2025.xlsx")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ETL execution result with success/failure details</returns>
    Task<EtlResult> ProcessMatchStatisticsAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}
