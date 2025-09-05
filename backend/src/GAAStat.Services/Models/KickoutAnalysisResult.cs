namespace GAAStat.Services.Models;

/// <summary>
/// Represents the result of kickout analysis processing
/// Contains metrics about the processing operation and data quality
/// </summary>
public class KickoutAnalysisResult
{
    /// <summary>
    /// Total number of kickout events extracted from the Excel sheet
    /// </summary>
    public int EventsExtracted { get; set; }
    
    /// <summary>
    /// Number of aggregated records created in the database
    /// </summary>
    public int RecordsCreated { get; set; }
    
    /// <summary>
    /// Number of rows processed from the Excel sheet
    /// </summary>
    public int ProcessedRows { get; set; }
    
    /// <summary>
    /// Number of rows skipped due to empty or invalid data
    /// </summary>
    public int SkippedRows { get; set; }
    
    /// <summary>
    /// Total processing time in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; set; }
    
    /// <summary>
    /// List of warning messages encountered during processing
    /// </summary>
    public List<string> WarningMessages { get; set; } = new();
    
    /// <summary>
    /// Processing efficiency ratio (events extracted / rows processed)
    /// </summary>
    public double ProcessingEfficiency => ProcessedRows > 0 ? (double)EventsExtracted / ProcessedRows : 0;
    
    /// <summary>
    /// Error rate (skipped rows / total rows)
    /// </summary>
    public double ErrorRate => (ProcessedRows + SkippedRows) > 0 ? (double)SkippedRows / (ProcessedRows + SkippedRows) : 0;
    
    /// <summary>
    /// Conversion rate (records created / events extracted)
    /// </summary>
    public double ConversionRate => EventsExtracted > 0 ? (double)RecordsCreated / EventsExtracted : 0;
}