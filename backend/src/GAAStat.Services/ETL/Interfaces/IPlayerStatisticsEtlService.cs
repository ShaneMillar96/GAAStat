using System.Threading;
using System.Threading.Tasks;
using GAAStat.Services.ETL.Models;

namespace GAAStat.Services.ETL.Interfaces;

/// <summary>
/// Service for processing player statistics from Excel files.
/// Orchestrates Extract-Transform-Load pipeline for 86-field player statistics.
/// </summary>
public interface IPlayerStatisticsEtlService
{
    /// <summary>
    /// Processes player statistics from Excel file.
    /// </summary>
    /// <param name="filePath">Absolute path to Excel file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ETL result with detailed statistics</returns>
    Task<PlayerEtlResult> ProcessPlayerStatisticsAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}
