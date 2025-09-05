# Scoreable Free Analysis Ingestion Plan

## Executive Summary

This document outlines the detailed plan for ingesting scoreable free analysis data from the Excel file's "Scoreable Frees Data" sheet into the `scoreable_free_analysis` database table. This sheet contains event-level tracking of free kick opportunities with detailed outcomes, distances, and player information for both Drum and opposition teams.

## Data Source Analysis

### Excel Sheet Structure
- **Source Sheet**: `Scoreable Frees Data`
- **Data Structure**: 48 rows × 21 columns (dual-column format)
- **Format**: Side-by-side data for Drum and Opposition
- **Event-Level Data**: Individual free kick attempts with distances and outcomes

### Column Structure Analysis
```
Columns 1-10: Drum Team Scoreable Frees
- Event, Time, Period, Team Name, Name, Outcome, Player, Location, Competition, Teams

Columns 11-21: Opposition Team Scoreable Frees  
- Event.1, Time.1, Period.1, Team Name.1, Name.1, Outcome.1, Player.1, Location.1, Competition.1, Teams.1, Unnamed: 10
```

### Sample Data Structure
```
Event | Time  | Period | Team Name | Name           | Outcome | Player      | Location | Competition | Teams
------|-------|--------|-----------|----------------|---------|-------------|----------|-------------|-------------
1     | 08:23 | 1      | Drum      | Scoreable free | Point   | C McCloskey | 18       | League      | Drum vs Glack
2     | 15:45 | 1      | Drum      | Scoreable free | Wide    | R Doherty   | 22       | League      | Drum vs Glack
3     | 23:12 | 2      | Drum      | Scoreable free | Goal    | C McCloskey | 16       | League      | Drum vs Glack
```

## Database Target Schema

### scoreable_free_analysis Table
```sql
CREATE TABLE scoreable_free_analysis (
    scoreable_free_analysis_id SERIAL PRIMARY KEY,
    match_id INTEGER NOT NULL REFERENCES matches(match_id),
    player_id INTEGER REFERENCES players(player_id),
    free_number INTEGER,
    free_type_id INTEGER REFERENCES free_types(free_type_id),
    distance VARCHAR(10),
    shot_outcome_id INTEGER REFERENCES shot_outcomes(shot_outcome_id),
    success BOOLEAN,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

## Detailed Ingestion Mapping

### Phase 1: Sheet Detection and Structure Validation

#### 1.1 Sheet Identification
```csharp
private bool IsScoreableFreeAnalysisSheet(string sheetName)
{
    return sheetName.Equals("Scoreable Frees Data", StringComparison.OrdinalIgnoreCase) ||
           sheetName.Contains("Scoreable Frees", StringComparison.OrdinalIgnoreCase) ||
           sheetName.Contains("Free Analysis", StringComparison.OrdinalIgnoreCase);
}
```

#### 1.2 Column Mapping Configuration
```csharp
private static class ScoreableFreeColumnMapping
{
    // Drum team columns (1-10)
    public const int DRUM_EVENT = 0;           // Column A
    public const int DRUM_TIME = 1;            // Column B  
    public const int DRUM_PERIOD = 2;          // Column C
    public const int DRUM_TEAM_NAME = 3;       // Column D
    public const int DRUM_NAME = 4;            // Column E (Free type)
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
    public const int OPP_NAME = 14;            // Column O (Free type)
    public const int OPP_OUTCOME = 15;         // Column P
    public const int OPP_PLAYER = 16;          // Column Q
    public const int OPP_LOCATION = 17;        // Column R
    public const int OPP_COMPETITION = 18;     // Column S
    public const int OPP_TEAMS = 19;           // Column T
}
```

### Phase 2: Data Extraction and Transformation

#### 2.1 Individual Free Kick Event Processing
```csharp
private ScoreableFreeEvent? ProcessScoreableFreeRow(ExcelWorksheet worksheet, int row, bool isOppositionColumn)
{
    try
    {
        var baseColumn = isOppositionColumn ? 10 : 0; // Opposition data starts at column 11 (index 10)
        
        // Extract basic event data
        var eventNum = worksheet.Cells[row, baseColumn + 1].Text?.Trim();
        var timeText = worksheet.Cells[row, baseColumn + 2].Text?.Trim();
        var periodText = worksheet.Cells[row, baseColumn + 3].Text?.Trim();
        var teamName = worksheet.Cells[row, baseColumn + 4].Text?.Trim();
        var freeType = worksheet.Cells[row, baseColumn + 5].Text?.Trim();
        var outcome = worksheet.Cells[row, baseColumn + 6].Text?.Trim();
        var player = worksheet.Cells[row, baseColumn + 7].Text?.Trim();
        var location = worksheet.Cells[row, baseColumn + 8].Text?.Trim();
        var competition = worksheet.Cells[row, baseColumn + 9].Text?.Trim();
        var teams = worksheet.Cells[row, baseColumn + 10].Text?.Trim();
        
        // Skip empty rows
        if (string.IsNullOrEmpty(eventNum) && string.IsNullOrEmpty(timeText) && string.IsNullOrEmpty(outcome))
            return null;
        
        return new ScoreableFreeEvent
        {
            EventNumber = ParseEventNumber(eventNum),
            Time = ParseTime(timeText),
            Period = ParsePeriod(periodText),
            TeamName = teamName ?? (isOppositionColumn ? "Opposition" : "Drum"),
            FreeType = freeType ?? "Scoreable free",
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
        _logger.LogWarning(ex, "Failed to process scoreable free row {Row}, column set {ColumnSet}", 
            row, isOppositionColumn ? "Opposition" : "Drum");
        return null;
    }
}
```

#### 2.2 Distance Calculation and Classification
```csharp
private string CalculateDistanceCategory(int? location)
{
    if (!location.HasValue) return "Unknown";
    
    // Map field locations to distance categories based on GAA field layout
    return location.Value switch
    {
        >= 1 and <= 12 => "Close Range",    // Very close to goal
        >= 13 and <= 21 => "13-21m",        // Medium distance
        >= 22 and <= 30 => "22-30m",        // Long range
        > 30 => "30m+",                      // Very long range
        _ => "Unknown"
    };
}

private double? CalculateApproximateDistance(int? location)
{
    if (!location.HasValue) return null;
    
    // Approximate distance in meters based on field position
    // GAA field is approximately 130-145m long, 80-90m wide
    return location.Value switch
    {
        >= 1 and <= 5 => 5.0,     // Very close
        >= 6 and <= 10 => 10.0,   // Close
        >= 11 and <= 15 => 15.0,  // Medium-close
        >= 16 and <= 20 => 20.0,  // Medium
        >= 21 and <= 25 => 25.0,  // Medium-far
        >= 26 and <= 30 => 30.0,  // Far
        > 30 => 35.0,             // Very far
        _ => null
    };
}
```

#### 2.3 Free Kick Success Determination
```csharp
private bool DetermineFreeKickSuccess(string outcome)
{
    if (string.IsNullOrEmpty(outcome)) return false;
    
    return outcome.ToLowerInvariant() switch
    {
        "goal" => true,
        "point" => true,
        "2 pointer" => true,
        "wide" => false,
        "short" => false,
        "save" => false,
        "45" => false,           // Results in 45m free - not a score
        "woodwork" => false,     // Hit post/crossbar
        "blocked" => false,
        _ => false
    };
}

private string ClassifyFreeKickType(string freeType, double? distance)
{
    if (string.IsNullOrEmpty(freeType)) return "Standard";
    
    // Handle different free kick types
    if (freeType.Contains("Quick", StringComparison.OrdinalIgnoreCase))
        return "Quick";
    
    if (freeType.Contains("Penalty", StringComparison.OrdinalIgnoreCase))
        return "Penalty";
    
    // Classify by distance if available
    if (distance.HasValue)
    {
        return distance.Value switch
        {
            <= 13 => "Close Range",
            <= 21 => "Medium Range", 
            <= 30 => "Long Range",
            _ => "Very Long Range"
        };
    }
    
    return "Standard";
}
```

### Phase 3: Database Record Creation

#### 3.1 Free Type Management
```csharp
private async Task<int> GetFreeTypeIdAsync(string freeType)
{
    var type = await _context.FreeTypes
        .FirstOrDefaultAsync(ft => ft.TypeName.Equals(freeType, StringComparison.OrdinalIgnoreCase));
    
    if (type != null)
        return type.FreeTypeId;
    
    // Create if doesn't exist
    var newType = new FreeType
    {
        TypeName = freeType,
        Description = $"Free kick type: {freeType}",
        IsActive = true
    };
    
    await _context.FreeTypes.AddAsync(newType);
    await _context.SaveChangesAsync();
    
    return newType.FreeTypeId;
}
```

#### 3.2 Player Resolution for Free Kick Takers
```csharp
private async Task<int?> ResolveScoreableFreePlayerAsync(string playerName, bool isOpposition)
{
    if (string.IsNullOrEmpty(playerName)) return null;
    
    // Clean player name
    var cleanName = CleanPlayerName(playerName);
    
    if (isOpposition)
    {
        return await GetOrCreateOppositionPlayerAsync(cleanName);
    }
    
    // For Drum players - free kick takers are usually key players, so more strict matching
    var player = await _context.Players
        .FirstOrDefaultAsync(p => 
            p.PlayerName.ToLower() == cleanName.ToLower() ||
            EF.Functions.TrigramsSimilarity(p.PlayerName, cleanName) > 0.8); // Higher threshold for free kick takers
    
    if (player != null)
    {
        // Update player record to note they take free kicks
        await MarkPlayerAsFreekickTaker(player.PlayerId);
        return player.PlayerId;
    }
    
    // Create new Drum player - free kick takers are important to track
    var newPlayer = new Player
    {
        PlayerName = cleanName,
        IsActive = true,
        // Could add flag for free kick specialist
    };
    
    await _context.Players.AddAsync(newPlayer);
    await _context.SaveChangesAsync();
    
    await MarkPlayerAsFreekickTaker(newPlayer.PlayerId);
    return newPlayer.PlayerId;
}

private async Task MarkPlayerAsFreekickTaker(int playerId)
{
    // This could update player attributes or create a separate tracking record
    // Implementation depends on how we want to track free kick specialization
    _logger.LogDebug("Player {PlayerId} identified as free kick taker", playerId);
    
    // For now, just log - could extend to update player roles/attributes
}
```

### Phase 4: Main Processing Pipeline

#### 4.1 Complete Sheet Processing
```csharp
public async Task<ServiceResult<ScoreableFreeAnalysisResult>> ProcessScoreableFreeAnalysisSheetAsync(
    ExcelWorksheet worksheet, 
    int jobId)
{
    var stopwatch = Stopwatch.StartNew();
    var allFreeEvents = new List<ScoreableFreeEvent>();
    var processedRows = 0;
    var skippedRows = 0;
    
    try
    {
        var lastRow = worksheet.Dimension?.End.Row ?? 0;
        _logger.LogInformation("Processing scoreable free analysis sheet with {RowCount} rows", lastRow);
        
        // Process each row, starting from row 2 (skip header)
        for (int row = 2; row <= lastRow; row++)
        {
            // Process Drum team data (columns 1-10)
            var drumFree = ProcessScoreableFreeRow(worksheet, row, false);
            if (drumFree != null)
            {
                allFreeEvents.Add(drumFree);
                processedRows++;
            }
            
            // Process Opposition team data (columns 11-21) 
            var oppFree = ProcessScoreableFreeRow(worksheet, row, true);
            if (oppFree != null)
            {
                allFreeEvents.Add(oppFree);
                processedRows++;
            }
            
            if (drumFree == null && oppFree == null)
                skippedRows++;
        }
        
        _logger.LogInformation(
            "Extracted {FreeCount} scoreable free events from {ProcessedRows} processed rows, {SkippedRows} skipped rows", 
            allFreeEvents.Count, processedRows, skippedRows);
        
        // Convert events to database records
        var freeRecords = new List<ScoreableFreeAnalysis>();
        var failedConversions = 0;
        
        foreach (var freeEvent in allFreeEvents)
        {
            try
            {
                var matchId = await ResolveMatchIdFromTeamsAsync(freeEvent.Teams);
                if (!matchId.HasValue)
                {
                    _logger.LogWarning("Could not resolve match ID for free kick event with teams: '{Teams}'", 
                        freeEvent.Teams);
                    failedConversions++;
                    continue;
                }
                
                var playerId = await ResolveScoreableFreePlayerAsync(freeEvent.Player, freeEvent.IsOpposition);
                var freeTypeId = await GetFreeTypeIdAsync(ClassifyFreeKickType(freeEvent.FreeType, 
                    CalculateApproximateDistance(freeEvent.Location)));
                var shotOutcomeId = await GetShotOutcomeIdAsync(freeEvent.Outcome);
                var distance = CalculateDistanceCategory(freeEvent.Location);
                var success = DetermineFreeKickSuccess(freeEvent.Outcome);
                
                var record = new ScoreableFreeAnalysis
                {
                    MatchId = matchId.Value,
                    PlayerId = playerId,
                    FreeNumber = freeEvent.EventNumber ?? 0,
                    FreeTypeId = freeTypeId,
                    Distance = distance,
                    ShotOutcomeId = shotOutcomeId,
                    Success = success
                };
                
                freeRecords.Add(record);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert free kick event {EventNumber} to database record", 
                    freeEvent.EventNumber);
                failedConversions++;
            }
        }
        
        // Save to database in batches
        const int batchSize = 25;
        var savedRecords = 0;
        
        for (int i = 0; i < freeRecords.Count; i += batchSize)
        {
            var batch = freeRecords.Skip(i).Take(batchSize).ToList();
            await _context.ScoreableFreeAnalysis.AddRangeAsync(batch);
            await _context.SaveChangesAsync();
            
            savedRecords += batch.Count;
            _logger.LogDebug("Saved scoreable free analysis batch {BatchNumber}: {RecordCount} records", 
                i / batchSize + 1, batch.Count);
        }
        
        stopwatch.Stop();
        
        _logger.LogInformation(
            "Scoreable free analysis processing completed: {SavedRecords} records saved from {ExtractedFrees} free events in {Duration}ms. {FailedConversions} failed conversions.", 
            savedRecords, allFreeEvents.Count, stopwatch.ElapsedMilliseconds, failedConversions);
        
        return ServiceResult<ScoreableFreeAnalysisResult>.Success(new ScoreableFreeAnalysisResult
        {
            FreeEventsExtracted = allFreeEvents.Count,
            RecordsCreated = savedRecords,
            FailedConversions = failedConversions,
            ProcessedRows = processedRows,
            SkippedRows = skippedRows,
            ProcessingTimeMs = stopwatch.ElapsedMilliseconds
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to process scoreable free analysis sheet");
        
        await _progressService.RecordValidationErrorAsync(
            jobId, "Scoreable Frees Data", null, null,
            EtlErrorTypes.PROCESSING_ERROR,
            $"Failed to process scoreable free analysis: {ex.Message}",
            "Check sheet format and data structure");
        
        return ServiceResult<ScoreableFreeAnalysisResult>.Failed($"Failed to process scoreable free analysis: {ex.Message}");
    }
}
```

### Phase 5: Data Validation and Quality Checks

#### 5.1 Free Kick Event Validation
```csharp
private async Task ValidateScoreableFreeEvents(
    List<ScoreableFreeEvent> freeEvents, 
    int jobId)
{
    foreach (var freeEvent in freeEvents.Where(e => e != null))
    {
        // Validate required fields
        if (string.IsNullOrEmpty(freeEvent.Outcome))
        {
            await _progressService.RecordValidationErrorAsync(
                jobId, "Scoreable Frees Data", null, "Outcome",
                EtlErrorTypes.MISSING_DATA,
                $"Missing outcome for scoreable free event {freeEvent.EventNumber}",
                "Ensure all free kick events have outcome data");
        }
        
        // Validate player name for Drum team frees
        if (!freeEvent.IsOpposition && string.IsNullOrEmpty(freeEvent.Player))
        {
            await _progressService.RecordValidationErrorAsync(
                jobId, "Scoreable Frees Data", null, "Player",
                EtlErrorTypes.MISSING_DATA,
                $"Missing player name for Drum free kick event {freeEvent.EventNumber}",
                "Ensure all Drum free kicks have player attribution");
        }
        
        // Validate location for distance calculation
        if (!freeEvent.Location.HasValue || freeEvent.Location < 1)
        {
            await _progressService.RecordValidationErrorAsync(
                jobId, "Scoreable Frees Data", null, "Location",
                EtlErrorTypes.MISSING_DATA,
                $"Missing or invalid location for free kick event {freeEvent.EventNumber}",
                "Location is required for distance calculation and analysis");
        }
        
        // Validate reasonable distance
        if (freeEvent.Location.HasValue && freeEvent.Location > 50)
        {
            await _progressService.RecordValidationErrorAsync(
                jobId, "Scoreable Frees Data", null, "Location",
                EtlErrorTypes.INVALID_RANGE,
                $"Unusually far location ({freeEvent.Location}) for scoreable free {freeEvent.EventNumber}",
                "Verify location accuracy - scoreable frees are typically closer to goal");
        }
    }
}
```

#### 5.2 Success Rate Analysis
```csharp
private async Task AnalyzeFreeKickSuccessRates(
    List<ScoreableFreeAnalysis> records, 
    int jobId)
{
    // Group by team and distance for success rate analysis
    var drumFrees = records.Where(r => r.PlayerId.HasValue && 
        !_context.Players.Any(p => p.PlayerId == r.PlayerId && p.PlayerName.StartsWith("[OPP]"))).ToList();
    
    var oppositionFrees = records.Where(r => !r.PlayerId.HasValue || 
        _context.Players.Any(p => p.PlayerId == r.PlayerId && p.PlayerName.StartsWith("[OPP]"))).ToList();
    
    await AnalyzeTeamFreeKickPerformance(drumFrees, "Drum", jobId);
    await AnalyzeTeamFreeKickPerformance(oppositionFrees, "Opposition", jobId);
    
    // Analyze by player (Drum team only)
    await AnalyzeIndividualFreeKickPerformance(drumFrees, jobId);
}

private async Task AnalyzeTeamFreeKickPerformance(
    List<ScoreableFreeAnalysis> teamFrees, 
    string teamName, 
    int jobId)
{
    if (!teamFrees.Any()) return;
    
    var totalFrees = teamFrees.Count;
    var successfulFrees = teamFrees.Count(f => f.Success);
    var successRate = (double)successfulFrees / totalFrees;
    
    // Group by distance for detailed analysis
    var byDistance = teamFrees.GroupBy(f => f.Distance).ToList();
    
    _logger.LogInformation(
        "{TeamName} free kick analysis: {TotalFrees} attempts, {SuccessfulFrees} successful, {SuccessRate:P1} success rate",
        teamName, totalFrees, successfulFrees, successRate);
    
    foreach (var distanceGroup in byDistance)
    {
        var distanceFrees = distanceGroup.ToList();
        var distanceSuccess = distanceFrees.Count(f => f.Success);
        var distanceSuccessRate = (double)distanceSuccess / distanceFrees.Count;
        
        _logger.LogInformation(
            "{TeamName} {Distance} frees: {Count} attempts, {Successful} successful, {SuccessRate:P1} success rate",
            teamName, distanceGroup.Key, distanceFrees.Count, distanceSuccess, distanceSuccessRate);
    }
    
    // Flag unusually high or low success rates
    if (successRate > 0.85)
    {
        await _progressService.RecordValidationErrorAsync(
            jobId, "Scoreable Frees Data", null, "Success Rate",
            "warning",
            $"{teamName} has unusually high free kick success rate: {successRate:P1}",
            "Verify outcome classification accuracy");
    }
    else if (successRate < 0.30 && totalFrees > 5)
    {
        await _progressService.RecordValidationErrorAsync(
            jobId, "Scoreable Frees Data", null, "Success Rate",
            "warning",
            $"{teamName} has unusually low free kick success rate: {successRate:P1}",
            "Check if all free kick attempts are being recorded");
    }
}

private async Task AnalyzeIndividualFreeKickPerformance(
    List<ScoreableFreeAnalysis> drumFrees, 
    int jobId)
{
    var playerGroups = drumFrees.Where(f => f.PlayerId.HasValue)
        .GroupBy(f => f.PlayerId.Value)
        .Where(g => g.Count() >= 3) // Only analyze players with 3+ attempts
        .ToList();
    
    foreach (var playerGroup in playerGroups)
    {
        var playerFrees = playerGroup.ToList();
        var playerId = playerGroup.Key;
        var player = await _context.Players.FirstOrDefaultAsync(p => p.PlayerId == playerId);
        
        if (player == null) continue;
        
        var totalAttempts = playerFrees.Count;
        var successful = playerFrees.Count(f => f.Success);
        var successRate = (double)successful / totalAttempts;
        
        _logger.LogInformation(
            "Player {PlayerName} free kick performance: {Total} attempts, {Successful} successful, {SuccessRate:P1} success rate",
            player.PlayerName, totalAttempts, successful, successRate);
        
        // Identify top free kick takers
        if (totalAttempts >= 5 && successRate >= 0.60)
        {
            _logger.LogInformation("🎯 {PlayerName} identified as reliable free kick taker: {SuccessRate:P1} success rate over {Attempts} attempts",
                player.PlayerName, successRate, totalAttempts);
        }
    }
}
```

### Phase 6: Performance and Error Handling

#### 6.1 Batch Processing for Large Datasets
```csharp
private async Task<List<ScoreableFreeAnalysis>> ProcessFreeEventsBatch(
    List<ScoreableFreeEvent> freeEventBatch, 
    int batchNumber)
{
    _logger.LogDebug("Processing scoreable free events batch {BatchNumber} with {EventCount} events", 
        batchNumber, freeEventBatch.Count);
    
    var records = new List<ScoreableFreeAnalysis>();
    
    foreach (var freeEvent in freeEventBatch)
    {
        try
        {
            var record = await ConvertFreeEventToRecord(freeEvent);
            if (record != null)
                records.Add(record);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to convert free event {EventNumber} in batch {BatchNumber}", 
                freeEvent.EventNumber, batchNumber);
        }
    }
    
    // Allow other tasks to run
    await Task.Yield();
    
    return records;
}
```

#### 6.2 Data Quality Reporting
```csharp
private async Task LogScoreableFreeQualityMetrics(ScoreableFreeAnalysisResult result, int jobId)
{
    var qualityMetrics = new
    {
        ConversionRate = result.FreeEventsExtracted > 0 ? 
            (double)result.RecordsCreated / result.FreeEventsExtracted : 0,
        FailureRate = result.FreeEventsExtracted > 0 ? 
            (double)result.FailedConversions / result.FreeEventsExtracted : 0,
        ProcessingEfficiency = result.ProcessedRows > 0 ? 
            (double)result.FreeEventsExtracted / result.ProcessedRows : 0
    };
    
    _logger.LogInformation(
        "Scoreable free analysis quality metrics for job {JobId}: " +
        "Conversion Rate: {ConversionRate:P2}, Failure Rate: {FailureRate:P2}, " +
        "Processing Efficiency: {ProcessingEfficiency:P2}",
        jobId, qualityMetrics.ConversionRate, qualityMetrics.FailureRate, 
        qualityMetrics.ProcessingEfficiency);
    
    // Record metrics for monitoring
    await _progressService.RecordProgressUpdateAsync(new EtlProgressUpdate
    {
        JobId = jobId,
        Stage = "scoreable_free_analysis_quality",
        Message = $"Scoreable free analysis quality: {qualityMetrics.ConversionRate:P1} conversion rate, " +
                 $"{qualityMetrics.FailureRate:P1} failure rate",
        Status = "completed"
    });
}
```

## Performance Considerations

### Expected Data Volume:
- **Free Kicks Per Team**: ~8-15 per match
- **Total Events**: ~120-240 across all matches
- **Processing Time Target**: < 1 minute
- **Success Rate Expected**: 40-70% typical for GAA

### Memory Management:
- Process in small batches of 25 records
- Immediate disposal of temporary objects
- Efficient player lookup with caching

## Success Criteria

- ✅ All scoreable free events successfully extracted from dual-column format
- ✅ Distance categories accurately calculated from field positions
- ✅ Player identification and free kick specialization tracking
- ✅ Success rates properly determined from outcomes
- ✅ Free kick types correctly classified
- ✅ Match associations properly resolved
- ✅ Individual and team performance analysis implemented
- ✅ Data validation and quality checks in place
- ✅ Processing completed within performance targets
- ✅ Comprehensive error handling and metrics reporting

This plan ensures that detailed scoreable free analysis data is accurately captured and provides valuable insights into free kick performance, player specialization, and tactical effectiveness in GAA matches.