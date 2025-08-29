using GAAStat.Services.Models;

namespace GAAStat.Services.Interfaces;

/// <summary>
/// Advanced Excel processing service for comprehensive GAA Excel format support
/// Handles all 31 sheet types with parallel processing and advanced validation
/// </summary>
public interface IAdvancedExcelProcessorService
{
    /// <summary>
    /// Processes a comprehensive GAA Excel file with all 31 sheet types
    /// </summary>
    /// <param name="excelStream">Excel file stream</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="config">Processing configuration</param>
    /// <param name="progressCallback">Progress tracking callback</param>
    /// <returns>Comprehensive processing result</returns>
    Task<ServiceResult<ComprehensiveProcessingResult>> ProcessComprehensiveExcelAsync(
        Stream excelStream,
        string fileName,
        AdvancedProcessingConfig? config = null,
        IProgress<ExcelProcessingProgress>? progressCallback = null);

    /// <summary>
    /// Performs advanced validation across all sheet types with cross-referencing
    /// </summary>
    /// <param name="excelStream">Excel file stream</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="validationLevel">Level of validation to perform</param>
    /// <returns>Comprehensive validation result</returns>
    Task<ServiceResult<AdvancedValidationResult>> ValidateComprehensiveExcelAsync(
        Stream excelStream,
        string fileName,
        ValidationLevel validationLevel = ValidationLevel.Full);

    /// <summary>
    /// Processes cumulative statistics sheets with season aggregation
    /// </summary>
    /// <param name="excelStream">Excel file stream</param>
    /// <param name="sheetNames">Specific sheet names to process</param>
    /// <param name="progressCallback">Progress tracking callback</param>
    /// <returns>Cumulative processing result</returns>
    Task<ServiceResult<CumulativeProcessingResult>> ProcessCumulativeStatsAsync(
        Stream excelStream,
        IEnumerable<string> sheetNames,
        IProgress<ExcelProcessingProgress>? progressCallback = null);

    /// <summary>
    /// Processes position-specific analysis sheets (Goalkeepers, Defenders, etc.)
    /// </summary>
    /// <param name="excelStream">Excel file stream</param>
    /// <param name="positions">Positions to process</param>
    /// <param name="config">Processing configuration</param>
    /// <returns>Position-specific analysis result</returns>
    Task<ServiceResult<PositionAnalysisResult>> ProcessPositionAnalysisAsync(
        Stream excelStream,
        IEnumerable<PlayerPosition> positions,
        AdvancedProcessingConfig? config = null);

    /// <summary>
    /// Processes specialized analysis sheets (KPIs, Shot analysis, etc.)
    /// </summary>
    /// <param name="excelStream">Excel file stream</param>
    /// <param name="analysisTypes">Types of analysis to process</param>
    /// <param name="config">Processing configuration</param>
    /// <returns>Specialized analysis result</returns>
    Task<ServiceResult<SpecializedAnalysisResult>> ProcessSpecializedAnalysisAsync(
        Stream excelStream,
        IEnumerable<AnalysisType> analysisTypes,
        AdvancedProcessingConfig? config = null);

    /// <summary>
    /// Detects and classifies all sheet types in the Excel file
    /// </summary>
    /// <param name="excelStream">Excel file stream</param>
    /// <returns>Sheet classification result</returns>
    Task<ServiceResult<SheetClassificationResult>> ClassifyExcelSheetsAsync(Stream excelStream);

    /// <summary>
    /// Performs cross-sheet data consistency validation
    /// </summary>
    /// <param name="excelStream">Excel file stream</param>
    /// <param name="rules">Validation rules to apply</param>
    /// <returns>Consistency validation result</returns>
    Task<ServiceResult<ConsistencyValidationResult>> ValidateDataConsistencyAsync(
        Stream excelStream,
        IEnumerable<ValidationRule>? rules = null);

    /// <summary>
    /// Generates comprehensive statistics and insights from all sheet types
    /// </summary>
    /// <param name="excelStream">Excel file stream</param>
    /// <param name="config">Analysis configuration</param>
    /// <returns>Comprehensive insights result</returns>
    Task<ServiceResult<ComprehensiveInsightsResult>> GenerateComprehensiveInsightsAsync(
        Stream excelStream,
        InsightGenerationConfig? config = null);
}