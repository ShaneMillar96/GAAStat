using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private readonly GAAStatDbContext _context;
    private readonly ILogger<ExcelProcessingService> _logger;

    public ExcelProcessingService(
        IExcelParsingService parsingService,
        IProgressTrackingService progressService,
        IDataTransformationService transformationService,
        GAAStatDbContext context,
        ILogger<ExcelProcessingService> logger)
    {
        _parsingService = parsingService;
        _progressService = progressService;
        _transformationService = transformationService;
        _context = context;
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
            // Phase 1: Initialize processing
            await RecordProgress(jobId, EtlStages.INITIALIZING, "Starting ETL processing", 7, 0);
            await _progressService.MarkJobStartedAsync(jobId);

            // Phase 2: Clear existing data
            await RecordProgress(jobId, EtlStages.CLEARING_DATA, "Clearing existing match data", 7, 1);
            await ClearExistingMatchDataAsync(jobId);

            // Phase 3: Parse Excel file
            await RecordProgress(jobId, EtlStages.PARSING_EXCEL, "Analyzing Excel file structure", 7, 2);
            
            var analysisResult = await _parsingService.AnalyzeExcelFileAsync(fileStream, fileName);
            if (!analysisResult.IsSuccess)
            {
                await _progressService.UpdateEtlJobStatusAsync(jobId, EtlJobStatus.FAILED, analysisResult.ErrorMessage);
                return ServiceResult<ExcelProcessingResult>.Failed(analysisResult.ErrorMessage!);
            }

            var analysis = analysisResult.Data;
            if (!analysis.IsValidGaaFile)
            {
                var errorMsg = string.Join("; ", analysis.ValidationErrors);
                await _progressService.UpdateEtlJobStatusAsync(jobId, EtlJobStatus.FAILED, $"Invalid GAA file: {errorMsg}");
                return ServiceResult<ExcelProcessingResult>.Failed($"Invalid GAA file: {errorMsg}");
            }

            // Phase 4: Parse match data from all sheets
            await RecordProgress(jobId, EtlStages.PARSING_EXCEL, "Parsing match data from sheets", 7, 3);
            
            // Reset stream position for parsing
            fileStream.Position = 0;
            var matchDataResult = await _parsingService.ParseAllMatchDataAsync(fileStream);
            if (!matchDataResult.IsSuccess)
            {
                await _progressService.UpdateEtlJobStatusAsync(jobId, EtlJobStatus.FAILED, matchDataResult.ErrorMessage);
                return ServiceResult<ExcelProcessingResult>.Failed(matchDataResult.ErrorMessage!);
            }

            var allMatchData = matchDataResult.Data.ToList();

            // Phase 5: Validate data
            await RecordProgress(jobId, EtlStages.VALIDATING_DATA, "Validating match data", 7, 4);
            
            var validationResult = await ValidateMatchDataAsync(jobId, allMatchData);
            if (!validationResult.IsSuccess)
            {
                await _progressService.UpdateEtlJobStatusAsync(jobId, EtlJobStatus.FAILED, "Data validation failed");
                return ServiceResult<ExcelProcessingResult>.Failed("Data validation failed");
            }

            warningMessages.AddRange(validationResult.ValidationErrors);

            // Phase 6: Save matches to database
            await RecordProgress(jobId, EtlStages.SAVING_MATCHES, "Creating match records", 7, 5);
            
            var saveResult = await SaveMatchesToDatabaseAsync(jobId, allMatchData);
            if (!saveResult.IsSuccess)
            {
                await _progressService.UpdateEtlJobStatusAsync(jobId, EtlJobStatus.FAILED, saveResult.ErrorMessage);
                return ServiceResult<ExcelProcessingResult>.Failed(saveResult.ErrorMessage!);
            }

            var matchesCreated = saveResult.Data.MatchesCreated;
            var matchIdMap = saveResult.Data.MatchIdMap;

            // Phase 7: Parse and save player statistics
            await RecordProgress(jobId, EtlStages.SAVING_PLAYER_STATS, "Processing player statistics", 7, 6);
            
            var playerStatsResult = await ProcessPlayerStatisticsAsync(jobId, fileStream, analysis.Sheets, matchIdMap);
            if (!playerStatsResult.IsSuccess)
            {
                // Log error but don't fail the entire process for player stats issues
                _logger.LogWarning("Player statistics processing failed: {Error}", playerStatsResult.ErrorMessage);
                warningMessages.Add($"Player statistics processing incomplete: {playerStatsResult.ErrorMessage}");
            }

            var playerStatsCreated = playerStatsResult.Data;

            // Phase 8: Finalize
            await RecordProgress(jobId, EtlStages.FINALIZING, "Finalizing ETL process", 7, 7);
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
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to process Excel file {FileName} for job {JobId}", fileName, jobId);
            
            await _progressService.UpdateEtlJobStatusAsync(jobId, EtlJobStatus.FAILED, ex.Message);
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
                .ToListAsync();

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
    /// Clears all existing match-related data from the database before importing new data
    /// Ensures referential integrity by deleting dependent records first
    /// </summary>
    private async Task ClearExistingMatchDataAsync(int jobId)
    {
        try
        {
            _logger.LogInformation("Starting data cleanup for ETL job {JobId}", jobId);

            // Clear dependent tables first (respecting foreign key constraints)
            var deletedCounts = new Dictionary<string, int>();

            // Delete match-dependent analysis data
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

            // Delete main matches table
            deletedCounts["matches"] = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM matches");

            // Clear previous ETL validation errors (optional cleanup)
            deletedCounts["etl_validation_errors"] = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM etl_validation_errors WHERE job_id != @p0", jobId);

            // Log cleanup results
            var totalDeleted = deletedCounts.Values.Sum();
            _logger.LogInformation("Data cleanup completed for ETL job {JobId}. Deleted {TotalRecords} records: {Details}",
                jobId, totalDeleted, string.Join(", ", deletedCounts.Select(kvp => $"{kvp.Key}: {kvp.Value}")));
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
            await _progressService.RecordValidationErrorsAsync(jobId, validationErrors);
            
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
                        m.Opposition.TeamName == match.AwayTeam);

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
                        "Remove duplicate match data from Excel file");
                    
                    // Still map existing match ID for player stats
                    matchIdMap[match.SheetName] = existingMatch.MatchId;
                    continue;
                }

                // Resolve or create opposition team
                var oppositionTeam = await GetOrCreateTeamAsync(match.AwayTeam == GaaConstants.DEFAULT_HOME_TEAM ? match.HomeTeam : match.AwayTeam);
                var season = await GetCurrentSeasonAsync();
                var venue = await GetVenueAsync(match.Venue ?? "Away");
                var competition = await GetOrCreateCompetitionAsync(match.Competition ?? "League");

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
                    MatchResultId = await DetermineMatchResultAsync(match.HomeGoals ?? 0, match.HomePoints ?? 0, match.AwayGoals ?? 0, match.AwayPoints ?? 0),
                    SeasonId = season.SeasonId
                };

                await _context.Matches.AddAsync(matchEntity);
                matchesCreated++;
            }

            await _context.SaveChangesAsync();

            // Get the generated match IDs after saving
            foreach (var match in matchData)
            {
                if (!matchIdMap.ContainsKey(match.SheetName))
                {
                    var savedMatch = await _context.Matches
                        .FirstOrDefaultAsync(m => 
                            m.Date == match.MatchDate && 
                            m.Opposition.TeamName == (match.AwayTeam == GaaConstants.DEFAULT_HOME_TEAM ? match.HomeTeam : match.AwayTeam));
                    
                    if (savedMatch != null)
                    {
                        matchIdMap[match.SheetName] = savedMatch.MatchId;
                    }
                }
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

        var result = await _context.MatchResults.FirstOrDefaultAsync(mr => mr.ResultCode == resultCode);
        return result?.MatchResultId ?? 1; // Default to first result if not found
    }

    private async Task<Team> GetOrCreateTeamAsync(string teamName)
    {
        var team = await _context.Teams.FirstOrDefaultAsync(t => t.TeamName == teamName);
        if (team == null)
        {
            team = new Team
            {
                TeamName = teamName,
                County = "Derry"
            };
            await _context.Teams.AddAsync(team);
            await _context.SaveChangesAsync();
        }
        return team;
    }

    private async Task<Season> GetCurrentSeasonAsync()
    {
        // For now, get or create 2025 season
        var season = await _context.Seasons.FirstOrDefaultAsync(s => s.SeasonName == "2025");
        if (season == null)
        {
            season = new Season
            {
                SeasonName = "2025",
                StartDate = new DateOnly(2025, 1, 1),
                EndDate = new DateOnly(2025, 12, 31),
                IsCurrent = true
            };
            await _context.Seasons.AddAsync(season);
            await _context.SaveChangesAsync();
        }
        return season;
    }

    private async Task<Venue> GetVenueAsync(string venueName)
    {
        var venueCode = venueName.ToUpperInvariant().Substring(0, 1); // H, A, or N
        var venue = await _context.Venues.FirstOrDefaultAsync(v => v.VenueCode == venueCode);
        
        if (venue != null)
            return venue;
            
        // Try to get any venue as fallback
        var fallbackVenue = await _context.Venues.FirstOrDefaultAsync();
        if (fallbackVenue != null)
            return fallbackVenue;
            
        // If no venues exist, create a default one
        var defaultVenue = new Venue
        {
            VenueCode = "A",
            VenueDescription = "Away"
        };
        
        await _context.Venues.AddAsync(defaultVenue);
        await _context.SaveChangesAsync();
        return defaultVenue;
    }

    private async Task<Competition> GetOrCreateCompetitionAsync(string competitionName)
    {
        var competition = await _context.Competitions.FirstOrDefaultAsync(c => c.CompetitionName == competitionName);
        if (competition == null)
        {
            // Get or create default competition type
            var competitionType = await _context.CompetitionTypes.FirstOrDefaultAsync();
            if (competitionType == null)
            {
                competitionType = new CompetitionType
                {
                    TypeName = "League",
                    Description = "Default league competition"
                };
                await _context.CompetitionTypes.AddAsync(competitionType);
                await _context.SaveChangesAsync();
            }
            
            competition = new Competition
            {
                CompetitionName = competitionName,
                Season = "2025",
                CompetitionTypeId = competitionType.CompetitionTypeId
            };
            await _context.Competitions.AddAsync(competition);
            await _context.SaveChangesAsync();
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

        var matchResult = await _context.MatchResults.FirstOrDefaultAsync(mr => mr.ResultCode == resultCode);
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
            await _context.MatchResults.AddAsync(matchResult);
            await _context.SaveChangesAsync();
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
            var totalPlayerStatsCreated = 0;
            var sheetsWithMatchData = sheets.Where(s => s.ContainsMatchData).ToList();

            _logger.LogInformation("Processing player statistics for {SheetCount} match sheets", sheetsWithMatchData.Count);

            foreach (var sheet in sheetsWithMatchData)
            {
                if (!matchIdMap.TryGetValue(sheet.Name, out var matchId))
                {
                    _logger.LogWarning("No match ID found for sheet {SheetName}, skipping player statistics", sheet.Name);
                    continue;
                }

                fileStream.Position = 0; // Reset stream for each sheet
                
                var playerStatsResult = await _parsingService.ParsePlayerStatisticsFromSheetAsync(
                    fileStream, sheet.Name, matchId);

                if (!playerStatsResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to parse player statistics from sheet {SheetName}: {Error}", 
                        sheet.Name, playerStatsResult.ErrorMessage);
                    
                    await _progressService.RecordValidationErrorAsync(
                        jobId,
                        sheet.Name,
                        null,
                        null,
                        EtlErrorTypes.SHEET_STRUCTURE,
                        $"Failed to parse player statistics: {playerStatsResult.ErrorMessage}",
                        "Check sheet format and data integrity");
                    
                    continue;
                }

                var playerStatistics = playerStatsResult.Data.ToList();
                if (!playerStatistics.Any())
                {
                    _logger.LogInformation("No player statistics found in sheet {SheetName}", sheet.Name);
                    continue;
                }

                var statsCreated = await SavePlayerStatisticsAsync(jobId, matchId, playerStatistics);
                totalPlayerStatsCreated += statsCreated;

                _logger.LogInformation("Created {StatsCount} player statistics records for sheet {SheetName}", 
                    statsCreated, sheet.Name);
            }

            return ServiceResult<int>.Success(totalPlayerStatsCreated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process player statistics for job {JobId}", jobId);
            return ServiceResult<int>.Failed($"Failed to process player statistics: {ex.Message}");
        }
    }

    private async Task<int> SavePlayerStatisticsAsync(
        int jobId,
        int matchId,
        IList<PlayerStatisticsData> playerStatistics)
    {
        var statsCreated = 0;

        foreach (var playerData in playerStatistics)
        {
            try
            {
                // Resolve or create player
                var player = await GetOrCreatePlayerAsync(playerData.PlayerName, playerData.JerseyNumber);
                
                // Update the statistics entity with the resolved player ID
                playerData.Statistics.PlayerId = player.PlayerId;
                playerData.Statistics.MatchId = matchId;

                // Check for duplicate statistics
                var existingStats = await _context.MatchPlayerStatistics
                    .FirstOrDefaultAsync(mps => 
                        mps.MatchId == matchId && 
                        mps.PlayerId == player.PlayerId);

                if (existingStats != null)
                {
                    _logger.LogWarning("Player statistics already exist for player {PlayerId} in match {MatchId}, skipping", 
                        player.PlayerId, matchId);
                    
                    await _progressService.RecordValidationErrorAsync(
                        jobId,
                        playerData.SheetName,
                        playerData.RowNumber,
                        null,
                        "duplicate_stats",
                        $"Statistics already exist for player {playerData.PlayerName}",
                        "Remove duplicate player data from Excel file");
                    
                    continue;
                }

                // Record validation errors for this player
                if (playerData.ValidationErrors.Any())
                {
                    await _progressService.RecordValidationErrorsAsync(jobId, playerData.ValidationErrors);
                }

                // Save statistics if player has meaningful data
                if (playerData.HasStatistics)
                {
                    await _context.MatchPlayerStatistics.AddAsync(playerData.Statistics);
                    statsCreated++;
                }
                else
                {
                    _logger.LogDebug("Skipping player {PlayerName} - no statistics to save", playerData.PlayerName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save statistics for player {PlayerName} in match {MatchId}", 
                    playerData.PlayerName, matchId);
                
                await _progressService.RecordValidationErrorAsync(
                    jobId,
                    playerData.SheetName,
                    playerData.RowNumber,
                    null,
                    EtlErrorTypes.MISSING_DATA,
                    $"Failed to save statistics for player {playerData.PlayerName}: {ex.Message}",
                    "Check player data integrity");
            }
        }

        if (statsCreated > 0)
        {
            await _context.SaveChangesAsync();
        }

        return statsCreated;
    }

    private async Task<Player> GetOrCreatePlayerAsync(string playerName, int? jerseyNumber)
    {
        // Try to find existing player by name first
        var player = await _context.Players
            .FirstOrDefaultAsync(p => p.PlayerName == playerName);

        if (player == null)
        {
            // Create new player
            player = new Player
            {
                PlayerName = playerName,
                JerseyNumber = jerseyNumber,
                IsActive = true
            };
            
            await _context.Players.AddAsync(player);
            await _context.SaveChangesAsync(); // Save to get ID
            
            _logger.LogInformation("Created new player: {PlayerName} (Jersey: {JerseyNumber})", 
                playerName, jerseyNumber);
        }
        else if (jerseyNumber.HasValue && player.JerseyNumber != jerseyNumber)
        {
            // Update jersey number if provided and different
            player.JerseyNumber = jerseyNumber;
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Updated jersey number for player {PlayerName}: {JerseyNumber}", 
                playerName, jerseyNumber);
        }

        return player;
    }

    #endregion
}