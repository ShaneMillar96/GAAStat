# Match Team Statistics Ingestion Plan

## Executive Summary

This document outlines the detailed plan for ingesting team-level match statistics from the Excel file's team match sheets into the `match_team_statistics` database table. Each match sheet contains approximately **235 statistical metrics** with first half, second half, and full game breakdowns for both Drum and opposition teams.

## Data Source Analysis

### Excel Sheet Structure
- **Source Sheets**: `0X. [Competition] vs [Opposition] [Date]` (8 sheets)
- **Data Structure**: 236 rows × 6 columns per sheet
- **Column Format**: 
  - Column A: Drum First Half
  - Column B: Drum Second Half  
  - Column C: Drum Full Game
  - Column D: Opposition First Half
  - Column E: Opposition Second Half
  - Column F: Opposition Full Game

### Sample Data Structure
```
Row | Metric Name              | Drum 1H | Drum 2H | Drum FG | Opp 1H | Opp 2H | Opp FG
----|--------------------------|---------|---------|---------|--------|--------|--------
1   | Match Header Info        | 2-06    | -       | -       | 0-11   | -      | -
4   | Total Possession         | 0.5754  | 0.3981  | 0.4805  | 0.4246 | 0.6019 | 0.5195
5   | Possession lost          | 11      | 8       | 19      | 12     | 7      | 19
... | ...                      | ...     | ...     | ...     | ...    | ...    | ...
236 | Last Metric             | X       | X       | X       | X      | X      | X
```

## Database Target Schema

### match_team_statistics Table
```sql
CREATE TABLE match_team_statistics (
    match_team_stat_id SERIAL PRIMARY KEY,
    match_id INTEGER NOT NULL REFERENCES matches(match_id),
    metric_definition_id INTEGER NOT NULL REFERENCES metric_definitions(metric_definition_id),
    drum_first_half DECIMAL(10,6),
    drum_second_half DECIMAL(10,6),
    drum_full_game DECIMAL(10,6),
    opposition_first_half DECIMAL(10,6),
    opposition_second_half DECIMAL(10,6),
    opposition_full_game DECIMAL(10,6),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

## Detailed Ingestion Mapping

### Phase 1: Sheet Identification and Parsing

#### 1.1 Sheet Detection Logic
```csharp
private bool IsTeamMatchStatsSheet(string sheetName)
{
    // Pattern: "XX. [Competition/Team] vs [Team] [Date?]"
    var pattern = @"^\d{2}\.\s+.+\s+vs\s+.+";
    return Regex.IsMatch(sheetName, pattern) && 
           !sheetName.Contains("Player Stats", StringComparison.OrdinalIgnoreCase);
}
```

#### 1.2 Metric Name Extraction
Starting from **Row 4** (after header rows), extract metric names from Column A:
- Row 1-3: Match header information (skip)
- Row 4+: Statistical metrics

```csharp
private string ExtractMetricName(ExcelWorksheet worksheet, int row)
{
    var cellValue = worksheet.Cells[row, 1].Text?.Trim();
    
    // Handle empty or merged cells
    if (string.IsNullOrEmpty(cellValue))
    {
        // Look backwards for the last non-empty value
        for (int i = row - 1; i >= 4; i--)
        {
            var previousValue = worksheet.Cells[i, 1].Text?.Trim();
            if (!string.IsNullOrEmpty(previousValue))
            {
                return $"{previousValue}_sub_{row - i}";
            }
        }
    }
    
    return cellValue ?? $"unknown_metric_row_{row}";
}
```

### Phase 2: Data Value Extraction and Transformation

#### 2.1 Numeric Value Processing
```csharp
private decimal? ProcessNumericValue(object cellValue)
{
    if (cellValue == null) return null;
    
    var stringValue = cellValue.ToString()?.Trim();
    
    // Handle common Excel representations
    if (string.IsNullOrEmpty(stringValue) || 
        stringValue.Equals("NaN", StringComparison.OrdinalIgnoreCase) ||
        stringValue.Equals("-", StringComparison.OrdinalIgnoreCase))
    {
        return null;
    }
    
    // Handle percentage values (0.0 to 1.0)
    if (decimal.TryParse(stringValue, out var decimalValue))
    {
        // Ensure percentage values are in correct range
        if (decimalValue > 1.0m && decimalValue <= 100.0m)
        {
            return decimalValue / 100.0m; // Convert percentage to decimal
        }
        return Math.Round(decimalValue, 6); // Limit to 6 decimal places
    }
    
    return null;
}
```

#### 2.2 Row Processing Logic
```csharp
private async Task<TeamStatisticRow?> ProcessStatisticRowAsync(
    ExcelWorksheet worksheet, 
    int row, 
    int matchId)
{
    var metricName = ExtractMetricName(worksheet, row);
    
    // Skip header rows and empty metrics
    if (row <= 3 || string.IsNullOrEmpty(metricName))
        return null;
    
    // Get or create metric definition
    var metricDefinitionId = await GetOrCreateMetricDefinitionAsync(metricName);
    
    return new TeamStatisticRow
    {
        MatchId = matchId,
        MetricDefinitionId = metricDefinitionId,
        DrumFirstHalf = ProcessNumericValue(worksheet.Cells[row, 1].Value),
        DrumSecondHalf = ProcessNumericValue(worksheet.Cells[row, 2].Value),
        DrumFullGame = ProcessNumericValue(worksheet.Cells[row, 3].Value),
        OppositionFirstHalf = ProcessNumericValue(worksheet.Cells[row, 4].Value),
        OppositionSecondHalf = ProcessNumericValue(worksheet.Cells[row, 5].Value),
        OppositionFullGame = ProcessNumericValue(worksheet.Cells[row, 6].Value)
    };
}
```

### Phase 3: Metric Definition Management

#### 3.1 Dynamic Metric Creation
```csharp
private async Task<int> GetOrCreateMetricDefinitionAsync(string metricName)
{
    // Normalize metric name
    var normalizedName = NormalizeMetricName(metricName);
    
    // Check if metric exists
    var existingMetric = await _context.MetricDefinitions
        .FirstOrDefaultAsync(md => md.MetricName == normalizedName);
    
    if (existingMetric != null)
        return existingMetric.MetricDefinitionId;
    
    // Create new metric definition
    var newMetric = new MetricDefinition
    {
        MetricName = normalizedName,
        DisplayName = metricName,
        MetricCategoryId = await GetDefaultCategoryAsync(),
        DataType = DetermineDataType(metricName),
        Description = $"Team statistic: {metricName}",
        IsActive = true
    };
    
    await _context.MetricDefinitions.AddAsync(newMetric);
    await _context.SaveChangesAsync();
    
    return newMetric.MetricDefinitionId;
}

private string NormalizeMetricName(string metricName)
{
    return metricName
        .ToLowerInvariant()
        .Replace(" ", "_")
        .Replace("-", "_")
        .Replace("(", "")
        .Replace(")", "")
        .Replace("%", "percent")
        .Trim();
}

private string DetermineDataType(string metricName)
{
    // Percentage indicators
    if (metricName.Contains("%") || metricName.Contains("Rate") || 
        metricName.Contains("Success") || metricName.Contains("Efficiency"))
        return "percentage";
    
    // Count indicators  
    if (metricName.Contains("Total") || metricName.Contains("Count") ||
        metricName.EndsWith("s")) // Plurals like "Attacks", "Tackles"
        return "count";
    
    return "decimal";
}
```

### Phase 4: Bulk Data Insertion

#### 4.1 Batch Processing Strategy
```csharp
public async Task<ServiceResult<int>> ProcessTeamStatsSheetAsync(
    ExcelWorksheet worksheet, 
    int matchId, 
    string sheetName,
    int jobId)
{
    var statisticsToInsert = new List<MatchTeamStatistic>();
    var processedRows = 0;
    var skippedRows = 0;
    
    try
    {
        // Determine last row with data
        var lastRow = worksheet.Dimension?.End.Row ?? 0;
        
        _logger.LogInformation("Processing team statistics sheet '{SheetName}' with {RowCount} rows", 
            sheetName, lastRow);
        
        // Process each row starting from row 4
        for (int row = 4; row <= lastRow; row++)
        {
            var statisticRow = await ProcessStatisticRowAsync(worksheet, row, matchId);
            
            if (statisticRow != null)
            {
                statisticsToInsert.Add(new MatchTeamStatistic
                {
                    MatchId = statisticRow.MatchId,
                    MetricDefinitionId = statisticRow.MetricDefinitionId,
                    DrumFirstHalf = statisticRow.DrumFirstHalf,
                    DrumSecondHalf = statisticRow.DrumSecondHalf,
                    DrumFullGame = statisticRow.DrumFullGame,
                    OppositionFirstHalf = statisticRow.OppositionFirstHalf,
                    OppositionSecondHalf = statisticRow.OppositionSecondHalf,
                    OppositionFullGame = statisticRow.OppositionFullGame
                });
                processedRows++;
            }
            else
            {
                skippedRows++;
            }
        }
        
        // Bulk insert statistics
        if (statisticsToInsert.Any())
        {
            await _context.MatchTeamStatistics.AddRangeAsync(statisticsToInsert);
            await _context.SaveChangesAsync();
        }
        
        _logger.LogInformation(
            "Team statistics processing completed for sheet '{SheetName}': " +
            "{ProcessedRows} rows processed, {SkippedRows} rows skipped, " +
            "{InsertedStats} statistics inserted", 
            sheetName, processedRows, skippedRows, statisticsToInsert.Count);
        
        return ServiceResult<int>.Success(statisticsToInsert.Count);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to process team statistics sheet '{SheetName}'", sheetName);
        
        await _progressService.RecordValidationErrorAsync(
            jobId, sheetName, null, null,
            EtlErrorTypes.PROCESSING_ERROR,
            $"Failed to process team statistics: {ex.Message}",
            "Check sheet format and data integrity");
        
        return ServiceResult<int>.Failed($"Failed to process team statistics: {ex.Message}");
    }
}
```

## Data Validation Rules

### 4.1 Value Range Validation
```csharp
private async Task ValidateStatisticValues(
    List<MatchTeamStatistic> statistics, 
    int jobId, 
    string sheetName)
{
    foreach (var stat in statistics)
    {
        // Validate percentage ranges (0-1)
        var percentageFields = new[]
        {
            (stat.DrumFirstHalf, "Drum First Half"),
            (stat.DrumSecondHalf, "Drum Second Half"),
            (stat.DrumFullGame, "Drum Full Game"),
            (stat.OppositionFirstHalf, "Opposition First Half"),
            (stat.OppositionSecondHalf, "Opposition Second Half"),
            (stat.OppositionFullGame, "Opposition Full Game")
        };
        
        foreach (var (value, fieldName) in percentageFields)
        {
            if (value.HasValue && (value < 0 || value > 1))
            {
                await _progressService.RecordValidationErrorAsync(
                    jobId, sheetName, null, fieldName,
                    EtlErrorTypes.INVALID_RANGE,
                    $"Value {value} is outside valid percentage range (0-1)",
                    "Check if value should be percentage (0-1) or count (>1)");
            }
        }
    }
}
```

### 4.2 Consistency Validation
```csharp
private async Task ValidateStatisticConsistency(
    List<MatchTeamStatistic> statistics,
    int jobId,
    string sheetName)
{
    foreach (var stat in statistics)
    {
        // Validate that full game values are consistent with halves (where applicable)
        if (stat.DrumFirstHalf.HasValue && stat.DrumSecondHalf.HasValue && stat.DrumFullGame.HasValue)
        {
            var expectedFullGame = stat.DrumFirstHalf.Value + stat.DrumSecondHalf.Value;
            var actualFullGame = stat.DrumFullGame.Value;
            
            if (Math.Abs(expectedFullGame - actualFullGame) > 0.01m) // Allow small rounding differences
            {
                await _progressService.RecordValidationErrorAsync(
                    jobId, sheetName, null, "Full Game Consistency",
                    EtlErrorTypes.CONSISTENCY_ERROR,
                    $"Drum full game value ({actualFullGame}) doesn't match sum of halves ({expectedFullGame})",
                    "Verify calculation methodology - some metrics may not be additive");
            }
        }
    }
}
```

## Error Handling Strategies

### 5.1 Sheet Structure Errors
```csharp
private async Task<bool> ValidateSheetStructure(
    ExcelWorksheet worksheet, 
    string sheetName, 
    int jobId)
{
    var errors = new List<string>();
    
    // Check minimum columns
    if (worksheet.Dimension?.End.Column < 6)
    {
        errors.Add($"Sheet must have at least 6 columns for team statistics");
    }
    
    // Check minimum rows
    if (worksheet.Dimension?.End.Row < 10)
    {
        errors.Add($"Sheet must have at least 10 rows of data");
    }
    
    // Validate header structure by checking for known patterns
    var hasScoreInRow1 = !string.IsNullOrEmpty(worksheet.Cells[1, 1].Text) && 
                         worksheet.Cells[1, 1].Text.Contains("-");
    
    if (!hasScoreInRow1)
    {
        errors.Add("Expected score format (X-XX) not found in row 1");
    }
    
    if (errors.Any())
    {
        foreach (var error in errors)
        {
            await _progressService.RecordValidationErrorAsync(
                jobId, sheetName, null, null,
                EtlErrorTypes.SHEET_STRUCTURE,
                error,
                "Ensure sheet follows expected GAA match statistics format");
        }
        return false;
    }
    
    return true;
}
```

### 5.2 Performance Monitoring
```csharp
public async Task<ServiceResult<TeamStatsProcessingResult>> ProcessAllTeamStatsAsync(
    Stream fileStream, 
    List<ExcelSheetAnalysis> teamSheets, 
    Dictionary<string, int> matchIdMap, 
    int jobId)
{
    var stopwatch = Stopwatch.StartNew();
    var totalStatsCreated = 0;
    var processedSheets = 0;
    var failedSheets = 0;
    
    try
    {
        using var package = new ExcelPackage(fileStream);
        
        foreach (var sheet in teamSheets)
        {
            var sheetStartTime = DateTime.UtcNow;
            
            if (!matchIdMap.TryGetValue(sheet.Name, out var matchId))
            {
                _logger.LogWarning("No match ID found for team stats sheet '{SheetName}'", sheet.Name);
                failedSheets++;
                continue;
            }
            
            var worksheet = package.Workbook.Worksheets[sheet.Name];
            if (worksheet == null)
            {
                _logger.LogWarning("Worksheet '{SheetName}' not found", sheet.Name);
                failedSheets++;
                continue;
            }
            
            var result = await ProcessTeamStatsSheetAsync(worksheet, matchId, sheet.Name, jobId);
            
            if (result.IsSuccess)
            {
                totalStatsCreated += result.Data;
                processedSheets++;
                
                var duration = DateTime.UtcNow - sheetStartTime;
                _logger.LogInformation(
                    "✅ Processed team stats sheet '{SheetName}': {Stats} statistics created in {Duration}ms",
                    sheet.Name, result.Data, duration.TotalMilliseconds);
            }
            else
            {
                failedSheets++;
                _logger.LogError("❌ Failed to process team stats sheet '{SheetName}': {Error}", 
                    sheet.Name, result.ErrorMessage);
            }
        }
        
        stopwatch.Stop();
        
        return ServiceResult<TeamStatsProcessingResult>.Success(new TeamStatsProcessingResult
        {
            TotalStatisticsCreated = totalStatsCreated,
            ProcessedSheets = processedSheets,
            FailedSheets = failedSheets,
            ProcessingTimeMs = stopwatch.ElapsedMilliseconds
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to process team statistics");
        return ServiceResult<TeamStatsProcessingResult>.Failed($"Team statistics processing failed: {ex.Message}");
    }
}
```

## Expected Data Volume

### Per Match Sheet:
- **Rows**: 232 statistical metrics (rows 4-236)
- **Statistics Per Sheet**: 232 × 1 = 232 records
- **Total for 8 Matches**: 232 × 8 = **1,856 records**

### Processing Performance Targets:
- **Per Sheet**: < 30 seconds
- **Total Processing**: < 4 minutes
- **Memory Usage**: < 500MB peak
- **Database Insertion**: Batch inserts of 100 records

## Integration Points

### 6.1 Service Layer Integration
```csharp
// Add to IExcelProcessingService interface
Task<ServiceResult<TeamStatsProcessingResult>> ProcessTeamStatisticsAsync(
    Stream fileStream,
    List<ExcelSheetAnalysis> teamSheets,
    Dictionary<string, int> matchIdMap,
    int jobId,
    CancellationToken cancellationToken = default);
```

### 6.2 Progress Tracking Integration
```csharp
// Update progress during processing
await _progressService.RecordProgressUpdateAsync(new EtlProgressUpdate
{
    JobId = jobId,
    Stage = EtlStages.PROCESSING_TEAM_STATISTICS,
    TotalSteps = teamSheets.Count,
    CompletedSteps = processedSheets,
    Status = "processing",
    Message = $"Processing team statistics: {processedSheets}/{teamSheets.Count} sheets completed"
});
```

## Success Criteria

- ✅ All 8 team match sheets successfully processed
- ✅ 232 metrics per sheet properly mapped to database
- ✅ All numeric values correctly transformed and validated
- ✅ Metric definitions automatically created for new metrics
- ✅ Comprehensive error logging and validation reporting
- ✅ Processing completed within performance targets
- ✅ Data integrity maintained across all transformations

This comprehensive plan ensures that all team-level match statistics are accurately captured from the Excel file and properly stored in the normalized database structure, providing the foundation for detailed GAA match analysis and reporting.