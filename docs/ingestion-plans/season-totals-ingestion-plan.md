# Season Totals Ingestion Plan

## Executive Summary

This document outlines the detailed plan for ingesting season totals data from the Excel file's "Cumulative Stats 2025" sheet into the `season_player_totals` database table. This sheet contains aggregated season-wide statistics for all players, providing cumulative performance metrics across all matches played during the 2025 season.

## Data Source Analysis

### Excel Sheet Structure
- **Source Sheet**: `Cumulative Stats 2025`
- **Data Structure**: ~25 rows × 85+ columns
- **Format**: One row per player with season aggregated statistics
- **Content**: Same column structure as individual match player stats but with season totals
- **Aggregation Level**: All matches combined into season-wide player summaries

### Sample Data Structure
```
Player Name   | Min | TE  | PSR  | TP | Scores | Goals | Points | Tackles | Cards | ... (80+ more cols)
--------------|-----|-----|------|----| -------|-------|--------|---------|-------|--------------------
C McCloskey   | 420 | 78  | 0.71 | 65 | 2-08   | 2     | 8      | 18      | 1     | ...
R Doherty     | 390 | 92  | 0.68 | 74 | 1-05   | 1     | 5      | 25      | 0     | ...
O Doherty     | 480 | 45  | 0.82 | 34 | 0-00   | 0     | 0      | 2       | 0     | ...
```

## Database Target Schema

### season_player_totals Table
```sql
CREATE TABLE season_player_totals (
    season_player_total_id SERIAL PRIMARY KEY,
    player_id INTEGER NOT NULL REFERENCES players(player_id),
    season_id INTEGER NOT NULL REFERENCES seasons(season_id),
    games_played INTEGER,
    total_minutes INTEGER,
    avg_engagement_efficiency DECIMAL(5,4),
    avg_possession_success_rate DECIMAL(5,4),
    total_scores INTEGER,
    total_goals INTEGER,
    total_points INTEGER,
    avg_conversion_rate DECIMAL(5,4),
    total_tackles INTEGER,
    avg_tackle_success_rate DECIMAL(5,4),
    total_turnovers_won INTEGER,
    total_interceptions INTEGER,
    total_yellow_cards INTEGER,
    total_black_cards INTEGER,
    total_red_cards INTEGER,
    season_statistics JSONB, -- Extended statistics
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

## Detailed Ingestion Mapping

### Phase 1: Sheet Detection and Structure Analysis

#### 1.1 Season Sheet Identification
```csharp
private bool IsSeasonTotalsSheet(string sheetName)
{
    return sheetName.Equals("Cumulative Stats 2025", StringComparison.OrdinalIgnoreCase) ||
           sheetName.Contains("Cumulative Stats", StringComparison.OrdinalIgnoreCase) ||
           sheetName.Contains("Season Totals", StringComparison.OrdinalIgnoreCase) ||
           (sheetName.Contains("2025", StringComparison.OrdinalIgnoreCase) && 
            sheetName.Contains("Stats", StringComparison.OrdinalIgnoreCase));
}

private string ExtractSeasonFromSheetName(string sheetName)
{
    // Extract year from sheet name
    var yearMatch = System.Text.RegularExpressions.Regex.Match(sheetName, @"\b(20\d{2})\b");
    return yearMatch.Success ? yearMatch.Groups[1].Value : "2025"; // Default to 2025
}
```

#### 1.2 Column Mapping (Same as Match Player Statistics)
```csharp
private static class SeasonTotalsColumnMapping
{
    // Core statistics (same mapping as match_player_statistics)
    public const int JERSEY_NUMBER = 0;         // Column A
    public const int PLAYER_NAME = 1;           // Column B
    public const int MINUTES_PLAYED = 2;        // Column C
    public const int TOTAL_ENGAGEMENTS = 3;     // Column D
    public const int ENGAGEMENT_EFFICIENCY = 4; // Column E
    public const int SCORES = 5;                // Column F
    public const int PSR = 6;                   // Column G
    public const int PSR_PER_TE = 7;            // Column H
    public const int TOTAL_POSSESSIONS = 8;     // Column I
    public const int TURNOVERS_WON = 9;         // Column J
    public const int INTERCEPTIONS = 10;        // Column K
    
    // Extended statistics start here
    public const int TOTAL_ATTACKS = 29;        // Column AD
    public const int SHOTS_TOTAL = 34;          // Column AI
    public const int POINTS = 35;               // Column AJ
    public const int GOALS = 37;                // Column AL
    public const int WIDES = 38;                // Column AM
    public const int CONVERSION_RATE = 56;      // Column BE
    
    // Tackle statistics
    public const int TACKLES_TOTAL = 57;        // Column BF
    public const int TACKLES_CONTACT = 64;      // Column BM
    public const int TACKLES_MISSED = 65;       // Column BN
    public const int TACKLE_PERCENTAGE = 66;    // Column BO
    
    // Disciplinary records
    public const int YELLOW_CARDS = 75;         // Column BX
    public const int BLACK_CARDS = 76;          // Column BY
    public const int RED_CARDS = 77;            // Column BZ
    
    // Goalkeeper statistics (if applicable)
    public const int KICKOUTS_TOTAL = 80;       // Column CC
    public const int KICKOUTS_RETAINED = 81;    // Column CD
    public const int KICKOUTS_LOST = 82;        // Column CE
    public const int KICKOUT_PERCENTAGE = 83;   // Column CF
    public const int SAVES = 84;                // Column CG
}
```

### Phase 2: Season Data Extraction and Processing

#### 2.1 Player Season Statistics Processing
```csharp
private async Task<List<SeasonPlayerData>> ProcessSeasonTotalsSheetAsync(
    ExcelWorksheet worksheet, 
    string seasonName)
{
    var seasonPlayerData = new List<SeasonPlayerData>();
    var lastRow = worksheet.Dimension?.End.Row ?? 0;
    var lastCol = worksheet.Dimension?.End.Column ?? 0;
    
    _logger.LogInformation("Processing season totals sheet for season '{SeasonName}' with {Rows} rows and {Cols} columns",
        seasonName, lastRow, lastCol);
    
    // Process each player row (starting from row 3, skip headers)
    for (int row = 3; row <= lastRow; row++)
    {
        var playerName = worksheet.Cells[row, SeasonTotalsColumnMapping.PLAYER_NAME + 1].Text?.Trim();
        
        // Skip empty rows
        if (string.IsNullOrEmpty(playerName))
            continue;
        
        var playerSeasonData = new SeasonPlayerData
        {
            PlayerName = CleanPlayerName(playerName),
            JerseyNumber = ParseIntValue(worksheet.Cells[row, SeasonTotalsColumnMapping.JERSEY_NUMBER + 1].Value),
            SeasonName = seasonName,
            
            // Core performance metrics
            TotalMinutes = ParseIntValue(worksheet.Cells[row, SeasonTotalsColumnMapping.MINUTES_PLAYED + 1].Value) ?? 0,
            TotalEngagements = ParseIntValue(worksheet.Cells[row, SeasonTotalsColumnMapping.TOTAL_ENGAGEMENTS + 1].Value) ?? 0,
            AvgEngagementEfficiency = ParseDecimalValue(worksheet.Cells[row, SeasonTotalsColumnMapping.ENGAGEMENT_EFFICIENCY + 1].Value),
            AvgPossessionSuccessRate = ParseDecimalValue(worksheet.Cells[row, SeasonTotalsColumnMapping.PSR + 1].Value),
            TotalPossessions = ParseIntValue(worksheet.Cells[row, SeasonTotalsColumnMapping.TOTAL_POSSESSIONS + 1].Value) ?? 0,
            TotalTurnoversWon = ParseIntValue(worksheet.Cells[row, SeasonTotalsColumnMapping.TURNOVERS_WON + 1].Value) ?? 0,
            TotalInterceptions = ParseIntValue(worksheet.Cells[row, SeasonTotalsColumnMapping.INTERCEPTIONS + 1].Value) ?? 0,
            
            // Scoring statistics
            SeasonScores = ParseScoreValue(worksheet.Cells[row, SeasonTotalsColumnMapping.SCORES + 1].Text),
            TotalGoals = ParseIntValue(worksheet.Cells[row, SeasonTotalsColumnMapping.GOALS + 1].Value) ?? 0,
            TotalPoints = ParseIntValue(worksheet.Cells[row, SeasonTotalsColumnMapping.POINTS + 1].Value) ?? 0,
            TotalShots = ParseIntValue(worksheet.Cells[row, SeasonTotalsColumnMapping.SHOTS_TOTAL + 1].Value) ?? 0,
            TotalWides = ParseIntValue(worksheet.Cells[row, SeasonTotalsColumnMapping.WIDES + 1].Value) ?? 0,
            AvgConversionRate = ParseDecimalValue(worksheet.Cells[row, SeasonTotalsColumnMapping.CONVERSION_RATE + 1].Value),
            
            // Defensive statistics
            TotalTackles = ParseIntValue(worksheet.Cells[row, SeasonTotalsColumnMapping.TACKLES_TOTAL + 1].Value) ?? 0,
            TotalContactTackles = ParseIntValue(worksheet.Cells[row, SeasonTotalsColumnMapping.TACKLES_CONTACT + 1].Value) ?? 0,
            TotalMissedTackles = ParseIntValue(worksheet.Cells[row, SeasonTotalsColumnMapping.TACKLES_MISSED + 1].Value) ?? 0,
            AvgTackleSuccessRate = ParseDecimalValue(worksheet.Cells[row, SeasonTotalsColumnMapping.TACKLE_PERCENTAGE + 1].Value),
            
            // Disciplinary records
            TotalYellowCards = ParseIntValue(worksheet.Cells[row, SeasonTotalsColumnMapping.YELLOW_CARDS + 1].Value) ?? 0,
            TotalBlackCards = ParseIntValue(worksheet.Cells[row, SeasonTotalsColumnMapping.BLACK_CARDS + 1].Value) ?? 0,
            TotalRedCards = ParseIntValue(worksheet.Cells[row, SeasonTotalsColumnMapping.RED_CARDS + 1].Value) ?? 0,
            
            // Goalkeeper statistics (if applicable)
            GoalkeeperStats = ExtractGoalkeeperStats(worksheet, row),
            
            // Extended season statistics
            ExtendedStats = ExtractExtendedSeasonStatistics(worksheet, row, lastCol)
        };
        
        seasonPlayerData.Add(playerSeasonData);
    }
    
    _logger.LogInformation("Extracted season data for {PlayerCount} players from season sheet", 
        seasonPlayerData.Count);
    
    return seasonPlayerData;
}
```

#### 2.2 Games Played Calculation
```csharp
private async Task<int> CalculateGamesPlayedAsync(string playerName, int seasonId)
{
    // Count matches where player has statistics recorded
    var gamesPlayed = await _context.MatchPlayerStatistics
        .Join(_context.Matches, mps => mps.MatchId, m => m.MatchId, (mps, m) => new { mps, m })
        .Join(_context.Players, combined => combined.mps.PlayerId, p => p.PlayerId, (combined, p) => new { combined.mps, combined.m, p })
        .Where(result => 
            result.p.PlayerName.Equals(playerName, StringComparison.OrdinalIgnoreCase) &&
            result.m.SeasonId == seasonId &&
            result.mps.MinutesPlayed > 0) // Only count games where player actually played
        .CountAsync();
    
    return gamesPlayed;
}

// Alternative: Estimate from total minutes (if match data not available)
private int EstimateGamesPlayedFromMinutes(int totalMinutes)
{
    // Typical GAA match is ~70-80 minutes including extra time
    const int TYPICAL_MATCH_MINUTES = 75;
    
    if (totalMinutes <= 0) return 0;
    
    // Estimate games played, rounded up (partial games count as games)
    return (int)Math.Ceiling((double)totalMinutes / TYPICAL_MATCH_MINUTES);
}
```

#### 2.3 Extended Statistics Extraction
```csharp
private GoalkeeperSeasonStats? ExtractGoalkeeperStats(ExcelWorksheet worksheet, int row)
{
    // Only extract goalkeeper stats if they exist and are meaningful
    var kickoutsTotal = ParseIntValue(worksheet.Cells[row, SeasonTotalsColumnMapping.KICKOUTS_TOTAL + 1].Value);
    var saves = ParseIntValue(worksheet.Cells[row, SeasonTotalsColumnMapping.SAVES + 1].Value);
    
    // If no goalkeeper-specific stats, return null
    if (!kickoutsTotal.HasValue && !saves.HasValue)
        return null;
    
    return new GoalkeeperSeasonStats
    {
        TotalKickouts = kickoutsTotal ?? 0,
        KickoutsRetained = ParseIntValue(worksheet.Cells[row, SeasonTotalsColumnMapping.KICKOUTS_RETAINED + 1].Value) ?? 0,
        KickoutsLost = ParseIntValue(worksheet.Cells[row, SeasonTotalsColumnMapping.KICKOUTS_LOST + 1].Value) ?? 0,
        KickoutSuccessRate = ParseDecimalValue(worksheet.Cells[row, SeasonTotalsColumnMapping.KICKOUT_PERCENTAGE + 1].Value),
        TotalSaves = saves ?? 0
    };
}

private Dictionary<string, object?> ExtractExtendedSeasonStatistics(
    ExcelWorksheet worksheet, 
    int row, 
    int maxCol)
{
    var extendedStats = new Dictionary<string, object?>();
    
    // Extract additional statistics not covered in core fields
    var additionalColumns = new Dictionary<int, string>
    {
        { SeasonTotalsColumnMapping.TOTAL_ATTACKS, "total_attacks" },
        { 11, "total_possession_lost" }, // TPL
        { 12, "kick_passes" }, // KP
        { 13, "hand_passes" }, // HP
        { 30, "kick_retained" }, // KR
        { 31, "kick_lost" }, // KL
        { 32, "carry_retained" }, // CR
        { 33, "carry_lost" }, // CL
        { 39, "shorts" }, // Short shots
        { 40, "saves_against" }, // Saves made by opposition keeper
        // Add more as needed based on actual column structure
    };
    
    foreach (var kvp in additionalColumns)
    {
        if (kvp.Key < maxCol)
        {
            var value = ProcessSeasonStatisticValue(worksheet.Cells[row, kvp.Key + 1].Value);
            if (value != null)
                extendedStats[kvp.Value] = value;
        }
    }
    
    return extendedStats;
}

private object? ProcessSeasonStatisticValue(object? cellValue)
{
    if (cellValue == null) return null;
    
    var stringValue = cellValue.ToString()?.Trim();
    if (string.IsNullOrEmpty(stringValue) || stringValue.Equals("NaN", StringComparison.OrdinalIgnoreCase))
        return null;
    
    // Try parsing as number first
    if (decimal.TryParse(stringValue, out var decimalValue))
        return decimalValue;
    
    return stringValue;
}
```

### Phase 3: Database Integration and Season Management

#### 3.1 Season Resolution
```csharp
private async Task<int> GetSeasonIdAsync(string seasonName)
{
    var season = await _context.Seasons
        .FirstOrDefaultAsync(s => s.SeasonName.Equals(seasonName, StringComparison.OrdinalIgnoreCase));
    
    if (season != null)
        return season.SeasonId;
    
    // Create season if it doesn't exist
    var newSeason = new Season
    {
        SeasonName = seasonName,
        StartDate = new DateOnly(int.Parse(seasonName), 1, 1),
        EndDate = new DateOnly(int.Parse(seasonName), 12, 31),
        IsCurrent = seasonName == DateTime.Now.Year.ToString()
    };
    
    await _context.Seasons.AddAsync(newSeason);
    await _context.SaveChangesAsync();
    
    _logger.LogInformation("Created new season: {SeasonName} (ID: {SeasonId})", 
        seasonName, newSeason.SeasonId);
    
    return newSeason.SeasonId;
}
```

#### 3.2 Player Resolution with Season Context
```csharp
private async Task<int?> ResolveSeasonPlayerAsync(SeasonPlayerData playerData)
{
    // Try exact name match first
    var player = await _context.Players
        .FirstOrDefaultAsync(p => p.PlayerName.Equals(playerData.PlayerName, StringComparison.OrdinalIgnoreCase));
    
    if (player != null)
    {
        // Update jersey number if different
        if (playerData.JerseyNumber.HasValue && player.JerseyNumber != playerData.JerseyNumber)
        {
            _logger.LogInformation("Updating jersey number for {PlayerName}: {OldNumber} -> {NewNumber}",
                player.PlayerName, player.JerseyNumber, playerData.JerseyNumber);
            player.JerseyNumber = playerData.JerseyNumber;
            await _context.SaveChangesAsync();
        }
        
        return player.PlayerId;
    }
    
    // Try fuzzy matching for name variations
    var similarPlayers = await _context.Players
        .Where(p => EF.Functions.TrigramsSimilarity(p.PlayerName, playerData.PlayerName) > 0.7)
        .OrderByDescending(p => EF.Functions.TrigramsSimilarity(p.PlayerName, playerData.PlayerName))
        .ToListAsync();
    
    if (similarPlayers.Any())
    {
        var bestMatch = similarPlayers.First();
        _logger.LogInformation("Fuzzy matched season player '{SeasonName}' to existing player '{PlayerName}' (similarity: {Similarity})",
            playerData.PlayerName, bestMatch.PlayerName, 
            EF.Functions.TrigramsSimilarity(bestMatch.PlayerName, playerData.PlayerName));
        return bestMatch.PlayerId;
    }
    
    // Create new player based on season data
    var newPlayer = new Player
    {
        PlayerName = playerData.PlayerName,
        JerseyNumber = playerData.JerseyNumber,
        IsActive = true
        // Position will be determined from match data or set later
    };
    
    await _context.Players.AddAsync(newPlayer);
    await _context.SaveChangesAsync();
    
    _logger.LogInformation("Created new player from season data: {PlayerName} (ID: {PlayerId})", 
        newPlayer.PlayerName, newPlayer.PlayerId);
    
    return newPlayer.PlayerId;
}
```

### Phase 4: Main Processing Pipeline

#### 4.1 Complete Season Totals Processing
```csharp
public async Task<ServiceResult<SeasonTotalsResult>> ProcessSeasonTotalsAsync(
    ExcelWorksheet worksheet, 
    int jobId)
{
    var stopwatch = Stopwatch.StartNew();
    var processedPlayers = 0;
    var createdRecords = 0;
    var failedPlayers = 0;
    
    try
    {
        // Extract season name from sheet
        var seasonName = ExtractSeasonFromSheetName(worksheet.Name);
        var seasonId = await GetSeasonIdAsync(seasonName);
        
        _logger.LogInformation("Processing season totals for season '{SeasonName}' (ID: {SeasonId})", 
            seasonName, seasonId);
        
        // Extract all player season data
        var seasonPlayerData = await ProcessSeasonTotalsSheetAsync(worksheet, seasonName);
        
        if (!seasonPlayerData.Any())
        {
            return ServiceResult<SeasonTotalsResult>.Failed("No season player data found in sheet");
        }
        
        // Clear existing season totals for this season to avoid duplicates
        await ClearExistingSeasonTotalsAsync(seasonId, jobId);
        
        // Process each player's season data
        var seasonTotalRecords = new List<SeasonPlayerTotal>();
        
        foreach (var playerData in seasonPlayerData)
        {
            try
            {
                var playerId = await ResolveSeasonPlayerAsync(playerData);
                
                if (!playerId.HasValue)
                {
                    _logger.LogWarning("Could not resolve player ID for season player: {PlayerName}", 
                        playerData.PlayerName);
                    failedPlayers++;
                    continue;
                }
                
                // Calculate or estimate games played
                var gamesPlayed = await CalculateGamesPlayedAsync(playerData.PlayerName, seasonId);
                if (gamesPlayed == 0)
                {
                    gamesPlayed = EstimateGamesPlayedFromMinutes(playerData.TotalMinutes);
                }
                
                // Create season total record
                var seasonRecord = new SeasonPlayerTotal
                {
                    PlayerId = playerId.Value,
                    SeasonId = seasonId,
                    GamesPlayed = gamesPlayed,
                    TotalMinutes = playerData.TotalMinutes,
                    AvgEngagementEfficiency = playerData.AvgEngagementEfficiency,
                    AvgPossessionSuccessRate = playerData.AvgPossessionSuccessRate,
                    TotalScores = (playerData.TotalGoals * 3) + playerData.TotalPoints,
                    TotalGoals = playerData.TotalGoals,
                    TotalPoints = playerData.TotalPoints,
                    AvgConversionRate = playerData.AvgConversionRate,
                    TotalTackles = playerData.TotalTackles,
                    AvgTackleSuccessRate = playerData.AvgTackleSuccessRate,
                    TotalTurnoversWon = playerData.TotalTurnoversWon,
                    TotalInterceptions = playerData.TotalInterceptions,
                    TotalYellowCards = playerData.TotalYellowCards,
                    TotalBlackCards = playerData.TotalBlackCards,
                    TotalRedCards = playerData.TotalRedCards,
                    SeasonStatistics = JsonSerializer.Serialize(CreateSeasonStatisticsObject(playerData))
                };
                
                seasonTotalRecords.Add(seasonRecord);
                processedPlayers++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process season data for player: {PlayerName}", 
                    playerData.PlayerName);
                failedPlayers++;
            }
        }
        
        // Save season totals in batches
        const int batchSize = 10;
        for (int i = 0; i < seasonTotalRecords.Count; i += batchSize)
        {
            var batch = seasonTotalRecords.Skip(i).Take(batchSize).ToList();
            await _context.SeasonPlayerTotals.AddRangeAsync(batch);
            await _context.SaveChangesAsync();
            
            createdRecords += batch.Count;
            _logger.LogDebug("Saved season totals batch {BatchNumber}: {RecordCount} records", 
                i / batchSize + 1, batch.Count);
        }
        
        stopwatch.Stop();
        
        _logger.LogInformation(
            "Season totals processing completed for season '{SeasonName}': " +
            "{CreatedRecords} records created, {ProcessedPlayers} players processed, " +
            "{FailedPlayers} players failed in {Duration}ms",
            seasonName, createdRecords, processedPlayers, failedPlayers, stopwatch.ElapsedMilliseconds);
        
        return ServiceResult<SeasonTotalsResult>.Success(new SeasonTotalsResult
        {
            SeasonName = seasonName,
            RecordsCreated = createdRecords,
            ProcessedPlayers = processedPlayers,
            FailedPlayers = failedPlayers,
            ProcessingTimeMs = stopwatch.ElapsedMilliseconds
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to process season totals");
        
        await _progressService.RecordValidationErrorAsync(
            jobId, "Cumulative Stats 2025", null, null,
            EtlErrorTypes.PROCESSING_ERROR,
            $"Failed to process season totals: {ex.Message}",
            "Check sheet format and data structure");
        
        return ServiceResult<SeasonTotalsResult>.Failed($"Failed to process season totals: {ex.Message}");
    }
}
```

#### 4.2 Season Statistics Object Creation
```csharp
private object CreateSeasonStatisticsObject(SeasonPlayerData playerData)
{
    var seasonStats = new
    {
        core_metrics = new
        {
            total_engagements = playerData.TotalEngagements,
            total_possessions = playerData.TotalPossessions,
            minutes_per_game = playerData.TotalMinutes > 0 && playerData.GamesPlayed > 0 ? 
                (decimal)playerData.TotalMinutes / playerData.GamesPlayed : 0
        },
        scoring_breakdown = new
        {
            total_shots = playerData.TotalShots,
            total_wides = playerData.TotalWides,
            goals_per_game = playerData.TotalGoals > 0 && playerData.GamesPlayed > 0 ? 
                (decimal)playerData.TotalGoals / playerData.GamesPlayed : 0,
            points_per_game = playerData.TotalPoints > 0 && playerData.GamesPlayed > 0 ? 
                (decimal)playerData.TotalPoints / playerData.GamesPlayed : 0
        },
        defensive_metrics = new
        {
            total_contact_tackles = playerData.TotalContactTackles,
            total_missed_tackles = playerData.TotalMissedTackles,
            tackles_per_game = playerData.TotalTackles > 0 && playerData.GamesPlayed > 0 ? 
                (decimal)playerData.TotalTackles / playerData.GamesPlayed : 0
        },
        disciplinary_record = new
        {
            total_cards = playerData.TotalYellowCards + playerData.TotalBlackCards + playerData.TotalRedCards,
            games_without_card = Math.Max(0, (playerData.GamesPlayed ?? 0) - 
                (playerData.TotalYellowCards + playerData.TotalBlackCards + playerData.TotalRedCards))
        },
        goalkeeper_metrics = playerData.GoalkeeperStats,
        extended_statistics = playerData.ExtendedStats
    };
    
    return seasonStats;
}
```

### Phase 5: Data Validation and Quality Checks

#### 5.1 Season Consistency Validation
```csharp
private async Task ValidateSeasonTotalsConsistency(
    List<SeasonPlayerData> seasonData, 
    int seasonId, 
    int jobId)
{
    foreach (var player in seasonData)
    {
        // Validate score consistency
        if (player.SeasonScores != null)
        {
            var calculatedTotal = (player.SeasonScores.Goals * 3) + player.SeasonScores.Points;
            var providedTotal = (player.TotalGoals * 3) + player.TotalPoints;
            
            if (calculatedTotal != providedTotal)
            {
                await _progressService.RecordValidationErrorAsync(
                    jobId, "Cumulative Stats 2025", null, "Score Consistency",
                    EtlErrorTypes.CONSISTENCY_ERROR,
                    $"Score inconsistency for {player.PlayerName}: calculated {calculatedTotal} vs provided {providedTotal}",
                    "Verify score calculation in season totals");
            }
        }
        
        // Validate tackle consistency
        if (player.TotalTackles > 0 && player.TotalContactTackles > 0 && player.TotalMissedTackles > 0)
        {
            var calculatedTotal = player.TotalContactTackles + player.TotalMissedTackles;
            if (Math.Abs(calculatedTotal - player.TotalTackles) > 1) // Allow for rounding
            {
                await _progressService.RecordValidationErrorAsync(
                    jobId, "Cumulative Stats 2025", null, "Tackle Consistency",
                    EtlErrorTypes.CONSISTENCY_ERROR,
                    $"Tackle count inconsistency for {player.PlayerName}: {player.TotalContactTackles} + {player.TotalMissedTackles} ≠ {player.TotalTackles}",
                    "Verify tackle calculations in season totals");
            }
        }
        
        // Validate reasonable ranges
        if (player.AvgConversionRate.HasValue && player.AvgConversionRate > 1.0m)
        {
            await _progressService.RecordValidationErrorAsync(
                jobId, "Cumulative Stats 2025", null, "Conversion Rate",
                EtlErrorTypes.INVALID_RANGE,
                $"Conversion rate > 100% for {player.PlayerName}: {player.AvgConversionRate:P1}",
                "Conversion rates should be between 0% and 100%");
        }
    }
}
```

#### 5.2 Season Performance Analysis
```csharp
private async Task AnalyzeSeasonPerformance(List<SeasonPlayerData> seasonData, int jobId)
{
    if (!seasonData.Any()) return;
    
    // Top performers analysis
    var topScorers = seasonData
        .OrderByDescending(p => (p.TotalGoals * 3) + p.TotalPoints)
        .Take(5)
        .ToList();
    
    var topTacklers = seasonData
        .OrderByDescending(p => p.TotalTackles)
        .Take(3)
        .ToList();
    
    _logger.LogInformation("Season performance analysis:");
    _logger.LogInformation("Top scorers: {TopScorers}", 
        string.Join(", ", topScorers.Select(p => $"{p.PlayerName} ({p.TotalGoals}-{p.TotalPoints:D2})")));
    _logger.LogInformation("Top tacklers: {TopTacklers}", 
        string.Join(", ", topTacklers.Select(p => $"{p.PlayerName} ({p.TotalTackles})")));
    
    // Season averages
    var avgMinutesPerPlayer = seasonData.Average(p => p.TotalMinutes);
    var avgScorePerPlayer = seasonData.Average(p => (p.TotalGoals * 3) + p.TotalPoints);
    var avgTacklesPerPlayer = seasonData.Average(p => p.TotalTackles);
    
    _logger.LogInformation(
        "Season averages: {AvgMinutes:F1} minutes, {AvgScore:F1} points, {AvgTackles:F1} tackles per player",
        avgMinutesPerPlayer, avgScorePerPlayer, avgTacklesPerPlayer);
    
    // Identify potential data quality issues
    var playersWithExcessiveMinutes = seasonData.Where(p => p.TotalMinutes > 600).ToList(); // >8 full games
    if (playersWithExcessiveMinutes.Any())
    {
        await _progressService.RecordValidationErrorAsync(
            jobId, "Cumulative Stats 2025", null, "High Minutes",
            "warning",
            $"Players with high minute totals: {string.Join(", ", playersWithExcessiveMinutes.Select(p => $"{p.PlayerName} ({p.TotalMinutes})"))}",
            "Verify minute calculations for players with unusually high totals");
    }
}
```

## Performance Considerations

### Expected Data Volume:
- **Players**: 20-30 player records
- **Processing Time Target**: < 1 minute
- **Season Statistics JSON**: ~1-2KB per player
- **Memory Usage**: Minimal (single sheet processing)

### Cleanup Strategy:
```csharp
private async Task ClearExistingSeasonTotalsAsync(int seasonId, int jobId)
{
    var existingRecords = await _context.SeasonPlayerTotals
        .Where(spt => spt.SeasonId == seasonId)
        .CountAsync();
    
    if (existingRecords > 0)
    {
        await _context.Database.ExecuteSqlRawAsync(
            "DELETE FROM season_player_totals WHERE season_id = @p0", seasonId);
        
        _logger.LogInformation(
            "Cleared {ExistingRecords} existing season total records for season ID {SeasonId}",
            existingRecords, seasonId);
    }
}
```

## Success Criteria

- ✅ All player season statistics successfully extracted and processed
- ✅ Games played calculated or estimated accurately  
- ✅ Season-level aggregations properly computed
- ✅ Player identification and matching working correctly
- ✅ Extended statistics captured in JSONB format
- ✅ Goalkeeper-specific statistics handled appropriately
- ✅ Data consistency validation implemented
- ✅ Season performance analysis and insights generated
- ✅ Efficient batch processing and error handling
- ✅ Comprehensive logging and quality metrics

This plan ensures that season-wide player statistics are accurately captured and provide comprehensive performance summaries for the entire 2025 season, enabling season-long analysis and player development tracking.