# KPI Definitions Ingestion Plan

## Executive Summary

This document outlines the detailed plan for ingesting Key Performance Indicator (KPI) definitions from the Excel file's "KPI Definitions" sheet into the `kpi_definitions` database table. This sheet contains metadata about the various metrics used in GAA statistical analysis, including calculation formulas, benchmark values, and position-specific relevance.

## Data Source Analysis

### Excel Sheet Structure
- **Source Sheet**: `KPI Definitions`
- **Data Structure**: ~50-80 rows × 8-10 columns
- **Format**: One row per KPI with definition, calculation, and benchmark data
- **Content**: Metric codes, names, descriptions, formulas, benchmarks, and position relevance

### Sample Data Structure
```
KPI Code | KPI Name           | Description                    | Calculation Method       | Excellent | Good | Average | Poor | Position Relevance
---------|-------------------|--------------------------------|--------------------------|-----------|------|---------|------|-------------------
1.0      | Kickout           | Kickout success rate          | Won/Total Kickouts       | >75%      | 60%  | 45%     | <30% | Goalkeeper
2.0      | Attacks           | Attack efficiency              | Successful/Total Attacks | >65%      | 50%  | 35%     | <20% | All
3.1      | PSR               | Possession Success Rate        | Retained/Total Poss      | >80%      | 70%  | 60%     | <50% | All
4.2      | Conversion        | Shot conversion rate           | Scores/Total Shots       | >60%      | 45%  | 30%     | <15% | Forwards
```

## Database Target Schema

### kpi_definitions Table
```sql
CREATE TABLE kpi_definitions (
    kpi_definition_id SERIAL PRIMARY KEY,
    kpi_code VARCHAR(10) NOT NULL UNIQUE,
    kpi_name VARCHAR(50) NOT NULL,
    description TEXT,
    calculation_formula TEXT,
    benchmark_values JSONB,
    position_relevance VARCHAR(100),
    data_type VARCHAR(20),
    unit_of_measure VARCHAR(20),
    is_percentage BOOLEAN DEFAULT FALSE,
    category VARCHAR(50),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

## Detailed Ingestion Mapping

### Phase 1: Sheet Detection and Structure Analysis

#### 1.1 KPI Definitions Sheet Identification
```csharp
private bool IsKpiDefinitionsSheet(string sheetName)
{
    return sheetName.Equals("KPI Definitions", StringComparison.OrdinalIgnoreCase) ||
           sheetName.Contains("KPI", StringComparison.OrdinalIgnoreCase) ||
           sheetName.Contains("Definitions", StringComparison.OrdinalIgnoreCase) ||
           sheetName.Contains("Metrics", StringComparison.OrdinalIgnoreCase);
}
```

#### 1.2 Column Mapping Configuration
```csharp
private static class KpiDefinitionsColumnMapping
{
    public const int KPI_CODE = 0;              // Column A
    public const int KPI_NAME = 1;              // Column B
    public const int DESCRIPTION = 2;           // Column C
    public const int CALCULATION_METHOD = 3;    // Column D
    public const int EXCELLENT_THRESHOLD = 4;   // Column E
    public const int GOOD_THRESHOLD = 5;        // Column F
    public const int AVERAGE_THRESHOLD = 6;     // Column G
    public const int POOR_THRESHOLD = 7;        // Column H
    public const int POSITION_RELEVANCE = 8;    // Column I
    public const int UNIT_MEASURE = 9;          // Column J (optional)
    public const int CATEGORY = 10;             // Column K (optional)
}
```

### Phase 2: Data Extraction and Processing

#### 2.1 Individual KPI Definition Processing
```csharp
private KpiDefinitionData? ProcessKpiDefinitionRow(ExcelWorksheet worksheet, int row)
{
    try
    {
        var kpiCode = worksheet.Cells[row, KpiDefinitionsColumnMapping.KPI_CODE + 1].Text?.Trim();
        var kpiName = worksheet.Cells[row, KpiDefinitionsColumnMapping.KPI_NAME + 1].Text?.Trim();
        
        // Skip empty rows
        if (string.IsNullOrEmpty(kpiCode) && string.IsNullOrEmpty(kpiName))
            return null;
        
        var description = worksheet.Cells[row, KpiDefinitionsColumnMapping.DESCRIPTION + 1].Text?.Trim();
        var calculationMethod = worksheet.Cells[row, KpiDefinitionsColumnMapping.CALCULATION_METHOD + 1].Text?.Trim();
        var positionRelevance = worksheet.Cells[row, KpiDefinitionsColumnMapping.POSITION_RELEVANCE + 1].Text?.Trim();
        
        // Extract benchmark thresholds
        var excellentText = worksheet.Cells[row, KpiDefinitionsColumnMapping.EXCELLENT_THRESHOLD + 1].Text?.Trim();
        var goodText = worksheet.Cells[row, KpiDefinitionsColumnMapping.GOOD_THRESHOLD + 1].Text?.Trim();
        var averageText = worksheet.Cells[row, KpiDefinitionsColumnMapping.AVERAGE_THRESHOLD + 1].Text?.Trim();
        var poorText = worksheet.Cells[row, KpiDefinitionsColumnMapping.POOR_THRESHOLD + 1].Text?.Trim();
        
        // Optional fields
        var unitMeasure = worksheet.Cells[row, KpiDefinitionsColumnMapping.UNIT_MEASURE + 1].Text?.Trim();
        var category = worksheet.Cells[row, KpiDefinitionsColumnMapping.CATEGORY + 1].Text?.Trim();
        
        return new KpiDefinitionData
        {
            KpiCode = kpiCode ?? string.Empty,
            KpiName = kpiName ?? string.Empty,
            Description = description,
            CalculationFormula = calculationMethod,
            PositionRelevance = positionRelevance,
            UnitOfMeasure = unitMeasure,
            Category = category,
            BenchmarkThresholds = new BenchmarkThresholds
            {
                Excellent = ParseBenchmarkValue(excellentText),
                Good = ParseBenchmarkValue(goodText),
                Average = ParseBenchmarkValue(averageText),
                Poor = ParseBenchmarkValue(poorText)
            }
        };
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Failed to process KPI definition row {Row}", row);
        return null;
    }
}
```

#### 2.2 Benchmark Value Parsing
```csharp
private BenchmarkValue? ParseBenchmarkValue(string? benchmarkText)
{
    if (string.IsNullOrEmpty(benchmarkText))
        return null;
    
    // Handle percentage values: "75%", ">80%", "<30%"
    if (benchmarkText.Contains("%"))
    {
        var cleanText = benchmarkText.Replace("%", "").Trim();
        var comparison = ExtractComparisonOperator(benchmarkText);
        
        if (decimal.TryParse(cleanText.TrimStart('>', '<', '='), out var percentValue))
        {
            return new BenchmarkValue
            {
                Value = percentValue / 100m, // Convert to decimal (0.75 instead of 75)
                ComparisonOperator = comparison,
                IsPercentage = true,
                OriginalText = benchmarkText
            };
        }
    }
    
    // Handle numeric values: "10", ">15", "<=5"
    var numericText = benchmarkText.TrimStart('>', '<', '=');
    if (decimal.TryParse(numericText, out var numericValue))
    {
        return new BenchmarkValue
        {
            Value = numericValue,
            ComparisonOperator = ExtractComparisonOperator(benchmarkText),
            IsPercentage = false,
            OriginalText = benchmarkText
        };
    }
    
    // Handle qualitative values: "High", "Medium", "Low"
    return new BenchmarkValue
    {
        QualitativeValue = benchmarkText,
        ComparisonOperator = "=",
        IsPercentage = false,
        OriginalText = benchmarkText
    };
}

private string ExtractComparisonOperator(string benchmarkText)
{
    if (benchmarkText.StartsWith(">=")) return ">=";
    if (benchmarkText.StartsWith("<=")) return "<=";
    if (benchmarkText.StartsWith(">")) return ">";
    if (benchmarkText.StartsWith("<")) return "<";
    if (benchmarkText.StartsWith("=")) return "=";
    
    return "="; // Default to equals
}
```

#### 2.3 Data Type and Category Inference
```csharp
private string InferDataType(KpiDefinitionData kpiData)
{
    // Check if any benchmark is a percentage
    var hasPercentage = kpiData.BenchmarkThresholds.Excellent?.IsPercentage == true ||
                       kpiData.BenchmarkThresholds.Good?.IsPercentage == true ||
                       kpiData.BenchmarkThresholds.Average?.IsPercentage == true ||
                       kpiData.BenchmarkThresholds.Poor?.IsPercentage == true;
    
    if (hasPercentage) return "percentage";
    
    // Check description and calculation for type hints
    var text = $"{kpiData.Description} {kpiData.CalculationFormula}".ToLowerInvariant();
    
    if (text.Contains("rate") || text.Contains("percentage") || text.Contains("success"))
        return "percentage";
    
    if (text.Contains("count") || text.Contains("total") || text.Contains("number"))
        return "count";
    
    if (text.Contains("average") || text.Contains("mean"))
        return "average";
    
    return "decimal";
}

private string InferCategory(KpiDefinitionData kpiData)
{
    if (!string.IsNullOrEmpty(kpiData.Category))
        return kpiData.Category;
    
    // Infer from KPI name and description
    var text = $"{kpiData.KpiName} {kpiData.Description}".ToLowerInvariant();
    
    if (text.Contains("kickout") || text.Contains("goalkeeper"))
        return "Goalkeeping";
    
    if (text.Contains("attack") || text.Contains("score") || text.Contains("shot"))
        return "Attacking";
    
    if (text.Contains("tackle") || text.Contains("defend") || text.Contains("intercept"))
        return "Defensive";
    
    if (text.Contains("possession") || text.Contains("pass"))
        return "Possession";
    
    if (text.Contains("discipline") || text.Contains("card") || text.Contains("foul"))
        return "Disciplinary";
    
    return "General";
}

private bool IsPercentageMetric(KpiDefinitionData kpiData)
{
    return kpiData.BenchmarkThresholds.Excellent?.IsPercentage == true ||
           kpiData.BenchmarkThresholds.Good?.IsPercentage == true ||
           InferDataType(kpiData) == "percentage";
}
```

### Phase 3: Database Integration

#### 3.1 Benchmark Values JSON Creation
```csharp
private string CreateBenchmarkValuesJson(BenchmarkThresholds thresholds)
{
    var benchmarkObject = new
    {
        excellent = CreateBenchmarkValueObject(thresholds.Excellent),
        good = CreateBenchmarkValueObject(thresholds.Good),
        average = CreateBenchmarkValueObject(thresholds.Average),
        poor = CreateBenchmarkValueObject(thresholds.Poor)
    };
    
    return JsonSerializer.Serialize(benchmarkObject, new JsonSerializerOptions 
    { 
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    });
}

private object? CreateBenchmarkValueObject(BenchmarkValue? benchmark)
{
    if (benchmark == null) return null;
    
    if (!string.IsNullOrEmpty(benchmark.QualitativeValue))
    {
        return new
        {
            type = "qualitative",
            value = benchmark.QualitativeValue,
            original = benchmark.OriginalText
        };
    }
    
    return new
    {
        type = benchmark.IsPercentage ? "percentage" : "numeric",
        value = benchmark.Value,
        comparison = benchmark.ComparisonOperator,
        is_percentage = benchmark.IsPercentage,
        original = benchmark.OriginalText
    };
}
```

#### 3.2 Database Record Creation
```csharp
private async Task<int> SaveKpiDefinitionsAsync(
    List<KpiDefinitionData> kpiDefinitions, 
    int jobId)
{
    var dbRecords = new List<KpiDefinition>();
    
    foreach (var kpiData in kpiDefinitions)
    {
        try
        {
            var dbRecord = new KpiDefinition
            {
                KpiCode = kpiData.KpiCode,
                KpiName = kpiData.KpiName,
                Description = kpiData.Description,
                CalculationFormula = kpiData.CalculationFormula,
                BenchmarkValues = CreateBenchmarkValuesJson(kpiData.BenchmarkThresholds),
                PositionRelevance = kpiData.PositionRelevance ?? "All",
                DataType = InferDataType(kpiData),
                UnitOfMeasure = kpiData.UnitOfMeasure,
                IsPercentage = IsPercentageMetric(kpiData),
                Category = InferCategory(kpiData),
                IsActive = true
            };
            
            dbRecords.Add(dbRecord);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create database record for KPI: {KpiCode}", kpiData.KpiCode);
            
            await _progressService.RecordValidationErrorAsync(
                jobId, "KPI Definitions", null, kpiData.KpiCode,
                EtlErrorTypes.PROCESSING_ERROR,
                $"Failed to process KPI definition: {ex.Message}",
                "Check KPI data format and benchmark values");
        }
    }
    
    // Clear existing KPI definitions to avoid conflicts
    await _context.Database.ExecuteSqlRawAsync("DELETE FROM kpi_definitions");
    
    // Save new KPI definitions
    await _context.KpiDefinitions.AddRangeAsync(dbRecords);
    await _context.SaveChangesAsync();
    
    _logger.LogInformation("Saved {RecordCount} KPI definitions for job {JobId}",
        dbRecords.Count, jobId);
    
    return dbRecords.Count;
}
```

### Phase 4: Main Processing Pipeline

#### 4.1 Complete KPI Definitions Processing
```csharp
public async Task<ServiceResult<KpiDefinitionsResult>> ProcessKpiDefinitionsAsync(
    ExcelWorksheet worksheet, 
    int jobId)
{
    var stopwatch = Stopwatch.StartNew();
    var processedDefinitions = 0;
    var skippedRows = 0;
    
    try
    {
        var lastRow = worksheet.Dimension?.End.Row ?? 0;
        _logger.LogInformation("Processing KPI definitions sheet with {RowCount} rows", lastRow);
        
        var kpiDefinitions = new List<KpiDefinitionData>();
        
        // Process each row starting from row 2 (skip header)
        for (int row = 2; row <= lastRow; row++)
        {
            var kpiDefinition = ProcessKpiDefinitionRow(worksheet, row);
            
            if (kpiDefinition != null)
            {
                kpiDefinitions.Add(kpiDefinition);
                processedDefinitions++;
            }
            else
            {
                skippedRows++;
            }
        }
        
        _logger.LogInformation(
            "Extracted {DefinitionCount} KPI definitions from {ProcessedRows} processed rows, {SkippedRows} skipped rows", 
            kpiDefinitions.Count, processedDefinitions, skippedRows);
        
        // Validate KPI definitions
        await ValidateKpiDefinitions(kpiDefinitions, jobId);
        
        // Save to database
        var savedRecords = await SaveKpiDefinitionsAsync(kpiDefinitions, jobId);
        
        // Generate KPI analysis report
        await GenerateKpiAnalysisReport(kpiDefinitions, jobId);
        
        stopwatch.Stop();
        
        return ServiceResult<KpiDefinitionsResult>.Success(new KpiDefinitionsResult
        {
            DefinitionsProcessed = processedDefinitions,
            RecordsCreated = savedRecords,
            SkippedRows = skippedRows,
            ProcessingTimeMs = stopwatch.ElapsedMilliseconds
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to process KPI definitions");
        
        await _progressService.RecordValidationErrorAsync(
            jobId, "KPI Definitions", null, null,
            EtlErrorTypes.PROCESSING_ERROR,
            $"Failed to process KPI definitions: {ex.Message}",
            "Check sheet format and data structure");
        
        return ServiceResult<KpiDefinitionsResult>.Failed($"Failed to process KPI definitions: {ex.Message}");
    }
}
```

### Phase 5: Data Validation and Quality Checks

#### 5.1 KPI Definition Validation
```csharp
private async Task ValidateKpiDefinitions(
    List<KpiDefinitionData> kpiDefinitions, 
    int jobId)
{
    var seenCodes = new HashSet<string>();
    
    foreach (var kpiDef in kpiDefinitions)
    {
        // Validate unique KPI codes
        if (seenCodes.Contains(kpiDef.KpiCode))
        {
            await _progressService.RecordValidationErrorAsync(
                jobId, "KPI Definitions", null, kpiDef.KpiCode,
                EtlErrorTypes.DUPLICATE_DATA,
                $"Duplicate KPI code: {kpiDef.KpiCode}",
                "Ensure KPI codes are unique across all definitions");
        }
        seenCodes.Add(kpiDef.KpiCode);
        
        // Validate required fields
        if (string.IsNullOrEmpty(kpiDef.KpiName))
        {
            await _progressService.RecordValidationErrorAsync(
                jobId, "KPI Definitions", null, kpiDef.KpiCode,
                EtlErrorTypes.MISSING_DATA,
                $"Missing KPI name for code: {kpiDef.KpiCode}",
                "KPI name is required for all definitions");
        }
        
        // Validate benchmark consistency
        await ValidateBenchmarkConsistency(kpiDef, jobId);
        
        // Validate calculation formula
        if (string.IsNullOrEmpty(kpiDef.CalculationFormula))
        {
            await _progressService.RecordValidationErrorAsync(
                jobId, "KPI Definitions", null, kpiDef.KpiCode,
                "warning",
                $"Missing calculation formula for KPI: {kpiDef.KpiCode}",
                "Consider adding calculation method for better understanding");
        }
    }
}

private async Task ValidateBenchmarkConsistency(KpiDefinitionData kpiDef, int jobId)
{
    var thresholds = kpiDef.BenchmarkThresholds;
    
    // Check if we have numeric thresholds to validate order
    var numericThresholds = new List<(string level, decimal? value)>
    {
        ("Excellent", thresholds.Excellent?.Value),
        ("Good", thresholds.Good?.Value),
        ("Average", thresholds.Average?.Value),
        ("Poor", thresholds.Poor?.Value)
    }.Where(t => t.value.HasValue).ToList();
    
    if (numericThresholds.Count >= 2)
    {
        // For most metrics, excellent should be higher than good, etc.
        // This assumes "higher is better" - some metrics might be reversed
        var isDescending = IsDescendingBenchmarkExpected(kpiDef);
        
        for (int i = 0; i < numericThresholds.Count - 1; i++)
        {
            var current = numericThresholds[i];
            var next = numericThresholds[i + 1];
            
            if (isDescending && current.value < next.value)
            {
                await _progressService.RecordValidationErrorAsync(
                    jobId, "KPI Definitions", null, kpiDef.KpiCode,
                    "warning",
                    $"Benchmark order unusual for {kpiDef.KpiCode}: {current.level} ({current.value}) < {next.level} ({next.value})",
                    "Verify benchmark thresholds are in correct order for this metric");
            }
        }
    }
}

private bool IsDescendingBenchmarkExpected(KpiDefinitionData kpiDef)
{
    // Most GAA metrics follow "higher is better" pattern
    var text = $"{kpiDef.KpiName} {kpiDef.Description}".ToLowerInvariant();
    
    // Metrics where lower is better (rare in GAA stats)
    if (text.Contains("error") || text.Contains("miss") || 
        text.Contains("foul") || text.Contains("card"))
        return false;
    
    return true; // Default: higher is better
}
```

### Phase 6: Analysis and Reporting

#### 6.1 KPI Analysis Report Generation
```csharp
private async Task GenerateKpiAnalysisReport(List<KpiDefinitionData> kpiDefinitions, int jobId)
{
    var categoryGroups = kpiDefinitions.GroupBy(k => InferCategory(k)).ToList();
    var positionGroups = kpiDefinitions.GroupBy(k => k.PositionRelevance ?? "All").ToList();
    
    _logger.LogInformation("KPI Definitions Analysis Report:");
    _logger.LogInformation("Total KPIs processed: {TotalKpis}", kpiDefinitions.Count);
    
    // Category breakdown
    _logger.LogInformation("KPIs by category:");
    foreach (var category in categoryGroups.OrderByDescending(g => g.Count()))
    {
        _logger.LogInformation("  {Category}: {Count} KPIs", category.Key, category.Count());
    }
    
    // Position relevance breakdown
    _logger.LogInformation("KPIs by position relevance:");
    foreach (var position in positionGroups.OrderByDescending(g => g.Count()))
    {
        _logger.LogInformation("  {Position}: {Count} KPIs", position.Key, position.Count());
    }
    
    // Data type analysis
    var dataTypes = kpiDefinitions.GroupBy(k => InferDataType(k)).ToList();
    _logger.LogInformation("KPIs by data type:");
    foreach (var dataType in dataTypes)
    {
        _logger.LogInformation("  {DataType}: {Count} KPIs", dataType.Key, dataType.Count());
    }
    
    // Identify KPIs with comprehensive benchmarks
    var wellDefinedKpis = kpiDefinitions.Where(k => 
        k.BenchmarkThresholds.Excellent != null && 
        k.BenchmarkThresholds.Good != null &&
        k.BenchmarkThresholds.Average != null &&
        k.BenchmarkThresholds.Poor != null).ToList();
    
    _logger.LogInformation("KPIs with complete benchmark sets: {Count}/{Total} ({Percentage:P1})",
        wellDefinedKpis.Count, kpiDefinitions.Count, 
        (double)wellDefinedKpis.Count / kpiDefinitions.Count);
    
    // Record insights for monitoring
    await _progressService.RecordProgressUpdateAsync(new EtlProgressUpdate
    {
        JobId = jobId,
        Stage = "kpi_definitions_analysis",
        Message = $"KPI analysis: {kpiDefinitions.Count} definitions, " +
                 $"{categoryGroups.Count} categories, " +
                 $"{wellDefinedKpis.Count} fully benchmarked",
        Status = "completed"
    });
}
```

#### 6.2 KPI Usage Recommendations
```csharp
private async Task GenerateKpiUsageRecommendations(
    List<KpiDefinitionData> kpiDefinitions, 
    int jobId)
{
    var recommendations = new List<string>();
    
    // Key attacking KPIs
    var attackingKpis = kpiDefinitions.Where(k => 
        InferCategory(k) == "Attacking").OrderBy(k => k.KpiCode).ToList();
    if (attackingKpis.Any())
    {
        recommendations.Add($"Primary attacking KPIs: {string.Join(", ", attackingKpis.Take(3).Select(k => k.KpiCode))}");
    }
    
    // Key defensive KPIs
    var defensiveKpis = kpiDefinitions.Where(k => 
        InferCategory(k) == "Defensive").OrderBy(k => k.KpiCode).ToList();
    if (defensiveKpis.Any())
    {
        recommendations.Add($"Primary defensive KPIs: {string.Join(", ", defensiveKpis.Take(3).Select(k => k.KpiCode))}");
    }
    
    // Position-specific recommendations
    var goalkeeperKpis = kpiDefinitions.Where(k => 
        k.PositionRelevance?.Contains("Goalkeeper") == true).ToList();
    if (goalkeeperKpis.Any())
    {
        recommendations.Add($"Goalkeeper-specific KPIs: {string.Join(", ", goalkeeperKpis.Select(k => k.KpiCode))}");
    }
    
    foreach (var recommendation in recommendations)
    {
        _logger.LogInformation("📊 KPI Recommendation: {Recommendation}", recommendation);
    }
}
```

## Performance Considerations

### Expected Data Volume:
- **KPI Definitions**: 50-100 definitions
- **Processing Time Target**: < 30 seconds
- **Memory Usage**: Minimal (metadata only)
- **JSON Benchmark Data**: ~200-500 bytes per KPI

### Optimization Strategies:
```csharp
// Efficient batch processing for KPI definitions
private async Task ProcessKpiDefinitionsBatch(
    List<KpiDefinitionData> kpiDefinitionBatch, 
    int batchNumber)
{
    _logger.LogDebug("Processing KPI definitions batch {BatchNumber} with {Count} definitions", 
        batchNumber, kpiDefinitionBatch.Count);
    
    // Process batch logic here
    
    // Allow other tasks to run
    await Task.Yield();
}
```

## Integration Points

### Service Layer Integration
```csharp
// Add to IExcelProcessingService interface
Task<ServiceResult<KpiDefinitionsResult>> ProcessKpiDefinitionsAsync(
    Stream fileStream,
    int jobId,
    CancellationToken cancellationToken = default);
```

### Progress Tracking
```csharp
// Update main processing pipeline
await _progressService.RecordProgressUpdateAsync(new EtlProgressUpdate
{
    JobId = jobId,
    Stage = EtlStages.PROCESSING_KPI_DEFINITIONS,
    TotalSteps = 1,
    CompletedSteps = 0,
    Status = "processing",
    Message = "Processing KPI definitions and benchmark data"
});
```

## Success Criteria

- ✅ All KPI definitions successfully extracted and processed
- ✅ Benchmark values properly parsed and stored as JSON
- ✅ Data types and categories accurately inferred
- ✅ Position relevance correctly captured
- ✅ Calculation formulas preserved for reference
- ✅ Duplicate KPI codes detected and handled
- ✅ Benchmark consistency validation implemented
- ✅ Comprehensive KPI analysis and reporting
- ✅ Usage recommendations generated
- ✅ Integration with existing metric systems

This plan ensures that KPI definitions are accurately captured from the Excel file and stored in a structured format that enables dynamic benchmarking, performance evaluation, and metric-driven analysis throughout the GAAStat application.