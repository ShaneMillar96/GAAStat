# Positional Analysis Ingestion Plan

## Executive Summary

This document outlines the detailed plan for ingesting positional analysis data from the Excel file's position-specific sheets into the `positional_analysis` database table. The Excel file contains four position-based sheets (`Goalkeepers`, `Defenders`, `Midfielders`, `Forwards`) that aggregate match performance data by playing position, providing positional benchmarks and comparative analysis.

## Data Source Analysis

### Excel Sheet Structure
- **Source Sheets**: 
  - `Goalkeepers` - Goalkeeper-specific statistics
  - `Defenders` - Defender performance metrics
  - `Midfielders` - Midfielder statistical analysis
  - `Forwards` - Forward/attacker statistics
- **Data Structure**: Variable rows × ~20-30 columns per sheet
- **Format**: Position-specific aggregated statistics across multiple matches
- **Content**: Averaged performance metrics, totals, and position-specific KPIs

### Sample Data Structure
```
Player Name       | Min | TE  | PSR  | Scores | Conversion% | Tackles | Success% | Pos-Specific
------------------|-----|-----|------|--------|-------------|---------|----------|-------------
O Doherty (GK)    | 480 | 45  | 0.82 | 0-00   | 0.00        | 2       | 100.0    | Saves: 12
C McCloskey (F)   | 420 | 78  | 0.71 | 2-08   | 0.65        | 18      | 72.2     | Inside 50: 15
R Doherty (M)     | 390 | 92  | 0.68 | 1-05   | 0.48        | 25      | 68.0     | Assist: 8
```

## Database Target Schema

### positional_analysis Table
```sql
CREATE TABLE positional_analysis (
    positional_analysis_id SERIAL PRIMARY KEY,
    match_id INTEGER REFERENCES matches(match_id),
    position_id INTEGER NOT NULL REFERENCES positions(position_id),
    avg_engagement_efficiency DECIMAL(5,4),
    avg_possession_success_rate DECIMAL(5,4),
    avg_conversion_rate DECIMAL(5,4),
    avg_tackle_success_rate DECIMAL(5,4),
    total_scores INTEGER,
    total_possessions INTEGER,
    total_tackles INTEGER,
    position_specific_metrics JSONB,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

## Detailed Ingestion Mapping

### Phase 1: Sheet Detection and Position Mapping

#### 1.1 Position Sheet Identification
```csharp
private readonly Dictionary<string, string> PositionSheetMapping = new()
{
    { "Goalkeepers", "Goalkeeper" },
    { "Goalkeeper", "Goalkeeper" },
    { "Defenders", "Defender" }, 
    { "Defense", "Defender" },
    { "Midfielders", "Midfielder" },
    { "Midfield", "Midfielder" },
    { "Forwards", "Forward" },
    { "Forward", "Forward" },
    { "Attackers", "Forward" }
};

private bool IsPositionalAnalysisSheet(string sheetName)
{
    return PositionSheetMapping.ContainsKey(sheetName);
}

private string GetPositionFromSheetName(string sheetName)
{
    return PositionSheetMapping.TryGetValue(sheetName, out var position) 
        ? position 
        : "Unknown";
}
```

#### 1.2 Position-Specific Column Mapping
```csharp
private static class PositionalColumnMapping
{
    // Core columns present in all position sheets
    public const int PLAYER_NAME = 0;           // Column A
    public const int MINUTES_PLAYED = 1;        // Column B
    public const int TOTAL_ENGAGEMENTS = 2;     // Column C
    public const int PSR = 3;                   // Column D
    public const int SCORES = 4;                // Column E
    public const int CONVERSION_RATE = 5;       // Column F
    public const int TOTAL_TACKLES = 6;         // Column G
    public const int TACKLE_SUCCESS_RATE = 7;   // Column H
    
    // Position-specific columns start from column I onwards
    public const int POSITION_SPECIFIC_START = 8;
}

// Position-specific metric definitions
private readonly Dictionary<string, List<string>> PositionSpecificMetrics = new()
{
    ["Goalkeeper"] = new List<string>
    {
        "Saves", "Kickouts Total", "Kickouts Retained", "Kickout Success Rate",
        "Long Kickouts", "Short Kickouts", "Clean Sheets", "Goals Conceded"
    },
    ["Defender"] = new List<string>
    {
        "Interceptions", "Clearances", "Aerial Duels Won", "Aerial Success Rate",
        "Blocks", "Defensive Actions", "Last Man Tackles", "Recovery Rate"
    },
    ["Midfielder"] = new List<string>
    {
        "Pass Completion Rate", "Key Passes", "Assists", "Turnovers Won",
        "Possession Retention", "Box to Box Actions", "Link Play Success", "Territory Gained"
    },
    ["Forward"] = new List<string>
    {
        "Inside 50 Entries", "Target Hit Rate", "Assists", "Pressure Acts",
        "1v1 Success Rate", "Ground Ball Gets", "Contested Marks", "Score Assists"
    }
};
```

### Phase 2: Data Extraction and Aggregation

#### 2.1 Position Sheet Processing
```csharp
private async Task<List<PositionalPlayerData>> ProcessPositionSheetAsync(
    ExcelWorksheet worksheet, 
    string sheetName)
{
    var position = GetPositionFromSheetName(sheetName);
    var positionSpecificCols = PositionSpecificMetrics.GetValueOrDefault(position, new List<string>());
    var playerDataList = new List<PositionalPlayerData>();
    
    var lastRow = worksheet.Dimension?.End.Row ?? 0;
    var lastCol = worksheet.Dimension?.End.Column ?? 0;
    
    _logger.LogInformation("Processing position sheet '{SheetName}' for position '{Position}' with {Rows} rows and {Cols} columns",
        sheetName, position, lastRow, lastCol);
    
    // Process each player row (starting from row 2, skip header)
    for (int row = 2; row <= lastRow; row++)
    {
        var playerName = worksheet.Cells[row, PositionalColumnMapping.PLAYER_NAME + 1].Text?.Trim();
        
        // Skip empty rows
        if (string.IsNullOrEmpty(playerName))
            continue;
        
        var playerData = new PositionalPlayerData
        {
            PlayerName = CleanPlayerName(playerName),
            Position = position,
            MinutesPlayed = ParseIntValue(worksheet.Cells[row, PositionalColumnMapping.MINUTES_PLAYED + 1].Value),
            TotalEngagements = ParseIntValue(worksheet.Cells[row, PositionalColumnMapping.TOTAL_ENGAGEMENTS + 1].Value),
            PossessionSuccessRate = ParseDecimalValue(worksheet.Cells[row, PositionalColumnMapping.PSR + 1].Value),
            Scores = ParseScoreValue(worksheet.Cells[row, PositionalColumnMapping.SCORES + 1].Text),
            ConversionRate = ParseDecimalValue(worksheet.Cells[row, PositionalColumnMapping.CONVERSION_RATE + 1].Value),
            TotalTackles = ParseIntValue(worksheet.Cells[row, PositionalColumnMapping.TOTAL_TACKLES + 1].Value),
            TackleSuccessRate = ParseDecimalValue(worksheet.Cells[row, PositionalColumnMapping.TACKLE_SUCCESS_RATE + 1].Value),
            PositionSpecificMetrics = ExtractPositionSpecificMetrics(worksheet, row, positionSpecificCols, lastCol)
        };
        
        playerDataList.Add(playerData);
    }
    
    _logger.LogInformation("Extracted {PlayerCount} players from position sheet '{SheetName}'", 
        playerDataList.Count, sheetName);
    
    return playerDataList;
}
```

#### 2.2 Position-Specific Metric Extraction
```csharp
private Dictionary<string, object?> ExtractPositionSpecificMetrics(
    ExcelWorksheet worksheet, 
    int row, 
    List<string> metricNames, 
    int maxCol)
{
    var metrics = new Dictionary<string, object?>();
    
    // Start from position-specific columns
    var startCol = PositionalColumnMapping.POSITION_SPECIFIC_START + 1;
    
    for (int col = startCol; col <= maxCol && (col - startCol) < metricNames.Count; col++)
    {
        var metricName = metricNames[col - startCol];
        var cellValue = worksheet.Cells[row, col].Value;
        
        // Determine value type based on metric name
        var processedValue = ProcessPositionSpecificValue(metricName, cellValue);
        metrics[metricName] = processedValue;
    }
    
    return metrics;
}

private object? ProcessPositionSpecificValue(string metricName, object? cellValue)
{
    if (cellValue == null || string.IsNullOrEmpty(cellValue.ToString()))
        return null;
    
    // Handle percentage metrics
    if (metricName.Contains("Rate", StringComparison.OrdinalIgnoreCase) ||
        metricName.Contains("Success", StringComparison.OrdinalIgnoreCase) ||
        metricName.Contains("%", StringComparison.OrdinalIgnoreCase))
    {
        return ParseDecimalValue(cellValue);
    }
    
    // Handle count metrics
    if (metricName.Contains("Total", StringComparison.OrdinalIgnoreCase) ||
        metricName.Contains("Count", StringComparison.OrdinalIgnoreCase) ||
        metricName.EndsWith("s", StringComparison.OrdinalIgnoreCase))
    {
        return ParseIntValue(cellValue);
    }
    
    // Default to decimal for numeric values, string otherwise
    if (decimal.TryParse(cellValue.ToString(), out var decimalValue))
        return decimalValue;
    
    return cellValue.ToString()?.Trim();
}
```

### Phase 3: Match-Level Aggregation

#### 3.1 Position Performance Aggregation
```csharp
public async Task<List<PositionalAnalysisRecord>> AggregatePositionalDataAsync(
    List<PositionalPlayerData> playerDataList, 
    string position,
    List<int> matchIds)
{
    var aggregatedRecords = new List<PositionalAnalysisRecord>();
    
    // Get position ID
    var positionId = await GetPositionIdAsync(position);
    
    // For each match, create aggregated positional analysis
    foreach (var matchId in matchIds)
    {
        // Filter players who played in this match (this is simplified - in reality, 
        // you'd need to cross-reference with match_player_statistics)
        var matchPlayers = await GetPlayersForMatchAndPositionAsync(matchId, positionId);
        var relevantPlayerData = playerDataList.Where(p => 
            matchPlayers.Any(mp => mp.PlayerName.Equals(p.PlayerName, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        
        if (!relevantPlayerData.Any())
            continue;
        
        var record = new PositionalAnalysisRecord
        {
            MatchId = matchId,
            PositionId = positionId,
            AvgEngagementEfficiency = CalculateAverage(relevantPlayerData, p => 
                p.TotalEngagements > 0 ? (decimal)p.TotalEngagements / (p.MinutesPlayed ?? 1) : 0),
            AvgPossessionSuccessRate = CalculateAverage(relevantPlayerData, p => p.PossessionSuccessRate),
            AvgConversionRate = CalculateAverage(relevantPlayerData, p => p.ConversionRate),
            AvgTackleSuccessRate = CalculateAverage(relevantPlayerData, p => p.TackleSuccessRate),
            TotalScores = relevantPlayerData.Sum(p => p.Scores?.Goals * 3 + p.Scores?.Points ?? 0),
            TotalPossessions = relevantPlayerData.Sum(p => CalculatePossessionsFromEngagements(p)),
            TotalTackles = relevantPlayerData.Sum(p => p.TotalTackles ?? 0),
            PositionSpecificMetrics = AggregatePositionSpecificMetrics(relevantPlayerData, position)
        };
        
        aggregatedRecords.Add(record);
    }
    
    return aggregatedRecords;
}

private decimal? CalculateAverage(List<PositionalPlayerData> players, Func<PositionalPlayerData, decimal?> selector)
{
    var values = players.Select(selector).Where(v => v.HasValue).Select(v => v.Value).ToList();
    return values.Any() ? values.Average() : null;
}

private Dictionary<string, object?> AggregatePositionSpecificMetrics(
    List<PositionalPlayerData> players, 
    string position)
{
    var aggregated = new Dictionary<string, object?>();
    var allMetrics = players.SelectMany(p => p.PositionSpecificMetrics.Keys).Distinct().ToList();
    
    foreach (var metric in allMetrics)
    {
        var values = players
            .Where(p => p.PositionSpecificMetrics.ContainsKey(metric))
            .Select(p => p.PositionSpecificMetrics[metric])
            .Where(v => v != null)
            .ToList();
        
        if (!values.Any()) continue;
        
        // Aggregate based on metric type
        if (metric.Contains("Rate") || metric.Contains("Success") || metric.Contains("%"))
        {
            // Calculate average for rates/percentages
            var numericValues = values.OfType<decimal>().ToList();
            aggregated[metric] = numericValues.Any() ? numericValues.Average() : null;
        }
        else
        {
            // Sum for counts/totals
            var numericValues = values.Where(v => decimal.TryParse(v?.ToString(), out _))
                .Select(v => decimal.Parse(v.ToString())).ToList();
            aggregated[metric] = numericValues.Any() ? numericValues.Sum() : null;
        }
    }
    
    return aggregated;
}
```

### Phase 4: Database Integration

#### 4.1 Position Reference Management
```csharp
private async Task<int> GetPositionIdAsync(string positionName)
{
    var position = await _context.Positions
        .FirstOrDefaultAsync(p => p.PositionName.Equals(positionName, StringComparison.OrdinalIgnoreCase));
    
    if (position != null)
        return position.PositionId;
    
    // Create if doesn't exist
    var newPosition = new Position
    {
        PositionName = positionName,
        Description = $"Playing position: {positionName}",
        IsActive = true
    };
    
    await _context.Positions.AddAsync(newPosition);
    await _context.SaveChangesAsync();
    
    return newPosition.PositionId;
}

private async Task<List<Player>> GetPlayersForMatchAndPositionAsync(int matchId, int positionId)
{
    // Get players who played in the match and play the specified position
    return await _context.MatchPlayerStatistics
        .Where(mps => mps.MatchId == matchId)
        .Join(_context.Players, mps => mps.PlayerId, p => p.PlayerId, (mps, p) => p)
        .Where(p => p.PositionId == positionId)
        .ToListAsync();
}
```

#### 4.2 Database Record Creation
```csharp
private async Task<int> SavePositionalAnalysisRecordsAsync(
    List<PositionalAnalysisRecord> records, 
    int jobId)
{
    var dbRecords = new List<PositionalAnalysis>();
    
    foreach (var record in records)
    {
        var dbRecord = new PositionalAnalysis
        {
            MatchId = record.MatchId,
            PositionId = record.PositionId,
            AvgEngagementEfficiency = record.AvgEngagementEfficiency,
            AvgPossessionSuccessRate = record.AvgPossessionSuccessRate,
            AvgConversionRate = record.AvgConversionRate,
            AvgTackleSuccessRate = record.AvgTackleSuccessRate,
            TotalScores = record.TotalScores,
            TotalPossessions = record.TotalPossessions,
            TotalTackles = record.TotalTackles,
            PositionSpecificMetrics = JsonSerializer.Serialize(record.PositionSpecificMetrics)
        };
        
        dbRecords.Add(dbRecord);
    }
    
    await _context.PositionalAnalysis.AddRangeAsync(dbRecords);
    await _context.SaveChangesAsync();
    
    _logger.LogInformation("Saved {RecordCount} positional analysis records for job {JobId}",
        dbRecords.Count, jobId);
    
    return dbRecords.Count;
}
```

### Phase 5: Main Processing Pipeline

#### 5.1 Complete Positional Analysis Processing
```csharp
public async Task<ServiceResult<PositionalAnalysisResult>> ProcessPositionalAnalysisAsync(
    Stream fileStream, 
    List<ExcelSheetAnalysis> positionSheets,
    List<int> matchIds,
    int jobId)
{
    var stopwatch = Stopwatch.StartNew();
    var totalRecordsCreated = 0;
    var processedSheets = 0;
    var failedSheets = 0;
    
    try
    {
        using var package = new ExcelPackage(fileStream);
        
        foreach (var sheet in positionSheets.Where(s => IsPositionalAnalysisSheet(s.Name)))
        {
            try
            {
                var worksheet = package.Workbook.Worksheets[sheet.Name];
                if (worksheet == null)
                {
                    _logger.LogWarning("Position sheet '{SheetName}' not found", sheet.Name);
                    failedSheets++;
                    continue;
                }
                
                _logger.LogInformation("Processing position sheet: '{SheetName}'", sheet.Name);
                
                // Extract player data from sheet
                var playerDataList = await ProcessPositionSheetAsync(worksheet, sheet.Name);
                
                if (!playerDataList.Any())
                {
                    _logger.LogWarning("No player data found in sheet '{SheetName}'", sheet.Name);
                    continue;
                }
                
                // Aggregate data by match and position
                var position = GetPositionFromSheetName(sheet.Name);
                var aggregatedRecords = await AggregatePositionalDataAsync(playerDataList, position, matchIds);
                
                if (aggregatedRecords.Any())
                {
                    var savedRecords = await SavePositionalAnalysisRecordsAsync(aggregatedRecords, jobId);
                    totalRecordsCreated += savedRecords;
                    
                    _logger.LogInformation(
                        "✅ Processed position sheet '{SheetName}': {PlayerCount} players, {RecordCount} analysis records created",
                        sheet.Name, playerDataList.Count, savedRecords);
                }
                
                processedSheets++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process position sheet '{SheetName}'", sheet.Name);
                failedSheets++;
                
                await _progressService.RecordValidationErrorAsync(
                    jobId, sheet.Name, null, null,
                    EtlErrorTypes.PROCESSING_ERROR,
                    $"Failed to process position sheet: {ex.Message}",
                    "Check sheet format and data structure");
            }
        }
        
        stopwatch.Stop();
        
        return ServiceResult<PositionalAnalysisResult>.Success(new PositionalAnalysisResult
        {
            RecordsCreated = totalRecordsCreated,
            ProcessedSheets = processedSheets,
            FailedSheets = failedSheets,
            ProcessingTimeMs = stopwatch.ElapsedMilliseconds
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to process positional analysis");
        return ServiceResult<PositionalAnalysisResult>.Failed($"Positional analysis processing failed: {ex.Message}");
    }
}
```

### Phase 6: Data Validation and Quality Assurance

#### 6.1 Position-Specific Validation
```csharp
private async Task ValidatePositionalData(
    List<PositionalPlayerData> playerDataList, 
    string position, 
    int jobId)
{
    foreach (var player in playerDataList)
    {
        // Validate core metrics
        if (player.PossessionSuccessRate.HasValue && 
            (player.PossessionSuccessRate < 0 || player.PossessionSuccessRate > 1))
        {
            await _progressService.RecordValidationErrorAsync(
                jobId, $"{position} Sheet", null, "PSR",
                EtlErrorTypes.INVALID_RANGE,
                $"Invalid PSR ({player.PossessionSuccessRate}) for player {player.PlayerName}",
                "PSR should be between 0 and 1");
        }
        
        // Validate position-specific constraints
        await ValidatePositionSpecificConstraints(player, position, jobId);
    }
}

private async Task ValidatePositionSpecificConstraints(
    PositionalPlayerData player, 
    string position, 
    int jobId)
{
    switch (position.ToLowerInvariant())
    {
        case "goalkeeper":
            await ValidateGoalkeeperData(player, jobId);
            break;
        case "defender":
            await ValidateDefenderData(player, jobId);
            break;
        case "midfielder":
            await ValidateMiddiefielderData(player, jobId);
            break;
        case "forward":
            await ValidateForwardData(player, jobId);
            break;
    }
}

private async Task ValidateGoalkeeperData(PositionalPlayerData player, int jobId)
{
    // Goalkeepers should have minimal scores
    if (player.Scores != null && (player.Scores.Goals > 1 || player.Scores.Points > 5))
    {
        await _progressService.RecordValidationErrorAsync(
            jobId, "Goalkeepers", null, "Scores",
            "warning",
            $"Unusually high scoring for goalkeeper {player.PlayerName}: {player.Scores.Goals}-{player.Scores.Points:D2}",
            "Verify player position assignment");
    }
    
    // Check for saves metric
    if (player.PositionSpecificMetrics.ContainsKey("Saves") &&
        !int.TryParse(player.PositionSpecificMetrics["Saves"]?.ToString(), out _))
    {
        await _progressService.RecordValidationErrorAsync(
            jobId, "Goalkeepers", null, "Saves",
            EtlErrorTypes.INVALID_DATA_TYPE,
            $"Invalid saves data for goalkeeper {player.PlayerName}",
            "Saves should be a numeric value");
    }
}

private async Task ValidateForwardData(PositionalPlayerData player, int jobId)
{
    // Forwards should generally have higher scoring rates
    if (player.ConversionRate.HasValue && player.ConversionRate < 0.2m && 
        player.Scores?.Goals + player.Scores?.Points > 5)
    {
        await _progressService.RecordValidationErrorAsync(
            jobId, "Forwards", null, "Conversion Rate",
            "warning",
            $"Low conversion rate ({player.ConversionRate:P1}) for high-scoring forward {player.PlayerName}",
            "Verify conversion rate calculation");
    }
}
```

#### 6.2 Cross-Position Comparative Analysis
```csharp
private async Task PerformCrossPositionAnalysis(
    List<PositionalAnalysisRecord> allRecords, 
    int jobId)
{
    // Group by match for comparative analysis
    var matchGroups = allRecords.GroupBy(r => r.MatchId).ToList();
    
    foreach (var matchGroup in matchGroups)
    {
        var matchRecords = matchGroup.ToList();
        
        // Analyze engagement efficiency across positions
        var avgEngagements = matchRecords
            .Where(r => r.AvgEngagementEfficiency.HasValue)
            .Select(r => new { Position = r.PositionId, Efficiency = r.AvgEngagementEfficiency.Value })
            .ToList();
        
        if (avgEngagements.Count >= 2)
        {
            var maxEfficiency = avgEngagements.Max(e => e.Efficiency);
            var minEfficiency = avgEngagements.Min(e => e.Efficiency);
            var efficiencySpread = maxEfficiency - minEfficiency;
            
            if (efficiencySpread > 0.5m) // Large spread indicates potential data issues
            {
                await _progressService.RecordValidationErrorAsync(
                    jobId, "Positional Analysis", null, "Engagement Efficiency",
                    "warning",
                    $"Large engagement efficiency spread ({efficiencySpread:F2}) in match {matchGroup.Key}",
                    "Verify positional data accuracy and calculation methods");
            }
        }
        
        // Log positional performance insights
        _logger.LogInformation(
            "Match {MatchId} positional analysis: {PositionCount} positions analyzed, " +
            "avg efficiency range {MinEfficiency:F2}-{MaxEfficiency:F2}",
            matchGroup.Key, matchRecords.Count,
            avgEngagements.Any() ? avgEngagements.Min(e => e.Efficiency) : 0,
            avgEngagements.Any() ? avgEngagements.Max(e => e.Efficiency) : 0);
    }
}
```

## Performance Considerations

### Expected Data Volume:
- **Position Sheets**: 4 sheets (Goalkeepers, Defenders, Midfielders, Forwards)
- **Players Per Position**: 3-8 players per position
- **Analysis Records**: 4 positions × 8 matches = 32 records
- **Processing Time Target**: < 2 minutes

### Memory Management:
```csharp
// Efficient processing of position data
private async Task ProcessPositionSheetsInSequence(
    ExcelPackage package, 
    List<ExcelSheetAnalysis> positionSheets,
    int jobId)
{
    // Process one position sheet at a time to manage memory
    foreach (var sheet in positionSheets)
    {
        await ProcessSinglePositionSheetAsync(package, sheet, jobId);
        
        // Force garbage collection between sheets
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        // Allow other tasks to run
        await Task.Yield();
    }
}
```

## Integration Points

### Service Layer Integration
```csharp
// Add to IExcelProcessingService interface
Task<ServiceResult<PositionalAnalysisResult>> ProcessPositionalAnalysisAsync(
    Stream fileStream,
    List<ExcelSheetAnalysis> positionSheets,
    List<int> matchIds,
    int jobId,
    CancellationToken cancellationToken = default);
```

### Progress Tracking
```csharp
// Update main processing pipeline
await _progressService.RecordProgressUpdateAsync(new EtlProgressUpdate
{
    JobId = jobId,
    Stage = EtlStages.PROCESSING_POSITIONAL_ANALYSIS,
    TotalSteps = positionSheets.Count,
    CompletedSteps = processedSheets,
    Status = "processing",
    Message = $"Processing positional analysis: {processedSheets}/{positionSheets.Count} sheets completed"
});
```

## Success Criteria

- ✅ All 4 position sheets successfully processed
- ✅ Player data correctly aggregated by position and match
- ✅ Position-specific metrics properly captured in JSONB format
- ✅ Core performance metrics accurately calculated
- ✅ Cross-position comparative analysis implemented
- ✅ Position-specific validation rules applied
- ✅ Data quality checks and error reporting in place
- ✅ Processing completed within performance targets
- ✅ Comprehensive logging and metrics tracking
- ✅ Memory-efficient processing for large datasets

This plan ensures that positional analysis data is accurately aggregated from individual position sheets and provides valuable insights into position-based team performance, enabling tactical analysis and player evaluation by playing position.