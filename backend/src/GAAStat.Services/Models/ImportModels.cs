namespace GAAStat.Services.Models;

/// <summary>
/// Summary of Excel import operation results
/// </summary>
public class ImportSummary
{
    public int ImportId { get; set; }
    public string? FileName { get; set; }
    public long FileSizeBytes { get; set; }
    public int MatchesImported { get; set; }
    public int PlayersProcessed { get; set; }
    public int StatisticsRecordsCreated { get; set; }
    public TimeSpan ProcessingDuration { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public IEnumerable<string> ValidationWarnings { get; set; } = new List<string>();
    public int? SnapshotId { get; set; }
}

/// <summary>
/// Excel file validation results
/// </summary>
public class ExcelValidationResult
{
    public bool IsValid { get; set; }
    public int SheetsFound { get; set; }
    public int ExpectedSheets { get; set; }
    public int MatchSheetsFound { get; set; }
    public int PlayerStatsSheetsFound { get; set; }
    public IEnumerable<string> MissingSheets { get; set; } = new List<string>();
    public IEnumerable<string> ValidationErrors { get; set; } = new List<string>();
    public IEnumerable<string> ValidationWarnings { get; set; } = new List<string>();
    public Dictionary<string, SheetValidationResult> SheetValidations { get; set; } = new();
}

/// <summary>
/// Individual sheet validation results
/// </summary>
public class SheetValidationResult
{
    public string SheetName { get; set; } = string.Empty;
    public SheetType SheetType { get; set; }
    public bool IsValid { get; set; }
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
    public IEnumerable<string> Errors { get; set; } = new List<string>();
    public IEnumerable<string> Warnings { get; set; } = new List<string>();
}

/// <summary>
/// Comprehensive types of sheets in GAA Excel files (31 total sheet types)
/// </summary>
public enum SheetType
{
    // Core match data sheets
    MatchStatistics,
    PlayerStatistics,
    
    // Template and blank sheets
    BlankMatchTemplate,
    BlankPlayerStatsTemplate,
    
    // Data processing and import sheets
    CsvDataFile,
    DataProcessing,
    ImportValidation,
    
    // Cumulative and season statistics
    CumulativeStats,
    CumulativePlayerStats,
    CumulativeMatchStats,
    SeasonSummary,
    
    // Position-specific analysis sheets
    Goalkeepers,
    Defenders,
    Midfielders,
    Forwards,
    
    // Specialized analysis sheets
    PlayerMatrix,
    KickoutAnalysis,
    KickoutStats,
    KickoutRetention,
    
    // Shot analysis sheets
    ShotsFromPlay,
    ShotsAnalysis,
    ScoreableFree,
    FreeKickAnalysis,
    
    // Performance and KPI sheets
    KpiDefinitions,
    PerformanceMetrics,
    PlayerRankings,
    TeamComparisons,
    
    // Additional specialized sheets
    TacticalAnalysis,
    SubstitutionAnalysis,
    
    // Unknown or unclassified
    Unknown
}

/// <summary>
/// Import history record for UI display
/// </summary>
public class ImportHistoryDto
{
    public int Id { get; set; }
    public string ImportType { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public int? MatchesImported { get; set; }
    public int? PlayersProcessed { get; set; }
    public DateTime ImportStartedAt { get; set; }
    public DateTime? ImportCompletedAt { get; set; }
    public string ImportStatus { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int? SnapshotId { get; set; }
    public TimeSpan? ProcessingDuration { get; set; }
}

/// <summary>
/// Raw player statistics data from Excel sheet
/// </summary>
public class PlayerStatisticsRow
{
    // Core player information
    public int? JerseyNumber { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public int? MinutesPlayed { get; set; }

    // Performance metrics
    public int? TotalEvents { get; set; }
    public decimal? EventsPerPsr { get; set; }
    public string? Scores { get; set; } // Format: "0-01", "1-02"
    public decimal? PerformanceSuccessRate { get; set; }
    public decimal? PsrPerTotalPossessions { get; set; }
    public int? TotalPossessions { get; set; }

    // Ball handling & possession
    public int? TurnoversWon { get; set; }
    public int? Interceptions { get; set; }
    public int? TotalPossessionsLost { get; set; }
    public int? KickPasses { get; set; }
    public int? HandPasses { get; set; }
    public int? HandlingErrors { get; set; }

    // Defensive actions
    public int? Turnovers { get; set; }
    public int? InterceptionsDefensive { get; set; }
    public int? SuccessfulTackles { get; set; }
    public int? Saves { get; set; }

    // Scoring statistics - from play (columns T-AB)
    public int? Fouls { get; set; }
    public int? Wides { get; set; }
    public int? KickoutWins { get; set; }
    public int? WinCategories { get; set; }
    public int? TotalAttempts { get; set; }

    // Scoring statistics - from frees (columns AC-AK)
    public int? KickRetained { get; set; }
    public int? KickLost { get; set; }
    public int? FreesTotals { get; set; }

    // Specialized statistics (columns AH-BC)
    public int? Points { get; set; }
    public int? TwoPointers { get; set; }
    public int? Goals { get; set; }
    public int? ShotsWide { get; set; }
    public int? ShotsSaved { get; set; }
    public int? ShotsShort { get; set; }

    // Disciplinary (columns BD-BI)
    public int? YellowCards { get; set; }
    public int? BlackCards { get; set; }
    public int? RedCards { get; set; }

    // Kickout analysis (columns BJ-BP, primarily for goalkeepers)
    public int? KickoutsWon { get; set; }
    public int? KickoutsLost { get; set; }
    public int? TotalKickouts { get; set; }
    public int? KickoutsRight { get; set; }
    public int? KickoutsLeft { get; set; }
    public decimal? KickoutRetentionPercentage { get; set; }
    public int? GoalkeeperSaves { get; set; }

    // Excel sheet reference for audit
    public string SheetName { get; set; } = string.Empty;
    public int RowNumber { get; set; }
}

/// <summary>
/// Real-time import progress tracking
/// </summary>
public class ImportProgress
{
    public int ImportId { get; set; }
    public string CurrentPhase { get; set; } = string.Empty;
    public string CurrentOperation { get; set; } = string.Empty;
    public int TotalSheets { get; set; }
    public int ProcessedSheets { get; set; }
    public int TotalRows { get; set; }
    public int ProcessedRows { get; set; }
    public decimal OverallProgress { get; set; }
    public decimal PhaseProgress { get; set; }
    public DateTime StartedAt { get; set; }
    public TimeSpan EstimatedRemaining { get; set; }
    public string? CurrentSheetName { get; set; }
    public SheetType? CurrentSheetType { get; set; }
    public IEnumerable<string> Warnings { get; set; } = new List<string>();
    public ImportPerformanceMetrics Metrics { get; set; } = new();
}

/// <summary>
/// Import performance tracking metrics
/// </summary>
public class ImportPerformanceMetrics
{
    public double SheetsPerSecond { get; set; }
    public double RowsPerSecond { get; set; }
    public long MemoryUsageBytes { get; set; }
    public double CpuUsagePercent { get; set; }
    public int DatabaseConnections { get; set; }
    public TimeSpan DatabaseResponseTime { get; set; }
    public int ValidationErrors { get; set; }
    public int ValidationWarnings { get; set; }
}

/// <summary>
/// Bulk operation configuration for performance optimization
/// </summary>
public class BulkOperationConfig
{
    public int BatchSize { get; set; } = 1000;
    public int MaxConcurrentBatches { get; set; } = 4;
    public bool EnableBulkInsert { get; set; } = true;
    public bool UseConnectionPooling { get; set; } = true;
    public int ConnectionPoolSize { get; set; } = 10;
    public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public bool OptimizeTransactionScope { get; set; } = true;
    public bool EnableParallelProcessing { get; set; } = true;
}

/// <summary>
/// Comprehensive cross-sheet validation result
/// </summary>
public class CrossSheetValidationResult
{
    public bool IsValid { get; set; }
    public IEnumerable<string> ValidationErrors { get; set; } = new List<string>();
    public IEnumerable<string> ValidationWarnings { get; set; } = new List<string>();
    public IEnumerable<string> CrossReferenceErrors { get; set; } = new List<string>();
    public Dictionary<string, SheetValidationResult> SheetResults { get; set; } = new();
    public ConsistencyCheckResult ConsistencyCheck { get; set; } = new();
    public DataIntegrityResult IntegrityCheck { get; set; } = new();
}

/// <summary>
/// Data consistency validation across sheets
/// </summary>
public class ConsistencyCheckResult
{
    public bool PlayerNamesConsistent { get; set; }
    public bool JerseyNumbersConsistent { get; set; }
    public bool MatchDatesConsistent { get; set; }
    public bool TeamNamesConsistent { get; set; }
    public bool ScoreConsistency { get; set; }
    public IEnumerable<string> InconsistencyDetails { get; set; } = new List<string>();
}

/// <summary>
/// Data integrity validation result
/// </summary>
public class DataIntegrityResult
{
    public bool TotalsMatch { get; set; }
    public bool CalculationsValid { get; set; }
    public bool ReferencesValid { get; set; }
    public bool NoOrphanedRecords { get; set; }
    public IEnumerable<string> IntegrityIssues { get; set; } = new List<string>();
    public Dictionary<string, decimal> CalculatedTotals { get; set; } = new();
}

/// <summary>
/// Match data extracted from Excel sheet
/// </summary>
public class MatchDataRow
{
    public int? MatchNumber { get; set; }
    public string Competition { get; set; } = string.Empty;
    public string HomeTeam { get; set; } = string.Empty;
    public string AwayTeam { get; set; } = string.Empty;
    public DateTime MatchDate { get; set; }
    public string? Venue { get; set; }
    public string? HomeScore { get; set; } // Format: "2-06"
    public string? AwayScore { get; set; } // Format: "1-05"
    public string SheetName { get; set; } = string.Empty;
    
    // Parsed score components
    public int? HomeGoals { get; set; }
    public int? HomePoints { get; set; }
    public int? AwayGoals { get; set; }
    public int? AwayPoints { get; set; }
}

/// <summary>
/// Comprehensive import result covering all sheet types
/// </summary>
public class ComprehensiveImportResult
{
    public int ImportId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    
    // Processing statistics
    public int TotalSheetsProcessed { get; set; }
    public int MatchSheetsProcessed { get; set; }
    public int PlayerStatsSheetsProcessed { get; set; }
    public int CumulativeSheetsProcessed { get; set; }
    public int AnalysisSheetsProcessed { get; set; }
    
    // Data statistics
    public int MatchesImported { get; set; }
    public int PlayersProcessed { get; set; }
    public int CumulativeRecordsCreated { get; set; }
    public int AnalysisRecordsCreated { get; set; }
    public int TotalRecordsCreated { get; set; }
    
    // Performance metrics
    public ImportPerformanceMetrics PerformanceMetrics { get; set; } = new();
    public Dictionary<SheetType, SheetProcessingResult> SheetResults { get; set; } = new();
    public IEnumerable<string> ProcessingWarnings { get; set; } = new List<string>();
    public int? SnapshotId { get; set; }
}

/// <summary>
/// Processing result for individual sheet type
/// </summary>
public class SheetProcessingResult
{
    public SheetType SheetType { get; set; }
    public int SheetsProcessed { get; set; }
    public int RowsProcessed { get; set; }
    public int RecordsCreated { get; set; }
    public bool IsSuccessful { get; set; }
    public TimeSpan ProcessingDuration { get; set; }
    public IEnumerable<string> Errors { get; set; } = new List<string>();
    public IEnumerable<string> Warnings { get; set; } = new List<string>();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Import cancellation operation result
/// </summary>
public class ImportCancellationResult
{
    public int ImportId { get; set; }
    public bool WasCancelled { get; set; }
    public bool RollbackPerformed { get; set; }
    public bool RollbackSuccessful { get; set; }
    public DateTime CancelledAt { get; set; }
    public TimeSpan ProcessingTimeBeforeCancellation { get; set; }
    public string CancellationReason { get; set; } = string.Empty;
    public int RecordsProcessedBeforeCancellation { get; set; }
    public int SheetsProcessedBeforeCancellation { get; set; }
    public string? RollbackErrorMessage { get; set; }
}

/// <summary>
/// Import performance statistics for monitoring
/// </summary>
public class ImportPerformanceStats
{
    public int TotalImports { get; set; }
    public int SuccessfulImports { get; set; }
    public int FailedImports { get; set; }
    public int CancelledImports { get; set; }
    public decimal SuccessRate { get; set; }
    public TimeSpan AverageImportDuration { get; set; }
    public TimeSpan FastestImport { get; set; }
    public TimeSpan SlowestImport { get; set; }
    public double AverageFileSize { get; set; }
    public double AverageRecordsPerSecond { get; set; }
    public IEnumerable<PerformanceRecommendation> Recommendations { get; set; } = new List<PerformanceRecommendation>();
    public Dictionary<string, int> ErrorFrequency { get; set; } = new();
    public IEnumerable<ImportTrend> Trends { get; set; } = new List<ImportTrend>();
}

/// <summary>
/// Performance optimization recommendation
/// </summary>
public class PerformanceRecommendation
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty; // Low, Medium, High, Critical
    public string? ActionRequired { get; set; }
    public decimal PotentialImprovementPercent { get; set; }
}

/// <summary>
/// Import performance trend analysis
/// </summary>
public class ImportTrend
{
    public DateTime Period { get; set; }
    public int ImportCount { get; set; }
    public TimeSpan AverageDuration { get; set; }
    public decimal SuccessRate { get; set; }
    public double AverageFileSize { get; set; }
    public string Trend { get; set; } = string.Empty; // Improving, Stable, Degrading
}

/// <summary>
/// Result of bulk database operation with performance metrics
/// </summary>
public class BulkOperationResult
{
    public int RecordsProcessed { get; set; }
    public int RecordsInserted { get; set; }
    public int RecordsSkipped { get; set; }
    public int RecordsFailed { get; set; }
    public TimeSpan OperationDuration { get; set; }
    public double RecordsPerSecond { get; set; }
    public bool IsSuccessful { get; set; }
    public IEnumerable<string> Errors { get; set; } = new List<string>();
    public IEnumerable<string> Warnings { get; set; } = new List<string>();
    public BulkOperationMetrics Metrics { get; set; } = new();
}

/// <summary>
/// Result of bulk upsert operation with insert/update breakdown
/// </summary>
public class BulkUpsertResult : BulkOperationResult
{
    public int RecordsUpdated { get; set; }
    public int NewRecordsInserted { get; set; }
    public int DuplicatesFound { get; set; }
    public Dictionary<string, int> UpsertBreakdown { get; set; } = new();
}

/// <summary>
/// Result of bulk clear operation
/// </summary>
public class BulkClearResult
{
    public int TablesCleared { get; set; }
    public int TotalRecordsDeleted { get; set; }
    public TimeSpan OperationDuration { get; set; }
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, int> RecordsDeletedByTable { get; set; } = new();
    public IEnumerable<string> TablesProcessed { get; set; } = new List<string>();
}

/// <summary>
/// Performance metrics for bulk operations
/// </summary>
public class BulkOperationMetrics
{
    public long MemoryUsedBytes { get; set; }
    public double CpuUsagePercent { get; set; }
    public int DatabaseConnections { get; set; }
    public TimeSpan AverageQueryTime { get; set; }
    public int BatchesProcessed { get; set; }
    public int OptimalBatchSize { get; set; }
    public bool ConnectionPoolOptimal { get; set; }
    public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
}

/// <summary>
/// Real-time progress tracking for bulk operations
/// </summary>
public class BulkOperationProgress
{
    public string Operation { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public int ProcessedRecords { get; set; }
    public int CurrentBatch { get; set; }
    public int TotalBatches { get; set; }
    public decimal OverallProgress { get; set; }
    public double RecordsPerSecond { get; set; }
    public TimeSpan EstimatedRemaining { get; set; }
    public string? CurrentTableBeingProcessed { get; set; }
    public BulkOperationMetrics CurrentMetrics { get; set; } = new();
}

/// <summary>
/// Database connection pool statistics
/// </summary>
public class ConnectionPoolStats
{
    public int TotalConnections { get; set; }
    public int ActiveConnections { get; set; }
    public int IdleConnections { get; set; }
    public int AvailableConnections { get; set; }
    public TimeSpan AverageConnectionTime { get; set; }
    public int ConnectionsCreatedPerSecond { get; set; }
    public int ConnectionsDisposedPerSecond { get; set; }
    public bool IsPoolStressed { get; set; }
    public IEnumerable<string> Recommendations { get; set; } = new List<string>();
    public Dictionary<string, object> PoolMetrics { get; set; } = new();
}

/// <summary>
/// Connection pool optimization result
/// </summary>
public class PoolOptimizationResult
{
    public bool OptimizationPerformed { get; set; }
    public int PreviousPoolSize { get; set; }
    public int NewPoolSize { get; set; }
    public int PreviousMinPoolSize { get; set; }
    public int NewMinPoolSize { get; set; }
    public TimeSpan OptimizationDuration { get; set; }
    public decimal PerformanceImprovementPercent { get; set; }
    public IEnumerable<string> OptimizationActions { get; set; } = new List<string>();
    public string? RecommendedConfiguration { get; set; }
}

/// <summary>
/// Advanced Excel processing configuration
/// </summary>
public class AdvancedProcessingConfig
{
    public bool EnableParallelSheetProcessing { get; set; } = true;
    public int MaxConcurrentSheets { get; set; } = 4;
    public bool ProcessCumulativeSheets { get; set; } = true;
    public bool ProcessAnalysisSheets { get; set; } = true;
    public bool EnableCrossSheetValidation { get; set; } = true;
    public ValidationLevel ValidationLevel { get; set; } = ValidationLevel.Standard;
    public TimeSpan ProcessingTimeout { get; set; } = TimeSpan.FromMinutes(10);
    public bool GenerateInsights { get; set; } = true;
    public bool OptimizeForPerformance { get; set; } = true;
    public BulkOperationConfig BulkOperationConfig { get; set; } = new();
}

/// <summary>
/// Validation levels for Excel processing
/// </summary>
public enum ValidationLevel
{
    Basic,
    Standard,
    Comprehensive,
    Full
}

/// <summary>
/// Player positions for position-specific analysis
/// </summary>
public enum PlayerPosition
{
    Goalkeeper,
    Defender,
    Midfielder,
    Forward,
    All
}

/// <summary>
/// Types of specialized analysis
/// </summary>
public enum AnalysisType
{
    KpiDefinitions,
    ShotsFromPlay,
    ScoreableFree,
    KickoutAnalysis,
    PlayerMatrix,
    TacticalAnalysis,
    SubstitutionAnalysis
}

/// <summary>
/// Excel processing progress with detailed sheet-level tracking
/// </summary>
public class ExcelProcessingProgress
{
    public string CurrentPhase { get; set; } = string.Empty;
    public string CurrentSheetName { get; set; } = string.Empty;
    public SheetType CurrentSheetType { get; set; }
    public int TotalSheets { get; set; }
    public int ProcessedSheets { get; set; }
    public int TotalRows { get; set; }
    public int ProcessedRows { get; set; }
    public decimal OverallProgress { get; set; }
    public decimal CurrentSheetProgress { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public TimeSpan EstimatedRemaining { get; set; }
    public double SheetsPerSecond { get; set; }
    public double RowsPerSecond { get; set; }
    public IEnumerable<string> CurrentWarnings { get; set; } = new List<string>();
    public ExcelProcessingMetrics Metrics { get; set; } = new();
}

/// <summary>
/// Performance metrics for Excel processing
/// </summary>
public class ExcelProcessingMetrics
{
    public long MemoryUsageBytes { get; set; }
    public double CpuUsagePercent { get; set; }
    public int ActiveThreads { get; set; }
    public TimeSpan AverageSheetProcessingTime { get; set; }
    public Dictionary<SheetType, TimeSpan> ProcessingTimeByType { get; set; } = new();
    public Dictionary<string, object> PerformanceCounters { get; set; } = new();
}

/// <summary>
/// Comprehensive processing result covering all sheet types
/// </summary>
public class ComprehensiveProcessingResult
{
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public TimeSpan TotalDuration { get; set; }
    
    // Sheet processing summary
    public int TotalSheetsFound { get; set; }
    public int TotalSheetsProcessed { get; set; }
    public int TotalSheetsSkipped { get; set; }
    public int TotalSheetsFailed { get; set; }
    
    // Data summary
    public int TotalRowsProcessed { get; set; }
    public int TotalRecordsCreated { get; set; }
    public int ValidationErrors { get; set; }
    public int ValidationWarnings { get; set; }
    
    // Processing details by sheet type
    public Dictionary<SheetType, SheetTypeProcessingResult> ProcessingResults { get; set; } = new();
    public CrossSheetValidationResult? CrossSheetValidation { get; set; }
    public ComprehensiveInsightsResult? GeneratedInsights { get; set; }
    public ExcelProcessingMetrics PerformanceMetrics { get; set; } = new();
}

/// <summary>
/// Processing result for a specific sheet type
/// </summary>
public class SheetTypeProcessingResult
{
    public SheetType SheetType { get; set; }
    public int SheetsFound { get; set; }
    public int SheetsProcessed { get; set; }
    public int RowsProcessed { get; set; }
    public int RecordsCreated { get; set; }
    public bool IsSuccessful { get; set; }
    public TimeSpan ProcessingDuration { get; set; }
    public IEnumerable<string> Errors { get; set; } = new List<string>();
    public IEnumerable<string> Warnings { get; set; } = new List<string>();
    public Dictionary<string, object> ProcessingMetadata { get; set; } = new();
}

/// <summary>
/// Advanced validation result with comprehensive diagnostics
/// </summary>
public class AdvancedValidationResult
{
    public bool IsValid { get; set; }
    public ValidationLevel ValidationLevel { get; set; }
    public int TotalSheetsValidated { get; set; }
    public int ValidationRulesApplied { get; set; }
    public DateTime ValidationPerformed { get; set; }
    public TimeSpan ValidationDuration { get; set; }
    
    // Validation results by category
    public Dictionary<SheetType, SheetValidationResult> SheetValidations { get; set; } = new();
    public CrossSheetValidationResult CrossSheetValidation { get; set; } = new();
    public DataQualityAssessment DataQuality { get; set; } = new();
    public IEnumerable<ValidationRecommendation> Recommendations { get; set; } = new List<ValidationRecommendation>();
}

/// <summary>
/// Cumulative statistics processing result
/// </summary>
public class CumulativeProcessingResult
{
    public bool IsSuccessful { get; set; }
    public int CumulativeSheetsProcessed { get; set; }
    public int SeasonRecordsCreated { get; set; }
    public int PlayerCumulativeRecordsCreated { get; set; }
    public int MatchCumulativeRecordsCreated { get; set; }
    public TimeSpan ProcessingDuration { get; set; }
    public IEnumerable<string> ProcessedSheetNames { get; set; } = new List<string>();
    public IEnumerable<string> Errors { get; set; } = new List<string>();
    public Dictionary<string, object> CumulativeStatistics { get; set; } = new();
}

/// <summary>
/// Position-specific analysis result
/// </summary>
public class PositionAnalysisResult
{
    public bool IsSuccessful { get; set; }
    public IEnumerable<PlayerPosition> PositionsProcessed { get; set; } = new List<PlayerPosition>();
    public Dictionary<PlayerPosition, PositionStatistics> PositionStats { get; set; } = new();
    public int TotalPlayersAnalyzed { get; set; }
    public TimeSpan ProcessingDuration { get; set; }
    public IEnumerable<string> Insights { get; set; } = new List<string>();
}

/// <summary>
/// Position-specific statistics
/// </summary>
public class PositionStatistics
{
    public PlayerPosition Position { get; set; }
    public int PlayerCount { get; set; }
    public Dictionary<string, decimal> AverageStats { get; set; } = new();
    public Dictionary<string, decimal> TopPerformers { get; set; } = new();
    public Dictionary<string, object> PositionSpecificMetrics { get; set; } = new();
}

/// <summary>
/// Specialized analysis processing result
/// </summary>
public class SpecializedAnalysisResult
{
    public bool IsSuccessful { get; set; }
    public IEnumerable<AnalysisType> AnalysisTypesProcessed { get; set; } = new List<AnalysisType>();
    public Dictionary<AnalysisType, AnalysisTypeResult> AnalysisResults { get; set; } = new();
    public TimeSpan ProcessingDuration { get; set; }
    public IEnumerable<string> KeyInsights { get; set; } = new List<string>();
}

/// <summary>
/// Result for specific analysis type
/// </summary>
public class AnalysisTypeResult
{
    public AnalysisType AnalysisType { get; set; }
    public bool IsSuccessful { get; set; }
    public int RecordsProcessed { get; set; }
    public Dictionary<string, object> Results { get; set; } = new();
    public IEnumerable<string> Insights { get; set; } = new List<string>();
    public IEnumerable<string> Errors { get; set; } = new List<string>();
}

/// <summary>
/// Sheet classification result
/// </summary>
public class SheetClassificationResult
{
    public int TotalSheets { get; set; }
    public int ClassifiedSheets { get; set; }
    public int UnclassifiedSheets { get; set; }
    public Dictionary<SheetType, IEnumerable<string>> SheetsByType { get; set; } = new();
    public IEnumerable<string> UnclassifiedSheetNames { get; set; } = new List<string>();
    public Dictionary<string, double> ClassificationConfidence { get; set; } = new();
}

/// <summary>
/// Consistency validation result across sheets
/// </summary>
public class ConsistencyValidationResult
{
    public bool IsConsistent { get; set; }
    public int ValidationRulesApplied { get; set; }
    public int InconsistenciesFound { get; set; }
    public Dictionary<string, IEnumerable<string>> InconsistenciesByType { get; set; } = new();
    public DataReferentialIntegrity ReferentialIntegrity { get; set; } = new();
    public IEnumerable<string> Recommendations { get; set; } = new List<string>();
}

/// <summary>
/// Data referential integrity assessment
/// </summary>
public class DataReferentialIntegrity
{
    public bool PlayerNamesConsistent { get; set; }
    public bool JerseyNumbersValid { get; set; }
    public bool MatchDatesConsistent { get; set; }
    public bool TeamNamesConsistent { get; set; }
    public bool ScoreTotalsMatch { get; set; }
    public bool StatisticalTotalsMatch { get; set; }
    public IEnumerable<string> IntegrityIssues { get; set; } = new List<string>();
}

/// <summary>
/// Data quality assessment result
/// </summary>
public class DataQualityAssessment
{
    public decimal OverallQualityScore { get; set; }
    public decimal CompletenessScore { get; set; }
    public decimal AccuracyScore { get; set; }
    public decimal ConsistencyScore { get; set; }
    public int MissingValuesCount { get; set; }
    public int InvalidValuesCount { get; set; }
    public int DuplicateRecordsCount { get; set; }
    public Dictionary<string, decimal> QualityScoresBySheet { get; set; } = new();
    public IEnumerable<string> QualityIssues { get; set; } = new List<string>();
}

/// <summary>
/// Validation recommendation
/// </summary>
public class ValidationRecommendation
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty; // Low, Medium, High, Critical
    public string? ActionRequired { get; set; }
    public IEnumerable<string> AffectedSheets { get; set; } = new List<string>();
}

/// <summary>
/// Validation rule for data consistency
/// </summary>
public class ValidationRule
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ValidationRuleType Type { get; set; }
    public string? Configuration { get; set; }
    public bool IsEnabled { get; set; } = true;
    public IEnumerable<SheetType> ApplicableSheetTypes { get; set; } = new List<SheetType>();
}

/// <summary>
/// Types of validation rules
/// </summary>
public enum ValidationRuleType
{
    DataTypeValidation,
    RangeValidation,
    CrossSheetReferenceValidation,
    BusinessLogicValidation,
    StatisticalConsistencyValidation,
    CalculationValidation
}

/// <summary>
/// Comprehensive insights generation result
/// </summary>
public class ComprehensiveInsightsResult
{
    public bool IsSuccessful { get; set; }
    public DateTime GeneratedAt { get; set; }
    public TimeSpan GenerationDuration { get; set; }
    
    // Performance insights
    public IEnumerable<PlayerPerformanceInsight> PlayerInsights { get; set; } = new List<PlayerPerformanceInsight>();
    public IEnumerable<TeamPerformanceInsight> TeamInsights { get; set; } = new List<TeamPerformanceInsight>();
    public IEnumerable<TacticalInsight> TacticalInsights { get; set; } = new List<TacticalInsight>();
    
    // Statistical insights
    public IEnumerable<StatisticalTrend> StatisticalTrends { get; set; } = new List<StatisticalTrend>();
    public IEnumerable<SeasonalPattern> SeasonalPatterns { get; set; } = new List<SeasonalPattern>();
    public IEnumerable<PerformanceAnomaly> PerformanceAnomalies { get; set; } = new List<PerformanceAnomaly>();
    
    // Recommendations
    public IEnumerable<ActionableRecommendation> Recommendations { get; set; } = new List<ActionableRecommendation>();
}

/// <summary>
/// Individual player performance insight
/// </summary>
public class PlayerPerformanceInsight
{
    public string PlayerName { get; set; } = string.Empty;
    public int? JerseyNumber { get; set; }
    public PlayerPosition Position { get; set; }
    public string InsightType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, decimal> KeyMetrics { get; set; } = new();
    public string Priority { get; set; } = string.Empty;
}

/// <summary>
/// Team performance insight
/// </summary>
public class TeamPerformanceInsight
{
    public string TeamName { get; set; } = string.Empty;
    public string InsightType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, decimal> TeamMetrics { get; set; } = new();
    public IEnumerable<string> KeyStrengths { get; set; } = new List<string>();
    public IEnumerable<string> AreasForImprovement { get; set; } = new List<string>();
}

/// <summary>
/// Tactical insight from match analysis
/// </summary>
public class TacticalInsight
{
    public string InsightType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? MatchContext { get; set; }
    public Dictionary<string, object> TacticalData { get; set; } = new();
    public string Impact { get; set; } = string.Empty;
}

/// <summary>
/// Statistical trend analysis
/// </summary>
public class StatisticalTrend
{
    public string TrendType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty; // Increasing, Decreasing, Stable
    public decimal Strength { get; set; }
    public Dictionary<string, object> TrendData { get; set; } = new();
}

/// <summary>
/// Seasonal pattern detection result
/// </summary>
public class SeasonalPattern
{
    public string PatternType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Seasonality { get; set; } = string.Empty;
    public Dictionary<string, object> PatternData { get; set; } = new();
    public decimal Confidence { get; set; }
}

/// <summary>
/// Performance anomaly detection result
/// </summary>
public class PerformanceAnomaly
{
    public string AnomalyType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string? PlayerOrTeam { get; set; }
    public Dictionary<string, object> AnomalyData { get; set; } = new();
}

/// <summary>
/// Actionable recommendation from analysis
/// </summary>
public class ActionableRecommendation
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string? TargetAudience { get; set; }
    public IEnumerable<string> Actions { get; set; } = new List<string>();
    public Dictionary<string, object> SupportingData { get; set; } = new();
}

/// <summary>
/// Insight generation configuration
/// </summary>
public class InsightGenerationConfig
{
    public bool GeneratePlayerInsights { get; set; } = true;
    public bool GenerateTeamInsights { get; set; } = true;
    public bool GenerateTacticalInsights { get; set; } = true;
    public bool DetectTrends { get; set; } = true;
    public bool DetectPatterns { get; set; } = true;
    public bool DetectAnomalies { get; set; } = true;
    public decimal MinimumConfidenceLevel { get; set; } = 0.7m;
    public int MaxInsightsPerCategory { get; set; } = 10;
    public bool IncludeRecommendations { get; set; } = true;
}