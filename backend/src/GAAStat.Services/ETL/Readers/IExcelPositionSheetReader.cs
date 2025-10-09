using GAAStat.Services.ETL.Models;
using OfficeOpenXml;

namespace GAAStat.Services.ETL.Readers;

/// <summary>
/// Reads position information from Excel position sheets during ETL Phase 0.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>
/// Extracts player-to-position mappings from 4 position-specific sheets in the Excel file:
/// - Goalkeepers → "GK"
/// - Defenders → "DEF"
/// - Midfieliers → "MID"
/// - Forwards → "FWD"
/// </para>
///
/// <para><strong>ETL Integration:</strong></para>
/// <para>
/// Called during ETL Phase 0 (before extracting player statistics) to build a position
/// mapping dictionary. This dictionary is used in Phase 1.5 to enrich player data with
/// position codes before database loading.
/// </para>
///
/// <para><strong>Implementation Strategy:</strong></para>
/// <para>
/// The implementation (ExcelPositionSheetReader) uses EPPlus to read Excel sheets.
/// It handles:
/// - Case-insensitive sheet name matching ("Goalkeepers" vs "goalkeepers")
/// - Missing sheets (graceful degradation)
/// - Duplicate players across sheets (last occurrence wins, logged as warning)
/// - Empty rows (detect via empty jersey number or name)
/// </para>
/// </remarks>
public interface IExcelPositionSheetReader
{
    /// <summary>
    /// Reads position mappings from all position sheets in the Excel file.
    /// </summary>
    /// <param name="package">The EPPlus Excel package to read from.</param>
    /// <returns>
    /// A PositionMappingResult containing:
    /// - Mappings dictionary (normalized player name → position code)
    /// - Number of sheets successfully processed
    /// - List of duplicate player warnings
    /// - Processing time in milliseconds
    /// </returns>
    /// <remarks>
    /// <para><strong>Expected Sheets:</strong></para>
    /// <list type="bullet">
    ///   <item>"Goalkeepers" (or case-insensitive variants)</item>
    ///   <item>"Defenders" (or case-insensitive variants)</item>
    ///   <item>"Midfielders" (or case-insensitive variants)</item>
    ///   <item>"Forwards" (or case-insensitive variants)</item>
    /// </list>
    ///
    /// <para><strong>Sheet Structure:</strong></para>
    /// <para>
    /// Each position sheet follows the same structure as player stats sheets:
    /// - Row 1-3: Headers (ignored)
    /// - Row 4+: Player data
    /// - Column A: Jersey number (integer, read but not used for identification)
    /// - Column B: Player name (string, used for identification after normalization)
    /// </para>
    ///
    /// <para><strong>Error Handling:</strong></para>
    /// <para>
    /// - Missing sheets: Logged as WARNING, other sheets continue processing
    /// - Corrupted sheet: Logged as ERROR, sheet skipped
    /// - No sheets found: Returns Failure result (IsSuccess = false)
    /// - All sheets failed: Returns Failure result
    /// </para>
    ///
    /// <para><strong>Performance:</strong></para>
    /// <para>
    /// Expected processing time: 500-800ms for ~169 players across 4 sheets.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if package is null.</exception>
    Task<PositionMappingResult> ReadPositionMappingsAsync(ExcelPackage package);
}
