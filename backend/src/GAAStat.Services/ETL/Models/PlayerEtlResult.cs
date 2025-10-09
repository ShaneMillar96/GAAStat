using System;
using System.Collections.Generic;
using System.Text;

namespace GAAStat.Services.ETL.Models;

/// <summary>
/// Result of player statistics ETL operation.
/// Extends base EtlResult with player-specific metrics.
/// </summary>
public class PlayerEtlResult : EtlResult
{
    /// <summary>
    /// Number of player stats sheets processed
    /// </summary>
    public int PlayerSheetsProcessed { get; set; }

    /// <summary>
    /// Number of new players created
    /// </summary>
    public int PlayersCreated { get; set; }

    /// <summary>
    /// Number of existing players updated
    /// </summary>
    public int PlayersUpdated { get; set; }

    /// <summary>
    /// Number of player statistics records created
    /// </summary>
    public int PlayerStatisticsCreated { get; set; }

    /// <summary>
    /// Number of players skipped due to validation errors
    /// </summary>
    public int PlayersSkipped { get; set; }

    /// <summary>
    /// Total number of validation errors
    /// </summary>
    public int ValidationErrorsTotal { get; set; }

    /// <summary>
    /// Total number of validation warnings
    /// </summary>
    public int ValidationWarningsTotal { get; set; }

    /// <summary>
    /// Average sheet processing time
    /// </summary>
    public TimeSpan AverageSheetProcessingTime { get; set; }

    /// <summary>
    /// Total number of fields processed (86 × players)
    /// </summary>
    public int FieldsProcessedTotal { get; set; }

    /// <summary>
    /// Errors grouped by type
    /// </summary>
    public Dictionary<string, int> ErrorsByType { get; set; } = new();

    /// <summary>
    /// Generates detailed summary report
    /// </summary>
    public string GetDetailedSummary()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Player Statistics ETL Summary ===");
        sb.AppendLine($"Status: {(Success ? "SUCCESS" : "FAILED")}");
        sb.AppendLine($"Duration: {Duration:mm\\:ss}");
        sb.AppendLine();
        sb.AppendLine("Processing:");
        sb.AppendLine($"  - Sheets Processed: {PlayerSheetsProcessed}");
        sb.AppendLine($"  - Players Created: {PlayersCreated}");
        sb.AppendLine($"  - Players Updated: {PlayersUpdated}");
        sb.AppendLine($"  - Statistics Records: {PlayerStatisticsCreated}");
        sb.AppendLine($"  - Players Skipped: {PlayersSkipped}");
        sb.AppendLine($"  - Fields Processed: {FieldsProcessedTotal:N0}");
        sb.AppendLine();
        sb.AppendLine("Validation:");
        sb.AppendLine($"  - Errors: {ValidationErrorsTotal}");
        sb.AppendLine($"  - Warnings: {ValidationWarningsTotal}");
        sb.AppendLine();
        sb.AppendLine("Performance:");
        sb.AppendLine($"  - Avg Sheet Time: {AverageSheetProcessingTime:ss\\.fff}s");
        if (Duration.TotalSeconds > 0)
        {
            sb.AppendLine($"  - Fields/Second: {(FieldsProcessedTotal / Duration.TotalSeconds):F0}");
        }

        if (ErrorsByType.Any())
        {
            sb.AppendLine();
            sb.AppendLine("Error Breakdown:");
            foreach (var kvp in ErrorsByType.OrderByDescending(x => x.Value))
            {
                sb.AppendLine($"  - {kvp.Key}: {kvp.Value}");
            }
        }

        return sb.ToString();
    }
}
