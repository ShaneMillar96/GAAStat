# Shot Analysis Ingestion Plan

## Executive Summary

This document outlines the detailed plan for ingesting shot analysis data from the Excel file's "Shots from play Data" sheet into the `shot_analysis` database table. This sheet contains event-level tracking of shot attempts with detailed outcomes, timing, and player information for both Drum and opposition teams.

## Data Source Analysis

### Excel Sheet Structure
- **Source Sheet**: `Shots from play Data`
- **Data Structure**: 131 rows × 21 columns (dual-column format)
- **Format**: Side-by-side data for Drum and Opposition
- **Event-Level Data**: Individual shot attempts with timestamps, players, and outcomes

### Column Structure Analysis
```
Columns 1-10: Drum Team Shots
- Event, Time, Period, Team Name, Name, Outcome, Player, Location, Competition, Teams

Columns 11-21: Opposition Team Shots  
- Event.1, Time.1, Period.1, Team Name.1, Name.1, Outcome.1, Player.1, Location.1, Competition.1, Teams.1, Unnamed: 10
```

### Sample Data Structure
```
Event | Time  | Period | Team Name | Name         | Outcome | Player      | Location | Competition | Teams
------|-------|--------|-----------|--------------|---------|-------------|----------|-------------|-------------
1     | 02:15 | 1      | Drum      | Shot from play| Point  | C McCloskey | 23       | League      | Drum vs Glack
2     | 05:30 | 1      | Drum      | Shot from play| Wide   | R Doherty   | 25       | League      | Drum vs Glack
3     | 08:45 | 1      | Drum      | Shot from play| Goal   | C McCloskey | 24       | League      | Drum vs Glack
```

## Database Target Schema

### shot_analysis Table
```sql
CREATE TABLE shot_analysis (
    shot_analysis_id SERIAL PRIMARY KEY,
    match_id INTEGER NOT NULL REFERENCES matches(match_id),
    player_id INTEGER REFERENCES players(player_id),
    shot_number INTEGER,
    shot_type_id INTEGER REFERENCES shot_types(shot_type_id),
    shot_outcome_id INTEGER REFERENCES shot_outcomes(shot_outcome_id),
    time_period VARCHAR(10),
    position_area_id INTEGER REFERENCES position_areas(position_area_id),
    team_type_id INTEGER REFERENCES team_types(team_type_id),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

## Detailed Ingestion Mapping

### Phase 1: Sheet Detection and Structure Validation

#### 1.1 Sheet Identification
```csharp
private bool IsShotAnalysisSheet(string sheetName)
{
    return sheetName.Equals("Shots from play Data", StringComparison.OrdinalIgnoreCase) ||
           sheetName.Contains("Shots from play", StringComparison.OrdinalIgnoreCase) ||
           sheetName.Contains("Shot Analysis", StringComparison.OrdinalIgnoreCase);
}
```

#### 1.2 Column Mapping Configuration
```csharp
private static class ShotColumnMapping
{
    // Drum team columns (1-10)
    public const int DRUM_EVENT = 0;           // Column A
    public const int DRUM_TIME = 1;            // Column B  
    public const int DRUM_PERIOD = 2;          // Column C
    public const int DRUM_TEAM_NAME = 3;       // Column D
    public const int DRUM_NAME = 4;            // Column E (Shot type)
    public const int DRUM_OUTCOME = 5;         // Column F
    public const int DRUM_PLAYER = 6;          // Column G
    public const int DRUM_LOCATION = 7;        // Column H
    public const int DRUM_COMPETITION = 8;     // Column I
    public const int DRUM_TEAMS = 9;           // Column J
    
    // Opposition team columns (11-21) 
    public const int OPP_EVENT = 10;           // Column K
    public const int OPP_TIME = 11;            // Column L
    public const int OPP_PERIOD = 12;          // Column M
    public const int OPP_TEAM_NAME = 13;       // Column N
    public const int OPP_NAME = 14;            // Column O (Shot type)
    public const int OPP_OUTCOME = 15;         // Column P
    public const int OPP_PLAYER = 16;          // Column Q
    public const int OPP_LOCATION = 17;        // Column R
    public const int OPP_COMPETITION = 18;     // Column S
    public const int OPP_TEAMS = 19;           // Column T
}
```

### Phase 2: Data Extraction and Transformation

#### 2.1 Individual Shot Event Processing
```csharp
private ShotEvent? ProcessShotRow(ExcelWorksheet worksheet, int row, bool isOppositionColumn)
{
    try
    {
        var baseColumn = isOppositionColumn ? 10 : 0; // Opposition data starts at column 11 (index 10)
        
        // Extract basic event data
        var eventNum = worksheet.Cells[row, baseColumn + 1].Text?.Trim();
        var timeText = worksheet.Cells[row, baseColumn + 2].Text?.Trim();
        var periodText = worksheet.Cells[row, baseColumn + 3].Text?.Trim();
        var teamName = worksheet.Cells[row, baseColumn + 4].Text?.Trim();
        var shotType = worksheet.Cells[row, baseColumn + 5].Text?.Trim();
        var outcome = worksheet.Cells[row, baseColumn + 6].Text?.Trim();
        var player = worksheet.Cells[row, baseColumn + 7].Text?.Trim();
        var location = worksheet.Cells[row, baseColumn + 8].Text?.Trim();
        var competition = worksheet.Cells[row, baseColumn + 9].Text?.Trim();
        var teams = worksheet.Cells[row, baseColumn + 10].Text?.Trim();
        
        // Skip empty rows
        if (string.IsNullOrEmpty(eventNum) && string.IsNullOrEmpty(timeText) && string.IsNullOrEmpty(outcome))
            return null;
        
        return new ShotEvent
        {
            EventNumber = ParseEventNumber(eventNum),
            Time = ParseTime(timeText),
            Period = ParsePeriod(periodText),
            TeamName = teamName ?? (isOppositionColumn ? "Opposition" : "Drum"),
            ShotType = shotType ?? "Shot from play",
            Outcome = outcome ?? "Unknown",
            Player = player,
            Location = ParseLocation(location),
            Competition = competition,
            Teams = teams,
            IsOpposition = isOppositionColumn
        };
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Failed to process shot row {Row}, column set {ColumnSet}", 
            row, isOppositionColumn ? "Opposition" : "Drum");
        return null;
    }
}
```

#### 2.2 Data Type Conversions
```csharp
private int? ParseEventNumber(string eventText)
{
    if (string.IsNullOrEmpty(eventText)) return null;
    return int.TryParse(eventText, out var eventNum) ? eventNum : null;
}

private string? ParseTimePeriod(string timeText, int period)
{
    if (string.IsNullOrEmpty(timeText)) return null;
    
    // Convert time to period format: "1H:05:30" or "2H:08:15"
    var time = ParseTime(timeText);
    if (!time.HasValue) return null;
    
    var periodPrefix = period switch
    {
        1 => "1H",
        2 => "2H", 
        _ => "UH" // Unknown half
    };
    
    return $"{periodPrefix}:{time.Value.Minutes:D2}:{time.Value.Seconds:D2}";
}

private TimeSpan? ParseTime(string timeText)
{
    if (string.IsNullOrEmpty(timeText)) return null;
    
    // Handle various time formats: "02:15", "0:02:15", "2.15"
    var cleanTime = timeText.Replace(".", ":");
    
    if (TimeSpan.TryParse(cleanTime, out var time))
        return time;
    
    // Try parsing as minutes:seconds only
    if (TimeSpan.TryParse($"0:{cleanTime}", out var timeMinSec))
        return timeMinSec;
    
    return null;
}

private int ParsePeriod(string periodText)
{
    if (string.IsNullOrEmpty(periodText)) return 0;
    
    return periodText.ToLowerInvariant() switch
    {
        "1" => 1,
        "2" => 2,
        "first half" => 1,
        "second half" => 2,
        "1st half" => 1,
        "2nd half" => 2,
        _ => int.TryParse(periodText, out var period) ? period : 0
    };
}

private int? ParseLocation(string locationText)
{
    if (string.IsNullOrEmpty(locationText)) return null;
    return int.TryParse(locationText, out var location) ? location : null;
}
```

### Phase 3: Position Area Calculation

#### 3.1 Field Position Mapping
```csharp
private async Task<int> GetPositionAreaIdAsync(int? location)
{
    if (!location.HasValue) return await GetDefaultPositionAreaAsync();
    
    // Map field locations to position areas based on GAA field layout
    var areaId = location.Value switch
    {
        >= 1 and <= 13 => await GetPositionAreaByNameAsync("Defensive Third"),
        >= 14 and <= 21 => await GetPositionAreaByNameAsync("Middle Third"),
        >= 22 and <= 30 => await GetPositionAreaByNameAsync("Attacking Third"),
        _ => await GetDefaultPositionAreaAsync()
    };
    
    return areaId;
}

private async Task<int> GetPositionAreaByNameAsync(string areaName)
{
    var area = await _context.PositionAreas
        .FirstOrDefaultAsync(pa => pa.AreaName.Equals(areaName, StringComparison.OrdinalIgnoreCase));
    
    if (area != null)
        return area.PositionAreaId;
    
    // Create if doesn't exist
    var newArea = new PositionArea
    {
        AreaName = areaName,
        Description = $"Field position area: {areaName}",
        IsActive = true
    };
    
    await _context.PositionAreas.AddAsync(newArea);
    await _context.SaveChangesAsync();
    
    return newArea.PositionAreaId;
}

private async Task<int> GetDefaultPositionAreaAsync()
{
    return await GetPositionAreaByNameAsync("Middle Third");
}
```

### Phase 4: Player Resolution and Team Type Management

#### 4.1 Player Identification
```csharp
private async Task<int?> ResolvePlayerIdAsync(string playerName, bool isOpposition)
{
    if (string.IsNullOrEmpty(playerName)) return null;
    
    // Clean player name
    var cleanName = CleanPlayerName(playerName);
    
    // For opposition players, we may not have them in our system
    if (isOpposition)
    {
        return await GetOrCreateOppositionPlayerAsync(cleanName);
    }
    
    // For Drum players, try to find existing player
    var player = await _context.Players
        .FirstOrDefaultAsync(p => 
            p.PlayerName.ToLower() == cleanName.ToLower() ||
            EF.Functions.TrigramsSimilarity(p.PlayerName, cleanName) > 0.7);
    
    if (player != null)
        return player.PlayerId;
    
    // Create new Drum player if not found
    var newPlayer = new Player
    {
        PlayerName = cleanName,
        IsActive = true,
        // Position will be determined later from other data
    };
    
    await _context.Players.AddAsync(newPlayer);
    await _context.SaveChangesAsync();
    
    return newPlayer.PlayerId;
}

private string CleanPlayerName(string playerName)
{
    if (string.IsNullOrEmpty(playerName)) return string.Empty;
    
    return playerName
        .Trim()
        .Replace("  ", " ") // Remove double spaces
        .ToTitleCase(); // Proper case
}

private async Task<int?> GetOrCreateOppositionPlayerAsync(string playerName)
{
    // For opposition players, create minimal records for tracking purposes
    var player = await _context.Players
        .FirstOrDefaultAsync(p => 
            p.PlayerName == playerName && 
            p.PlayerName.StartsWith("[OPP]"));
    
    if (player != null)
        return player.PlayerId;
    
    var oppPlayer = new Player
    {
        PlayerName = $"[OPP] {playerName}",
        IsActive = true,
        // Mark as opposition player
    };
    
    await _context.Players.AddAsync(oppPlayer);
    await _context.SaveChangesAsync();
    
    return oppPlayer.PlayerId;
}
```

#### 4.2 Reference Data Management
```csharp
private async Task<int> GetShotTypeIdAsync(string shotType)
{
    var type = await _context.ShotTypes
        .FirstOrDefaultAsync(st => st.TypeName.Equals(shotType, StringComparison.OrdinalIgnoreCase));
    
    if (type != null)
        return type.ShotTypeId;
    
    // Create if doesn't exist
    var newType = new ShotType
    {
        TypeName = shotType,
        Description = $"Shot type: {shotType}",
        IsActive = true
    };
    
    await _context.ShotTypes.AddAsync(newType);
    await _context.SaveChangesAsync();
    
    return newType.ShotTypeId;
}

private async Task<int> GetShotOutcomeIdAsync(string outcome)
{
    var outcomeRecord = await _context.ShotOutcomes
        .FirstOrDefaultAsync(so => so.OutcomeName.Equals(outcome, StringComparison.OrdinalIgnoreCase));
    
    if (outcomeRecord != null)
        return outcomeRecord.ShotOutcomeId;
    
    // Create if doesn't exist
    var newOutcome = new ShotOutcome
    {
        OutcomeName = outcome,
        IsSuccessful = IsSuccessfulShotOutcome(outcome),
        PointValue = GetPointValue(outcome),
        Description = $"Shot outcome: {outcome}",
        IsActive = true
    };
    
    await _context.ShotOutcomes.AddAsync(newOutcome);
    await _context.SaveChangesAsync();
    
    return newOutcome.ShotOutcomeId;
}

private bool IsSuccessfulShotOutcome(string outcome)
{
    return outcome.ToLowerInvariant() switch
    {
        "goal" => true,
        "point" => true,
        "2 pointer" => true,
        _ => false
    };
}

private int GetPointValue(string outcome)
{
    return outcome.ToLowerInvariant() switch
    {
        "goal" => 3,
        "point" => 1,
        "2 pointer" => 2,
        _ => 0
    };
}

private async Task<int> GetTeamTypeIdAsync(string teamType)
{
    var type = await _context.TeamTypes
        .FirstOrDefaultAsync(tt => tt.TypeName.Equals(teamType, StringComparison.OrdinalIgnoreCase));
    
    if (type != null)
        return type.TeamTypeId;
    
    // Create if doesn't exist
    var newType = new TeamType
    {
        TypeName = teamType,
        Description = $"Team type: {teamType}",
        IsActive = true
    };
    
    await _context.TeamTypes.AddAsync(newType);
    await _context.SaveChangesAsync();
    
    return newType.TeamTypeId;
}
```

### Phase 5: Main Processing Pipeline

#### 5.1 Complete Sheet Processing
```csharp
public async Task<ServiceResult<ShotAnalysisResult>> ProcessShotAnalysisSheetAsync(
    ExcelWorksheet worksheet, 
    int jobId)
{
    var stopwatch = Stopwatch.StartNew();
    var allShotEvents = new List<ShotEvent>();
    var processedRows = 0;
    var skippedRows = 0;
    
    try
    {
        var lastRow = worksheet.Dimension?.End.Row ?? 0;
        _logger.LogInformation("Processing shot analysis sheet with {RowCount} rows", lastRow);
        
        // Process each row, starting from row 2 (skip header)
        for (int row = 2; row <= lastRow; row++)
        {
            // Process Drum team data (columns 1-10)
            var drumShot = ProcessShotRow(worksheet, row, false);
            if (drumShot != null)
            {
                allShotEvents.Add(drumShot);
                processedRows++;
            }
            
            // Process Opposition team data (columns 11-21) 
            var oppShot = ProcessShotRow(worksheet, row, true);
            if (oppShot != null)
            {
                allShotEvents.Add(oppShot);
                processedRows++;
            }
            
            if (drumShot == null && oppShot == null)
                skippedRows++;
        }
        
        _logger.LogInformation(
            "Extracted {ShotCount} shot events from {ProcessedRows} processed rows, {SkippedRows} skipped rows", 
            allShotEvents.Count, processedRows, skippedRows);
        
        // Convert events to database records
        var shotRecords = new List<ShotAnalysis>();
        
        foreach (var shotEvent in allShotEvents)
        {
            var matchId = await ResolveMatchIdFromTeamsAsync(shotEvent.Teams);
            if (!matchId.HasValue)
            {
                _logger.LogWarning("Could not resolve match ID for shot event with teams: '{Teams}'", 
                    shotEvent.Teams);
                continue;
            }
            
            var playerId = await ResolvePlayerIdAsync(shotEvent.Player, shotEvent.IsOpposition);
            var shotTypeId = await GetShotTypeIdAsync(shotEvent.ShotType);
            var shotOutcomeId = await GetShotOutcomeIdAsync(shotEvent.Outcome);
            var positionAreaId = await GetPositionAreaIdAsync(shotEvent.Location);
            var teamTypeId = await GetTeamTypeIdAsync(shotEvent.IsOpposition ? "Opposition" : "Drum");
            
            var record = new ShotAnalysis
            {
                MatchId = matchId.Value,
                PlayerId = playerId,
                ShotNumber = shotEvent.EventNumber ?? 0,
                ShotTypeId = shotTypeId,
                ShotOutcomeId = shotOutcomeId,
                TimePeriod = ParseTimePeriod(shotEvent.Time?.ToString(), shotEvent.Period),
                PositionAreaId = positionAreaId,
                TeamTypeId = teamTypeId
            };
            
            shotRecords.Add(record);
        }
        
        // Save to database in batches
        const int batchSize = 50;
        var savedRecords = 0;
        
        for (int i = 0; i < shotRecords.Count; i += batchSize)
        {
            var batch = shotRecords.Skip(i).Take(batchSize).ToList();
            await _context.ShotAnalysis.AddRangeAsync(batch);
            await _context.SaveChangesAsync();
            
            savedRecords += batch.Count;
            _logger.LogDebug("Saved shot analysis batch {BatchNumber}: {RecordCount} records", 
                i / batchSize + 1, batch.Count);
        }
        
        stopwatch.Stop();
        
        _logger.LogInformation(
            "Shot analysis processing completed: {SavedRecords} records saved from {ExtractedShots} shot events in {Duration}ms", 
            savedRecords, allShotEvents.Count, stopwatch.ElapsedMilliseconds);
        
        return ServiceResult<ShotAnalysisResult>.Success(new ShotAnalysisResult
        {
            ShotEventsExtracted = allShotEvents.Count,
            RecordsCreated = savedRecords,
            ProcessedRows = processedRows,
            SkippedRows = skippedRows,
            ProcessingTimeMs = stopwatch.ElapsedMilliseconds
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to process shot analysis sheet");
        
        await _progressService.RecordValidationErrorAsync(
            jobId, "Shots from play Data", null, null,
            EtlErrorTypes.PROCESSING_ERROR,
            $"Failed to process shot analysis: {ex.Message}",
            "Check sheet format and data structure");
        
        return ServiceResult<ShotAnalysisResult>.Failed($"Failed to process shot analysis: {ex.Message}");
    }
}
```

### Phase 6: Data Validation Rules

#### 6.1 Shot Event Validation
```csharp
private async Task ValidateShotEvents(
    List<ShotEvent> shotEvents, 
    int jobId)
{
    foreach (var shotEvent in shotEvents.Where(e => e != null))
    {
        // Validate required fields
        if (string.IsNullOrEmpty(shotEvent.Outcome))
        {
            await _progressService.RecordValidationErrorAsync(
                jobId, "Shots from play Data", null, "Outcome",
                EtlErrorTypes.MISSING_DATA,
                $"Missing outcome for shot event {shotEvent.EventNumber}",
                "Ensure all shot events have outcome data");
        }
        
        // Validate player name for Drum team shots
        if (!shotEvent.IsOpposition && string.IsNullOrEmpty(shotEvent.Player))
        {
            await _progressService.RecordValidationErrorAsync(
                jobId, "Shots from play Data", null, "Player",
                EtlErrorTypes.MISSING_DATA,
                $"Missing player name for Drum shot event {shotEvent.EventNumber}",
                "Ensure all Drum shots have player attribution");
        }
        
        // Validate time format
        if (shotEvent.Time.HasValue && shotEvent.Time.Value.TotalHours > 2)
        {
            await _progressService.RecordValidationErrorAsync(
                jobId, "Shots from play Data", null, "Time",
                EtlErrorTypes.INVALID_TIME,
                $"Invalid time format for shot event {shotEvent.EventNumber}: {shotEvent.Time}",
                "Check time format - should be MM:SS or HH:MM:SS within game duration");
        }
        
        // Validate location range
        if (shotEvent.Location.HasValue && (shotEvent.Location < 1 || shotEvent.Location > 30))
        {
            await _progressService.RecordValidationErrorAsync(
                jobId, "Shots from play Data", null, "Location",
                EtlErrorTypes.INVALID_RANGE,
                $"Invalid location {shotEvent.Location} for shot event {shotEvent.EventNumber}",
                "Location should be between 1-30 representing field positions");
        }
    }
}
```

#### 6.2 Statistical Consistency Checks
```csharp
private async Task ValidateShotStatistics(List<ShotAnalysis> records, int jobId)
{
    // Group by match and team for validation
    var shotsByMatch = records.GroupBy(r => new { r.MatchId, r.TeamTypeId }).ToList();
    
    foreach (var matchGroup in shotsByMatch)
    {
        var shots = matchGroup.ToList();
        var teamType = await _context.TeamTypes
            .FirstOrDefaultAsync(tt => tt.TeamTypeId == matchGroup.Key.TeamTypeId);
        
        // Validate shot distribution
        var totalShots = shots.Count;
        var successfulShots = shots.Count(s => 
            _context.ShotOutcomes.Any(so => so.ShotOutcomeId == s.ShotOutcomeId && so.IsSuccessful));
        
        if (totalShots > 50) // Unusually high number of shots
        {
            await _progressService.RecordValidationErrorAsync(
                jobId, "Shots from play Data", null, "Shot Count",
                "warning",
                $"High shot count ({totalShots}) for match {matchGroup.Key.MatchId}, team {teamType?.TypeName}",
                "Verify shot data accuracy - high shot counts may indicate data duplication");
        }
        
        var successRate = totalShots > 0 ? (double)successfulShots / totalShots : 0;
        if (successRate > 0.8) // Unusually high success rate
        {
            await _progressService.RecordValidationErrorAsync(
                jobId, "Shots from play Data", null, "Success Rate",
                "warning",
                $"High success rate ({successRate:P1}) for match {matchGroup.Key.MatchId}, team {teamType?.TypeName}",
                "Verify outcome classification - unusually high success rates may indicate data issues");
        }
    }
}
```

## Performance Considerations

### 7.1 Processing Metrics
- **Expected Shots**: ~20-40 per team per match
- **Total Events**: ~320-640 across all matches
- **Database Records**: 1:1 mapping with shot events
- **Processing Time Target**: < 2 minutes

### 7.2 Memory Management
```csharp
// Process shots in batches to manage memory usage
private async Task<List<ShotAnalysis>> ConvertShotEventsBatch(
    List<ShotEvent> shotEventBatch, 
    int batchNumber)
{
    _logger.LogDebug("Converting shot events batch {BatchNumber} with {EventCount} events", 
        batchNumber, shotEventBatch.Count);
    
    var records = new List<ShotAnalysis>();
    
    foreach (var shotEvent in shotEventBatch)
    {
        // Conversion logic here
        var record = await ConvertShotEventToRecord(shotEvent);
        if (record != null)
            records.Add(record);
    }
    
    // Allow other tasks to run
    await Task.Yield();
    
    return records;
}
```

## Error Handling Strategies

### 8.1 Match Association Failures
```csharp
private async Task HandleMatchAssociationFailure(
    ShotEvent shotEvent, 
    int jobId)
{
    _logger.LogWarning("Could not associate shot event {EventNumber} with match: '{Teams}'", 
        shotEvent.EventNumber, shotEvent.Teams);
    
    await _progressService.RecordValidationErrorAsync(
        jobId, "Shots from play Data", null, "Match Association",
        "warning",
        $"Could not resolve match for shot event {shotEvent.EventNumber} with teams '{shotEvent.Teams}'",
        "Verify team names match existing match data");
}
```

### 8.2 Data Quality Reporting
```csharp
private async Task LogShotAnalysisQualityMetrics(ShotAnalysisResult result, int jobId)
{
    var qualityMetrics = new
    {
        ConversionRate = result.ShotEventsExtracted > 0 ? 
            (double)result.RecordsCreated / result.ShotEventsExtracted : 0,
        ProcessingEfficiency = result.ProcessedRows > 0 ? 
            (double)result.ShotEventsExtracted / result.ProcessedRows : 0,
        ErrorRate = result.SkippedRows > 0 ? 
            (double)result.SkippedRows / (result.ProcessedRows + result.SkippedRows) : 0
    };
    
    _logger.LogInformation(
        "Shot analysis quality metrics for job {JobId}: " +
        "Conversion Rate: {ConversionRate:P2}, Processing Efficiency: {ProcessingEfficiency:P2}, " +
        "Error Rate: {ErrorRate:P2}",
        jobId, qualityMetrics.ConversionRate, qualityMetrics.ProcessingEfficiency, 
        qualityMetrics.ErrorRate);
    
    // Record metrics for monitoring
    await _progressService.RecordProgressUpdateAsync(new EtlProgressUpdate
    {
        JobId = jobId,
        Stage = "shot_analysis_quality",
        Message = $"Shot analysis quality: {qualityMetrics.ConversionRate:P1} conversion rate, " +
                 $"{qualityMetrics.ErrorRate:P1} error rate",
        Status = "completed"
    });
}
```

## Integration Points

### 9.1 Service Layer Integration
```csharp
// Add to IExcelProcessingService interface
Task<ServiceResult<ShotAnalysisResult>> ProcessShotAnalysisAsync(
    Stream fileStream,
    int jobId,
    CancellationToken cancellationToken = default);
```

### 9.2 Progress Tracking
```csharp
// Update main processing pipeline
await _progressService.RecordProgressUpdateAsync(new EtlProgressUpdate
{
    JobId = jobId,
    Stage = EtlStages.PROCESSING_SHOT_ANALYSIS,
    TotalSteps = 1,
    CompletedSteps = 0,
    Status = "processing",
    Message = "Processing shot analysis data"
});
```

## Success Criteria

- ✅ All shot events successfully extracted from dual-column format
- ✅ Player identification and resolution working for both teams
- ✅ Shot outcomes properly classified and point values assigned
- ✅ Position areas accurately calculated from field locations
- ✅ Time periods properly formatted and stored
- ✅ Match associations correctly resolved
- ✅ Data validation and quality checks implemented
- ✅ Processing completed within performance targets
- ✅ Comprehensive error handling and reporting
- ✅ Statistical consistency validation in place

This plan ensures that detailed shot analysis data is accurately captured from the Excel sheet and stored as individual shot records, providing granular data for advanced GAA match analysis and player performance evaluation.