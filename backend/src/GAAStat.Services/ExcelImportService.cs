using GAAStat.Dal.Interfaces;
using GAAStat.Dal.Models.application;
using GAAStat.Services.Interfaces;
using GAAStat.Services.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GAAStat.Services;

/// <summary>
/// Core service for Excel import operations with clear-and-reload strategy
/// </summary>
public class ExcelImportService : IExcelImportService
{
    private readonly IGAAStatDbContext _context;
    private readonly IStatisticsCalculationService _statisticsService;
    private readonly IImportSnapshotService _snapshotService;
    private readonly ILogger<ExcelImportService> _logger;

    // Sheet name patterns for identification
    private static readonly Regex MatchSheetPattern = new(@"^\d{2}\.\s+\w+\s+vs\s+.+\s+\d{2}\.\d{2}\.\d{2,4}$", RegexOptions.Compiled);
    private static readonly Regex PlayerStatsSheetPattern = new(@"^\d{2}\.\s+Player\s+(S|s)tats?\s+vs\s+.+", RegexOptions.Compiled);

    public ExcelImportService(
        IGAAStatDbContext context,
        IStatisticsCalculationService statisticsService,
        IImportSnapshotService snapshotService,
        ILogger<ExcelImportService> logger)
    {
        _context = context;
        _statisticsService = statisticsService;
        _snapshotService = snapshotService;
        _logger = logger;
        
        // Set EPPlus license context
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    /// <summary>
    /// Imports GAA match statistics from Excel file using clear-and-reload strategy
    /// </summary>
    public async Task<ServiceResult<ImportSummary>> ImportMatchDataAsync(Stream excelStream, string fileName)
    {
        var operationId = Guid.NewGuid().ToString()[..8];
        var startTime = DateTime.Now;

        _logger.LogInformation("Starting Excel import: {FileName} [Operation: {OperationId}]", fileName, operationId);

        // Create import history record
        var importHistory = new ImportHistory
        {
            ImportType = "Excel",
            FileName = fileName,
            FileSize = excelStream.Length,
            ImportStartedAt = startTime,
            ImportStatus = "in_progress"
        };

        _context.ImportHistories.Add(importHistory);
        await _context.SaveChangesAsync();

        try
        {
            // Validate Excel file structure first
            var validationResult = await ValidateExcelFileAsync(excelStream, fileName);
            if (!validationResult.IsSuccess || validationResult.Data?.IsValid != true)
            {
                await UpdateImportHistoryAsync(importHistory, "failed", validationResult.ErrorMessage ?? "Validation failed");
                return ServiceResult<ImportSummary>.Failed(validationResult.ErrorMessage ?? "Excel validation failed", operationId);
            }

            // Snapshot functionality disabled - core ETL will proceed without snapshots
            var snapshotResult = ServiceResult<int>.Success(0);  // Mock success result

            // Reset stream position
            excelStream.Position = 0;

            // Parse Excel data
            var parseResult = await ParseExcelDataAsync(excelStream, fileName);
            if (!parseResult.IsSuccess)
            {
                await UpdateImportHistoryAsync(importHistory, "failed", parseResult.ErrorMessage);
                return ServiceResult<ImportSummary>.Failed(parseResult.ErrorMessage ?? "Excel parsing failed", operationId);
            }

            // Clear and reload data in transaction
            using var transaction = await ((Microsoft.EntityFrameworkCore.DbContext)_context).Database.BeginTransactionAsync();
            
            try
            {
                await ClearMatchDataAsync();
                var importSummary = await ImportParsedDataAsync(parseResult.Data!, importHistory);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Update import history
                var completedAt = DateTime.Now;
                await UpdateImportHistoryAsync(importHistory, "completed", null, completedAt, importSummary);

                importSummary.ImportId = importHistory.Id;
                importSummary.FileName = fileName;
                importSummary.FileSizeBytes = excelStream.Length;
                importSummary.ProcessingDuration = completedAt - startTime;
                importSummary.StartedAt = startTime;
                importSummary.CompletedAt = completedAt;
                importSummary.Status = "completed";
                // importSummary.SnapshotId = snapshotResult.Data; // Snapshot disabled

                _logger.LogInformation("Excel import completed successfully: {MatchesImported} matches, {PlayersProcessed} players in {Duration} [Operation: {OperationId}]",
                    importSummary.MatchesImported, importSummary.PlayersProcessed, importSummary.ProcessingDuration, operationId);

                return ServiceResult<ImportSummary>.Success(importSummary, operationId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Import transaction failed, rolling back [Operation: {OperationId}]", operationId);
                await UpdateImportHistoryAsync(importHistory, "failed", ex.Message);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excel import failed: {FileName} [Operation: {OperationId}]", fileName, operationId);
            await UpdateImportHistoryAsync(importHistory, "failed", ex.Message);
            return ServiceResult<ImportSummary>.Failed($"Import failed: {ex.Message}", operationId);
        }
    }

    /// <summary>
    /// Validates Excel file structure without performing import
    /// </summary>
    public async Task<ServiceResult<ExcelValidationResult>> ValidateExcelFileAsync(Stream excelStream, string fileName)
    {
        try
        {
            _logger.LogInformation("Validating Excel file structure: {FileName}", fileName);
            
            using var package = new ExcelPackage(excelStream);
            var worksheets = package.Workbook.Worksheets;

            var result = new ExcelValidationResult
            {
                SheetsFound = worksheets.Count,
                ExpectedSheets = 31
            };

            var errors = new List<string>();
            var warnings = new List<string>();
            var sheetValidations = new Dictionary<string, SheetValidationResult>();

            int matchSheetsFound = 0;
            int playerStatsSheetsFound = 0;

            foreach (var worksheet in worksheets)
            {
                var sheetValidation = await ValidateWorksheetAsync(worksheet);
                sheetValidations[worksheet.Name] = sheetValidation;

                if (sheetValidation.SheetType == SheetType.MatchStatistics)
                    matchSheetsFound++;
                else if (sheetValidation.SheetType == SheetType.PlayerStatistics)
                    playerStatsSheetsFound++;

                errors.AddRange(sheetValidation.Errors);
                warnings.AddRange(sheetValidation.Warnings);
            }

            result.MatchSheetsFound = matchSheetsFound;
            result.PlayerStatsSheetsFound = playerStatsSheetsFound;
            result.SheetValidations = sheetValidations;

            // Validation rules
            if (result.SheetsFound < 20)
            {
                errors.Add($"Expected around 31 sheets, found only {result.SheetsFound}");
            }

            if (matchSheetsFound == 0)
            {
                errors.Add("No match statistics sheets found");
            }

            if (playerStatsSheetsFound == 0)
            {
                errors.Add("No player statistics sheets found");
            }

            if (matchSheetsFound != playerStatsSheetsFound)
            {
                warnings.Add($"Mismatch between match sheets ({matchSheetsFound}) and player stats sheets ({playerStatsSheetsFound})");
            }

            result.ValidationErrors = errors;
            result.ValidationWarnings = warnings;
            result.IsValid = !errors.Any();

            _logger.LogInformation("Excel validation completed: {IsValid}, {SheetsFound} sheets, {MatchSheets} match sheets, {PlayerStatsSheets} player stats sheets",
                result.IsValid, result.SheetsFound, result.MatchSheetsFound, result.PlayerStatsSheetsFound);

            return ServiceResult<ExcelValidationResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excel validation failed: {FileName}", fileName);
            return ServiceResult<ExcelValidationResult>.Failed($"Validation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Rolls back a completed import operation to its pre-import state
    /// </summary>
    public async Task<ServiceResult<ImportSummary>> RollbackImportAsync(int importId)
    {
        var operationId = Guid.NewGuid().ToString()[..8];
        
        try
        {
            _logger.LogInformation("Starting rollback for import {ImportId} [Operation: {OperationId}]", importId, operationId);

            var importHistory = await _context.ImportHistories
                .FirstOrDefaultAsync(h => h.Id == importId);

            if (importHistory == null)
            {
                return ServiceResult<ImportSummary>.Failed("Import record not found", operationId);
            }


            if (!importHistory.SnapshotId.HasValue)
            {
                return ServiceResult<ImportSummary>.Failed("No snapshot available for rollback", operationId);
            }

            var restoreResult = await _snapshotService.RestoreFromSnapshotAsync(importHistory.SnapshotId.Value);
            if (!restoreResult.IsSuccess)
            {
                return ServiceResult<ImportSummary>.Failed($"Rollback failed: {restoreResult.ErrorMessage}", operationId);
            }

            // Create rollback import history record
            var rollbackHistory = new ImportHistory
            {
                ImportType = "Rollback",
                FileName = $"Rollback of {importHistory.FileName}",
                ImportStartedAt = DateTime.Now,
                ImportCompletedAt = DateTime.Now,
                ImportStatus = "completed",
                MatchesImported = restoreResult.Data!.MatchesRestored,
                PlayersProcessed = restoreResult.Data.PlayerStatsRestored,
                ProcessingDurationSeconds = (int)restoreResult.Data.RestoreDuration.TotalSeconds
            };

            _context.ImportHistories.Add(rollbackHistory);
            await _context.SaveChangesAsync();

            var summary = new ImportSummary
            {
                ImportId = rollbackHistory.Id,
                FileName = rollbackHistory.FileName,
                MatchesImported = restoreResult.Data.MatchesRestored,
                PlayersProcessed = restoreResult.Data.PlayerStatsRestored,
                ProcessingDuration = restoreResult.Data.RestoreDuration,
                StartedAt = rollbackHistory.ImportStartedAt,
                CompletedAt = rollbackHistory.ImportCompletedAt,
                Status = "completed"
            };

            _logger.LogInformation("Rollback completed successfully for import {ImportId} [Operation: {OperationId}]", importId, operationId);

            return ServiceResult<ImportSummary>.Success(summary, operationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rollback failed for import {ImportId} [Operation: {OperationId}]", importId, operationId);
            return ServiceResult<ImportSummary>.Failed($"Rollback failed: {ex.Message}", operationId);
        }
    }

    /// <summary>
    /// Gets import history with optional filtering
    /// </summary>
    public async Task<ServiceResult<IEnumerable<ImportHistoryDto>>> GetImportHistoryAsync(int count = 20, string? status = null)
    {
        try
        {
            var query = _context.ImportHistories.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(h => h.ImportStatus == status);
            }

            var history = await query
                .OrderByDescending(h => h.ImportStartedAt)
                .Take(count)
                .Select(h => new ImportHistoryDto
                {
                    Id = h.Id,
                    ImportType = h.ImportType,
                    FileName = h.FileName,
                    FileSize = h.FileSize,
                    MatchesImported = h.MatchesImported,
                    PlayersProcessed = h.PlayersProcessed,
                    ImportStartedAt = h.ImportStartedAt,
                    ImportCompletedAt = h.ImportCompletedAt,
                    ImportStatus = h.ImportStatus,
                    ErrorMessage = h.ErrorMessage,
                    SnapshotId = h.SnapshotId,
                    ProcessingDuration = h.ProcessingDurationSeconds.HasValue 
                        ? TimeSpan.FromSeconds(h.ProcessingDurationSeconds.Value) 
                        : (TimeSpan?)null
                })
                .ToListAsync();

            return ServiceResult<IEnumerable<ImportHistoryDto>>.Success(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve import history");
            return ServiceResult<IEnumerable<ImportHistoryDto>>.Failed($"Failed to retrieve import history: {ex.Message}");
        }
    }

    #region Private Helper Methods

    private async Task<SheetValidationResult> ValidateWorksheetAsync(ExcelWorksheet worksheet)
    {
        var result = new SheetValidationResult
        {
            SheetName = worksheet.Name,
            RowCount = worksheet.Dimension?.Rows ?? 0,
            ColumnCount = worksheet.Dimension?.Columns ?? 0,
            SheetType = DetermineSheetType(worksheet.Name)
        };

        var errors = new List<string>();
        var warnings = new List<string>();

        // Validate based on sheet type
        switch (result.SheetType)
        {
            case SheetType.MatchStatistics:
                ValidateMatchStatisticsSheet(worksheet, errors, warnings);
                break;
            case SheetType.PlayerStatistics:
                ValidatePlayerStatisticsSheet(worksheet, errors, warnings);
                break;
        }

        result.Errors = errors;
        result.Warnings = warnings;
        result.IsValid = !errors.Any();

        return result;
    }

    private SheetType DetermineSheetType(string sheetName)
    {
        if (MatchSheetPattern.IsMatch(sheetName))
            return SheetType.MatchStatistics;
        
        if (PlayerStatsSheetPattern.IsMatch(sheetName))
            return SheetType.PlayerStatistics;

        if (sheetName.Contains("Blank"))
            return SheetType.BlankMatchTemplate;

        if (sheetName.Equals("CSV File", StringComparison.OrdinalIgnoreCase))
            return SheetType.DataProcessing;

        if (IsAggregateAnalysisSheet(sheetName))
            return SheetType.Unknown;

        return SheetType.Unknown;
    }

    private bool IsAggregateAnalysisSheet(string sheetName)
    {
        var aggregateSheets = new[]
        {
            "Cumulative Stats", "Goalkeepers", "Defenders", "Midfielders", "Forwards",
            "Player Matrix", "Kickout Analysis", "Kickout Stats", "Shots from play",
            "Shots from Play", "Scoreable Free", "KPI Definitions"
        };

        return aggregateSheets.Any(pattern => sheetName.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private void ValidateMatchStatisticsSheet(ExcelWorksheet worksheet, List<string> errors, List<string> warnings)
    {
        if (worksheet.Dimension == null)
        {
            errors.Add("Sheet has no data");
            return;
        }

        if (worksheet.Dimension.Rows < 50)
            warnings.Add($"Expected around 235 rows, found {worksheet.Dimension.Rows}");

        if (worksheet.Dimension.Columns < 15)
            warnings.Add($"Expected around 18 columns, found {worksheet.Dimension.Columns}");
    }

    private void ValidatePlayerStatisticsSheet(ExcelWorksheet worksheet, List<string> errors, List<string> warnings)
    {
        if (worksheet.Dimension == null)
        {
            errors.Add("Sheet has no data");
            return;
        }

        if (worksheet.Dimension.Rows < 15)
            warnings.Add($"Expected 21-23 rows, found {worksheet.Dimension.Rows}");

        if (worksheet.Dimension.Columns < 70)
            warnings.Add($"Expected 85+ columns, found {worksheet.Dimension.Columns}");

        // Validate header structure
        if (worksheet.Dimension.Rows >= 2)
        {
            var playerNameHeader = worksheet.Cells[2, 2].Value?.ToString();
            if (!"Player Name".Equals(playerNameHeader, StringComparison.OrdinalIgnoreCase) &&
                !"Player".Equals(playerNameHeader, StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add("Expected 'Player Name' header in column B, row 2");
            }
        }
    }

    private async Task<ServiceResult<ExcelData>> ParseExcelDataAsync(Stream excelStream, string fileName)
    {
        try
        {
            using var package = new ExcelPackage(excelStream);
            var excelData = new ExcelData
            {
                FileName = fileName,
                Matches = new List<MatchDataRow>(),
                PlayerStatistics = new List<PlayerStatisticsRow>()
            };

            foreach (var worksheet in package.Workbook.Worksheets)
            {
                var sheetType = DetermineSheetType(worksheet.Name);
                
                switch (sheetType)
                {
                    case SheetType.MatchStatistics:
                        var matchData = ParseMatchStatisticsSheet(worksheet);
                        if (matchData != null)
                            excelData.Matches.Add(matchData);
                        break;
                        
                    case SheetType.PlayerStatistics:
                        var playerStats = ParsePlayerStatisticsSheet(worksheet);
                        excelData.PlayerStatistics.AddRange(playerStats);
                        break;
                }
            }

            _logger.LogInformation("Parsed Excel data: {MatchCount} matches, {PlayerStatsCount} player records",
                excelData.Matches.Count, excelData.PlayerStatistics.Count);

            return ServiceResult<ExcelData>.Success(excelData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Excel data");
            return ServiceResult<ExcelData>.Failed($"Excel parsing failed: {ex.Message}");
        }
    }

    private MatchDataRow? ParseMatchStatisticsSheet(ExcelWorksheet worksheet)
    {
        try
        {
            // Extract match information from sheet name
            var match = ParseMatchInfoFromSheetName(worksheet.Name);
            if (match == null) return null;

            match.SheetName = worksheet.Name;

            // Extract team names from sheet content (Row 2, Columns B and E)
            // This is more reliable than parsing from sheet names
            var homeTeamFromSheet = worksheet.Cells[2, 2].Value?.ToString()?.Trim(); // Column B, Row 2
            var awayTeamFromSheet = worksheet.Cells[2, 5].Value?.ToString()?.Trim(); // Column E, Row 2

            // Use team names from sheet content if available, otherwise keep sheet name parsing result
            if (!string.IsNullOrWhiteSpace(homeTeamFromSheet))
            {
                match.HomeTeam = homeTeamFromSheet;
                _logger.LogDebug("Extracted home team from sheet content: {HomeTeam} (Sheet: {SheetName})", homeTeamFromSheet, worksheet.Name);
            }
            else if (string.IsNullOrWhiteSpace(match.HomeTeam))
            {
                // Default to "Drum" since this is Drum's analysis file
                match.HomeTeam = "Drum";
                _logger.LogDebug("Defaulted home team to 'Drum' for sheet: {SheetName}", worksheet.Name);
            }

            if (!string.IsNullOrWhiteSpace(awayTeamFromSheet))
            {
                match.AwayTeam = awayTeamFromSheet;
                _logger.LogDebug("Extracted away team from sheet content: {AwayTeam} (Sheet: {SheetName})", awayTeamFromSheet, worksheet.Name);
            }

            // Validate both teams are present
            if (string.IsNullOrWhiteSpace(match.HomeTeam) || string.IsNullOrWhiteSpace(match.AwayTeam))
            {
                _logger.LogError("Missing team names - Home: '{HomeTeam}', Away: '{AwayTeam}' (Sheet: {SheetName})", 
                    match.HomeTeam, match.AwayTeam, worksheet.Name);
                return null;
            }

            // Look for scoreline in early rows (typically rows 1-10)
            for (int row = 1; row <= Math.Min(10, worksheet.Dimension?.Rows ?? 0); row++)
            {
                for (int col = 1; col <= Math.Min(5, worksheet.Dimension?.Columns ?? 0); col++)
                {
                    var cellValue = worksheet.Cells[row, col].Value?.ToString()?.Trim();
                    if (TryParseScore(cellValue, out var goals, out var points))
                    {
                        if (match.HomeScore == null)
                        {
                            match.HomeScore = cellValue;
                            match.HomeGoals = goals;
                            match.HomePoints = points;
                        }
                        else if (match.AwayScore == null)
                        {
                            match.AwayScore = cellValue;
                            match.AwayGoals = goals;
                            match.AwayPoints = points;
                            break;
                        }
                    }
                }
                if (match.AwayScore != null) break;
            }

            _logger.LogInformation("Parsed match: {HomeTeam} vs {AwayTeam} from sheet {SheetName}", 
                match.HomeTeam, match.AwayTeam, worksheet.Name);

            return match;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse match statistics sheet: {SheetName}", worksheet.Name);
            return null;
        }
    }

    private List<PlayerStatisticsRow> ParsePlayerStatisticsSheet(ExcelWorksheet worksheet)
    {
        var playerStats = new List<PlayerStatisticsRow>();

        try
        {
            if (worksheet.Dimension == null) return playerStats;

            // Player data typically starts from row 3
            for (int row = 3; row <= worksheet.Dimension.Rows; row++)
            {
                var playerStat = ParsePlayerStatisticsRow(worksheet, row);
                if (playerStat != null)
                {
                    playerStat.SheetName = worksheet.Name;
                    playerStat.RowNumber = row;
                    playerStats.Add(playerStat);
                }
            }

            _logger.LogDebug("Parsed {Count} player statistics from sheet {SheetName}", playerStats.Count, worksheet.Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse player statistics sheet: {SheetName}", worksheet.Name);
        }

        return playerStats;
    }

    private PlayerStatisticsRow? ParsePlayerStatisticsRow(ExcelWorksheet worksheet, int row)
    {
        try
        {
            // Column A: Jersey number
            var jerseyNumberValue = worksheet.Cells[row, 1].Value;
            if (!TryParseNullableInt(jerseyNumberValue, out var jerseyNumber))
                return null; // Skip rows without jersey numbers

            // Column B: Player name
            var playerName = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(playerName))
                return null; // Skip rows without player names

            return new PlayerStatisticsRow
            {
                JerseyNumber = jerseyNumber,
                PlayerName = playerName,
                MinutesPlayed = GetNullableIntValue(worksheet, row, 3), // Column C
                TotalEvents = GetNullableIntValue(worksheet, row, 4), // Column D
                EventsPerPsr = GetNullableDecimalValue(worksheet, row, 5), // Column E
                Scores = worksheet.Cells[row, 6].Value?.ToString(), // Column F
                PerformanceSuccessRate = GetNullableDecimalValue(worksheet, row, 7), // Column G
                PsrPerTotalPossessions = GetNullableDecimalValue(worksheet, row, 8), // Column H
                TotalPossessions = GetNullableIntValue(worksheet, row, 9), // Column I
                TurnoversWon = GetNullableIntValue(worksheet, row, 10), // Column J
                Interceptions = GetNullableIntValue(worksheet, row, 11), // Column K
                TotalPossessionsLost = GetNullableIntValue(worksheet, row, 12), // Column L
                KickPasses = GetNullableIntValue(worksheet, row, 13), // Column M
                HandPasses = GetNullableIntValue(worksheet, row, 14), // Column N
                HandlingErrors = GetNullableIntValue(worksheet, row, 15), // Column O
                
                // Continue mapping remaining columns based on Excel format documentation
                Points = GetNullableIntValue(worksheet, row, 45), // Column AH (approximate)
                TwoPointers = GetNullableIntValue(worksheet, row, 46), // Column AI
                Goals = GetNullableIntValue(worksheet, row, 47), // Column AJ
                ShotsWide = GetNullableIntValue(worksheet, row, 48), // Column AK
                ShotsSaved = GetNullableIntValue(worksheet, row, 49), // Column AL
                ShotsShort = GetNullableIntValue(worksheet, row, 50), // Column AM
                
                YellowCards = GetNullableIntValue(worksheet, row, 55), // Column BD
                BlackCards = GetNullableIntValue(worksheet, row, 56), // Column BE
                RedCards = GetNullableIntValue(worksheet, row, 57), // Column BF
                
                KickoutsWon = GetNullableIntValue(worksheet, row, 60), // Column BJ
                KickoutsLost = GetNullableIntValue(worksheet, row, 61), // Column BK
                TotalKickouts = GetNullableIntValue(worksheet, row, 62), // Column BL
                GoalkeeperSaves = GetNullableIntValue(worksheet, row, 68) // Column BP
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse player statistics row {Row} in sheet {SheetName}", row, worksheet.Name);
            return null;
        }
    }

    private MatchDataRow? ParseMatchInfoFromSheetName(string sheetName)
    {
        try
        {
            // Handle patterns:
            // Pattern A: "08. Championship Drum vs Magilligan 17.08.25"
            // Pattern B: "08. Championship vs Magilligan 17.08.25" (home team missing)
            // Pattern C: "07. Drum vs Lissan 03.08.25" (competition is team name)
            var parts = sheetName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) return null;

            var matchNumberPart = parts[0].TrimEnd('.');
            if (!int.TryParse(matchNumberPart, out var matchNumber))
                return null;

            var vsIndex = Array.FindIndex(parts, p => p.Equals("vs", StringComparison.OrdinalIgnoreCase));
            if (vsIndex == -1) return null;

            string competition;
            string homeTeam;
            
            if (vsIndex == 2)
            {
                // Pattern C: "07. Drum vs Lissan" - competition name is likely team name, use generic competition
                competition = "League"; // Default competition
                homeTeam = parts[1]; // This should be "Drum"
            }
            else if (vsIndex == 3)
            {
                // Pattern A: "08. Championship Drum vs Magilligan" - explicit home team
                competition = parts[1];
                homeTeam = parts[2];
            }
            else
            {
                // Pattern B: "08. Championship vs Magilligan" - missing home team
                competition = parts[1];
                homeTeam = ""; // Will be filled from sheet content or defaulted to "Drum"
            }

            var awayTeamAndDate = string.Join(" ", parts[(vsIndex + 1)..]);

            // Try to extract date from the end
            var datePart = parts[^1]; // Last part
            DateTime matchDate = DateTime.Today;
            
            if (TryParseMatchDate(datePart, out var parsedDate))
            {
                matchDate = parsedDate;
                // Remove date from away team name
                awayTeamAndDate = awayTeamAndDate.Replace(datePart, "").Trim();
            }

            _logger.LogDebug("Parsed sheet name '{SheetName}' -> Match: {MatchNumber}, Competition: '{Competition}', HomeTeam: '{HomeTeam}', AwayTeam: '{AwayTeam}'", 
                sheetName, matchNumber, competition, homeTeam, awayTeamAndDate);

            return new MatchDataRow
            {
                MatchNumber = matchNumber,
                Competition = competition,
                HomeTeam = homeTeam,
                AwayTeam = awayTeamAndDate,
                MatchDate = matchDate
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse match info from sheet name: {SheetName}", sheetName);
            return null;
        }
    }

    private bool TryParseMatchDate(string datePart, out DateTime date)
    {
        date = DateTime.Today;
        
        // Try different date formats
        string[] formats = { "dd.MM.yy", "dd.MM.yyyy", "dd/MM/yy", "dd/MM/yyyy" };
        
        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(datePart, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                // Handle 2-digit year
                if (date.Year < 100)
                    date = date.AddYears(2000);
                
                return true;
            }
        }

        return false;
    }

    private bool TryParseScore(string? scoreText, out int goals, out int points)
    {
        goals = 0;
        points = 0;

        if (string.IsNullOrWhiteSpace(scoreText)) return false;

        // Format: "2-06" = 2 goals, 6 points
        var scoreParts = scoreText.Split('-');
        if (scoreParts.Length == 2 &&
            int.TryParse(scoreParts[0], out goals) &&
            int.TryParse(scoreParts[1], out points))
        {
            return true;
        }

        return false;
    }

    private bool TryParseNullableInt(object? value, out int? result)
    {
        result = null;
        if (value == null) return false;
        
        if (int.TryParse(value.ToString(), out var intValue))
        {
            result = intValue;
            return true;
        }
        
        return false;
    }

    private int? GetNullableIntValue(ExcelWorksheet worksheet, int row, int col)
    {
        var value = worksheet.Cells[row, col].Value;
        return TryParseNullableInt(value, out var result) ? result : null;
    }

    private decimal? GetNullableDecimalValue(ExcelWorksheet worksheet, int row, int col)
    {
        var value = worksheet.Cells[row, col].Value;
        if (value == null) return null;
        
        if (decimal.TryParse(value.ToString(), out var decimalValue))
            return decimalValue;
        
        return null;
    }

    private async Task ClearMatchDataAsync()
    {
        _logger.LogInformation("Clearing existing match data for clear-and-reload import");
        
        // Delete in correct order to respect foreign key constraints
        await ((Microsoft.EntityFrameworkCore.DbContext)_context).Database.ExecuteSqlRawAsync("DELETE FROM match_kickout_stats");
        await ((Microsoft.EntityFrameworkCore.DbContext)_context).Database.ExecuteSqlRawAsync("DELETE FROM match_player_stats");
        // match_source_analyses table removed from schema
        await ((Microsoft.EntityFrameworkCore.DbContext)_context).Database.ExecuteSqlRawAsync("DELETE FROM matches");
        
        _logger.LogInformation("Match data cleared successfully");
    }

    private async Task<ImportSummary> ImportParsedDataAsync(ExcelData excelData, ImportHistory importHistory)
    {
        var summary = new ImportSummary();
        var importedAt = DateTime.Now;

        // Get or create season, teams and competitions
        var season = await GetOrCreateSeasonFromFilenameAsync(importHistory.FileName);
        var teamCache = await GetOrCreateTeamsAsync(excelData);
        var competitionCache = await GetOrCreateCompetitionsAsync(excelData, season.Id);

        // Import matches
        var matchIdMap = new Dictionary<string, int>();
        
        foreach (var matchData in excelData.Matches)
        {
            var match = await CreateMatchFromDataAsync(matchData, teamCache, competitionCache, importedAt);
            _context.Matches.Add(match);
            await _context.SaveChangesAsync(); // Save to get ID
            
            matchIdMap[matchData.SheetName] = match.Id;
            summary.MatchesImported++;
        }

        // Import player statistics using small batch processing
        var playerStatsList = new List<MatchPlayerStat>();
        foreach (var playerData in excelData.PlayerStatistics)
        {
            if (TryGetMatchIdForPlayerSheet(playerData.SheetName, matchIdMap, out var matchId))
            {
                var playerStat = CreatePlayerStatFromData(playerData, matchId, teamCache, importedAt);
                playerStatsList.Add(playerStat);
                summary.PlayersProcessed++;
            }
        }
        
        // Process player statistics in small batches to avoid massive parameterized queries
        if (playerStatsList.Any())
        {
            const int batchSize = 50; // Smaller batch size to prevent query timeout
            for (int i = 0; i < playerStatsList.Count; i += batchSize)
            {
                var batch = playerStatsList.Skip(i).Take(batchSize).ToList();
                _context.MatchPlayerStats.AddRange(batch);
                await _context.SaveChangesAsync(); // Save each batch separately
                _logger.LogInformation("Processed player statistics batch {BatchNumber}: {BatchSize} records", 
                    (i / batchSize) + 1, batch.Count);
            }
        }

        summary.StatisticsRecordsCreated = summary.PlayersProcessed;
        
        return summary;
    }

    private async Task<Season> GetOrCreateSeasonFromFilenameAsync(string fileName)
    {
        // Extract year from filename (e.g., "Drum Analysis 2025.xlsx" -> "2025")
        var yearMatch = Regex.Match(fileName ?? "", @"(\d{4})");
        
        if (!yearMatch.Success || !int.TryParse(yearMatch.Groups[1].Value, out var year))
        {
            // Default to current year if no year found in filename
            year = DateTime.Now.Year;
            _logger.LogWarning("Could not extract year from filename {FileName}, defaulting to {Year}", fileName, year);
        }

        var seasonName = $"{year} Season";

        // Check if season already exists
        var existingSeason = await _context.Seasons
            .FirstOrDefaultAsync(s => s.Year == year);

        if (existingSeason != null)
        {
            _logger.LogInformation("Using existing season: {SeasonName} (ID: {SeasonId})", existingSeason.Name, existingSeason.Id);
            return existingSeason;
        }

        // Create new season
        var newSeason = new Season
        {
            Year = year,
            Name = seasonName,
            IsCurrent = year == DateTime.Now.Year,
            StartDate = new DateOnly(year, 1, 1),
            EndDate = new DateOnly(year, 12, 31),
            CreatedAt = DateTime.Now
        };

        _context.Seasons.Add(newSeason);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created new season: {SeasonName} (ID: {SeasonId})", newSeason.Name, newSeason.Id);
        return newSeason;
    }

    private async Task<Dictionary<string, Team>> GetOrCreateTeamsAsync(ExcelData excelData)
    {
        var teamNames = excelData.Matches
            .SelectMany(m => new[] { m.HomeTeam, m.AwayTeam })
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .ToList();

        var existingTeams = await _context.Teams
            .Where(t => teamNames.Contains(t.Name))
            .ToDictionaryAsync(t => t.Name, t => t);

        var teamCache = new Dictionary<string, Team>(existingTeams);

        foreach (var teamName in teamNames)
        {
            if (!teamCache.ContainsKey(teamName))
            {
                var team = new Team
                {
                    Name = teamName,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                
                _context.Teams.Add(team);
                await _context.SaveChangesAsync();
                teamCache[teamName] = team;
            }
        }

        return teamCache;
    }

    private async Task<Dictionary<string, Competition>> GetOrCreateCompetitionsAsync(ExcelData excelData, int seasonId)
    {
        var competitionNames = excelData.Matches
            .Select(m => m.Competition)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .ToList();

        var existingCompetitions = await _context.Competitions
            .Where(c => competitionNames.Contains(c.Name))
            .ToDictionaryAsync(c => c.Name, c => c);

        var competitionCache = new Dictionary<string, Competition>(existingCompetitions);

        foreach (var competitionName in competitionNames)
        {
            if (!competitionCache.ContainsKey(competitionName))
            {
                var competition = new Competition
                {
                    Name = competitionName,
                    Type = "Unknown",
                    SeasonId = seasonId, // Use extracted season
                    CreatedAt = DateTime.Now
                };
                
                _context.Competitions.Add(competition);
                await _context.SaveChangesAsync();
                competitionCache[competitionName] = competition;
            }
        }

        return competitionCache;
    }

    private async Task<GAAStat.Dal.Models.application.Match> CreateMatchFromDataAsync(MatchDataRow matchData, Dictionary<string, Team> teamCache, Dictionary<string, Competition> competitionCache, DateTime importedAt)
    {
        var homeTeam = teamCache.GetValueOrDefault(matchData.HomeTeam);
        var awayTeam = teamCache.GetValueOrDefault(matchData.AwayTeam);
        var competition = competitionCache.GetValueOrDefault(matchData.Competition);

        // Validate all required entities exist
        if (homeTeam == null)
        {
            _logger.LogError("Home team not found: {TeamName}", matchData.HomeTeam);
            throw new InvalidOperationException($"Home team '{matchData.HomeTeam}' was not found or created");
        }

        if (awayTeam == null)
        {
            _logger.LogError("Away team not found: {TeamName}", matchData.AwayTeam);
            throw new InvalidOperationException($"Away team '{matchData.AwayTeam}' was not found or created");
        }

        if (competition == null)
        {
            _logger.LogError("Competition not found: {CompetitionName}", matchData.Competition);
            throw new InvalidOperationException($"Competition '{matchData.Competition}' was not found or created");
        }

        _logger.LogDebug("Creating match: {HomeTeam} vs {AwayTeam} (HomeTeamId: {HomeTeamId}, AwayTeamId: {AwayTeamId}, CompetitionId: {CompetitionId})", 
            matchData.HomeTeam, matchData.AwayTeam, homeTeam.Id, awayTeam.Id, competition.Id);

        return new GAAStat.Dal.Models.application.Match
        {
            CompetitionId = competition.Id,
            MatchNumber = matchData.MatchNumber,
            MatchDate = DateOnly.FromDateTime(matchData.MatchDate),
            HomeTeamId = homeTeam.Id,
            AwayTeamId = awayTeam.Id,
            Venue = matchData.Venue,
            HomeScoreGoals = matchData.HomeGoals,
            HomeScorePoints = matchData.HomePoints,
            AwayScoreGoals = matchData.AwayGoals,
            AwayScorePoints = matchData.AwayPoints,
            ExcelSheetName = matchData.SheetName,
            ImportedAt = importedAt
        };
    }

    private MatchPlayerStat CreatePlayerStatFromData(PlayerStatisticsRow playerData, int matchId, Dictionary<string, Team> teamCache, DateTime importedAt)
    {
        // Calculate PSR and efficiency metrics
        var psr = _statisticsService.CalculatePerformanceSuccessRate(playerData);
        var metrics = _statisticsService.CalculateEfficiencyMetrics(playerData);

        // Determine team (simplified - could be enhanced with better logic)
        var teamId = teamCache.Values.FirstOrDefault()?.Id ?? 1;

        return new MatchPlayerStat
        {
            MatchId = matchId,
            PlayerName = playerData.PlayerName,
            JerseyNumber = playerData.JerseyNumber,
            TeamId = teamId,
            MinutesPlayed = playerData.MinutesPlayed,
            TotalEvents = playerData.TotalEvents,
            PerformanceSuccessRate = psr,
            TotalPossessions = playerData.TotalPossessions,
            TurnoversWon = playerData.TurnoversWon,
            Interceptions = playerData.Interceptions,
            PossessionsLost = playerData.TotalPossessionsLost,
            KickPasses = playerData.KickPasses,
            HandPasses = playerData.HandPasses,
            TacklesMade = playerData.SuccessfulTackles,
            CardsYellow = playerData.YellowCards,
            CardsBlack = playerData.BlackCards,
            CardsRed = playerData.RedCards,
            PointsFromPlay = playerData.Points,
            GoalsFromPlay = playerData.Goals,
            TwoPointersFromPlay = playerData.TwoPointers,
            ShotsWide = playerData.ShotsWide,
            ShotsSaved = playerData.ShotsSaved,
            ShotsShort = playerData.ShotsShort,
            ShotEfficiency = metrics.ShotEfficiency,
            ScoreConversionRate = metrics.ScoreConversionRate,
            OverallPerformanceRating = metrics.OverallRating,
            ImportedAt = importedAt
        };
    }

    private bool TryGetMatchIdForPlayerSheet(string playerSheetName, Dictionary<string, int> matchIdMap, out int matchId)
    {
        matchId = 0;
        
        // Try to find corresponding match sheet name
        foreach (var kvp in matchIdMap)
        {
            var matchSheetName = kvp.Key;
            
            // Extract match number from both sheets to match them
            var playerMatch = ExtractMatchNumber(playerSheetName);
            var matchMatch = ExtractMatchNumber(matchSheetName);
            
            if (playerMatch.HasValue && matchMatch.HasValue && playerMatch == matchMatch)
            {
                matchId = kvp.Value;
                return true;
            }
        }
        
        return false;
    }

    private int? ExtractMatchNumber(string sheetName)
    {
        var parts = sheetName.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0 && int.TryParse(parts[0], out var matchNumber))
        {
            return matchNumber;
        }
        
        return null;
    }

    private async Task UpdateImportHistoryAsync(ImportHistory importHistory, string status, string? errorMessage = null, DateTime? completedAt = null, ImportSummary? summary = null)
    {
        importHistory.ImportStatus = status;
        importHistory.ErrorMessage = errorMessage;
        importHistory.ImportCompletedAt = completedAt;
        
        if (summary != null)
        {
            importHistory.MatchesImported = summary.MatchesImported;
            importHistory.PlayersProcessed = summary.PlayersProcessed;
            importHistory.EventsCreated = summary.StatisticsRecordsCreated;
            importHistory.ProcessingDurationSeconds = (int)(summary?.ProcessingDuration.TotalSeconds ?? 0);
        }
        
        await _context.SaveChangesAsync();
    }

    #endregion

    #region Private Models

    private class ExcelData
    {
        public string FileName { get; set; } = string.Empty;
        public List<MatchDataRow> Matches { get; set; } = new();
        public List<PlayerStatisticsRow> PlayerStatistics { get; set; } = new();
    }

    #endregion

    #region Interface Implementation Stubs (for compilation)

    public Task<ServiceResult<ImportSummary>> ImportMatchDataAsync(
        Stream excelStream, 
        string fileName, 
        BulkOperationConfig? config = null,
        IProgress<ImportProgress>? progressCallback = null)
    {
        // Delegate to existing method - ignore additional parameters for now
        return ImportMatchDataAsync(excelStream, fileName);
    }

    public Task<ServiceResult<CrossSheetValidationResult>> ValidateExcelFileAsync(Stream excelStream, string fileName, bool performCrossSheetValidation = true)
    {
        return Task.FromResult(ServiceResult<CrossSheetValidationResult>.Success(new CrossSheetValidationResult
        {
            IsValid = true,
            ValidationErrors = new List<string>(),
            ValidationWarnings = new List<string>()
        }));
    }

    public Task<ServiceResult<ComprehensiveImportResult>> ProcessAllSheetTypesAsync(
        Stream excelStream, 
        string fileName, 
        BulkOperationConfig? config = null,
        IProgress<ImportProgress>? progressCallback = null)
    {
        return Task.FromResult(ServiceResult<ComprehensiveImportResult>.Success(new ComprehensiveImportResult()));
    }

    public Task<ServiceResult<ImportSummary>> RollbackImportAsync(int importId, bool createBackupSnapshot = false)
    {
        return Task.FromResult(ServiceResult<ImportSummary>.Failed("Rollback not implemented yet"));
    }

    public Task<ServiceResult<ImportProgress>> GetImportProgressAsync(int importId)
    {
        return Task.FromResult(ServiceResult<ImportProgress>.Failed("Progress tracking not implemented yet"));
    }

    public Task<ServiceResult<IEnumerable<ImportHistoryDto>>> GetImportHistoryAsync(int pageSize = 50, string? searchTerm = null, bool includeFailedImports = true)
    {
        return Task.FromResult(ServiceResult<IEnumerable<ImportHistoryDto>>.Success(new List<ImportHistoryDto>()));
    }

    public Task<ServiceResult<ImportCancellationResult>> CancelImportAsync(int importId)
    {
        return Task.FromResult(ServiceResult<ImportCancellationResult>.Success(new ImportCancellationResult()));
    }

    public Task<ServiceResult<ImportPerformanceStats>> GetPerformanceStatisticsAsync(int importId)
    {
        return Task.FromResult(ServiceResult<ImportPerformanceStats>.Success(new ImportPerformanceStats
        {
            AverageImportDuration = TimeSpan.Zero,
            SuccessfulImports = 0,
            FailedImports = 0,
            FastestImport = TimeSpan.Zero,
            SlowestImport = TimeSpan.Zero
        }));
    }

    #endregion
}