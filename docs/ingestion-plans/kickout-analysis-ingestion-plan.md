# Kickout Analysis Ingestion Plan

## Executive Summary

This document outlines the detailed plan for ingesting kickout analysis data from the Excel file's specialized "Kickout Analysis Data" sheet into the `kickout_analysis` database table. This sheet contains event-level tracking of kickout performance with detailed outcomes for both Drum and opposition teams.

## Data Source Analysis

### Excel Sheet Structure
- **Source Sheet**: `Kickout Analysis Data`
- **Data Structure**: 139 rows × 21 columns (dual-column format)
- **Format**: Side-by-side data for Drum and Opposition
- **Event-Level Data**: Individual kickout attempts with timestamps and outcomes

### Column Structure Analysis
```
Columns 1-10: Drum Team Kickouts
- Event, Time, Period, Team Name, Name, Outcome, Player, Location, Competition, Teams

Columns 11-21: Opposition Team Kickouts  
- Event.1, Time.1, Period.1, Team Name.1, Name.1, Outcome.1, Player.1, Location.1, Competition.1, Teams.1, Unnamed: 10
```

### Sample Data Structure
```
Event | Time  | Period | Team Name | Name    | Outcome    | Player      | Location | Competition | Teams
------|-------|--------|-----------|---------|------------|-------------|----------|-------------|-------------
1     | 11:23 | 1      | Drum      | Kickout | Won Clean  | O Doherty   | 1        | League      | Drum vs Glack
2     | 13:45 | 1      | Drum      | Kickout | Break Won  | O Doherty   | 1        | League      | Drum vs Glack
3     | 16:12 | 1      | Drum      | Kickout | Break Lost | O Doherty   | 1        | League      | Drum vs Glack
```

## Database Target Schema

### kickout_analysis Table
```sql
CREATE TABLE kickout_analysis (
    kickout_analysis_id SERIAL PRIMARY KEY,
    match_id INTEGER NOT NULL REFERENCES matches(match_id),
    time_period_id INTEGER REFERENCES time_periods(time_period_id),
    kickout_type_id INTEGER REFERENCES kickout_types(kickout_type_id),
    team_type_id INTEGER REFERENCES team_types(team_type_id),
    total_attempts INTEGER,
    successful INTEGER,
    success_rate DECIMAL(5,4),
    outcome_breakdown JSONB,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

## Detailed Ingestion Mapping

### Phase 1: Sheet Detection and Structure Validation

#### 1.1 Sheet Identification
```csharp
private bool IsKickoutAnalysisSheet(string sheetName)
{
    return sheetName.Equals("Kickout Analysis Data", StringComparison.OrdinalIgnoreCase) ||
           sheetName.Contains("Kickout Analysis", StringComparison.OrdinalIgnoreCase);
}
```

#### 1.2 Column Mapping Configuration
```csharp
private static class KickoutColumnMapping
{
    // Drum team columns (1-10)
    public const int DRUM_EVENT = 0;           // Column A
    public const int DRUM_TIME = 1;            // Column B  
    public const int DRUM_PERIOD = 2;          // Column C
    public const int DRUM_TEAM_NAME = 3;       // Column D
    public const int DRUM_NAME = 4;            // Column E
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
    public const int OPP_NAME = 14;            // Column O
    public const int OPP_OUTCOME = 15;         // Column P
    public const int OPP_PLAYER = 16;          // Column Q
    public const int OPP_LOCATION = 17;        // Column R
    public const int OPP_COMPETITION = 18;     // Column S
    public const int OPP_TEAMS = 19;           // Column T
}
```

### Phase 2: Data Extraction and Transformation

#### 2.1 Individual Kickout Event Processing
```csharp
private KickoutEvent? ProcessKickoutRow(ExcelWorksheet worksheet, int row, bool isOppositionColumn)
{
    try
    {
        var baseColumn = isOppositionColumn ? 10 : 0; // Opposition data starts at column 11 (index 10)
        
        // Extract basic event data
        var eventNum = worksheet.Cells[row, baseColumn + 1].Text?.Trim();
        var timeText = worksheet.Cells[row, baseColumn + 2].Text?.Trim();
        var periodText = worksheet.Cells[row, baseColumn + 3].Text?.Trim();
        var teamName = worksheet.Cells[row, baseColumn + 4].Text?.Trim();
        var kickoutType = worksheet.Cells[row, baseColumn + 5].Text?.Trim();
        var outcome = worksheet.Cells[row, baseColumn + 6].Text?.Trim();
        var player = worksheet.Cells[row, baseColumn + 7].Text?.Trim();
        var location = worksheet.Cells[row, baseColumn + 8].Text?.Trim();
        var competition = worksheet.Cells[row, baseColumn + 9].Text?.Trim();
        var teams = worksheet.Cells[row, baseColumn + 10].Text?.Trim();
        
        // Skip empty rows
        if (string.IsNullOrEmpty(eventNum) && string.IsNullOrEmpty(timeText) && string.IsNullOrEmpty(outcome))
            return null;
        
        return new KickoutEvent
        {
            EventNumber = ParseEventNumber(eventNum),
            Time = ParseTime(timeText),
            Period = ParsePeriod(periodText),
            TeamName = teamName ?? (isOppositionColumn ? "Opposition" : "Drum"),
            KickoutType = kickoutType ?? "Kickout",
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
        _logger.LogWarning(ex, "Failed to process kickout row {Row}, column set {ColumnSet}", 
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

private TimeSpan? ParseTime(string timeText)
{
    if (string.IsNullOrEmpty(timeText)) return null;
    
    // Handle various time formats: "11:23", "0:11:23", "11.23"
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

### Phase 3: Match Association and Aggregation

#### 3.1 Match ID Resolution
```csharp
private async Task<int?> ResolveMatchIdFromTeamsAsync(string teamsText)
{
    if (string.IsNullOrEmpty(teamsText)) return null;
    
    // Parse team names from "Drum vs Opposition" format
    var (homeTeam, awayTeam) = _transformationService.ExtractMatchTeams(teamsText);
    
    if (string.IsNullOrEmpty(homeTeam) || string.IsNullOrEmpty(awayTeam))
        return null;
    
    // Find match by team names
    var match = await _context.Matches
        .Include(m => m.Opposition)
        .FirstOrDefaultAsync(m => 
            (homeTeam.Contains("Drum", StringComparison.OrdinalIgnoreCase) && 
             m.Opposition.TeamName.Equals(awayTeam, StringComparison.OrdinalIgnoreCase)) ||
            (awayTeam.Contains("Drum", StringComparison.OrdinalIgnoreCase) && 
             m.Opposition.TeamName.Equals(homeTeam, StringComparison.OrdinalIgnoreCase)));
    
    return match?.MatchId;
}
```

#### 3.2 Kickout Data Aggregation
```csharp
private async Task<List<KickoutAnalysisRecord>> AggregateKickoutDataAsync(
    List<KickoutEvent> events, 
    int matchId)
{
    var aggregatedData = new List<KickoutAnalysisRecord>();
    
    // Group by match, team type, and period
    var groupedEvents = events
        .Where(e => e != null)
        .GroupBy(e => new { 
            MatchId = matchId,
            TeamType = e.IsOpposition ? "Opposition" : "Drum",
            Period = e.Period
        })
        .ToList();
    
    foreach (var group in groupedEvents)
    {
        var teamEvents = group.ToList();
        var totalAttempts = teamEvents.Count;
        
        // Count successful kickouts
        var successful = teamEvents.Count(e => IsSuccessfulOutcome(e.Outcome));
        var successRate = totalAttempts > 0 ? (decimal)successful / totalAttempts : 0;
        
        // Create outcome breakdown
        var outcomeBreakdown = teamEvents
            .GroupBy(e => e.Outcome)
            .ToDictionary(g => g.Key, g => g.Count());
        
        var record = new KickoutAnalysisRecord
        {
            MatchId = group.Key.MatchId,
            TimePeriodId = await GetTimePeriodIdAsync(group.Key.Period),
            KickoutTypeId = await GetKickoutTypeIdAsync("Long"), // Default to Long for now
            TeamTypeId = await GetTeamTypeIdAsync(group.Key.TeamType),
            TotalAttempts = totalAttempts,
            Successful = successful,
            SuccessRate = Math.Round(successRate, 4),
            OutcomeBreakdown = JsonSerializer.Serialize(outcomeBreakdown)
        };
        
        aggregatedData.Add(record);
    }
    
    return aggregatedData;
}

private bool IsSuccessfulOutcome(string outcome)
{
    if (string.IsNullOrEmpty(outcome)) return false;
    
    return outcome.ToLowerInvariant() switch
    {
        "won clean" => true,
        "break won" => true,
        "short won" => true,
        _ => false
    };
}
```

### Phase 4: Database Reference Resolution

#### 4.1 Lookup Table Management
```csharp
private async Task<int> GetTimePeriodIdAsync(int period)
{
    var timePeriod = await _context.TimePeriods
        .FirstOrDefaultAsync(tp => tp.PeriodNumber == period);
    
    if (timePeriod != null)
        return timePeriod.TimePeriodId;
    
    // Create if doesn't exist
    var newPeriod = new TimePeriod
    {
        PeriodName = period switch
        {
            1 => "First Half",
            2 => "Second Half",
            _ => $"Period {period}"
        },
        PeriodNumber = period,
        IsActive = true
    };
    
    await _context.TimePeriods.AddAsync(newPeriod);
    await _context.SaveChangesAsync();
    
    return newPeriod.TimePeriodId;
}

private async Task<int> GetKickoutTypeIdAsync(string kickoutType)
{
    var type = await _context.KickoutTypes
        .FirstOrDefaultAsync(kt => kt.TypeName.Equals(kickoutType, StringComparison.OrdinalIgnoreCase));
    
    if (type != null)
        return type.KickoutTypeId;
    
    // Create if doesn't exist
    var newType = new KickoutType
    {
        TypeName = kickoutType,
        Description = $"Kickout type: {kickoutType}",
        IsActive = true
    };
    
    await _context.KickoutTypes.AddAsync(newType);
    await _context.SaveChangesAsync();
    
    return newType.KickoutTypeId;
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
public async Task<ServiceResult<KickoutAnalysisResult>> ProcessKickoutAnalysisSheetAsync(
    ExcelWorksheet worksheet, 
    int jobId)
{
    var stopwatch = Stopwatch.StartNew();
    var allEvents = new List<KickoutEvent>();
    var processedRows = 0;
    var skippedRows = 0;
    
    try
    {
        var lastRow = worksheet.Dimension?.End.Row ?? 0;
        _logger.LogInformation("Processing kickout analysis sheet with {RowCount} rows", lastRow);
        
        // Process each row, starting from row 2 (skip header)
        for (int row = 2; row <= lastRow; row++)
        {
            // Process Drum team data (columns 1-10)
            var drumEvent = ProcessKickoutRow(worksheet, row, false);
            if (drumEvent != null)
            {
                allEvents.Add(drumEvent);
                processedRows++;
            }
            
            // Process Opposition team data (columns 11-21) 
            var oppEvent = ProcessKickoutRow(worksheet, row, true);
            if (oppEvent != null)
            {
                allEvents.Add(oppEvent);
                processedRows++;
            }
            
            if (drumEvent == null && oppEvent == null)
                skippedRows++;
        }
        
        _logger.LogInformation(
            "Extracted {EventCount} kickout events from {ProcessedRows} processed rows, {SkippedRows} skipped rows", 
            allEvents.Count, processedRows, skippedRows);
        
        // Group events by match
        var eventsByMatch = await GroupEventsByMatchAsync(allEvents);
        var totalRecordsCreated = 0;
        
        foreach (var matchGroup in eventsByMatch)
        {
            var matchId = matchGroup.Key;
            var events = matchGroup.Value;
            
            // Aggregate events into database records
            var analysisRecords = await AggregateKickoutDataAsync(events, matchId);
            
            // Save to database
            if (analysisRecords.Any())
            {
                var dbRecords = analysisRecords.Select(r => new KickoutAnalysis
                {
                    MatchId = r.MatchId,
                    TimePeriodId = r.TimePeriodId,
                    KickoutTypeId = r.KickoutTypeId,
                    TeamTypeId = r.TeamTypeId,
                    TotalAttempts = r.TotalAttempts,
                    Successful = r.Successful,
                    SuccessRate = r.SuccessRate,
                    OutcomeBreakdown = r.OutcomeBreakdown
                }).ToList();
                
                await _context.KickoutAnalysis.AddRangeAsync(dbRecords);
                await _context.SaveChangesAsync();
                
                totalRecordsCreated += dbRecords.Count;
                
                _logger.LogInformation(
                    "Saved {RecordCount} kickout analysis records for match {MatchId}", 
                    dbRecords.Count, matchId);
            }
        }
        
        stopwatch.Stop();
        
        return ServiceResult<KickoutAnalysisResult>.Success(new KickoutAnalysisResult
        {
            EventsExtracted = allEvents.Count,
            RecordsCreated = totalRecordsCreated,
            ProcessedRows = processedRows,
            SkippedRows = skippedRows,
            ProcessingTimeMs = stopwatch.ElapsedMilliseconds
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to process kickout analysis sheet");
        
        await _progressService.RecordValidationErrorAsync(
            jobId, "Kickout Analysis Data", null, null,
            EtlErrorTypes.PROCESSING_ERROR,
            $"Failed to process kickout analysis: {ex.Message}",
            "Check sheet format and data structure");
        
        return ServiceResult<KickoutAnalysisResult>.Failed($"Failed to process kickout analysis: {ex.Message}");
    }
}
```

#### 5.2 Match Grouping Logic
```csharp
private async Task<Dictionary<int, List<KickoutEvent>>> GroupEventsByMatchAsync(List<KickoutEvent> events)
{
    var eventsByMatch = new Dictionary<int, List<KickoutEvent>>();
    
    foreach (var kickoutEvent in events)
    {
        var matchId = await ResolveMatchIdFromTeamsAsync(kickoutEvent.Teams);
        
        if (matchId.HasValue)
        {
            if (!eventsByMatch.ContainsKey(matchId.Value))
                eventsByMatch[matchId.Value] = new List<KickoutEvent>();
            
            eventsByMatch[matchId.Value].Add(kickoutEvent);
        }
        else
        {
            _logger.LogWarning("Could not resolve match ID for kickout event with teams: '{Teams}'", 
                kickoutEvent.Teams);
        }
    }
    
    return eventsByMatch;
}
```

## Data Validation Rules

### 6.1 Event Data Validation
```csharp
private async Task ValidateKickoutEvents(
    List<KickoutEvent> events, 
    int jobId)
{
    foreach (var kickoutEvent in events.Where(e => e != null))
    {
        // Validate required fields
        if (string.IsNullOrEmpty(kickoutEvent.Outcome))
        {
            await _progressService.RecordValidationErrorAsync(
                jobId, "Kickout Analysis Data", null, "Outcome",
                EtlErrorTypes.MISSING_DATA,
                $"Missing outcome for kickout event {kickoutEvent.EventNumber}",
                "Ensure all kickout events have outcome data");
        }
        
        // Validate time format
        if (kickoutEvent.Time.HasValue && kickoutEvent.Time.Value.TotalHours > 2)
        {
            await _progressService.RecordValidationErrorAsync(
                jobId, "Kickout Analysis Data", null, "Time",
                EtlErrorTypes.INVALID_TIME,
                $"Invalid time format for event {kickoutEvent.EventNumber}: {kickoutEvent.Time}",
                "Check time format - should be MM:SS or HH:MM:SS");
        }
        
        // Validate period
        if (kickoutEvent.Period < 1 || kickoutEvent.Period > 2)
        {
            await _progressService.RecordValidationErrorAsync(
                jobId, "Kickout Analysis Data", null, "Period",
                EtlErrorTypes.INVALID_RANGE,
                $"Invalid period {kickoutEvent.Period} for event {kickoutEvent.EventNumber}",
                "Period should be 1 (First Half) or 2 (Second Half)");
        }
    }
}
```

## Performance Considerations

### 7.1 Processing Metrics
- **Expected Events**: ~100-200 per match
- **Total Events**: ~800-1600 across all matches
- **Aggregated Records**: ~16 per match (2 teams × 2 periods × 4 kickout types)
- **Processing Time Target**: < 2 minutes

### 7.2 Memory Management
```csharp
// Process events in batches to avoid memory issues
private async Task ProcessEventBatchAsync(List<KickoutEvent> eventBatch, int batchNumber)
{
    const int BATCH_SIZE = 50;
    
    _logger.LogDebug("Processing kickout event batch {BatchNumber} with {EventCount} events", 
        batchNumber, eventBatch.Count);
    
    // Process batch logic here
    
    // Allow other tasks to run
    await Task.Yield();
}
```

## Error Handling Strategies

### 8.1 Missing Data Handling
```csharp
private void HandleMissingKickoutData(string teams, string reason, int jobId)
{
    _logger.LogWarning("Missing kickout data for teams '{Teams}': {Reason}", teams, reason);
    
    // Record as warning, not error - allows processing to continue
    _ = Task.Run(async () => await _progressService.RecordValidationErrorAsync(
        jobId, "Kickout Analysis Data", null, null,
        "warning",
        $"Missing kickout data for {teams}: {reason}",
        "Some matches may not have kickout analysis data"));
}
```

### 8.2 Data Quality Monitoring
```csharp
private async Task LogDataQualityMetrics(KickoutAnalysisResult result, int jobId)
{
    var qualityMetrics = new
    {
        SuccessRate = result.EventsExtracted > 0 ? (double)result.RecordsCreated / result.EventsExtracted : 0,
        ProcessingEfficiency = result.ProcessedRows > 0 ? (double)result.EventsExtracted / result.ProcessedRows : 0,
        ErrorRate = result.SkippedRows > 0 ? (double)result.SkippedRows / (result.ProcessedRows + result.SkippedRows) : 0
    };
    
    _logger.LogInformation(
        "Kickout analysis quality metrics for job {JobId}: " +
        "Success Rate: {SuccessRate:P2}, Processing Efficiency: {ProcessingEfficiency:P2}, Error Rate: {ErrorRate:P2}",
        jobId, qualityMetrics.SuccessRate, qualityMetrics.ProcessingEfficiency, qualityMetrics.ErrorRate);
}
```

## Success Criteria

- ✅ All kickout events successfully extracted from dual-column format
- ✅ Event data properly aggregated by match, team, and period
- ✅ Outcome breakdowns accurately captured in JSON format
- ✅ All lookup tables properly populated and referenced
- ✅ Match associations correctly resolved
- ✅ Data quality validation and error reporting implemented
- ✅ Processing completed within performance targets
- ✅ Comprehensive logging and monitoring in place

This plan ensures that detailed kickout analysis data is accurately captured from the specialized Excel sheet and transformed into meaningful aggregated statistics for GAA match analysis.