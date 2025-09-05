# Position Averages Ingestion Plan

## Executive Summary

This document outlines the detailed plan for ingesting position averages data into the `position_averages` database table. Unlike other ingestion plans, this data is **calculated and derived** from the positional analysis sheets (`Goalkeepers`, `Defenders`, `Midfielders`, `Forwards`) rather than directly extracted. The position averages represent benchmark statistics for each playing position, enabling comparative performance analysis.

## Data Source Analysis

### Source Data Origin
- **Primary Source**: Calculated from `Goalkeepers`, `Defenders`, `Midfielders`, `Forwards` sheets
- **Secondary Source**: Cross-referenced with `match_player_statistics` for validation
- **Calculation Type**: Statistical aggregation and averaging across all players in each position
- **Purpose**: Create positional benchmarks for performance evaluation

### Calculation Methodology
```
Position Average = Σ(Player Values) / Count(Players in Position)

For each position:
1. Aggregate all players in that position across all matches
2. Calculate mean, median, and standard deviation for key metrics
3. Determine percentile thresholds (25th, 50th, 75th, 90th)
4. Create benchmark ranges for performance evaluation
```

## Database Target Schema

### position_averages Table
```sql
CREATE TABLE position_averages (
    position_average_id SERIAL PRIMARY KEY,
    position_id INTEGER NOT NULL REFERENCES positions(position_id),
    season_id INTEGER REFERENCES seasons(season_id),
    metric_name VARCHAR(100) NOT NULL,
    sample_size INTEGER,
    average_value DECIMAL(10,6),
    median_value DECIMAL(10,6),
    std_deviation DECIMAL(10,6),
    percentile_25 DECIMAL(10,6),
    percentile_50 DECIMAL(10,6),
    percentile_75 DECIMAL(10,6),
    percentile_90 DECIMAL(10,6),
    min_value DECIMAL(10,6),
    max_value DECIMAL(10,6),
    confidence_level DECIMAL(5,4),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

## Detailed Calculation and Ingestion Mapping

### Phase 1: Data Collection and Preparation

#### 1.1 Player Data Aggregation by Position
```csharp
private async Task<Dictionary<int, List<PositionalPlayerMetrics>>> CollectPlayerDataByPositionAsync()
{
    var playersByPosition = new Dictionary<int, List<PositionalPlayerMetrics>>();
    
    // Get all players with position assignments
    var playersWithPositions = await _context.Players
        .Where(p => p.PositionId.HasValue && p.IsActive)
        .Include(p => p.Position)
        .ToListAsync();
    
    foreach (var player in playersWithPositions)
    {
        var positionId = player.PositionId!.Value;
        
        if (!playersByPosition.ContainsKey(positionId))
            playersByPosition[positionId] = new List<PositionalPlayerMetrics>();
        
        // Aggregate player's performance across all matches
        var playerMetrics = await CalculatePlayerAggregateMetricsAsync(player.PlayerId);
        if (playerMetrics != null)
        {
            playersByPosition[positionId].Add(playerMetrics);
        }
    }
    
    return playersByPosition;
}

private async Task<PositionalPlayerMetrics?> CalculatePlayerAggregateMetricsAsync(int playerId)
{
    var playerStats = await _context.MatchPlayerStatistics
        .Where(mps => mps.PlayerId == playerId)
        .ToListAsync();
    
    if (!playerStats.Any())
        return null;
    
    return new PositionalPlayerMetrics
    {
        PlayerId = playerId,
        GamesPlayed = playerStats.Count,
        TotalMinutes = playerStats.Sum(s => s.MinutesPlayed ?? 0),
        
        // Core performance metrics
        AvgEngagementEfficiency = CalculateAverage(playerStats, s => s.EngagementEfficiency),
        AvgPossessionSuccessRate = CalculateAverage(playerStats, s => s.PossessionSuccessRate),
        AvgConversionRate = CalculateAverage(playerStats, s => s.ConversionRate),
        AvgTackleSuccessRate = CalculateAverage(playerStats, s => s.TacklePercentage),
        
        // Totals and rates
        TotalScores = playerStats.Sum(s => (s.Goals ?? 0) * 3 + (s.Points ?? 0)),
        TotalPossessions = playerStats.Sum(s => s.TotalPossessions ?? 0),
        TotalTackles = playerStats.Sum(s => s.TacklesTotal ?? 0),
        TotalTurnovers = playerStats.Sum(s => s.TurnoversWon ?? 0),
        TotalInterceptions = playerStats.Sum(s => s.Interceptions ?? 0),
        
        // Per-game averages
        ScoresPerGame = CalculatePerGameAverage(playerStats, s => (s.Goals ?? 0) * 3 + (s.Points ?? 0)),
        PossessionsPerGame = CalculatePerGameAverage(playerStats, s => s.TotalPossessions ?? 0),
        TacklesPerGame = CalculatePerGameAverage(playerStats, s => s.TacklesTotal ?? 0),
        
        // Position-specific metrics (will be extracted based on position)
        PositionSpecificMetrics = await ExtractPositionSpecificMetricsForPlayerAsync(playerId)
    };
}

private decimal? CalculateAverage(List<MatchPlayerStatistic> stats, Func<MatchPlayerStatistic, decimal?> selector)
{
    var values = stats.Select(selector).Where(v => v.HasValue).Select(v => v.Value).ToList();
    return values.Any() ? values.Average() : null;
}

private decimal CalculatePerGameAverage(List<MatchPlayerStatistic> stats, Func<MatchPlayerStatistic, int> selector)
{
    var total = stats.Sum(selector);
    return stats.Count > 0 ? (decimal)total / stats.Count : 0;
}
```

### Phase 2: Statistical Calculations

#### 2.1 Core Statistical Analysis Functions
```csharp
private PositionAverageData CalculatePositionAverageData(
    List<PositionalPlayerMetrics> playersInPosition, 
    string metricName,
    Func<PositionalPlayerMetrics, decimal?> metricSelector)
{
    var values = playersInPosition
        .Select(metricSelector)
        .Where(v => v.HasValue)
        .Select(v => v.Value)
        .OrderBy(v => v)
        .ToList();
    
    if (!values.Any())
    {
        return new PositionAverageData
        {
            MetricName = metricName,
            SampleSize = 0,
            ConfidenceLevel = 0
        };
    }
    
    var sampleSize = values.Count;
    var average = values.Average();
    var median = CalculateMedian(values);
    var stdDeviation = CalculateStandardDeviation(values, average);
    
    return new PositionAverageData
    {
        MetricName = metricName,
        SampleSize = sampleSize,
        AverageValue = average,
        MedianValue = median,
        StdDeviation = stdDeviation,
        Percentile25 = CalculatePercentile(values, 25),
        Percentile50 = median, // Same as median
        Percentile75 = CalculatePercentile(values, 75),
        Percentile90 = CalculatePercentile(values, 90),
        MinValue = values.Min(),
        MaxValue = values.Max(),
        ConfidenceLevel = CalculateConfidenceLevel(sampleSize, stdDeviation)
    };
}

private decimal CalculateMedian(List<decimal> sortedValues)
{
    var count = sortedValues.Count;
    if (count == 0) return 0;
    
    if (count % 2 == 0)
    {
        // Even number of values - average of middle two
        return (sortedValues[count / 2 - 1] + sortedValues[count / 2]) / 2;
    }
    else
    {
        // Odd number of values - middle value
        return sortedValues[count / 2];
    }
}

private decimal CalculateStandardDeviation(List<decimal> values, decimal mean)
{
    if (values.Count <= 1) return 0;
    
    var sumOfSquaredDifferences = values.Sum(value => 
        (double)Math.Pow((double)(value - mean), 2));
    
    var variance = sumOfSquaredDifferences / (values.Count - 1); // Sample standard deviation
    return (decimal)Math.Sqrt(variance);
}

private decimal CalculatePercentile(List<decimal> sortedValues, int percentile)
{
    if (!sortedValues.Any()) return 0;
    
    var index = (percentile / 100.0) * (sortedValues.Count - 1);
    var lowerIndex = (int)Math.Floor(index);
    var upperIndex = (int)Math.Ceiling(index);
    
    if (lowerIndex == upperIndex)
        return sortedValues[lowerIndex];
    
    // Linear interpolation between values
    var weight = index - lowerIndex;
    return sortedValues[lowerIndex] + (decimal)weight * (sortedValues[upperIndex] - sortedValues[lowerIndex]);
}

private decimal CalculateConfidenceLevel(int sampleSize, decimal stdDeviation)
{
    // Simple confidence level based on sample size and variation
    if (sampleSize < 3) return 0.5m;
    if (sampleSize < 5) return 0.6m;
    if (sampleSize < 10) return 0.7m;
    if (sampleSize < 15) return 0.8m;
    
    // Adjust for variation - lower std deviation = higher confidence
    if (stdDeviation > 0.3m) return 0.75m;
    if (stdDeviation > 0.2m) return 0.85m;
    return 0.9m;
}
```

### Phase 3: Metric Definition and Processing

#### 3.1 Core Metrics for All Positions
```csharp
private readonly List<(string metricName, Func<PositionalPlayerMetrics, decimal?> selector)> CoreMetrics = new()
{
    ("avg_engagement_efficiency", p => p.AvgEngagementEfficiency),
    ("avg_possession_success_rate", p => p.AvgPossessionSuccessRate),
    ("avg_conversion_rate", p => p.AvgConversionRate),
    ("avg_tackle_success_rate", p => p.AvgTackleSuccessRate),
    ("scores_per_game", p => p.ScoresPerGame),
    ("possessions_per_game", p => p.PossessionsPerGame),
    ("tackles_per_game", p => p.TacklesPerGame),
    ("minutes_per_game", p => p.GamesPlayed > 0 ? (decimal)p.TotalMinutes / p.GamesPlayed : null),
    ("turnovers_per_game", p => p.GamesPlayed > 0 ? (decimal)p.TotalTurnovers / p.GamesPlayed : null),
    ("interceptions_per_game", p => p.GamesPlayed > 0 ? (decimal)p.TotalInterceptions / p.GamesPlayed : null)
};
```

#### 3.2 Position-Specific Metrics
```csharp
private readonly Dictionary<string, List<(string metricName, Func<PositionalPlayerMetrics, decimal?> selector)>> PositionSpecificMetrics = new()
{
    ["Goalkeeper"] = new List<(string, Func<PositionalPlayerMetrics, decimal?>)>
    {
        ("saves_per_game", p => GetPositionMetric(p, "saves_per_game")),
        ("kickout_success_rate", p => GetPositionMetric(p, "kickout_success_rate")),
        ("clean_sheets_rate", p => GetPositionMetric(p, "clean_sheets_rate")),
        ("goals_conceded_per_game", p => GetPositionMetric(p, "goals_conceded_per_game"))
    },
    
    ["Defender"] = new List<(string, Func<PositionalPlayerMetrics, decimal?>)>
    {
        ("interceptions_per_game", p => p.GamesPlayed > 0 ? (decimal)p.TotalInterceptions / p.GamesPlayed : null),
        ("clearances_per_game", p => GetPositionMetric(p, "clearances_per_game")),
        ("aerial_success_rate", p => GetPositionMetric(p, "aerial_success_rate")),
        ("defensive_actions_per_game", p => GetPositionMetric(p, "defensive_actions_per_game"))
    },
    
    ["Midfielder"] = new List<(string, Func<PositionalPlayerMetrics, decimal?>)>
    {
        ("pass_completion_rate", p => GetPositionMetric(p, "pass_completion_rate")),
        ("assists_per_game", p => GetPositionMetric(p, "assists_per_game")),
        ("key_passes_per_game", p => GetPositionMetric(p, "key_passes_per_game")),
        ("box_to_box_actions", p => GetPositionMetric(p, "box_to_box_actions"))
    },
    
    ["Forward"] = new List<(string, Func<PositionalPlayerMetrics, decimal?>)>
    {
        ("inside_50_entries_per_game", p => GetPositionMetric(p, "inside_50_entries_per_game")),
        ("target_hit_rate", p => GetPositionMetric(p, "target_hit_rate")),
        ("pressure_acts_per_game", p => GetPositionMetric(p, "pressure_acts_per_game")),
        ("ground_ball_gets_per_game", p => GetPositionMetric(p, "ground_ball_gets_per_game"))
    }
};

private decimal? GetPositionMetric(PositionalPlayerMetrics player, string metricName)
{
    return player.PositionSpecificMetrics?.TryGetValue(metricName, out var value) == true 
        ? (decimal?)value 
        : null;
}
```

### Phase 4: Main Processing Pipeline

#### 4.1 Complete Position Averages Calculation
```csharp
public async Task<ServiceResult<PositionAveragesResult>> CalculateAndSavePositionAveragesAsync(int jobId, int? seasonId = null)
{
    var stopwatch = Stopwatch.StartNew();
    var totalRecordsCreated = 0;
    var processedPositions = 0;
    
    try
    {
        _logger.LogInformation("Starting position averages calculation for job {JobId}", jobId);
        
        // Get current season if not specified
        if (!seasonId.HasValue)
        {
            seasonId = await GetCurrentSeasonIdAsync();
        }
        
        // Collect player data grouped by position
        var playersByPosition = await CollectPlayerDataByPositionAsync();
        
        if (!playersByPosition.Any())
        {
            return ServiceResult<PositionAveragesResult>.Failed("No positional player data found for calculation");
        }
        
        // Clear existing position averages for the season
        await ClearExistingPositionAveragesAsync(seasonId.Value);
        
        // Calculate averages for each position
        var positionAverageRecords = new List<PositionAverage>();
        
        foreach (var positionGroup in playersByPosition)
        {
            var positionId = positionGroup.Key;
            var players = positionGroup.Value;
            
            if (players.Count < 2)
            {
                _logger.LogWarning("Insufficient data for position {PositionId}: only {PlayerCount} player(s)", 
                    positionId, players.Count);
                continue;
            }
            
            var positionName = await GetPositionNameAsync(positionId);
            _logger.LogInformation("Calculating averages for {PositionName}: {PlayerCount} players", 
                positionName, players.Count);
            
            // Process core metrics
            foreach (var (metricName, selector) in CoreMetrics)
            {
                var averageData = CalculatePositionAverageData(players, metricName, selector);
                
                if (averageData.SampleSize > 0)
                {
                    var record = CreatePositionAverageRecord(positionId, seasonId.Value, averageData);
                    positionAverageRecords.Add(record);
                }
            }
            
            // Process position-specific metrics
            if (PositionSpecificMetrics.ContainsKey(positionName))
            {
                foreach (var (metricName, selector) in PositionSpecificMetrics[positionName])
                {
                    var averageData = CalculatePositionAverageData(players, metricName, selector);
                    
                    if (averageData.SampleSize > 0)
                    {
                        var record = CreatePositionAverageRecord(positionId, seasonId.Value, averageData);
                        positionAverageRecords.Add(record);
                    }
                }
            }
            
            processedPositions++;
        }
        
        // Save position averages in batches
        const int batchSize = 20;
        for (int i = 0; i < positionAverageRecords.Count; i += batchSize)
        {
            var batch = positionAverageRecords.Skip(i).Take(batchSize).ToList();
            await _context.PositionAverages.AddRangeAsync(batch);
            await _context.SaveChangesAsync();
            
            totalRecordsCreated += batch.Count;
            _logger.LogDebug("Saved position averages batch {BatchNumber}: {RecordCount} records", 
                i / batchSize + 1, batch.Count);
        }
        
        // Generate position benchmark report
        await GeneratePositionBenchmarkReport(positionAverageRecords, jobId);
        
        stopwatch.Stop();
        
        _logger.LogInformation(
            "Position averages calculation completed: {TotalRecords} records created for {ProcessedPositions} positions in {Duration}ms",
            totalRecordsCreated, processedPositions, stopwatch.ElapsedMilliseconds);
        
        return ServiceResult<PositionAveragesResult>.Success(new PositionAveragesResult
        {
            RecordsCreated = totalRecordsCreated,
            PositionsProcessed = processedPositions,
            SeasonId = seasonId.Value,
            ProcessingTimeMs = stopwatch.ElapsedMilliseconds
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to calculate position averages");
        
        await _progressService.RecordValidationErrorAsync(
            jobId, "Position Averages", null, null,
            EtlErrorTypes.PROCESSING_ERROR,
            $"Failed to calculate position averages: {ex.Message}",
            "Check player data availability and position assignments");
        
        return ServiceResult<PositionAveragesResult>.Failed($"Failed to calculate position averages: {ex.Message}");
    }
}

private PositionAverage CreatePositionAverageRecord(int positionId, int seasonId, PositionAverageData averageData)
{
    return new PositionAverage
    {
        PositionId = positionId,
        SeasonId = seasonId,
        MetricName = averageData.MetricName,
        SampleSize = averageData.SampleSize,
        AverageValue = averageData.AverageValue,
        MedianValue = averageData.MedianValue,
        StdDeviation = averageData.StdDeviation,
        Percentile25 = averageData.Percentile25,
        Percentile50 = averageData.Percentile50,
        Percentile75 = averageData.Percentile75,
        Percentile90 = averageData.Percentile90,
        MinValue = averageData.MinValue,
        MaxValue = averageData.MaxValue,
        ConfidenceLevel = averageData.ConfidenceLevel
    };
}
```

### Phase 5: Validation and Quality Assurance

#### 5.1 Statistical Validation
```csharp
private async Task ValidatePositionAverages(
    List<PositionAverage> positionAverages, 
    int jobId)
{
    foreach (var average in positionAverages)
    {
        // Validate statistical consistency
        if (average.MinValue > average.AverageValue || average.AverageValue > average.MaxValue)
        {
            await _progressService.RecordValidationErrorAsync(
                jobId, "Position Averages", null, average.MetricName,
                EtlErrorTypes.CONSISTENCY_ERROR,
                $"Statistical inconsistency for {average.MetricName}: min({average.MinValue}) > avg({average.AverageValue}) > max({average.MaxValue})",
                "Verify calculation logic for statistical measures");
        }
        
        // Validate percentile ordering
        if (average.Percentile25 > average.Percentile75)
        {
            await _progressService.RecordValidationErrorAsync(
                jobId, "Position Averages", null, average.MetricName,
                EtlErrorTypes.CONSISTENCY_ERROR,
                $"Percentile ordering error for {average.MetricName}: P25({average.Percentile25}) > P75({average.Percentile75})",
                "Check percentile calculation implementation");
        }
        
        // Validate sample size adequacy
        if (average.SampleSize < 3)
        {
            await _progressService.RecordValidationErrorAsync(
                jobId, "Position Averages", null, average.MetricName,
                "warning",
                $"Small sample size for {average.MetricName}: {average.SampleSize} players",
                "Consider collecting more data for reliable averages");
        }
    }
}
```

### Phase 6: Benchmark Reporting

#### 6.1 Position Benchmark Report Generation
```csharp
private async Task GeneratePositionBenchmarkReport(
    List<PositionAverage> positionAverages, 
    int jobId)
{
    var positionGroups = positionAverages.GroupBy(pa => pa.PositionId).ToList();
    
    _logger.LogInformation("Position Benchmark Report:");
    
    foreach (var positionGroup in positionGroups)
    {
        var positionName = await GetPositionNameAsync(positionGroup.Key);
        var metrics = positionGroup.ToList();
        
        _logger.LogInformation("📊 {PositionName} Benchmarks ({MetricCount} metrics):", 
            positionName, metrics.Count);
        
        // Key performance metrics for this position
        var keyMetrics = metrics.Where(m => 
            m.MetricName.Contains("success_rate") || 
            m.MetricName.Contains("per_game") ||
            m.MetricName.Contains("efficiency")).ToList();
        
        foreach (var metric in keyMetrics.Take(5)) // Top 5 key metrics
        {
            _logger.LogInformation(
                "  {MetricName}: avg={AvgValue:F2}, P75={P75:F2}, P90={P90:F2} (n={SampleSize})",
                metric.MetricName, metric.AverageValue, metric.Percentile75, 
                metric.Percentile90, metric.SampleSize);
        }
        
        // Identify standout metrics (high variation)
        var highVariationMetrics = metrics
            .Where(m => m.StdDeviation > 0.2m * Math.Abs(m.AverageValue ?? 0))
            .OrderByDescending(m => m.StdDeviation)
            .Take(2);
        
        foreach (var metric in highVariationMetrics)
        {
            _logger.LogInformation(
                "  ⚠️  High variation in {MetricName}: std={StdDev:F2} (avg={Avg:F2})",
                metric.MetricName, metric.StdDeviation, metric.AverageValue);
        }
    }
    
    // Cross-position comparisons
    await GenerateCrossPositionComparisons(positionAverages, jobId);
}

private async Task GenerateCrossPositionComparisons(
    List<PositionAverage> positionAverages, 
    int jobId)
{
    var coreMetrics = positionAverages
        .Where(pa => CoreMetrics.Any(cm => cm.metricName == pa.MetricName))
        .GroupBy(pa => pa.MetricName)
        .Where(g => g.Count() > 1) // Multiple positions have this metric
        .ToList();
    
    _logger.LogInformation("Cross-Position Comparisons:");
    
    foreach (var metricGroup in coreMetrics.Take(3)) // Top 3 comparable metrics
    {
        _logger.LogInformation("🔄 {MetricName} across positions:", metricGroup.Key);
        
        var orderedPositions = metricGroup.OrderByDescending(pa => pa.AverageValue).ToList();
        
        foreach (var positionAvg in orderedPositions)
        {
            var positionName = await GetPositionNameAsync(positionAvg.PositionId);
            _logger.LogInformation("  {PositionName}: {AvgValue:F2} ± {StdDev:F2}",
                positionName, positionAvg.AverageValue, positionAvg.StdDeviation);
        }
    }
}
```

### Phase 7: Helper Functions

#### 7.1 Utility Functions
```csharp
private async Task<int> GetCurrentSeasonIdAsync()
{
    var currentSeason = await _context.Seasons
        .OrderByDescending(s => s.StartDate)
        .FirstOrDefaultAsync();
    
    return currentSeason?.SeasonId ?? 1;
}

private async Task<string> GetPositionNameAsync(int positionId)
{
    var position = await _context.Positions.FirstOrDefaultAsync(p => p.PositionId == positionId);
    return position?.PositionName ?? "Unknown";
}

private async Task ClearExistingPositionAveragesAsync(int seasonId)
{
    var existingCount = await _context.PositionAverages
        .Where(pa => pa.SeasonId == seasonId)
        .CountAsync();
    
    if (existingCount > 0)
    {
        await _context.Database.ExecuteSqlRawAsync(
            "DELETE FROM position_averages WHERE season_id = @p0", seasonId);
        
        _logger.LogInformation("Cleared {ExistingCount} existing position average records for season {SeasonId}",
            existingCount, seasonId);
    }
}
```

## Performance Considerations

### Expected Data Volume:
- **Positions**: 4 positions (Goalkeeper, Defender, Midfielder, Forward)
- **Metrics per Position**: 10-15 core + position-specific metrics
- **Total Records**: ~50-60 position average records
- **Processing Time Target**: < 2 minutes

### Memory Optimization:
- Process positions sequentially to manage memory usage
- Use streaming calculations for large datasets
- Dispose of intermediate collections promptly

## Success Criteria

- ✅ Position averages calculated for all active positions
- ✅ Core and position-specific metrics properly processed
- ✅ Statistical measures (mean, median, percentiles) accurately computed
- ✅ Sample sizes adequate for reliable benchmarks (n≥3)
- ✅ Statistical consistency validation passed
- ✅ Cross-position comparative analysis generated
- ✅ Benchmark reports provide actionable insights
- ✅ Confidence levels appropriately assigned
- ✅ Integration with existing positional analysis data
- ✅ Performance targets met for calculation time

This plan ensures that position averages are accurately calculated from existing player performance data, providing valuable benchmark statistics that enable comparative performance evaluation and position-specific analysis within the GAAStat application.