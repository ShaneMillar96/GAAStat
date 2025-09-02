using GAAStat.Services.Models;

namespace GAAStat.Services.Interfaces;

/// <summary>
/// Service for parsing Excel files and extracting GAA match data
/// Handles EPPlus integration for reading Excel files and detecting sheet structures
/// </summary>
public interface IExcelParsingService
{
    /// <summary>
    /// Analyzes an Excel file to detect sheets and validate GAA data structure
    /// </summary>
    /// <param name="fileStream">Excel file stream</param>
    /// <param name="fileName">Original file name for context</param>
    /// <returns>Analysis result with sheet information and validation status</returns>
    Task<ServiceResult<ExcelFileAnalysis>> AnalyzeExcelFileAsync(Stream fileStream, string fileName);
    
    /// <summary>
    /// Parses match data from a specific Excel sheet
    /// Phase 1: Focuses on basic match header information
    /// </summary>
    /// <param name="fileStream">Excel file stream</param>
    /// <param name="sheetName">Name of sheet to parse</param>
    /// <returns>Parsed match data or validation errors</returns>
    Task<ServiceResult<MatchData>> ParseMatchDataFromSheetAsync(Stream fileStream, string sheetName);
    
    /// <summary>
    /// Parses match data from all detected match sheets in Excel file
    /// </summary>
    /// <param name="fileStream">Excel file stream</param>
    /// <returns>Collection of parsed match data from all sheets</returns>
    Task<ServiceResult<IEnumerable<MatchData>>> ParseAllMatchDataAsync(Stream fileStream);
    
    /// <summary>
    /// Validates Excel file format and basic structure
    /// </summary>
    /// <param name="fileStream">Excel file stream</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="fileSizeBytes">File size for validation</param>
    /// <returns>Validation result with specific error messages</returns>
    Task<ServiceResult> ValidateExcelFileAsync(Stream fileStream, string fileName, long fileSizeBytes);
    
    /// <summary>
    /// Parses player statistics from a specific Excel sheet
    /// Phase 2: Extracts detailed player performance data
    /// </summary>
    /// <param name="fileStream">Excel file stream</param>
    /// <param name="sheetName">Name of sheet to parse</param>
    /// <param name="matchId">Database match ID for linking statistics</param>
    /// <returns>Collection of player statistics with validation errors</returns>
    Task<ServiceResult<IEnumerable<PlayerStatisticsData>>> ParsePlayerStatisticsFromSheetAsync(
        Stream fileStream, string sheetName, int matchId);
    
    /// <summary>
    /// Validates player statistics sheet structure and data
    /// </summary>
    /// <param name="fileStream">Excel file stream</param>
    /// <param name="sheetName">Name of sheet to validate</param>
    /// <returns>Validation result with detailed error information</returns>
    Task<ServiceResult<PlayerStatsValidationResult>> ValidatePlayerStatisticsSheetAsync(
        Stream fileStream, string sheetName);
}