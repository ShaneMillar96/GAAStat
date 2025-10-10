using GAAStat.Services.ETL.Models;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;

namespace GAAStat.Services.ETL.Readers;

/// <summary>
/// Reads KPI definitions from the "KPI Definitions" Excel sheet.
/// Extracts event metadata, PSR values, and definitions.
/// </summary>
public class ExcelKpiDataReader
{
    private readonly ILogger<ExcelKpiDataReader> _logger;

    // Sheet name constant
    private const string KpiSheetName = "KPI Definitions";

    // Header row and data start row
    private const int HeaderRow = 2;
    private const int DataStartRow = 4; // Row 3 is empty, data starts at row 4

    // Column indices (1-based)
    private const int ColEventNumber = 1;     // Column A: "Event #"
    private const int ColEventName = 2;       // Column B: "Event Name"
    private const int ColOutcome = 3;         // Column C: "Outcome"
    private const int ColTeamAssignment = 4;  // Column D: "Assign to which team"
    private const int ColPsrValue = 5;        // Column E: "PSR Value"
    private const int ColDefinition = 6;      // Column F: "Definition"

    public ExcelKpiDataReader(ILogger<ExcelKpiDataReader> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Reads KPI definitions from the Excel file
    /// </summary>
    /// <param name="filePath">Absolute path to Excel file</param>
    /// <returns>List of KPI definition data</returns>
    public async Task<List<KpiDefinitionData>> ReadKpiDefinitionsAsync(string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        var kpiDefinitions = new List<KpiDefinitionData>();

        if (!File.Exists(filePath))
        {
            _logger.LogError("Excel file not found: {FilePath}", filePath);
            throw new FileNotFoundException($"Excel file not found: {filePath}");
        }

        _logger.LogInformation("Reading Excel file: {FilePath}", filePath);

        using var package = new ExcelPackage(new FileInfo(filePath));

        // Find KPI Definitions sheet (case-insensitive)
        var worksheet = package.Workbook.Worksheets
            .FirstOrDefault(ws => ws.Name.Equals(KpiSheetName, StringComparison.OrdinalIgnoreCase));

        if (worksheet == null)
        {
            _logger.LogWarning("KPI Definitions sheet not found in Excel file");
            throw new InvalidOperationException($"Sheet '{KpiSheetName}' not found in Excel file");
        }

        _logger.LogDebug("Found KPI Definitions sheet with {Rows} rows",
            worksheet.Dimension?.End.Row ?? 0);

        // Extract data rows starting from DataStartRow
        kpiDefinitions = await Task.Run(() => ExtractKpiDefinitions(worksheet));

        _logger.LogInformation("Extracted {Count} KPI definitions", kpiDefinitions.Count);

        return kpiDefinitions;
    }

    /// <summary>
    /// Extracts KPI definitions from the worksheet
    /// </summary>
    private List<KpiDefinitionData> ExtractKpiDefinitions(ExcelWorksheet sheet)
    {
        var definitions = new List<KpiDefinitionData>();

        if (sheet.Dimension == null)
        {
            _logger.LogWarning("Worksheet has no data");
            return definitions;
        }

        int maxRow = sheet.Dimension.End.Row;

        // Track forward-fill values for merged cells
        int? lastEventNumber = null;
        string? lastEventName = null;

        // Track consecutive empty rows (stop after 5 consecutive empty rows)
        int consecutiveEmptyRows = 0;
        const int maxConsecutiveEmptyRows = 5;

        // Start from row 4 (after headers in row 2, row 3 is empty)
        for (int row = DataStartRow; row <= maxRow; row++)
        {
            // Check if row is empty (all columns null/empty)
            if (IsRowEmpty(sheet, row))
            {
                consecutiveEmptyRows++;
                _logger.LogTrace("Empty row at {Row} (consecutive: {Count})", row, consecutiveEmptyRows);

                // Stop after multiple consecutive empty rows (allows for separator rows between events)
                if (consecutiveEmptyRows >= maxConsecutiveEmptyRows)
                {
                    _logger.LogDebug("Encountered {Count} consecutive empty rows at {Row}, stopping extraction",
                        consecutiveEmptyRows, row);
                    break;
                }
                continue; // Skip this empty row but keep going
            }

            // Reset consecutive empty counter when we find data
            consecutiveEmptyRows = 0;

            try
            {
                // Read raw values
                var eventNumberCell = sheet.Cells[row, ColEventNumber].Value;
                var eventNameCell = sheet.Cells[row, ColEventName].Value;
                var outcomeCell = sheet.Cells[row, ColOutcome].Value;
                var teamAssignmentCell = sheet.Cells[row, ColTeamAssignment].Value;
                var psrValueCell = sheet.Cells[row, ColPsrValue].Value;
                var definitionCell = sheet.Cells[row, ColDefinition].Value;

                // Forward-fill event number and event name (for merged cells)
                int? currentEventNumber = ParseInt(eventNumberCell, row, "Event Number");
                string? currentEventName = ParseString(eventNameCell);

                if (currentEventNumber.HasValue)
                {
                    lastEventNumber = currentEventNumber.Value;
                }
                if (!string.IsNullOrWhiteSpace(currentEventName))
                {
                    lastEventName = currentEventName;
                }

                // Use forward-filled values if current cell is empty (merged cell)
                var finalEventNumber = currentEventNumber ?? lastEventNumber ?? 0;
                var finalEventName = currentEventName ?? lastEventName ?? string.Empty;

                var definition = new KpiDefinitionData
                {
                    EventNumber = finalEventNumber,
                    EventName = finalEventName,
                    Outcome = ParseString(outcomeCell) ?? string.Empty,
                    TeamAssignment = ParseString(teamAssignmentCell) ?? string.Empty,
                    PsrValue = ParseDecimal(psrValueCell, row, "PSR Value") ?? 0m,
                    Definition = ParseString(definitionCell) ?? string.Empty,
                    SourceRowNumber = row
                };

                definitions.Add(definition);

                _logger.LogTrace("Extracted row {Row}: Event {EventNumber} - {EventName} ({Outcome})",
                    row, definition.EventNumber, definition.EventName, definition.Outcome);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing row {Row}", row);
                throw new InvalidOperationException($"Failed to parse KPI definition at row {row}: {ex.Message}", ex);
            }
        }

        return definitions;
    }

    /// <summary>
    /// Checks if a row is completely empty
    /// </summary>
    private bool IsRowEmpty(ExcelWorksheet sheet, int row)
    {
        // Check key columns for content
        for (int col = ColEventNumber; col <= ColDefinition; col++)
        {
            var value = sheet.Cells[row, col].Value;
            if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Safely parses an integer value from Excel cell
    /// </summary>
    private int? ParseInt(object? value, int row, string fieldName)
    {
        if (value == null) return null;

        if (value is double d) return (int)d;
        if (value is int i) return i;

        if (int.TryParse(value.ToString(), out var result))
            return result;

        throw new FormatException($"Invalid integer value for '{fieldName}' at row {row}: {value}");
    }

    /// <summary>
    /// Safely parses a decimal value from Excel cell
    /// </summary>
    private decimal? ParseDecimal(object? value, int row, string fieldName)
    {
        if (value == null) return null;

        if (value is double d) return (decimal)d;
        if (value is decimal dec) return dec;

        if (decimal.TryParse(value.ToString(), out var result))
            return result;

        throw new FormatException($"Invalid decimal value for '{fieldName}' at row {row}: {value}");
    }

    /// <summary>
    /// Safely parses a string value from Excel cell
    /// </summary>
    private string? ParseString(object? value)
    {
        return value?.ToString()?.Trim();
    }

    /// <summary>
    /// Validates header row matches expected structure (defensive check)
    /// </summary>
    public bool ValidateHeaders(ExcelWorksheet sheet)
    {
        if (sheet.Dimension == null)
        {
            _logger.LogWarning("Cannot validate headers: worksheet has no data");
            return false;
        }

        var expectedHeaders = new Dictionary<int, string>
        {
            { ColEventNumber, "Event #" },
            { ColEventName, "Event Name" },
            { ColOutcome, "Outcome" },
            { ColTeamAssignment, "Assign to which team" },
            { ColPsrValue, "PSR Value" },
            { ColDefinition, "Definition" }
        };

        foreach (var (col, expectedHeader) in expectedHeaders)
        {
            var actualHeader = sheet.Cells[HeaderRow, col].Text?.Trim();
            if (!expectedHeader.Equals(actualHeader, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Header mismatch at column {Col}: expected '{Expected}', found '{Actual}'",
                    col, expectedHeader, actualHeader);
                return false;
            }
        }

        _logger.LogDebug("Header validation passed");
        return true;
    }
}
