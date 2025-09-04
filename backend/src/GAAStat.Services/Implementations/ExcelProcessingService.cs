using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using GAAStat.Dal.Contexts;
using GAAStat.Dal.Models.Application;
using GAAStat.Services.Interfaces;
using GAAStat.Services.Models;

namespace GAAStat.Services.Implementations;

/// <summary>
/// Main service for orchestrating Excel ETL processing operations
/// Coordinates parsing, validation, and database operations for GAA match data
/// </summary>
public class ExcelProcessingService : IExcelProcessingService
{
    private readonly IExcelParsingService _parsingService;
    private readonly IProgressTrackingService _progressService;
    private readonly IDataTransformationService _transformationService;
    private readonly IReferenceDataSeedingService _referenceDataService;
    private readonly GAAStatDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExcelProcessingService> _logger;

    public ExcelProcessingService(
        IExcelParsingService parsingService,
        IProgressTrackingService progressService,
        IDataTransformationService transformationService,
        IReferenceDataSeedingService referenceDataService,
        GAAStatDbContext context,
        IServiceProvider serviceProvider,
        ILogger<ExcelProcessingService> logger)
    {
        _parsingService = parsingService;
        _progressService = progressService;
        _transformationService = transformationService;
        _referenceDataService = referenceDataService;
        _context = context;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Processes uploaded Excel file through complete ETL pipeline
    /// Phase 1: Creates match headers with basic information
    /// </summary>
    public async Task<ServiceResult<ExcelProcessingResult>> ProcessExcelFileAsync(
        Stream fileStream, 
        string fileName, 
        string? createdBy = null)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Create ETL job record
        var jobResult = await _progressService.CreateEtlJobAsync(fileName, fileStream.Length, createdBy);
        if (!jobResult.IsSuccess)
        {
            return ServiceResult<ExcelProcessingResult>.Failed(jobResult.ErrorMessage!);
        }

        var jobId = jobResult.Data;
        var warningMessages = new List<string>();

        try
        {
            // CRITICAL FIX: Add overall timeout to prevent jobs hanging indefinitely
            using var overallTimeoutSource = new CancellationTokenSource(TimeSpan.FromMinutes(30));
            var overallCancellationToken = overallTimeoutSource.Token;
            
            // Phase 1: Initialize processing
            await RecordProgress(jobId, EtlStages.INITIALIZING, "Starting ETL processing", 10, 0);
            await _progressService.MarkJobStartedAsync(jobId);

            // Phase 2: Seed reference data
            await RecordProgress(jobId, EtlStages.SEEDING_REFERENCE_DATA, "Seeding reference data tables", 10, 1);
            var seedResult = await _referenceDataService.SeedAllReferenceDataAsync();
            if (!seedResult.IsSuccess)
            {
                await _progressService.UpdateEtlJobStatusAsync(jobId, EtlJobStatus.FAILED, seedResult.ErrorMessage);
                return ServiceResult<ExcelProcessingResult>.Failed($"Reference data seeding failed: {seedResult.ErrorMessage}");
            }

            if (seedResult.Data.WarningMessages.Any())
            {
                warningMessages.AddRange(seedResult.Data.WarningMessages);
            }

            _logger.LogInformation("Seeded {TotalSeeded} reference data records for job {JobId}", 
                seedResult.Data.TotalSeeded, jobId);

            // Phase 3: Clear existing data
            await RecordProgress(jobId, EtlStages.CLEARING_DATA, "Clearing existing match data", 10, 2);
            await ClearExistingMatchDataAsync(jobId);

            // Phase 4: Parse Excel file
            await RecordProgress(jobId, EtlStages.PARSING_EXCEL, "Analyzing Excel file structure", 10, 3).ConfigureAwait(false);
            
            var analysisResult = await _parsingService.AnalyzeExcelFileAsync(fileStream, fileName).ConfigureAwait(false);
            if (!analysisResult.IsSuccess)
            {
                await _progressService.UpdateEtlJobStatusAsync(jobId, EtlJobStatus.FAILED, analysisResult.ErrorMessage).ConfigureAwait(false);
                return ServiceResult<ExcelProcessingResult>.Failed(analysisResult.ErrorMessage!);
            }

            var analysis = analysisResult.Data;
            if (!analysis.IsValidGaaFile)
            {
                var errorMsg = string.Join("; ", analysis.ValidationErrors);
                await _progressService.UpdateEtlJobStatusAsync(jobId, EtlJobStatus.FAILED, $"Invalid GAA file: {errorMsg}");
                return ServiceResult<ExcelProcessingResult>.Failed($"Invalid GAA file: {errorMsg}");
            }

            // Phase 5: Parse match data from all sheets
            await RecordProgress(jobId, EtlStages.PARSING_EXCEL, "Parsing match data from sheets", 10, 4).ConfigureAwait(false);
            
            // Reset stream position for parsing
            fileStream.Position = 0;
            var matchDataResult = await _parsingService.ParseAllMatchDataAsync(fileStream).ConfigureAwait(false);
            if (!matchDataResult.IsSuccess)
            {
                await _progressService.UpdateEtlJobStatusAsync(jobId, EtlJobStatus.FAILED, matchDataResult.ErrorMessage).ConfigureAwait(false);
                return ServiceResult<ExcelProcessingResult>.Failed(matchDataResult.ErrorMessage!);
            }

            var allMatchData = matchDataResult.Data.ToList();

            // Phase 6: Validate data
            await RecordProgress(jobId, EtlStages.VALIDATING_DATA, "Validating match data", 10, 5).ConfigureAwait(false);
            
            var validationResult = await ValidateMatchDataAsync(jobId, allMatchData).ConfigureAwait(false);
            if (!validationResult.IsSuccess)
            {
                await _progressService.UpdateEtlJobStatusAsync(jobId, EtlJobStatus.FAILED, "Data validation failed").ConfigureAwait(false);
                return ServiceResult<ExcelProcessingResult>.Failed("Data validation failed");
            }

            warningMessages.AddRange(validationResult.ValidationErrors);

            // Phase 7: Save matches to database
            await RecordProgress(jobId, EtlStages.SAVING_MATCHES, "Creating match records", 10, 6).ConfigureAwait(false);
            
            var saveResult = await SaveMatchesToDatabaseAsync(jobId, allMatchData).ConfigureAwait(false);
            if (!saveResult.IsSuccess)
            {
                await _progressService.UpdateEtlJobStatusAsync(jobId, EtlJobStatus.FAILED, saveResult.ErrorMessage).ConfigureAwait(false);
                return ServiceResult<ExcelProcessingResult>.Failed(saveResult.ErrorMessage!);
            }

            var matchesCreated = saveResult.Data.MatchesCreated;
            var matchIdMap = saveResult.Data.MatchIdMap;

            // Phase 8: Parse and save player statistics
            await RecordProgress(jobId, EtlStages.SAVING_PLAYER_STATS, "Processing player statistics", 10, 7).ConfigureAwait(false);
            
            var playerStatsResult = await ProcessPlayerStatisticsAsync(jobId, fileStream, analysis.Sheets, matchIdMap).ConfigureAwait(false);
            if (!playerStatsResult.IsSuccess)
            {
                // Log error but don't fail the entire process for player stats issues
                _logger.LogWarning("Player statistics processing failed: {Error}", playerStatsResult.ErrorMessage);
                warningMessages.Add($"Player statistics processing incomplete: {playerStatsResult.ErrorMessage}");
            }

            var playerStatsCreated = playerStatsResult.Data;

            // Phase 9: Process KPI definitions (temporarily disabled due to compilation errors)
            await RecordProgress(jobId, EtlStages.SAVING_SPECIALIZED_ANALYTICS, "Processing KPI definitions", 10, 8);
            // await ProcessKpiDefinitionsAsync(fileStream, fileName, jobId);
            
            // Phase 10: Process team statistics (placeholder for future implementation)
            await RecordProgress(jobId, EtlStages.SAVING_TEAM_STATS, "Processing team statistics", 10, 9);
            // TODO: Implement team statistics processing
            
            // Phase 11: Process specialized analytics (placeholder for future implementation)
            await RecordProgress(jobId, EtlStages.SAVING_SPECIALIZED_ANALYTICS, "Processing specialized analytics", 10, 10);
            // TODO: Implement kickout, shot, and scoreable free analysis processing

            // Phase 12: Finalize
            await RecordProgress(jobId, EtlStages.FINALIZING, "Finalizing ETL process", 10, 11);
            await _progressService.MarkJobCompletedAsync(jobId);

            stopwatch.Stop();

            var result = new ExcelProcessingResult
            {
                JobId = jobId,
                FileName = fileName,
                SheetsProcessed = analysis.Sheets.Count(s => s.ContainsMatchData),
                MatchesCreated = matchesCreated,
                PlayerStatisticsCreated = playerStatsCreated,
                ProcessingDuration = stopwatch.Elapsed,
                WarningMessages = warningMessages
            };

            _logger.LogInformation("Successfully processed Excel file {FileName} in {Duration}ms. Created {MatchCount} matches and {PlayerStatsCount} player statistics", 
                fileName, stopwatch.ElapsedMilliseconds, matchesCreated, playerStatsCreated);

            return ServiceResult<ExcelProcessingResult>.Success(result);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogError("ETL processing timed out after 30 minutes for job {JobId} file {FileName}", jobId, fileName);
            
            await _progressService.UpdateEtlJobStatusAsync(jobId, EtlJobStatus.FAILED, "ETL processing timed out after 30 minutes");
            return ServiceResult<ExcelProcessingResult>.Failed("ETL processing timed out after 30 minutes");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to process Excel file {FileName} for job {JobId}", fileName, jobId);
            
            // CRITICAL FIX: Always mark job as failed when exception occurs
            try
            {
                await _progressService.UpdateEtlJobStatusAsync(jobId, EtlJobStatus.FAILED, ex.Message);
            }
            catch (Exception statusUpdateEx)
            {
                _logger.LogError(statusUpdateEx, "Failed to update job status to failed for job {JobId}", jobId);
            }
            
            return ServiceResult<ExcelProcessingResult>.Failed($"ETL processing failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates uploaded Excel file before processing
    /// Checks file format, size, and basic GAA data structure
    /// </summary>
    public async Task<ServiceResult<ExcelFileAnalysis>> ValidateAndAnalyzeFileAsync(
        Stream fileStream, 
        string fileName, 
        long fileSizeBytes)
    {
        try
        {
            // Basic file validation
            var validationResult = await _parsingService.ValidateExcelFileAsync(fileStream, fileName, fileSizeBytes);
            if (!validationResult.IsSuccess)
            {
                var analysis = new ExcelFileAnalysis
                {
                    FileName = fileName,
                    FileSizeBytes = fileSizeBytes,
                    IsValidGaaFile = false,
                    ValidationErrors = validationResult.ValidationErrors.ToList()
                };
                return ServiceResult<ExcelFileAnalysis>.Success(analysis);
            }

            // Analyze file structure
            fileStream.Position = 0;
            return await _parsingService.AnalyzeExcelFileAsync(fileStream, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate and analyze file {FileName}", fileName);
            return ServiceResult<ExcelFileAnalysis>.Failed("Failed to validate Excel file");
        }
    }

    /// <summary>
    /// Retrieves current processing status for an ETL job
    /// </summary>
    public async Task<ServiceResult<EtlProgressUpdate?>> GetProcessingStatusAsync(int jobId)
    {
        return await _progressService.GetCurrentProgressAsync(jobId);
    }

    /// <summary>
    /// Retrieves validation errors for a completed or failed ETL job
    /// </summary>
    public async Task<ServiceResult<IEnumerable<ValidationError>>> GetValidationErrorsAsync(int jobId)
    {
        try
        {
            var errors = await _context.EtlValidationErrors
                .Where(e => e.JobId == jobId)
                .OrderBy(e => e.CreatedAt)
                .Select(e => new ValidationError
                {
                    SheetName = e.SheetName,
                    RowNumber = e.RowNumber,
                    ColumnName = e.ColumnName,
                    ErrorType = e.ErrorType ?? string.Empty,
                    ErrorMessage = e.ErrorMessage ?? string.Empty,
                    SuggestedFix = e.SuggestedFix
                })
                .ToListAsync().ConfigureAwait(false);

            return ServiceResult<IEnumerable<ValidationError>>.Success(errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve validation errors for job {JobId}", jobId);
            return ServiceResult<IEnumerable<ValidationError>>.Failed("Failed to retrieve validation errors");
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Clears ALL data tables from the database before importing new data
    /// Ensures referential integrity by deleting dependent records first
    /// Preserves only ETL tracking tables and flyway schema history
    /// </summary>
    private async Task ClearExistingMatchDataAsync(int jobId)
    {
        try
        {
            _logger.LogInformation("Starting comprehensive data cleanup for ETL job {JobId}", jobId);

            // Clear dependent tables first (respecting foreign key constraints)
            var deletedCounts = new Dictionary<string, int>();

            // Delete all analysis and statistics data (dependent tables)
            deletedCounts["match_player_statistics"] = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM match_player_statistics");

            deletedCounts["match_team_statistics"] = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM match_team_statistics");

            deletedCounts["kickout_analysis"] = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM kickout_analysis");

            deletedCounts["shot_analysis"] = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM shot_analysis");

            deletedCounts["scoreable_free_analysis"] = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM scoreable_free_analysis");

            deletedCounts["positional_analysis"] = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM positional_analysis");

            deletedCounts["season_player_totals"] = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM season_player_totals");

            deletedCounts["position_averages"] = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM position_averages");

            // Delete main entity tables
            deletedCounts["matches"] = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM matches");

            deletedCounts["players"] = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM players");

            deletedCounts["teams"] = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM teams");

            deletedCounts["venues"] = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM venues");

            deletedCounts["competitions"] = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM competitions");

            deletedCounts["seasons"] = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM seasons");

            // Delete lookup/reference tables (will be re-seeded)
            deletedCounts["competition_types"] = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM competition_types");

            deletedCounts["match_results"] = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM match_results");

            deletedCounts["positions"] = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM positions");

            deletedCounts["time_periods"] = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM time_periods");

            deletedCounts["team_types"] = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM team_types");

            deletedCounts["kickout_types"] = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM kickout_types");

            deletedCounts["shot_types"] = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM shot_types");

            deletedCounts["shot_outcomes"] = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM shot_outcomes");

            deletedCounts["position_areas"] = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM position_areas");

            deletedCounts["free_types"] = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM free_types");

            deletedCounts["metric_categories"] = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM metric_categories");

            deletedCounts["metric_definitions"] = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM metric_definitions");

            deletedCounts["kpi_definitions"] = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM kpi_definitions");

            // Clear previous ETL validation errors (optional cleanup)
            deletedCounts["etl_validation_errors"] = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM etl_validation_errors WHERE job_id != @p0", jobId);

            // Log cleanup results
            var totalDeleted = deletedCounts.Values.Sum();
            _logger.LogInformation("Comprehensive data cleanup completed for ETL job {JobId}. Deleted {TotalRecords} records from {TableCount} tables: {Details}",
                jobId, totalDeleted, deletedCounts.Count, string.Join(", ", deletedCounts.Select(kvp => $"{kvp.Key}: {kvp.Value}")));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear existing data for ETL job {JobId}", jobId);
            throw;
        }
    }

    private async Task RecordProgress(int jobId, string stage, string message, int totalSteps, int completedSteps)
    {
        var progress = new EtlProgressUpdate
        {
            JobId = jobId,
            Stage = stage,
            TotalSteps = totalSteps,
            CompletedSteps = completedSteps,
            Status = "processing",
            Message = message
        };

        await _progressService.RecordProgressUpdateAsync(progress);
    }

    private async Task<ServiceResult> ValidateMatchDataAsync(int jobId, IList<MatchData> matchData)
    {
        var validationErrors = new List<ValidationError>();

        foreach (var match in matchData)
        {
            // Validate match date - treat as warning, not blocking error
            if (!match.MatchDate.HasValue)
            {
                validationErrors.Add(new ValidationError
                {
                    SheetName = match.SheetName,
                    ErrorType = "warning", // Changed from INVALID_DATE to warning to allow processing
                    ErrorMessage = "Could not extract date from sheet name (likely due to Excel 31-character limit). Date will be null - can be added later via UI",
                    SuggestedFix = "Sheet names are truncated by Excel at 31 characters. Add date manually through the application UI"
                });
            }

            // Validate team names
            if (string.IsNullOrWhiteSpace(match.HomeTeam) || string.IsNullOrWhiteSpace(match.AwayTeam))
            {
                validationErrors.Add(new ValidationError
                {
                    SheetName = match.SheetName,
                    ErrorType = EtlErrorTypes.MISSING_DATA,
                    ErrorMessage = "Could not extract team names from sheet name",
                    SuggestedFix = "Ensure sheet name follows pattern: 'XX. [Team1] vs [Team2] DD.MM.YY'"
                });
            }

            // Validate scores if present
            if (!string.IsNullOrEmpty(match.HomeScore))
            {
                var (homeGoals, homePoints) = _transformationService.ParseScore(match.HomeScore);
                if (homeGoals == 0 && homePoints == 0 && match.HomeScore != "0-00")
                {
                    validationErrors.Add(new ValidationError
                    {
                        SheetName = match.SheetName,
                        RowNumber = 3,
                        ColumnName = "Home Score",
                        ErrorType = EtlErrorTypes.INVALID_SCORE,
                        ErrorMessage = $"Invalid score format: '{match.HomeScore}'",
                        SuggestedFix = "Ensure scores follow GAA format: 'G-PP' (e.g., '2-06')"
                    });
                }
            }

            if (!string.IsNullOrEmpty(match.AwayScore))
            {
                var (awayGoals, awayPoints) = _transformationService.ParseScore(match.AwayScore);
                if (awayGoals == 0 && awayPoints == 0 && match.AwayScore != "0-00")
                {
                    validationErrors.Add(new ValidationError
                    {
                        SheetName = match.SheetName,
                        RowNumber = 3,
                        ColumnName = "Away Score",
                        ErrorType = EtlErrorTypes.INVALID_SCORE,
                        ErrorMessage = $"Invalid score format: '{match.AwayScore}'",
                        SuggestedFix = "Ensure scores follow GAA format: 'G-PP' (e.g., '0-11')"
                    });
                }
            }

            // Add validation warnings to match data
            foreach (var warning in match.ValidationWarnings)
            {
                validationErrors.Add(new ValidationError
                {
                    SheetName = match.SheetName,
                    ErrorType = "warning",
                    ErrorMessage = warning
                });
            }
        }

        // Record validation errors if any
        if (validationErrors.Any())
        {
            await _progressService.RecordValidationErrorsAsync(jobId, validationErrors).ConfigureAwait(false);
            
            // Separate critical errors from warnings
            var criticalErrors = validationErrors.Where(e => e.ErrorType != "warning").ToList();
            if (criticalErrors.Any())
            {
                return ServiceResult.ValidationFailed(criticalErrors.Select(e => e.ErrorMessage));
            }
        }

        // If we only have warnings, proceed with success
        return ServiceResult.Success();
    }

    private async Task<ServiceResult<MatchSaveResult>> SaveMatchesToDatabaseAsync(int jobId, IList<MatchData> matchData)
    {
        try
        {
            var result = new MatchSaveResult();
            var matchesCreated = 0;
            var matchIdMap = new Dictionary<string, int>();

            foreach (var match in matchData)
            {
                // Check for duplicate matches
                var existingMatch = await _context.Matches
                    .FirstOrDefaultAsync(m => 
                        m.Date == match.MatchDate && 
                        m.Opposition.TeamName == match.AwayTeam).ConfigureAwait(false);

                if (existingMatch != null)
                {
                    _logger.LogWarning("Skipping duplicate match: {HomeTeam} vs {AwayTeam} on {MatchDate}", 
                        match.HomeTeam, match.AwayTeam, match.MatchDate);
                    
                    await _progressService.RecordValidationErrorAsync(
                        jobId,
                        match.SheetName,
                        null,
                        null,
                        EtlErrorTypes.DUPLICATE_MATCH,
                        $"Match already exists: {match.HomeTeam} vs {match.AwayTeam} on {match.MatchDate}",
                        "Remove duplicate match data from Excel file").ConfigureAwait(false);
                    
                    // Still map existing match ID for player stats
                    matchIdMap[match.SheetName] = existingMatch.MatchId;
                    continue;
                }

                // Resolve or create opposition team
                var oppositionTeam = await GetOrCreateTeamAsync(match.AwayTeam == GaaConstants.DEFAULT_HOME_TEAM ? match.HomeTeam : match.AwayTeam).ConfigureAwait(false);
                var season = await GetCurrentSeasonAsync().ConfigureAwait(false);
                var venue = await GetVenueAsync(match.Venue ?? "Away").ConfigureAwait(false);
                var competition = await GetOrCreateCompetitionAsync(match.Competition ?? "League").ConfigureAwait(false);

                // Create match record
                var matchEntity = new Match
                {
                    MatchNumber = ExtractMatchNumber(match.SheetName),
                    Date = match.MatchDate ?? DateOnly.FromDateTime(DateTime.Now), // Use current date as fallback
                    CompetitionId = competition.CompetitionId,
                    OppositionId = oppositionTeam.TeamId,
                    VenueId = venue.VenueId,
                    DrumScore = match.HomeScore ?? "0-00",
                    OppositionScore = match.AwayScore ?? "0-00",
                    DrumGoals = match.HomeGoals ?? 0,
                    DrumPoints = match.HomePoints ?? 0,
                    OppositionGoals = match.AwayGoals ?? 0,
                    OppositionPoints = match.AwayPoints ?? 0,
                    PointDifference = CalculatePointDifference(match.HomeGoals ?? 0, match.HomePoints ?? 0, match.AwayGoals ?? 0, match.AwayPoints ?? 0),
                    MatchResultId = await DetermineMatchResultAsync(match.HomeGoals ?? 0, match.HomePoints ?? 0, match.AwayGoals ?? 0, match.AwayPoints ?? 0).ConfigureAwait(false),
                    SeasonId = season.SeasonId
                };

                await _context.Matches.AddAsync(matchEntity).ConfigureAwait(false);
                matchesCreated++;
            }

            await _context.SaveChangesAsync().ConfigureAwait(false);

            // CRITICAL FIX: Get the generated match IDs after saving using match_number
            // The previous approach using date+team failed because multiple matches had duplicate dates
            // due to Excel sheet name truncation causing date extraction failures
            foreach (var match in matchData)
            {
                if (!matchIdMap.ContainsKey(match.SheetName))
                {
                    var matchNumber = ExtractMatchNumber(match.SheetName);
                    var savedMatch = await _context.Matches
                        .FirstOrDefaultAsync(m => m.MatchNumber == matchNumber).ConfigureAwait(false);
                    
                    if (savedMatch != null)
                    {
                        matchIdMap[match.SheetName] = savedMatch.MatchId;
                        _logger.LogInformation("✅ Added to matchIdMap: Sheet '{SheetName}' -> Match ID {MatchId} (Match Number {MatchNumber})", 
                            match.SheetName, savedMatch.MatchId, matchNumber);
                    }
                    else
                    {
                        _logger.LogWarning("❌ Failed to find saved match for sheet '{SheetName}' with match number {MatchNumber}", 
                            match.SheetName, matchNumber);
                    }
                }
            }

            // Log final matchIdMap contents for debugging
            _logger.LogInformation("🗺️ Final matchIdMap populated with {MapCount} entries:", matchIdMap.Count);
            foreach (var kvp in matchIdMap.OrderBy(x => x.Key))
            {
                _logger.LogInformation("  '{SheetName}' -> Match ID {MatchId}", kvp.Key, kvp.Value);
            }
            
            result.MatchesCreated = matchesCreated;
            result.MatchIdMap = matchIdMap;
            return ServiceResult<MatchSaveResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save matches to database for job {JobId}", jobId);
            return ServiceResult<MatchSaveResult>.Failed("Failed to save matches to database");
        }
    }

    private int ExtractMatchNumber(string sheetName)
    {
        var parts = sheetName.Split('.');
        if (parts.Length > 0 && int.TryParse(parts[0], out var matchNumber))
        {
            return matchNumber;
        }
        return 0;
    }

    private int CalculatePointDifference(int homeGoals, int homePoints, int awayGoals, int awayPoints)
    {
        var homeTotal = homeGoals * GaaConstants.POINTS_PER_GOAL + homePoints;
        var awayTotal = awayGoals * GaaConstants.POINTS_PER_GOAL + awayPoints;
        return homeTotal - awayTotal;
    }

    private async Task<int> DetermineMatchResult(int homeGoals, int homePoints, int awayGoals, int awayPoints)
    {
        var pointDifference = CalculatePointDifference(homeGoals, homePoints, awayGoals, awayPoints);
        
        string resultCode = pointDifference switch
        {
            > 0 => "W",
            < 0 => "L",
            _ => "D"
        };

        var result = await _context.MatchResults.FirstOrDefaultAsync(mr => mr.ResultCode == resultCode).ConfigureAwait(false);
        return result?.MatchResultId ?? 1; // Default to first result if not found
    }

    private async Task<Team> GetOrCreateTeamAsync(string teamName)
    {
        var team = await _context.Teams.FirstOrDefaultAsync(t => t.TeamName == teamName).ConfigureAwait(false);
        if (team == null)
        {
            team = new Team
            {
                TeamName = teamName,
                County = "Derry"
            };
            await _context.Teams.AddAsync(team).ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }
        return team;
    }

    private async Task<Season> GetCurrentSeasonAsync()
    {
        // For now, get or create 2025 season
        var season = await _context.Seasons.FirstOrDefaultAsync(s => s.SeasonName == "2025").ConfigureAwait(false);
        if (season == null)
        {
            season = new Season
            {
                SeasonName = "2025",
                StartDate = new DateOnly(2025, 1, 1),
                EndDate = new DateOnly(2025, 12, 31),
                IsCurrent = true
            };
            await _context.Seasons.AddAsync(season).ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }
        return season;
    }

    private async Task<Venue> GetVenueAsync(string venueName)
    {
        var venueCode = venueName.ToUpperInvariant().Substring(0, 1); // H, A, or N
        var venue = await _context.Venues.FirstOrDefaultAsync(v => v.VenueCode == venueCode).ConfigureAwait(false);
        
        if (venue != null)
            return venue;
            
        // Try to get any venue as fallback
        var fallbackVenue = await _context.Venues.FirstOrDefaultAsync().ConfigureAwait(false);
        if (fallbackVenue != null)
            return fallbackVenue;
            
        // If no venues exist, create a default one
        var defaultVenue = new Venue
        {
            VenueCode = "A",
            VenueDescription = "Away"
        };
        
        await _context.Venues.AddAsync(defaultVenue).ConfigureAwait(false);
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return defaultVenue;
    }

    private async Task<Competition> GetOrCreateCompetitionAsync(string competitionName)
    {
        var competition = await _context.Competitions.FirstOrDefaultAsync(c => c.CompetitionName == competitionName).ConfigureAwait(false);
        if (competition == null)
        {
            // Get or create default competition type
            var competitionType = await _context.CompetitionTypes.FirstOrDefaultAsync().ConfigureAwait(false);
            if (competitionType == null)
            {
                competitionType = new CompetitionType
                {
                    TypeName = "League",
                    Description = "Default league competition"
                };
                await _context.CompetitionTypes.AddAsync(competitionType).ConfigureAwait(false);
                await _context.SaveChangesAsync().ConfigureAwait(false);
            }
            
            competition = new Competition
            {
                CompetitionName = competitionName,
                Season = "2025",
                CompetitionTypeId = competitionType.CompetitionTypeId
            };
            await _context.Competitions.AddAsync(competition).ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }
        return competition;
    }

    private async Task<int> DetermineMatchResultAsync(int homeGoals, int homePoints, int awayGoals, int awayPoints)
    {
        var pointDifference = CalculatePointDifference(homeGoals, homePoints, awayGoals, awayPoints);
        
        // Get or create match result
        var resultCode = pointDifference switch
        {
            > 0 => "W", // Win
            < 0 => "L", // Loss  
            _ => "D"    // Draw
        };

        var matchResult = await _context.MatchResults.FirstOrDefaultAsync(mr => mr.ResultCode == resultCode).ConfigureAwait(false);
        if (matchResult == null)
        {
            matchResult = new MatchResult
            {
                ResultCode = resultCode,
                ResultDescription = pointDifference switch
                {
                    > 0 => "Win",
                    < 0 => "Loss",
                    _ => "Draw"
                }
            };
            await _context.MatchResults.AddAsync(matchResult).ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }
        
        return matchResult.MatchResultId;
    }

    private async Task<ServiceResult<int>> ProcessPlayerStatisticsAsync(
        int jobId, 
        Stream fileStream, 
        IEnumerable<SheetInfo> sheets, 
        Dictionary<string, int> matchIdMap)
    {
        try
        {
            // Add timeout to prevent hanging - 10 minutes maximum for player stats processing
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            var cancellationToken = cancellationTokenSource.Token;
            var totalPlayerStatsCreated = 0;
            var sheetsWithPlayerData = sheets.Where(s => s.ContainsPlayerData).ToList();

            _logger.LogInformation("Processing player statistics for {SheetCount} player stats sheets", sheetsWithPlayerData.Count);

            // CRITICAL FIX: Load ExcelPackage ONCE to prevent stream exhaustion
            _logger.LogInformation("🔧 Loading ExcelPackage once for all sheet processing to prevent stream issues");
            fileStream.Position = 0;
            
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage(fileStream);
            
            _logger.LogInformation("✅ Successfully loaded ExcelPackage with {SheetCount} worksheets", package.Workbook.Worksheets.Count);
            
            // Debug logging: show which sheets were identified as player data sheets
            foreach (var sheet in sheetsWithPlayerData)
            {
                _logger.LogInformation("Player stats sheet detected: '{SheetName}'", sheet.Name);
            }
            
            // Debug logging: show all sheets and their flags
            _logger.LogInformation("All sheets analysis:");
            foreach (var sheet in sheets)
            {
                _logger.LogInformation("Sheet: '{SheetName}' - ContainsMatchData: {ContainsMatchData}, ContainsPlayerData: {ContainsPlayerData}", 
                    sheet.Name, sheet.ContainsMatchData, sheet.ContainsPlayerData);
            }
            
            // Debug logging: show matchIdMap contents
            _logger.LogInformation("MatchIdMap contents ({MapCount} entries):", matchIdMap.Count);
            foreach (var kvp in matchIdMap)
            {
                _logger.LogInformation("  Match Sheet: '{SheetName}' -> Match ID: {MatchId}", kvp.Key, kvp.Value);
            }

            var sheetIndex = 0;
            foreach (var sheet in sheetsWithPlayerData)
            {
                sheetIndex++;
                var sheetStartTime = DateTime.UtcNow;
                _logger.LogInformation("🔄 Starting to process player stats sheet {SheetIndex}/{TotalSheets}: '{SheetName}' at {Timestamp}", 
                    sheetIndex, sheetsWithPlayerData.Count, sheet.Name, sheetStartTime);
                
                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();
                
                // Add per-sheet timeout - 2 minutes maximum for each sheet
                using var sheetCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));
                using var combinedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken, sheetCancellationTokenSource.Token);
                var sheetCancellationToken = combinedCancellationTokenSource.Token;
                
                try
                {
                
                // Find the corresponding match ID for this player stats sheet
                _logger.LogInformation("🔍 Looking for match ID for player stats sheet: '{SheetName}'", sheet.Name);
                var matchId = FindMatchIdForPlayerStatsSheet(sheet.Name, matchIdMap);
                if (matchId == null)
                {
                    _logger.LogWarning("❌ No matching match found for player stats sheet {SheetName}, skipping player statistics", sheet.Name);
                    continue;
                }
                _logger.LogInformation("✅ Found match ID {MatchId} for player stats sheet '{SheetName}'", matchId.Value, sheet.Name);

                // CRITICAL FIX: Get worksheet from the loaded package instead of recreating from stream
                var worksheet = package.Workbook.Worksheets[sheet.Name];
                if (worksheet == null)
                {
                    _logger.LogWarning("❌ Worksheet '{SheetName}' not found in loaded package, skipping", sheet.Name);
                    continue;
                }
                
                _logger.LogInformation("📊 Parsing player statistics from worksheet '{SheetName}' for match ID {MatchId} at {StartTime}", 
                    sheet.Name, matchId.Value, DateTime.UtcNow);
                
                // CRITICAL FIX: Pass worksheet directly instead of stream to prevent package recreation
                var parseStartTime = DateTime.UtcNow;
                var playerStatsResult = await _parsingService.ParsePlayerStatisticsFromWorksheetAsync(
                    worksheet, matchId.Value, sheetCancellationToken).ConfigureAwait(false);
                var parseEndTime = DateTime.UtcNow;

                _logger.LogInformation("✅ Completed parsing player statistics from sheet '{SheetName}', Success: {Success}, Duration: {Duration}ms", 
                    sheet.Name, playerStatsResult.IsSuccess, (parseEndTime - parseStartTime).TotalMilliseconds);

                if (!playerStatsResult.IsSuccess)
                {
                    _logger.LogWarning("❌ Failed to parse player statistics from sheet {SheetName}: {Error}", 
                        sheet.Name, playerStatsResult.ErrorMessage);
                    
                    await _progressService.RecordValidationErrorAsync(
                        jobId,
                        sheet.Name,
                        null,
                        null,
                        EtlErrorTypes.SHEET_STRUCTURE,
                        $"Failed to parse player statistics: {playerStatsResult.ErrorMessage}",
                        "Check sheet format and data integrity").ConfigureAwait(false);
                    
                    continue;
                }

                var playerStatistics = playerStatsResult.Data.ToList();
                if (!playerStatistics.Any())
                {
                    _logger.LogInformation("ℹ️ No player statistics found in sheet {SheetName}", sheet.Name);
                    continue;
                }

                _logger.LogInformation("💾 Starting to save {Count} player statistics records for match ID {MatchId} from sheet '{SheetName}' at {StartTime}", 
                    playerStatistics.Count, matchId.Value, sheet.Name, DateTime.UtcNow);
                
                var saveStartTime = DateTime.UtcNow;
                
                // CRITICAL FIX: Create new DbContext scope for each sheet to prevent context state corruption
                _logger.LogDebug("🔄 Creating new DbContext scope for sheet '{SheetName}' to prevent context state issues", sheet.Name);
                using var scope = _serviceProvider.CreateScope();
                var scopedContext = scope.ServiceProvider.GetRequiredService<GAAStatDbContext>();
                
                var statsCreated = await SavePlayerStatisticsWithScopedContextAsync(
                    scopedContext, jobId, matchId.Value, playerStatistics, sheetCancellationToken).ConfigureAwait(false);
                var saveEndTime = DateTime.UtcNow;
                totalPlayerStatsCreated += statsCreated;
                
                _logger.LogInformation("✅ Successfully saved {StatsCreated} player statistics for match ID {MatchId} from sheet '{SheetName}' at {EndTime}, Duration: {Duration}ms", 
                    statsCreated, matchId.Value, sheet.Name, saveEndTime, (saveEndTime - saveStartTime).TotalMilliseconds);

                _logger.LogInformation("📊 SHEET SUMMARY for {SheetName}: {StatsCreated} player statistics created, {PlayersProcessed} players processed, {PlayersSkipped} players skipped", 
                    sheet.Name, statsCreated, playerStatistics.Count, playerStatistics.Count - statsCreated);
                
                var sheetEndTime = DateTime.UtcNow;
                _logger.LogInformation("🏁 Completed processing sheet {SheetIndex}/{TotalSheets}: '{SheetName}' at {EndTime}, Total Duration: {Duration}ms", 
                    sheetIndex, sheetsWithPlayerData.Count, sheet.Name, sheetEndTime, (sheetEndTime - sheetStartTime).TotalMilliseconds);
                }
                catch (OperationCanceledException ex) when (sheetCancellationTokenSource.Token.IsCancellationRequested)
                {
                    _logger.LogError("Sheet processing timed out after 2 minutes for sheet '{SheetName}' in job {JobId} - continuing with remaining sheets", sheet.Name, jobId);
                    
                    await _progressService.RecordValidationErrorAsync(
                        jobId,
                        sheet.Name,
                        null,
                        null,
                        EtlErrorTypes.SHEET_STRUCTURE,
                        $"Sheet '{sheet.Name}' processing timed out after 2 minutes",
                        "Check sheet data complexity and optimize processing").ConfigureAwait(false);
                    
                    // CRITICAL FIX: Continue with remaining sheets instead of exiting entire function
                    continue;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process player statistics for sheet '{SheetName}' in job {JobId}", sheet.Name, jobId);
                    // Continue with other sheets instead of failing completely
                    continue;
                }
            }

            return ServiceResult<int>.Success(totalPlayerStatsCreated);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
        {
            _logger.LogError("Player statistics processing timed out after 10 minutes for job {JobId}", jobId);
            return ServiceResult<int>.Failed("Player statistics processing timed out - operation took longer than expected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process player statistics for job {JobId}", jobId);
            return ServiceResult<int>.Failed($"Failed to process player statistics: {ex.Message}");
        }
    }

    /// <summary>
    /// Find the corresponding match ID for a player stats sheet by matching team names and dates
    /// Player stats sheet: "07. Player Stats vs Lissan 03.08.25" or "07. Player Stats vs Lissan 03.0" (truncated)
    /// Match sheet: "07. Drum vs Lissan 03.08.25"
    /// </summary>
    private int? FindMatchIdForPlayerStatsSheet(string playerStatsSheetName, Dictionary<string, int> matchIdMap)
    {
        // CRITICAL FIX: Trim whitespace from sheet name to handle trailing spaces
        var trimmedPlayerStatsSheetName = playerStatsSheetName.Trim();
        
        // ENHANCED DEBUG: Special logging for Match 1
        var isMatch1 = playerStatsSheetName.StartsWith("01.");
        if (isMatch1)
        {
            _logger.LogInformation("🔍 MATCH 1 MATCHING: FindMatchIdForPlayerStatsSheet called for: '{PlayerStatsSheet}'", playerStatsSheetName);
            _logger.LogInformation("🔍 MATCH 1 MATCHING: Trimmed name: '{TrimmedName}'", trimmedPlayerStatsSheetName);
        }
        else
        {
            _logger.LogInformation("🔍 FindMatchIdForPlayerStatsSheet called for: '{PlayerStatsSheet}' (trimmed: '{TrimmedName}')", 
                playerStatsSheetName, trimmedPlayerStatsSheetName);
        }
        
        // Extract sheet number from player stats sheet (primary matching approach)
        var playerStatsSheetNumber = ExtractSheetNumber(trimmedPlayerStatsSheetName);
        
        if (isMatch1)
        {
            _logger.LogInformation("🔍 MATCH 1 MATCHING: Extracted sheet number: '{SheetNumber}'", playerStatsSheetNumber ?? "NULL");
        }
        else
        {
            _logger.LogInformation("🔢 Extracted sheet number from player stats sheet: '{SheetNumber}'", playerStatsSheetNumber ?? "NULL");
        }
        
        // First try: match by sheet number prefix (most reliable approach)
        // "08. Player stats vs Magilligan" matches "08. Drum vs Magilligan"
        if (!string.IsNullOrEmpty(playerStatsSheetNumber))
        {
            if (isMatch1)
            {
                _logger.LogInformation("🔍 MATCH 1 MATCHING: Trying to match sheet number '{SheetNumber}' with available matches:", playerStatsSheetNumber);
                foreach (var kvp in matchIdMap)
                {
                    _logger.LogInformation("🔍 MATCH 1 MATCHING: Available match sheet: '{MatchSheet}' -> ID {MatchId}", kvp.Key, kvp.Value);
                }
            }
            else
            {
                _logger.LogInformation("🔍 Trying sheet number matching with number: '{SheetNumber}'", playerStatsSheetNumber);
            }
            
            foreach (var kvp in matchIdMap)
            {
                var matchSheetName = kvp.Key.Trim(); // CRITICAL FIX: Also trim match sheet names
                var matchId = kvp.Value;
                var matchSheetNumber = ExtractSheetNumber(matchSheetName);
                
                if (isMatch1)
                {
                    _logger.LogInformation("🔍 MATCH 1 MATCHING: Comparing player number '{PlayerNumber}' vs match sheet '{MatchSheet}' number '{MatchNumber}'", 
                        playerStatsSheetNumber, matchSheetName, matchSheetNumber ?? "NULL");
                }
                else
                {
                    _logger.LogInformation("📊 Comparing: Player sheet number '{PlayerNumber}' vs Match sheet '{MatchSheet}' number '{MatchNumber}'", 
                        playerStatsSheetNumber, matchSheetName, matchSheetNumber ?? "NULL");
                }
                
                if (playerStatsSheetNumber == matchSheetNumber)
                {
                    if (isMatch1)
                    {
                        _logger.LogInformation("✅ MATCH 1 MATCHING SUCCESS! Mapped '{PlayerStatsSheet}' to '{MatchSheet}' (ID: {MatchId}) via number '{SheetNumber}'", 
                            trimmedPlayerStatsSheetName, matchSheetName, matchId, playerStatsSheetNumber);
                    }
                    else
                    {
                        _logger.LogInformation("✅ MATCH FOUND! Mapped player stats sheet '{PlayerStatsSheet}' to match sheet '{MatchSheet}' (Match ID: {MatchId}) via sheet number match ({SheetNumber})", 
                            trimmedPlayerStatsSheetName, matchSheetName, matchId, playerStatsSheetNumber);
                    }
                    return matchId;
                }
            }
            
            if (isMatch1)
            {
                _logger.LogError("❌ MATCH 1 MATCHING FAILED! No matches found for player stats sheet '{PlayerStatsSheet}' with number '{SheetNumber}'", 
                    trimmedPlayerStatsSheetName, playerStatsSheetNumber);
            }
            else
            {
                _logger.LogWarning("❌ No sheet number matches found for player stats sheet '{PlayerStatsSheet}' with number '{SheetNumber}'", 
                    trimmedPlayerStatsSheetName, playerStatsSheetNumber);
            }
        }
        else 
        {
            if (isMatch1)
            {
                _logger.LogError("❌ MATCH 1 MATCHING: Could not extract sheet number from player stats sheet: '{PlayerStatsSheet}'", trimmedPlayerStatsSheetName);
            }
            else
            {
                _logger.LogWarning("❌ Could not extract sheet number from player stats sheet: '{PlayerStatsSheet}'", trimmedPlayerStatsSheetName);
            }
        }

        // Fallback: Extract the opposition team and date from the player stats sheet name
        var (_, oppositionTeam) = _transformationService.ExtractMatchTeams(playerStatsSheetName);
        var playerStatsDate = _transformationService.ExtractDateFromSheetName(playerStatsSheetName);

        // Second try: exact match on opposition team and date
        foreach (var kvp in matchIdMap)
        {
            var matchSheetName = kvp.Key;
            var matchId = kvp.Value;
            
            // Extract teams and date from match sheet
            var (_, matchOppositionTeam) = _transformationService.ExtractMatchTeams(matchSheetName);
            var matchDate = _transformationService.ExtractDateFromSheetName(matchSheetName);
            
            if (string.Equals(oppositionTeam, matchOppositionTeam, StringComparison.OrdinalIgnoreCase) &&
                playerStatsDate.HasValue && matchDate.HasValue &&
                playerStatsDate.Value == matchDate.Value)
            {
                _logger.LogDebug("Mapped player stats sheet '{PlayerStatsSheet}' to match sheet '{MatchSheet}' (Match ID: {MatchId}) via team and date match", 
                    playerStatsSheetName, matchSheetName, matchId);
                return matchId;
            }
        }
        
        _logger.LogWarning("Could not find matching match for player stats sheet '{PlayerStatsSheet}'. Sheet Number: '{SheetNumber}', Opposition: '{Opposition}', Date: {Date}", 
            playerStatsSheetName, playerStatsSheetNumber, oppositionTeam, playerStatsDate);
        return null;
    }
    
    /// <summary>
    /// Extract sheet number from sheet names like "07. Player Stats vs..." or "07. Drum vs..."
    /// CRITICAL FIX: Handle various numbering formats and whitespace
    /// </summary>
    private string? ExtractSheetNumber(string sheetName)
    {
        if (string.IsNullOrEmpty(sheetName))
            return null;
        
        // CRITICAL FIX: Trim whitespace and handle various number formats
        var trimmedSheetName = sheetName.Trim();
        
        // Try different number extraction patterns
        var patterns = new[]
        {
            @"^(\d{2})\.",     // "01." - two digit format
            @"^(\d{1})\.",     // "1." - single digit format  
            @"^0*(\d+)\."      // "001." or "01." - handle leading zeros
        };
        
        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(trimmedSheetName, pattern);
            if (match.Success)
            {
                // Normalize to 2-digit format for consistent matching
                var number = int.Parse(match.Groups[1].Value);
                var normalizedNumber = number.ToString("00");
                _logger.LogDebug("Extracted sheet number '{Number}' (normalized: '{Normalized}') from sheet name '{SheetName}'", 
                    match.Groups[1].Value, normalizedNumber, sheetName);
                return normalizedNumber;
            }
        }
        
        _logger.LogWarning("Could not extract sheet number from sheet name: '{SheetName}'", sheetName);
        return null;
    }

    private async Task<int> SavePlayerStatisticsAsync(
        int jobId,
        int matchId,
        IList<PlayerStatisticsData> playerStatistics,
        CancellationToken cancellationToken = default)
    {
        const int batchSize = 100; // Process in smaller batches to avoid memory issues
        var totalStatsCreated = 0;

        _logger.LogInformation("Processing {PlayerCount} player statistics in batches of {BatchSize}", 
            playerStatistics.Count, batchSize);

        // Process players in batches
        for (int i = 0; i < playerStatistics.Count; i += batchSize)
        {
            var batch = playerStatistics.Skip(i).Take(batchSize).ToList();
            var batchStatsCreated = await ProcessPlayerStatisticsBatch(jobId, matchId, batch, i / batchSize + 1, cancellationToken);
            totalStatsCreated += batchStatsCreated;
            
            _logger.LogDebug("Batch {BatchNumber} processed: {StatsCreated} statistics created", 
                i / batchSize + 1, batchStatsCreated);
        }

        _logger.LogInformation("Player statistics processing completed: {TotalStats} statistics created from {PlayerCount} players", 
            totalStatsCreated, playerStatistics.Count);

        return totalStatsCreated;
    }

    /// <summary>
    /// Save player statistics using a scoped DbContext to prevent context state corruption
    /// This method creates fresh context state for each sheet processing iteration
    /// </summary>
    private async Task<int> SavePlayerStatisticsWithScopedContextAsync(
        GAAStatDbContext scopedContext,
        int jobId,
        int matchId,
        IList<PlayerStatisticsData> playerStatistics,
        CancellationToken cancellationToken = default)
    {
        const int batchSize = 100;
        var totalStatsCreated = 0;

        _logger.LogInformation("Processing {PlayerCount} player statistics in batches of {BatchSize} using scoped context", 
            playerStatistics.Count, batchSize);

        // Process players in batches
        for (int i = 0; i < playerStatistics.Count; i += batchSize)
        {
            var batch = playerStatistics.Skip(i).Take(batchSize).ToList();
            var batchStatsCreated = await ProcessPlayerStatisticsBatchWithScopedContext(
                scopedContext, jobId, matchId, batch, i / batchSize + 1, cancellationToken);
            totalStatsCreated += batchStatsCreated;
            
            _logger.LogDebug("Batch {BatchNumber} processed with scoped context: {StatsCreated} statistics created", 
                i / batchSize + 1, batchStatsCreated);
        }

        _logger.LogInformation("Player statistics processing completed with scoped context: {TotalStats} statistics created from {PlayerCount} players", 
            totalStatsCreated, playerStatistics.Count);

        return totalStatsCreated;
    }

    private async Task<int> ProcessPlayerStatisticsBatch(
        int jobId,
        int matchId,
        IList<PlayerStatisticsData> playerBatch,
        int batchNumber,
        CancellationToken cancellationToken = default)
    {
        var statsToAdd = new List<MatchPlayerStatistics>();
        var validationErrors = new List<EtlValidationError>();

        _logger.LogDebug("Processing batch {BatchNumber}: {PlayerCount} players, {StatisticsCount} with statistics", 
            batchNumber, playerBatch.Count, playerBatch.Count(p => p.HasStatistics));

        // Step 1: Resolve all players in this batch (optimized for batch processing)
        var playerRequests = playerBatch.Where(p => p.HasStatistics)
            .Select(p => (dynamic)new { PlayerName = p.PlayerName, JerseyNumber = p.JerseyNumber })
            .ToList();
        var playerResolutions = await GetOrCreatePlayersBatchAsync(playerRequests, cancellationToken);
        
        // Track any players that failed resolution
        foreach (var playerData in playerBatch.Where(p => p.HasStatistics))
        {
            if (!playerResolutions.ContainsKey(playerData.PlayerName))
            {
                validationErrors.Add(new EtlValidationError
                {
                    JobId = jobId,
                    SheetName = playerData.SheetName,
                    RowNumber = playerData.RowNumber,
                    ErrorType = EtlErrorTypes.MISSING_DATA,
                    ErrorMessage = $"Failed to resolve player {playerData.PlayerName}",
                    SuggestedFix = "Check player name format",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        // Step 2: Check for existing statistics in bulk
        var playerIds = playerResolutions.Values.Select(p => p.PlayerId).ToList();
        var existingStats = new HashSet<(int PlayerId, int MatchId)>();
        
        if (playerIds.Any())
        {
            var existing = await _context.MatchPlayerStatistics
                .Where(mps => mps.MatchId == matchId && playerIds.Contains(mps.PlayerId))
                .Select(mps => new { mps.PlayerId, mps.MatchId })
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            existingStats = existing.Select(e => (e.PlayerId, e.MatchId)).ToHashSet();
        }

        // Step 3: Prepare statistics for bulk insert
        foreach (var playerData in playerBatch)
        {
            try
            {
                // Record validation errors for this player
                if (playerData.ValidationErrors.Any())
                {
                    foreach (var validationError in playerData.ValidationErrors)
                    {
                        validationErrors.Add(new EtlValidationError
                        {
                            JobId = jobId,
                            SheetName = validationError.SheetName,
                            RowNumber = validationError.RowNumber,
                            ColumnName = validationError.ColumnName,
                            ErrorType = validationError.ErrorType,
                            ErrorMessage = validationError.ErrorMessage,
                            SuggestedFix = validationError.SuggestedFix,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                if (!playerData.HasStatistics)
                {
                    _logger.LogWarning("⚠️ SKIPPING PLAYER: {PlayerName} (Jersey #{JerseyNumber}) - No statistics to save. TE={TE}, TP={TP}, Goals={Goals}, Points={Points}", 
                        playerData.PlayerName, playerData.JerseyNumber, playerData.Statistics.TotalEngagements, 
                        playerData.Statistics.TotalPossessions, playerData.Statistics.Goals, playerData.Statistics.Points);
                        
                    // Record this as a validation warning to track all skipped players
                    validationErrors.Add(new EtlValidationError
                    {
                        JobId = jobId,
                        SheetName = playerData.SheetName,
                        RowNumber = playerData.RowNumber,
                        ErrorType = "player_skipped",
                        ErrorMessage = $"Player {playerData.PlayerName} (#{playerData.JerseyNumber}) skipped - all statistics are zero",
                        SuggestedFix = "Verify player participated in match or has valid statistics in Excel",
                        CreatedAt = DateTime.UtcNow
                    });
                    continue;
                }

                if (!playerResolutions.TryGetValue(playerData.PlayerName, out var player))
                {
                    _logger.LogWarning("Player {PlayerName} could not be resolved, skipping", playerData.PlayerName);
                    continue;
                }

                // Check for duplicates
                if (existingStats.Contains((player.PlayerId, matchId)))
                {
                    _logger.LogWarning("Player statistics already exist for player {PlayerId} in match {MatchId}, skipping", 
                        player.PlayerId, matchId);
                    
                    validationErrors.Add(new EtlValidationError
                    {
                        JobId = jobId,
                        SheetName = playerData.SheetName,
                        RowNumber = playerData.RowNumber,
                        ErrorType = "duplicate_stats",
                        ErrorMessage = $"Statistics already exist for player {playerData.PlayerName}",
                        SuggestedFix = "Remove duplicate player data from Excel file",
                        CreatedAt = DateTime.UtcNow
                    });
                    
                    continue;
                }

                // Create properly mapped statistics entity
                var mappedStats = playerData.Statistics;
                mappedStats.PlayerId = player.PlayerId;
                mappedStats.MatchId = matchId;
                
                // Clear navigation properties to avoid EF Core conflicts
                mappedStats.Match = null!;
                mappedStats.Player = null!;
                
                statsToAdd.Add(mappedStats);
                
                _logger.LogDebug("Added statistics for player {PlayerName} (ID: {PlayerId}) in match {MatchId}", 
                    playerData.PlayerName, player.PlayerId, matchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process statistics for player {PlayerName} in match {MatchId}", 
                    playerData.PlayerName, matchId);
                
                validationErrors.Add(new EtlValidationError
                {
                    JobId = jobId,
                    SheetName = playerData.SheetName,
                    RowNumber = playerData.RowNumber,
                    ErrorType = EtlErrorTypes.MISSING_DATA,
                    ErrorMessage = $"Failed to process statistics for player {playerData.PlayerName}: {ex.Message}",
                    SuggestedFix = "Check player data integrity",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        // Step 4: Bulk insert statistics and validation errors
        var statsCreated = 0;
        if (statsToAdd.Any())
        {
            try
            {
                _logger.LogDebug("Attempting to save {StatsCount} player statistics for batch {BatchNumber}", 
                    statsToAdd.Count, batchNumber);

                // Validate data before saving
                foreach (var stat in statsToAdd)
                {
                    ValidateStatisticsEntity(stat, validationErrors, jobId);
                }

                _context.MatchPlayerStatistics.AddRange(statsToAdd);
                await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                statsCreated = statsToAdd.Count;
                
                _logger.LogInformation("Successfully bulk inserted {StatsCount} player statistics for batch {BatchNumber}", 
                    statsCreated, batchNumber);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database constraint violation when saving player statistics for batch {BatchNumber}. Inner exception: {InnerException}", 
                    batchNumber, dbEx.InnerException?.Message);
                
                // Add detailed error for each statistic
                foreach (var stat in statsToAdd)
                {
                    validationErrors.Add(new EtlValidationError
                    {
                        JobId = jobId,
                        SheetName = "batch_" + batchNumber,
                        ErrorType = EtlErrorTypes.MISSING_DATA,
                        ErrorMessage = $"Database constraint violation for Player ID {stat.PlayerId} in Match ID {stat.MatchId}: {dbEx.InnerException?.Message ?? dbEx.Message}",
                        SuggestedFix = "Check foreign key constraints and data integrity",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error when saving player statistics for batch {BatchNumber}", batchNumber);
                
                validationErrors.Add(new EtlValidationError
                {
                    JobId = jobId,
                    SheetName = "batch_" + batchNumber,
                    ErrorType = EtlErrorTypes.MISSING_DATA,
                    ErrorMessage = $"Failed to save player statistics: {ex.Message}",
                    SuggestedFix = "Check data integrity and constraints",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        if (validationErrors.Any())
        {
            _context.EtlValidationErrors.AddRange(validationErrors);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            
            _logger.LogDebug("Recorded {ErrorCount} validation errors for batch {BatchNumber}", 
                validationErrors.Count, batchNumber);
        }

        return statsCreated;
    }

    /// <summary>
    /// Process player statistics batch using scoped DbContext to prevent context state corruption
    /// </summary>
    private async Task<int> ProcessPlayerStatisticsBatchWithScopedContext(
        GAAStatDbContext scopedContext,
        int jobId,
        int matchId,
        IList<PlayerStatisticsData> playerBatch,
        int batchNumber,
        CancellationToken cancellationToken = default)
    {
        var statsToAdd = new List<MatchPlayerStatistics>();
        var validationErrors = new List<EtlValidationError>();

        _logger.LogDebug("Processing batch {BatchNumber} with scoped context: {PlayerCount} players, {StatisticsCount} with statistics", 
            batchNumber, playerBatch.Count, playerBatch.Count(p => p.HasStatistics));

        // Step 1: Resolve all players in this batch using the scoped context
        var playerRequests = playerBatch.Where(p => p.HasStatistics)
            .Select(p => (dynamic)new { PlayerName = p.PlayerName, JerseyNumber = p.JerseyNumber })
            .ToList();
        var playerResolutions = await GetOrCreatePlayersBatchWithScopedContextAsync(scopedContext, playerRequests, cancellationToken);
        
        // Track any players that failed resolution
        foreach (var playerData in playerBatch.Where(p => p.HasStatistics))
        {
            if (!playerResolutions.ContainsKey(playerData.PlayerName))
            {
                validationErrors.Add(new EtlValidationError
                {
                    JobId = jobId,
                    SheetName = playerData.SheetName,
                    RowNumber = playerData.RowNumber,
                    ErrorType = EtlErrorTypes.MISSING_DATA,
                    ErrorMessage = $"Failed to resolve player {playerData.PlayerName}",
                    SuggestedFix = "Check player name format",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        // Step 2: Check for existing statistics in bulk using scoped context
        var playerIds = playerResolutions.Values.Select(p => p.PlayerId).ToList();
        var existingStats = new HashSet<(int PlayerId, int MatchId)>();
        
        if (playerIds.Any())
        {
            var existing = await scopedContext.MatchPlayerStatistics
                .Where(mps => mps.MatchId == matchId && playerIds.Contains(mps.PlayerId))
                .Select(mps => new { mps.PlayerId, mps.MatchId })
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            existingStats = existing.Select(e => (e.PlayerId, e.MatchId)).ToHashSet();
        }

        // Step 3: Prepare statistics for bulk insert
        foreach (var playerData in playerBatch)
        {
            try
            {
                // Record validation errors for this player
                if (playerData.ValidationErrors.Any())
                {
                    foreach (var validationError in playerData.ValidationErrors)
                    {
                        validationErrors.Add(new EtlValidationError
                        {
                            JobId = jobId,
                            SheetName = validationError.SheetName,
                            RowNumber = validationError.RowNumber,
                            ColumnName = validationError.ColumnName,
                            ErrorType = validationError.ErrorType,
                            ErrorMessage = validationError.ErrorMessage,
                            SuggestedFix = validationError.SuggestedFix,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                if (!playerData.HasStatistics)
                {
                    _logger.LogWarning("⚠️ SKIPPING PLAYER: {PlayerName} (Jersey #{JerseyNumber}) - No statistics to save. TE={TE}, TP={TP}, Goals={Goals}, Points={Points}", 
                        playerData.PlayerName, playerData.JerseyNumber, playerData.Statistics.TotalEngagements, 
                        playerData.Statistics.TotalPossessions, playerData.Statistics.Goals, playerData.Statistics.Points);
                        
                    // Record this as a validation warning to track all skipped players
                    validationErrors.Add(new EtlValidationError
                    {
                        JobId = jobId,
                        SheetName = playerData.SheetName,
                        RowNumber = playerData.RowNumber,
                        ErrorType = "player_skipped",
                        ErrorMessage = $"Player {playerData.PlayerName} (#{playerData.JerseyNumber}) skipped - all statistics are zero",
                        SuggestedFix = "Verify player participated in match or has valid statistics in Excel",
                        CreatedAt = DateTime.UtcNow
                    });
                    continue;
                }

                if (!playerResolutions.TryGetValue(playerData.PlayerName, out var player))
                {
                    _logger.LogWarning("Player {PlayerName} could not be resolved, skipping", playerData.PlayerName);
                    continue;
                }

                // Check for duplicates
                if (existingStats.Contains((player.PlayerId, matchId)))
                {
                    _logger.LogWarning("Player statistics already exist for player {PlayerId} in match {MatchId}, skipping", 
                        player.PlayerId, matchId);
                    
                    validationErrors.Add(new EtlValidationError
                    {
                        JobId = jobId,
                        SheetName = playerData.SheetName,
                        RowNumber = playerData.RowNumber,
                        ErrorType = "duplicate_stats",
                        ErrorMessage = $"Statistics already exist for player {playerData.PlayerName}",
                        SuggestedFix = "Remove duplicate player data from Excel file",
                        CreatedAt = DateTime.UtcNow
                    });
                    
                    continue;
                }

                // Create properly mapped statistics entity
                var mappedStats = playerData.Statistics;
                mappedStats.PlayerId = player.PlayerId;
                mappedStats.MatchId = matchId;
                
                // Clear navigation properties to avoid EF Core conflicts
                mappedStats.Match = null!;
                mappedStats.Player = null!;
                
                statsToAdd.Add(mappedStats);
                
                _logger.LogDebug("Added statistics for player {PlayerName} (ID: {PlayerId}) in match {MatchId}", 
                    playerData.PlayerName, player.PlayerId, matchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process statistics for player {PlayerName} in match {MatchId}", 
                    playerData.PlayerName, matchId);
                
                validationErrors.Add(new EtlValidationError
                {
                    JobId = jobId,
                    SheetName = playerData.SheetName,
                    RowNumber = playerData.RowNumber,
                    ErrorType = EtlErrorTypes.MISSING_DATA,
                    ErrorMessage = $"Failed to process statistics for player {playerData.PlayerName}: {ex.Message}",
                    SuggestedFix = "Check player data integrity",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        // Step 4: Bulk insert statistics and validation errors using scoped context
        var statsCreated = 0;
        if (statsToAdd.Any())
        {
            try
            {
                _logger.LogDebug("Attempting to save {StatsCount} player statistics for batch {BatchNumber} with scoped context", 
                    statsToAdd.Count, batchNumber);

                // Log detailed information about what we're trying to save
                foreach (var stat in statsToAdd.Take(3)) // Log first 3 for debugging
                {
                    _logger.LogDebug("Saving stat for Player ID {PlayerId}, Match ID {MatchId}, Engagement Efficiency: {EngagementEfficiency}", 
                        stat.PlayerId, stat.MatchId, stat.EngagementEfficiency);
                }

                // Validate data before saving
                foreach (var stat in statsToAdd)
                {
                    ValidateStatisticsEntity(stat, validationErrors, jobId);
                }

                _logger.LogDebug("Adding {StatsCount} statistics to context", statsToAdd.Count);
                scopedContext.MatchPlayerStatistics.AddRange(statsToAdd);
                
                _logger.LogDebug("Calling SaveChangesAsync for {StatsCount} statistics", statsToAdd.Count);
                await scopedContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                statsCreated = statsToAdd.Count;
                
                _logger.LogInformation("Successfully bulk inserted {StatsCount} player statistics for batch {BatchNumber} using scoped context", 
                    statsCreated, batchNumber);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database constraint violation when saving player statistics for batch {BatchNumber} with scoped context. Inner exception: {InnerException}. Full exception: {FullException}", 
                    batchNumber, dbEx.InnerException?.Message, dbEx.ToString());
                
                // Add detailed error for each statistic
                foreach (var stat in statsToAdd)
                {
                    validationErrors.Add(new EtlValidationError
                    {
                        JobId = jobId,
                        SheetName = "batch_" + batchNumber,
                        ErrorType = EtlErrorTypes.MISSING_DATA,
                        ErrorMessage = $"Database constraint violation for Player ID {stat.PlayerId} in Match ID {stat.MatchId}: {dbEx.InnerException?.Message ?? dbEx.Message}",
                        SuggestedFix = "Check foreign key constraints and data integrity",
                        CreatedAt = DateTime.UtcNow
                    });
                }
                
                // Re-throw to surface the error to the calling code
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error when saving player statistics for batch {BatchNumber} with scoped context. Full exception: {FullException}", 
                    batchNumber, ex.ToString());
                
                validationErrors.Add(new EtlValidationError
                {
                    JobId = jobId,
                    SheetName = "batch_" + batchNumber,
                    ErrorType = EtlErrorTypes.MISSING_DATA,
                    ErrorMessage = $"Failed to save player statistics: {ex.Message}",
                    SuggestedFix = "Check data integrity and constraints",
                    CreatedAt = DateTime.UtcNow
                });
                
                // Re-throw to surface the error to the calling code
                throw;
            }
        }

        if (validationErrors.Any())
        {
            scopedContext.EtlValidationErrors.AddRange(validationErrors);
            await scopedContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            
            _logger.LogDebug("Recorded {ErrorCount} validation errors for batch {BatchNumber}", 
                validationErrors.Count, batchNumber);
        }

        return statsCreated;
    }

    /// <summary>
    /// Batch version of GetOrCreatePlayerAsync that processes multiple players efficiently
    /// Reduces database round trips and prevents connection exhaustion
    /// </summary>
    private async Task<Dictionary<string, Player>> GetOrCreatePlayersBatchAsync(
        List<dynamic> playerRequests,
        CancellationToken cancellationToken = default)
    {
        var playerResolutions = new Dictionary<string, Player>();
        
        if (!playerRequests.Any())
            return playerResolutions;

        // Extract player names for batch lookup
        var playerNames = playerRequests.Select(p => (string)p.PlayerName).ToList();
        
        _logger.LogDebug("Batch resolving {PlayerCount} players", playerNames.Count);

        // Step 1: Find all existing players in one query
        var existingPlayers = await _context.Players
            .Where(p => playerNames.Contains(p.PlayerName))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        // Add existing players to results
        foreach (var player in existingPlayers)
        {
            playerResolutions[player.PlayerName] = player;
        }

        // Step 2: Identify players that need to be created
        var existingNames = existingPlayers.Select(p => p.PlayerName).ToHashSet();
        var playersToCreate = playerRequests
            .Where(p => !existingNames.Contains((string)p.PlayerName))
            .ToList();

        // Step 3: Create new players in batch
        if (playersToCreate.Any())
        {
            var newPlayers = new List<Player>();
            
            foreach (var playerRequest in playersToCreate)
            {
                var player = new Player
                {
                    PlayerName = ((string)playerRequest.PlayerName).Trim(),
                    JerseyNumber = (int?)playerRequest.JerseyNumber,
                    IsActive = true
                };
                newPlayers.Add(player);
            }

            // Add all new players and save in one transaction
            await _context.Players.AddRangeAsync(newPlayers, cancellationToken).ConfigureAwait(false);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            
            _logger.LogInformation("Created {NewPlayerCount} new players in batch", newPlayers.Count);

            // Add new players to results
            foreach (var player in newPlayers)
            {
                playerResolutions[player.PlayerName] = player;
            }
        }

        // Step 4: Update jersey numbers if needed (batch update)
        var playersNeedingJerseyUpdate = new List<Player>();
        
        foreach (var playerRequest in playerRequests)
        {
            var playerName = (string)playerRequest.PlayerName;
            var jerseyNumber = (int?)playerRequest.JerseyNumber;
            
            if (playerResolutions.TryGetValue(playerName, out var player) &&
                jerseyNumber.HasValue && player.JerseyNumber != jerseyNumber)
            {
                player.JerseyNumber = jerseyNumber;
                playersNeedingJerseyUpdate.Add(player);
            }
        }
        
        if (playersNeedingJerseyUpdate.Any())
        {
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Updated jersey numbers for {UpdateCount} players", playersNeedingJerseyUpdate.Count);
        }

        _logger.LogDebug("Batch player resolution completed: {ResolvedCount} players resolved", playerResolutions.Count);
        return playerResolutions;
    }

    /// <summary>
    /// Scoped context version of GetOrCreatePlayersBatchAsync to prevent context state corruption
    /// </summary>
    private async Task<Dictionary<string, Player>> GetOrCreatePlayersBatchWithScopedContextAsync(
        GAAStatDbContext scopedContext,
        List<dynamic> playerRequests,
        CancellationToken cancellationToken = default)
    {
        var playerResolutions = new Dictionary<string, Player>();
        
        if (!playerRequests.Any())
            return playerResolutions;

        // Extract player names for batch lookup
        var playerNames = playerRequests.Select(p => (string)p.PlayerName).ToList();
        
        _logger.LogDebug("Batch resolving {PlayerCount} players with scoped context", playerNames.Count);

        // Step 1: Find all existing players in one query using scoped context
        var existingPlayers = await scopedContext.Players
            .Where(p => playerNames.Contains(p.PlayerName))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        // Add existing players to results
        foreach (var player in existingPlayers)
        {
            playerResolutions[player.PlayerName] = player;
        }

        // Step 2: Identify players that need to be created
        var existingNames = existingPlayers.Select(p => p.PlayerName).ToHashSet();
        var playersToCreate = playerRequests
            .Where(p => !existingNames.Contains((string)p.PlayerName))
            .ToList();

        // Step 3: Create new players in batch using scoped context
        if (playersToCreate.Any())
        {
            var newPlayers = new List<Player>();
            
            foreach (var playerRequest in playersToCreate)
            {
                var player = new Player
                {
                    PlayerName = ((string)playerRequest.PlayerName).Trim(),
                    JerseyNumber = (int?)playerRequest.JerseyNumber,
                    IsActive = true
                };
                newPlayers.Add(player);
            }

            // Add all new players and save in one transaction using scoped context
            await scopedContext.Players.AddRangeAsync(newPlayers, cancellationToken).ConfigureAwait(false);
            await scopedContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            
            _logger.LogInformation("Created {NewPlayerCount} new players in batch using scoped context", newPlayers.Count);

            // Add new players to results
            foreach (var player in newPlayers)
            {
                playerResolutions[player.PlayerName] = player;
            }
        }

        // Step 4: Update jersey numbers if needed (batch update) using scoped context
        var playersNeedingJerseyUpdate = new List<Player>();
        
        foreach (var playerRequest in playerRequests)
        {
            var playerName = (string)playerRequest.PlayerName;
            var jerseyNumber = (int?)playerRequest.JerseyNumber;
            
            if (playerResolutions.TryGetValue(playerName, out var player) &&
                jerseyNumber.HasValue && player.JerseyNumber != jerseyNumber)
            {
                player.JerseyNumber = jerseyNumber;
                playersNeedingJerseyUpdate.Add(player);
            }
        }
        
        if (playersNeedingJerseyUpdate.Any())
        {
            await scopedContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Updated jersey numbers for {UpdateCount} players using scoped context", playersNeedingJerseyUpdate.Count);
        }

        _logger.LogDebug("Batch player resolution completed with scoped context: {ResolvedCount} players resolved", playerResolutions.Count);
        return playerResolutions;
    }

    private async Task<Player> GetOrCreatePlayerAsync(string playerName, int? jerseyNumber)
    {
        // Legacy method - delegates to batch version for single player
        var request = new List<dynamic> { new { PlayerName = playerName, JerseyNumber = jerseyNumber } };
        var results = await GetOrCreatePlayersBatchAsync(request);
        
        if (results.TryGetValue(playerName, out var player))
            return player;
            
        throw new InvalidOperationException($"Failed to resolve player: {playerName}");
    }

    /// <summary>
    /// Validates a MatchPlayerStatistics entity before database save
    /// Checks for constraint violations and data limits
    /// </summary>
    private void ValidateStatisticsEntity(MatchPlayerStatistics stat, List<EtlValidationError> errors, int jobId)
    {
        // Check required fields
        if (stat.MatchId <= 0)
        {
            errors.Add(new EtlValidationError
            {
                JobId = jobId,
                SheetName = "validation",
                ErrorType = EtlErrorTypes.MISSING_DATA,
                ErrorMessage = $"Invalid match ID: {stat.MatchId}",
                SuggestedFix = "Ensure match exists in database",
                CreatedAt = DateTime.UtcNow
            });
        }

        if (stat.PlayerId <= 0)
        {
            errors.Add(new EtlValidationError
            {
                JobId = jobId,
                SheetName = "validation",
                ErrorType = EtlErrorTypes.MISSING_DATA,
                ErrorMessage = $"Invalid player ID: {stat.PlayerId}",
                SuggestedFix = "Ensure player exists in database",
                CreatedAt = DateTime.UtcNow
            });
        }

        // Validate decimal precision constraints (database expects decimal(5,4))
        ValidateDecimalPrecision(stat.EngagementEfficiency, "EngagementEfficiency", errors, jobId);
        ValidateDecimalPrecision(stat.PossessionSuccessRate, "PossessionSuccessRate", errors, jobId);
        ValidateDecimalPrecision(stat.PossessionsPerTe, "PossessionsPerTe", errors, jobId);
        ValidateDecimalPrecision(stat.ConversionRate, "ConversionRate", errors, jobId);
        ValidateDecimalPrecision(stat.TacklePercentage, "TacklePercentage", errors, jobId);
        ValidateDecimalPrecision(stat.KickoutPercentage, "KickoutPercentage", errors, jobId);

        // Validate string length constraints
        if (!string.IsNullOrEmpty(stat.Scores) && stat.Scores.Length > 20)
        {
            errors.Add(new EtlValidationError
            {
                JobId = jobId,
                SheetName = "validation",
                ErrorType = EtlErrorTypes.MISSING_DATA,
                ErrorMessage = $"Scores field too long: '{stat.Scores}' ({stat.Scores.Length} characters, max 20)",
                SuggestedFix = "Truncate scores text to 20 characters",
                CreatedAt = DateTime.UtcNow
            });
            
            // Truncate to prevent constraint violation
            stat.Scores = stat.Scores.Substring(0, 20);
        }

        // Validate database check constraints
        ValidateCheckConstraints(stat, errors, jobId);
    }

    /// <summary>
    /// Validates decimal precision doesn't exceed database constraint (5,4)
    /// Maximum value: 9.9999 (5 digits total, 4 after decimal)
    /// </summary>
    private void ValidateDecimalPrecision(decimal? value, string fieldName, List<EtlValidationError> errors, int jobId)
    {
        if (!value.HasValue) return;

        // Check if the value exceeds decimal(5,4) limits
        if (value.Value < -9.9999m || value.Value > 9.9999m)
        {
            errors.Add(new EtlValidationError
            {
                JobId = jobId,
                SheetName = "validation",
                ErrorType = EtlErrorTypes.MISSING_DATA,
                ErrorMessage = $"{fieldName} value {value} exceeds decimal(5,4) constraint (max ±9.9999)",
                SuggestedFix = $"Adjust {fieldName} calculation or increase database precision",
                CreatedAt = DateTime.UtcNow
            });
        }

        // Check decimal places (should not exceed 4)
        var decimalPlaces = BitConverter.GetBytes(decimal.GetBits(value.Value)[3])[2];
        if (decimalPlaces > 4)
        {
            errors.Add(new EtlValidationError
            {
                JobId = jobId,
                SheetName = "validation",
                ErrorType = EtlErrorTypes.MISSING_DATA,
                ErrorMessage = $"{fieldName} has too many decimal places: {value} (max 4 decimal places)",
                SuggestedFix = "Round value to 4 decimal places",
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Validates and fixes database check constraints to prevent constraint violations
    /// </summary>
    private void ValidateCheckConstraints(MatchPlayerStatistics stat, List<EtlValidationError> errors, int jobId)
    {
        var constraintViolations = new List<string>();
        
        // Check constraint: chk_kickout_totals
        // (kickouts_retained + kickouts_lost) <= kickouts_total
        if (stat.KickoutsRetained + stat.KickoutsLost > stat.KickoutsTotal)
        {
            var originalTotal = stat.KickoutsTotal;
            stat.KickoutsTotal = stat.KickoutsRetained + stat.KickoutsLost;
            
            constraintViolations.Add($"Kickout totals constraint violation: retained ({stat.KickoutsRetained}) + lost ({stat.KickoutsLost}) > total ({originalTotal}). Fixed total to {stat.KickoutsTotal}");
        }
        
        // Check constraint: chk_carry_totals  
        // (carry_retained + carry_lost) <= total_attacks
        if (stat.CarryRetained + stat.CarryLost > stat.TotalAttacks)
        {
            var originalTotal = stat.TotalAttacks;
            stat.TotalAttacks = Math.Max(stat.TotalAttacks, stat.CarryRetained + stat.CarryLost);
            
            constraintViolations.Add($"Carry totals constraint violation: carry_retained ({stat.CarryRetained}) + carry_lost ({stat.CarryLost}) > total_attacks ({originalTotal}). Fixed total_attacks to {stat.TotalAttacks}");
        }
        
        // Check constraint: chk_tackle_totals
        // (tackles_contact + tackles_missed) <= tackles_total  
        if (stat.TacklesContact + stat.TacklesMissed > stat.TacklesTotal)
        {
            var originalTotal = stat.TacklesTotal;
            stat.TacklesTotal = stat.TacklesContact + stat.TacklesMissed;
            
            constraintViolations.Add($"Tackle totals constraint violation: contact ({stat.TacklesContact}) + missed ({stat.TacklesMissed}) > total ({originalTotal}). Fixed total to {stat.TacklesTotal}");
        }
        
        // Check constraint: chk_percentage_rates
        // All percentage fields should be between 0.0 and 1.0
        // UPDATED: Engagement efficiency can now be > 1.0 since we read from Excel TE/PSR column directly
        // Excel values can exceed 1.0 (e.g., 1.5000) so we allow 0.0 to 2.0 range
        ValidateExtendedPercentageRange(stat, stat.EngagementEfficiency, nameof(stat.EngagementEfficiency), constraintViolations, 0.0m, 2.0m);
        ValidatePercentageRange(stat, stat.PossessionSuccessRate, nameof(stat.PossessionSuccessRate), constraintViolations);
        ValidatePercentageRange(stat, stat.ConversionRate, nameof(stat.ConversionRate), constraintViolations);
        ValidatePercentageRange(stat, stat.TacklePercentage, nameof(stat.TacklePercentage), constraintViolations);
        ValidatePercentageRange(stat, stat.KickoutPercentage, nameof(stat.KickoutPercentage), constraintViolations);
        
        // Check constraint: chk_positive_stats
        // All integer statistics should be >= 0
        var negativeStats = new List<(string field, int value)>();
        
        if (stat.MinutesPlayed < 0) negativeStats.Add((nameof(stat.MinutesPlayed), stat.MinutesPlayed));
        if (stat.TotalEngagements < 0) negativeStats.Add((nameof(stat.TotalEngagements), stat.TotalEngagements));
        if (stat.TotalPossessions < 0) negativeStats.Add((nameof(stat.TotalPossessions), stat.TotalPossessions));
        if (stat.TurnoversWon < 0) negativeStats.Add((nameof(stat.TurnoversWon), stat.TurnoversWon));
        if (stat.Interceptions < 0) negativeStats.Add((nameof(stat.Interceptions), stat.Interceptions));
        if (stat.TotalAttacks < 0) negativeStats.Add((nameof(stat.TotalAttacks), stat.TotalAttacks));
        if (stat.KickRetained < 0) negativeStats.Add((nameof(stat.KickRetained), stat.KickRetained));
        if (stat.KickLost < 0) negativeStats.Add((nameof(stat.KickLost), stat.KickLost));
        if (stat.CarryRetained < 0) negativeStats.Add((nameof(stat.CarryRetained), stat.CarryRetained));
        if (stat.CarryLost < 0) negativeStats.Add((nameof(stat.CarryLost), stat.CarryLost));
        if (stat.ShotsTotal < 0) negativeStats.Add((nameof(stat.ShotsTotal), stat.ShotsTotal));
        if (stat.Goals < 0) negativeStats.Add((nameof(stat.Goals), stat.Goals));
        if (stat.Points < 0) negativeStats.Add((nameof(stat.Points), stat.Points));
        if (stat.Wides < 0) negativeStats.Add((nameof(stat.Wides), stat.Wides));
        if (stat.TacklesTotal < 0) negativeStats.Add((nameof(stat.TacklesTotal), stat.TacklesTotal));
        if (stat.TacklesContact < 0) negativeStats.Add((nameof(stat.TacklesContact), stat.TacklesContact));
        if (stat.TacklesMissed < 0) negativeStats.Add((nameof(stat.TacklesMissed), stat.TacklesMissed));
        if (stat.FreesConcededTotal < 0) negativeStats.Add((nameof(stat.FreesConcededTotal), stat.FreesConcededTotal));
        if (stat.YellowCards < 0) negativeStats.Add((nameof(stat.YellowCards), stat.YellowCards));
        if (stat.BlackCards < 0) negativeStats.Add((nameof(stat.BlackCards), stat.BlackCards));
        if (stat.RedCards < 0) negativeStats.Add((nameof(stat.RedCards), stat.RedCards));
        if (stat.KickoutsTotal < 0) negativeStats.Add((nameof(stat.KickoutsTotal), stat.KickoutsTotal));
        if (stat.KickoutsRetained < 0) negativeStats.Add((nameof(stat.KickoutsRetained), stat.KickoutsRetained));
        if (stat.KickoutsLost < 0) negativeStats.Add((nameof(stat.KickoutsLost), stat.KickoutsLost));
        if (stat.Saves < 0) negativeStats.Add((nameof(stat.Saves), stat.Saves));
        
        if (negativeStats.Any())
        {
            foreach (var (field, value) in negativeStats)
            {
                constraintViolations.Add($"Negative value constraint violation: {field} = {value} (must be >= 0). Setting to 0");
                // Use reflection to set the field to 0
                var property = typeof(MatchPlayerStatistics).GetProperty(field);
                property?.SetValue(stat, 0);
            }
        }
        
        // Log constraint violations as warnings and create validation errors
        if (constraintViolations.Any())
        {
            foreach (var violation in constraintViolations)
            {
                _logger.LogWarning("Database constraint validation and fix applied for Player {PlayerId} in Match {MatchId}: {Violation}", 
                    stat.PlayerId, stat.MatchId, violation);
                
                errors.Add(new EtlValidationError
                {
                    JobId = jobId,
                    SheetName = "constraint_validation",
                    ErrorType = "constraint_violation_fixed",
                    ErrorMessage = violation,
                    SuggestedFix = "Data automatically corrected to satisfy database constraints",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
    }
    
    /// <summary>
    /// Validates that percentage values are within valid range (0.0 to 1.0)
    /// </summary>
    private void ValidatePercentageRange(MatchPlayerStatistics stat, decimal? percentage, string fieldName, List<string> constraintViolations)
    {
        if (!percentage.HasValue) return;
        
        var originalValue = percentage.Value;
        var clampedValue = Math.Max(0.0m, Math.Min(1.0m, originalValue));
        
        if (originalValue != clampedValue)
        {
            constraintViolations.Add($"Percentage range violation: {fieldName} = {originalValue} (must be 0.0-1.0). Fixed to {clampedValue}");
            
            // Use reflection to set the corrected value
            var property = typeof(MatchPlayerStatistics).GetProperty(fieldName);
            property?.SetValue(stat, clampedValue);
        }
    }

    /// <summary>
    /// Validates that values are within a custom range (e.g., engagement efficiency can exceed 1.0)
    /// </summary>
    private void ValidateExtendedPercentageRange(MatchPlayerStatistics stat, decimal? percentage, string fieldName, List<string> constraintViolations, decimal minValue, decimal maxValue)
    {
        if (!percentage.HasValue) return;
        
        var originalValue = percentage.Value;
        
        if (originalValue < minValue || originalValue > maxValue)
        {
            constraintViolations.Add($"Extended range violation: {fieldName} = {originalValue:F4} (must be {minValue:F1}-{maxValue:F1}). Player {stat.PlayerId}");
        }
    }

    // KPI processing methods temporarily removed to fix compilation errors

    #endregion
}