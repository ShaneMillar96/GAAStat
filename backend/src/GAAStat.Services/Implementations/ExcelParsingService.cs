using OfficeOpenXml;
using Microsoft.Extensions.Logging;
using GAAStat.Services.Interfaces;
using GAAStat.Services.Models;

namespace GAAStat.Services.Implementations;

/// <summary>
/// Service for parsing Excel files and extracting GAA match data
/// Handles EPPlus integration for reading Excel files and detecting sheet structures
/// </summary>
public class ExcelParsingService : IExcelParsingService
{
    private readonly IDataTransformationService _transformationService;
    private readonly ILogger<ExcelParsingService> _logger;

    public ExcelParsingService(IDataTransformationService transformationService, ILogger<ExcelParsingService> logger)
    {
        _transformationService = transformationService;
        _logger = logger;
    }

    /// <summary>
    /// Analyzes an Excel file to detect sheets and validate GAA data structure
    /// </summary>
    public async Task<ServiceResult<ExcelFileAnalysis>> AnalyzeExcelFileAsync(Stream fileStream, string fileName)
    {
        try
        {
            // CRITICAL FIX: Wrap synchronous Excel operations in Task.Run to prevent thread pool blocking
            return await Task.Run(() =>
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                
                using var package = new ExcelPackage(fileStream);
                var analysis = new ExcelFileAnalysis
                {
                    FileName = fileName,
                    FileSizeBytes = fileStream.Length,
                    SheetCount = package.Workbook.Worksheets.Count
                };

                var validationErrors = new List<string>();

                // Validate basic structure
                if (package.Workbook.Worksheets.Count == 0)
                {
                    validationErrors.Add("Excel file contains no worksheets");
                }
                else if (package.Workbook.Worksheets.Count > FileConstants.MAX_SHEET_COUNT)
                {
                    validationErrors.Add($"Excel file contains too many sheets ({package.Workbook.Worksheets.Count}). Maximum allowed: {FileConstants.MAX_SHEET_COUNT}");
                }

                // Analyze each sheet
                foreach (var worksheet in package.Workbook.Worksheets)
                {
                    var sheetInfo = AnalyzeWorksheet(worksheet);
                    analysis.Sheets.Add(sheetInfo);
                }

                // Determine if this is a valid GAA file
                var matchSheets = analysis.Sheets.Where(s => s.ContainsMatchData).ToList();
                if (matchSheets.Count == 0)
                {
                    validationErrors.Add("No GAA match data sheets detected");
                    analysis.IsValidGaaFile = false;
                }
                else
                {
                    analysis.IsValidGaaFile = true;
                    _logger.LogInformation("Detected {MatchSheetCount} match data sheets in file {FileName}", 
                        matchSheets.Count, fileName);
                }

                analysis.ValidationErrors = validationErrors;
                return ServiceResult<ExcelFileAnalysis>.Success(analysis);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze Excel file {FileName}", fileName);
            return ServiceResult<ExcelFileAnalysis>.Failed("Failed to analyze Excel file structure");
        }
    }

    /// <summary>
    /// Parses match data from a specific Excel sheet
    /// Phase 1: Focuses on basic match header information
    /// </summary>
    public async Task<ServiceResult<MatchData>> ParseMatchDataFromSheetAsync(Stream fileStream, string sheetName)
    {
        try
        {
            // CRITICAL FIX: Wrap synchronous Excel operations in Task.Run to prevent thread pool blocking
            return await Task.Run(async () =>
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                
                using var package = new ExcelPackage(fileStream);
                var worksheet = package.Workbook.Worksheets[sheetName];
                
                if (worksheet == null)
                {
                    return ServiceResult<MatchData>.Failed($"Worksheet '{sheetName}' not found");
                }

                var matchData = await ParseMatchHeaderAsync(worksheet).ConfigureAwait(false);
                return ServiceResult<MatchData>.Success(matchData);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse match data from sheet {SheetName}", sheetName);
            return ServiceResult<MatchData>.Failed($"Failed to parse match data from sheet '{sheetName}'");
        }
    }

    /// <summary>
    /// Parses match data from all detected match sheets in Excel file
    /// </summary>
    public async Task<ServiceResult<IEnumerable<MatchData>>> ParseAllMatchDataAsync(Stream fileStream)
    {
        try
        {
            // CRITICAL FIX: Wrap synchronous Excel operations in Task.Run to prevent thread pool blocking
            return await Task.Run(async () =>
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                
                using var package = new ExcelPackage(fileStream);
                var allMatchData = new List<MatchData>();

                foreach (var worksheet in package.Workbook.Worksheets)
                {
                    if (IsMatchDataSheet(worksheet))
                    {
                        try
                        {
                            var matchData = await ParseMatchHeaderAsync(worksheet).ConfigureAwait(false);
                            allMatchData.Add(matchData);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse match data from sheet {SheetName}, skipping", worksheet.Name);
                            // Continue with other sheets rather than failing entirely
                        }
                    }
                }

                _logger.LogInformation("Successfully parsed {MatchCount} matches from Excel file", allMatchData.Count);
                return ServiceResult<IEnumerable<MatchData>>.Success(allMatchData);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse all match data from Excel file");
            return ServiceResult<IEnumerable<MatchData>>.Failed("Failed to parse match data from Excel file");
        }
    }

    /// <summary>
    /// Validates Excel file format and basic structure
    /// </summary>
    public async Task<ServiceResult> ValidateExcelFileAsync(Stream fileStream, string fileName, long fileSizeBytes)
    {
        try
        {
            return await Task.Run(() =>
            {
                var validationErrors = new List<string>();

                // File size validation
                if (fileSizeBytes > FileConstants.MAX_FILE_SIZE_MB * 1024 * 1024)
                {
                    validationErrors.Add($"File size ({fileSizeBytes / (1024 * 1024)}MB) exceeds maximum allowed ({FileConstants.MAX_FILE_SIZE_MB}MB)");
                }

                // File extension validation
                var extension = Path.GetExtension(fileName);
                if (!FileConstants.ALLOWED_EXTENSIONS.Contains(extension))
                {
                    validationErrors.Add($"File extension '{extension}' is not supported. Allowed: {string.Join(", ", FileConstants.ALLOWED_EXTENSIONS)}");
                }

                // File name validation
                if (fileName.Length > FileConstants.MAX_FILENAME_LENGTH)
                {
                    validationErrors.Add($"File name is too long ({fileName.Length} characters). Maximum: {FileConstants.MAX_FILENAME_LENGTH}");
                }

                // Try to open the Excel file to validate format
                try
                {
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    using var package = new ExcelPackage(fileStream);
                    
                    if (package.Workbook.Worksheets.Count == 0)
                    {
                        validationErrors.Add("Excel file contains no worksheets");
                    }
                }
                catch (Exception ex)
                {
                    validationErrors.Add($"Invalid Excel file format: {ex.Message}");
                }

                if (validationErrors.Any())
                {
                    return ServiceResult.ValidationFailed(validationErrors);
                }

                return ServiceResult.Success();
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate Excel file {FileName}", fileName);
            return ServiceResult.Failed("Failed to validate Excel file");
        }
    }


    #region Private Helper Methods

    private SheetInfo AnalyzeWorksheet(ExcelWorksheet worksheet)
    {
        var sheetInfo = new SheetInfo
        {
            Name = worksheet.Name,
            RowCount = worksheet.Dimension?.Rows ?? 0,
            ColumnCount = worksheet.Dimension?.Columns ?? 0
        };

        // Check if this is a match data sheet (summary sheet, not player stats sheet)
        sheetInfo.ContainsMatchData = IsMatchDataSheet(worksheet);
        
        // Check if this is a player statistics sheet
        sheetInfo.ContainsPlayerData = IsPlayerStatsSheet(worksheet);

        if (sheetInfo.ContainsMatchData)
        {
            // Extract team names from sheet name
            var (competition, oppositionTeam) = _transformationService.ExtractMatchTeams(worksheet.Name);
            sheetInfo.DetectedTeamNames = $"{competition} vs {oppositionTeam}";

            // Extract match date from sheet name
            sheetInfo.DetectedMatchDate = _transformationService.ExtractDateFromSheetName(worksheet.Name);
        }
        
        if (sheetInfo.ContainsPlayerData)
        {
            // Extract team names from player stats sheet name for mapping to matches
            var (competition, oppositionTeam) = _transformationService.ExtractMatchTeams(worksheet.Name);
            sheetInfo.DetectedTeamNames = $"{competition} vs {oppositionTeam}";

            // Extract match date from sheet name
            sheetInfo.DetectedMatchDate = _transformationService.ExtractDateFromSheetName(worksheet.Name);
        }

        return sheetInfo;
    }

    private bool IsMatchDataSheet(ExcelWorksheet worksheet)
    {
        // Use the new match summary sheet detection logic
        return IsMatchSummarySheet(worksheet);
    }

    /// <summary>
    /// Dynamically finds the row where match score data begins by analyzing cell content patterns
    /// Prioritizes rows with GAA score format (G-PP) over single numbers
    /// </summary>
    private int FindScoreDataRow(ExcelWorksheet worksheet, int maxRowsToSearch = 10)
    {
        if (worksheet.Dimension == null)
            return 4; // Default to row 4 based on Excel structure shown

        // First priority: Look for properly formatted GAA scores (G-PP format) in correct columns
        // Column D (4) = Drum full-time score, Column G (7) = Opposition full-time score
        for (int row = 1; row <= Math.Min(maxRowsToSearch, worksheet.Dimension.Rows); row++)
        {
            var drumScore = worksheet.Cells[row, 4].Value?.ToString();    // Column D
            var oppositionScore = worksheet.Cells[row, 7].Value?.ToString(); // Column G
            
            // Check if we have GAA score format in expected team score positions
            if (IsScoreFormat(drumScore) && IsScoreFormat(oppositionScore))
            {
                _logger.LogInformation("Found GAA formatted scores at row {Row} in sheet '{SheetName}': Drum '{Drum}' vs Opposition '{Opposition}'", 
                    row, worksheet.Name, drumScore, oppositionScore);
                return row;
            }
            
            // Also check if one team score is formatted correctly
            if (IsScoreFormat(drumScore) || IsScoreFormat(oppositionScore))
            {
                _logger.LogDebug("Found partial GAA score format at row {Row} in sheet '{SheetName}'", 
                    row, worksheet.Name);
                return row;
            }
        }

        // Second priority: Look for GAA scores in any column within first 6 columns
        for (int row = 1; row <= Math.Min(maxRowsToSearch, worksheet.Dimension.Rows); row++)
        {
            for (int col = 1; col <= Math.Min(6, worksheet.Dimension.Columns); col++)
            {
                var cellValue = worksheet.Cells[row, col].Value?.ToString();
                if (!string.IsNullOrEmpty(cellValue) && IsScoreFormat(cellValue))
                {
                    _logger.LogDebug("Found GAA score format at row {Row}, col {Col}: '{Value}' in sheet '{SheetName}'", 
                        row, col, cellValue, worksheet.Name);
                    return row;
                }
            }
        }

        // Third priority: Look for team name patterns (fallback for unusual structures)
        for (int row = 1; row <= Math.Min(maxRowsToSearch, worksheet.Dimension.Rows); row++)
        {
            var drumCell = worksheet.Cells[row, 4].Value?.ToString();      // Column D
            var oppositionCell = worksheet.Cells[row, 7].Value?.ToString(); // Column G

            // Look for team names that might indicate score row
            if (HasTeamNamePattern(drumCell, oppositionCell))
            {
                _logger.LogDebug("Found team name pattern at row {Row} in sheet '{SheetName}': Drum '{Drum}' | Opposition '{Opposition}'", 
                    row, worksheet.Name, drumCell, oppositionCell);
                return row;
            }
        }

        _logger.LogWarning("No score data row found in sheet '{SheetName}', using default row 4", worksheet.Name);
        return 4; // Default to row 4 based on Excel structure
    }

    /// <summary>
    /// Check if cells contain team name patterns (Drum vs opposition)
    /// </summary>
    private bool HasTeamNamePattern(string? cell1, string? cell4)
    {
        if (string.IsNullOrWhiteSpace(cell1) && string.IsNullOrWhiteSpace(cell4))
            return false;

        var drum = cell1?.ToLowerInvariant().Contains("drum") == true;
        var opposition = !string.IsNullOrWhiteSpace(cell4) && 
                        !cell4.ToLowerInvariant().Contains("drum") && 
                        cell4.Length > 2;

        return drum || opposition;
    }

    /// <summary>
    /// Check if this is a player statistics sheet (not a match summary sheet)
    /// </summary>
    private bool IsPlayerStatsSheet(ExcelWorksheet worksheet)
    {
        // CRITICAL FIX: Trim whitespace from sheet name to handle trailing spaces
        var sheetName = worksheet.Name.Trim().ToLowerInvariant();
        
        // ENHANCED DEBUG: Special logging for Match 1
        var isMatch1 = worksheet.Name.StartsWith("01.");
        if (isMatch1)
        {
            _logger.LogInformation("🔍 MATCH 1 DETECTION: Processing sheet '{SheetName}'", worksheet.Name);
            _logger.LogInformation("🔍 MATCH 1 DETECTION: Trimmed/lowercase = '{TrimmedName}'", sheetName);
        }
        
        // Check for player stats sheet indicators - more comprehensive patterns
        var playerStatsPatterns = new[] 
        { 
            "player stats vs",  // "07. Player Stats vs Lissan"
            "player statistics vs", // Alternative naming
            "stats vs",         // "08. stats vs Magilligan" (lowercase)
            "analysis vs",      // "XX. Analysis vs Opposition"
            "individual stats", // Generic individual stats
            "individual statistics" // Alternative naming
        };
        
        // ENHANCED DEBUG: Check each pattern for Match 1
        if (isMatch1)
        {
            foreach (var pattern in playerStatsPatterns)
            {
                var contains = sheetName.Contains(pattern);
                _logger.LogInformation("🔍 MATCH 1 DETECTION: Pattern '{Pattern}' match: {Contains}", pattern, contains);
            }
        }
        
        // Use case-insensitive comparison since sheetName is already lowercase
        var hasPlayerStatsIndicators = playerStatsPatterns.Any(pattern => 
            sheetName.Contains(pattern));
        
        if (hasPlayerStatsIndicators)
        {
            if (isMatch1)
                _logger.LogInformation("✅ MATCH 1 DETECTION: Sheet '{SheetName}' IDENTIFIED as player statistics sheet", worksheet.Name);
            else
                _logger.LogInformation("Sheet '{SheetName}' (trimmed: '{TrimmedName}') identified as player statistics sheet", 
                    worksheet.Name, sheetName);
            return true;
        }
        
        if (isMatch1)
            _logger.LogWarning("❌ MATCH 1 DETECTION: Sheet '{SheetName}' NOT identified as player statistics sheet", worksheet.Name);
        else
            _logger.LogDebug("Sheet '{SheetName}' (trimmed: '{TrimmedName}') NOT identified as player statistics sheet", 
                worksheet.Name, sheetName);
        return false;
    }

    /// <summary>
    /// Check if this is a match summary sheet (contains team vs team scores)
    /// </summary>
    private bool IsMatchSummarySheet(ExcelWorksheet worksheet)
    {
        var sheetName = worksheet.Name.ToLowerInvariant();
        
        // Exclude sheets that should never be processed as match sheets
        var excludedPatterns = new[] 
        { 
            "csv file", "cumulative", "summary", "total", "overview", 
            "stats summary", "player stats", "analysis"
        };
        
        if (excludedPatterns.Any(pattern => sheetName.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogDebug("Sheet '{SheetName}' excluded from match processing due to pattern match", worksheet.Name);
            return false;
        }
        
        // Don't process player stats sheets as match sheets
        if (IsPlayerStatsSheet(worksheet))
            return false;
            
        // Check for versus pattern (primary indicator of match sheet)
        var hasVersusPattern = sheetName.Contains(" vs ") || sheetName.Contains(" v ");
        
        // Check for minimum dimensions
        var hasMinimumSize = worksheet.Dimension != null && 
                           worksheet.Dimension.Rows >= 3 &&
                           worksheet.Dimension.Columns >= 3;

        var result = hasVersusPattern && hasMinimumSize;
        
        if (result)
        {
            _logger.LogDebug("Sheet '{SheetName}' identified as match summary sheet", worksheet.Name);
        }
        
        return result;
    }

    private async Task<MatchData> ParseMatchHeaderAsync(ExcelWorksheet worksheet)
    {
        var matchData = new MatchData
        {
            SheetName = worksheet.Name
        };

        // Extract teams and competition from sheet name
        var (competition, oppositionTeam) = _transformationService.ExtractMatchTeams(worksheet.Name);
        matchData.Competition = competition;
        
        // Determine home/away based on venue logic
        var venue = _transformationService.DetermineVenue(worksheet.Name);
        matchData.Venue = venue;
        
        if (venue == "Home")
        {
            matchData.HomeTeam = GaaConstants.DEFAULT_HOME_TEAM;
            matchData.AwayTeam = oppositionTeam;
        }
        else
        {
            matchData.HomeTeam = oppositionTeam;
            matchData.AwayTeam = GaaConstants.DEFAULT_HOME_TEAM;
        }

        // Extract match date - allow null for truncated sheet names
        matchData.MatchDate = _transformationService.ExtractDateFromSheetName(worksheet.Name);

        // Dynamically find the row with score data
        var scoreDataRow = FindScoreDataRow(worksheet);
        
        // Extract scores from detected row
        // Based on Excel structure: Column D (4) = Drum full-time score, Column G (7) = Opposition full-time score
        try
        {
            var homeScoreCell = worksheet.Cells[scoreDataRow, 4].Value?.ToString();  // Column D (Drum full-time)
            var awayScoreCell = worksheet.Cells[scoreDataRow, 7].Value?.ToString();  // Column G (Opposition full-time)

            // Also try column B and E as alternatives (1st half scores) if full-time scores are not available
            if (string.IsNullOrEmpty(homeScoreCell) || !IsScoreFormat(homeScoreCell))
            {
                var altHomeScore = worksheet.Cells[scoreDataRow, 2].Value?.ToString(); // Column B (Drum 1st half)
                if (!string.IsNullOrEmpty(altHomeScore) && IsScoreFormat(altHomeScore))
                {
                    homeScoreCell = altHomeScore;
                }
            }
            
            if (string.IsNullOrEmpty(awayScoreCell) || !IsScoreFormat(awayScoreCell))
            {
                var altAwayScore = worksheet.Cells[scoreDataRow, 5].Value?.ToString(); // Column E (Opposition 1st half)
                if (!string.IsNullOrEmpty(altAwayScore) && IsScoreFormat(altAwayScore))
                {
                    awayScoreCell = altAwayScore;
                }
            }

            if (!string.IsNullOrEmpty(homeScoreCell))
            {
                matchData.HomeScore = homeScoreCell;
                var (homeGoals, homePoints) = _transformationService.ParseScore(homeScoreCell);
                matchData.HomeGoals = homeGoals;
                matchData.HomePoints = homePoints;
            }

            if (!string.IsNullOrEmpty(awayScoreCell))
            {
                matchData.AwayScore = awayScoreCell;
                var (awayGoals, awayPoints) = _transformationService.ParseScore(awayScoreCell);
                matchData.AwayGoals = awayGoals;
                matchData.AwayPoints = awayPoints;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse scores from sheet {SheetName}", worksheet.Name);
            matchData.ValidationWarnings.Add("Could not parse match scores from expected locations");
        }

        return matchData;
    }

    private bool IsScoreFormat(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        // Check for GAA score pattern: X-XX
        return System.Text.RegularExpressions.Regex.IsMatch(value, @"^\d+-\d+");
    }

    private int? FindHeaderRow(ExcelWorksheet worksheet)
    {
        if (worksheet.Dimension == null)
            return null;

        // Look for header indicators in first few rows
        for (int row = 1; row <= Math.Min(10, worksheet.Dimension.Rows); row++)
        {
            // Look for common GAA statistics headers
            for (int col = 1; col <= Math.Min(15, worksheet.Dimension.Columns); col++)
            {
                var cellValue = worksheet.Cells[row, col].Value?.ToString()?.ToLowerInvariant();
                if (cellValue != null && IsPlayerStatsHeader(cellValue))
                {
                    _logger.LogDebug("Found header row {Row} in sheet '{SheetName}' with header '{Header}'", 
                        row, worksheet.Name, cellValue);
                    return row;
                }
            }
        }

        // Default to row 2 for player stats sheets
        _logger.LogWarning("No header row found in sheet '{SheetName}', using default row 2", worksheet.Name);
        return 2;
    }

    /// <summary>
    /// Finds the row where player statistics data begins (after headers)
    /// </summary>
    private int FindPlayerStatsDataStartRow(ExcelWorksheet worksheet)
    {
        var headerRow = FindHeaderRow(worksheet);
        if (headerRow.HasValue)
        {
            // Data typically starts 1-2 rows after headers
            var candidateRow = headerRow.Value + 1;
            
            // Verify this row has player data by checking for name or number
            if (HasPlayerDataInRow(worksheet, candidateRow))
            {
                return candidateRow;
            }
            
            // Try next row
            candidateRow = headerRow.Value + 2;
            if (HasPlayerDataInRow(worksheet, candidateRow))
            {
                return candidateRow;
            }
        }

        // Fallback to constant if dynamic detection fails
        return ExcelColumnMappings.PLAYER_STATS_START_ROW + 1; // Convert to 1-based
    }

    /// <summary>
    /// Check if a cell value looks like a player statistics header
    /// </summary>
    private bool IsPlayerStatsHeader(string cellValue)
    {
        var value = cellValue.ToLowerInvariant().Trim();
        return value.Contains("player") || 
               value.Contains("name") ||
               value == "min" ||
               value == "te" ||
               value == "psr" ||
               value == "pts" ||
               value == "gls" ||
               value.Contains("jersey") ||
               value.Contains("no");
    }

    /// <summary>
    /// Check if a row contains player data (name or jersey number)
    /// </summary>
    private bool HasPlayerDataInRow(ExcelWorksheet worksheet, int row)
    {
        if (worksheet.Dimension == null || row > worksheet.Dimension.Rows)
            return false;

        // Check first few columns for player name or jersey number
        for (int col = 1; col <= Math.Min(3, worksheet.Dimension.Columns); col++)
        {
            var cellValue = worksheet.Cells[row, col].Value?.ToString()?.Trim();
            if (!string.IsNullOrEmpty(cellValue))
            {
                // Could be player name (contains letters) or jersey number
                if (IsPlayerName(cellValue) || IsJerseyNumber(cellValue))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsPlayerName(string value)
    {
        // Simple check: has letters and is longer than 2 characters
        return value.Length > 2 && value.Any(char.IsLetter) && !value.All(char.IsDigit);
    }

    private bool IsJerseyNumber(string value)
    {
        // Jersey numbers are typically 1-99
        return int.TryParse(value, out var number) && number >= 1 && number <= 99;
    }

    private IEnumerable<string> ExtractColumnHeaders(ExcelWorksheet worksheet, int headerRow)
    {
        var headers = new List<string>();
        
        if (worksheet.Dimension == null)
            return headers;

        for (int col = 1; col <= worksheet.Dimension.Columns; col++)
        {
            var header = worksheet.Cells[headerRow, col].Value?.ToString()?.Trim() ?? string.Empty;
            headers.Add(header);
        }

        return headers;
    }

    /// <summary>
    /// Parses player statistics from a specific Excel sheet
    /// Phase 2: Extracts detailed player performance data
    /// </summary>
    public async Task<ServiceResult<IEnumerable<PlayerStatisticsData>>> ParsePlayerStatisticsFromSheetAsync(
        Stream fileStream, string sheetName, int matchId, CancellationToken cancellationToken = default)
    {
        try
        {
            // CRITICAL FIX: Wrap synchronous Excel operations in Task.Run to prevent thread pool blocking
            return await Task.Run(() =>
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                
                using var package = new ExcelPackage(fileStream);
                var worksheet = package.Workbook.Worksheets[sheetName];
                
                if (worksheet == null)
                {
                    return ServiceResult<IEnumerable<PlayerStatisticsData>>.Failed($"Worksheet '{sheetName}' not found");
                }

                // Perform inline validation using the same worksheet to avoid stream conflicts
                var validationData = ValidatePlayerStatisticsSheet(worksheet, sheetName);
                _logger.LogInformation("Sheet {SheetName} validation: Columns={ColumnCount}, PlayerRows={PlayerRowCount}, IsValidStructure={IsValidStructure}", 
                    sheetName, validationData.ColumnCount, validationData.PlayerRowCount, validationData.IsValidStructure);
                
                if (!validationData.IsValidStructure)
                {
                    var errorMessages = validationData.ValidationErrors.Select(e => e.ErrorMessage).ToList();
                    _logger.LogWarning("Sheet {SheetName} has structure issues but proceeding anyway. Errors: {Errors}", 
                        sheetName, string.Join("; ", errorMessages));
                    
                    // Continue with parsing to see what we can extract, rather than failing completely
                }

                var playerStatistics = new List<PlayerStatisticsData>();
                var mapper = new PlayerStatisticsMapper(_logger, _transformationService);

                // Dynamically find where player data starts
                var startRow = FindPlayerStatsDataStartRow(worksheet);
                var maxRow = Math.Min(worksheet.Dimension?.Rows ?? 0, 
                                      startRow + ExcelColumnMappings.MAX_PLAYERS_PER_MATCH);

                _logger.LogInformation("Sheet {SheetName}: StartRow={StartRow}, MaxRow={MaxRow}, TotalRows={TotalRows}, TotalColumns={TotalColumns}", 
                    sheetName, startRow, maxRow, worksheet.Dimension?.Rows, worksheet.Dimension?.Columns);

                for (int row = startRow; row <= maxRow; row++)
                {
                    // Extract row data
                    var rowData = new object?[ExcelColumnMappings.TOTAL_COLUMNS];
                    for (int col = 0; col < ExcelColumnMappings.TOTAL_COLUMNS && col < (worksheet.Dimension?.Columns ?? 0); col++)
                    {
                        rowData[col] = worksheet.Cells[row, col + 1].Value; // Convert to 1-based
                    }

                    // Check if this row has valid player data
                    if (!ExcelColumnMappings.IsValidPlayerDataRow(rowData))
                    {
                        continue; // Skip empty or invalid rows
                    }

                    // Extract player information
                    var playerName = rowData[ExcelColumnMappings.PlayerInfo.PLAYER_NAME]?.ToString()?.Trim();
                    if (string.IsNullOrWhiteSpace(playerName))
                    {
                        continue; // Skip rows without player names
                    }

                    // Parse jersey number (from # column)
                    var jerseyNumber = ExtractJerseyNumber(rowData[ExcelColumnMappings.PlayerInfo.JERSEY_NUMBER]);

                    // Create player statistics data
                    var playerStatsData = new PlayerStatisticsData
                    {
                        PlayerName = playerName,
                        JerseyNumber = jerseyNumber,
                        RowNumber = row,
                        SheetName = sheetName,
                        PlayerId = 0 // Will be resolved during processing
                    };

                    // Map statistics using the mapper
                    var mapResult = mapper.MapToEntity(rowData, matchId, 0, row, sheetName);
                    if (mapResult.IsSuccess && mapResult.Data != null)
                    {
                        playerStatsData.Statistics = mapResult.Data;
                        playerStatsData.ValidationErrors = mapResult.ValidationErrors;
                    }
                    else
                    {
                        playerStatsData.ValidationErrors = mapResult.ValidationErrors;
                    }

                    playerStatistics.Add(playerStatsData);
                }

                _logger.LogInformation("Parsed {PlayerCount} player statistics from sheet {SheetName}", 
                    playerStatistics.Count, sheetName);

                return ServiceResult<IEnumerable<PlayerStatisticsData>>.Success(playerStatistics);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse player statistics from sheet {SheetName}", sheetName);
            return ServiceResult<IEnumerable<PlayerStatisticsData>>.Failed(
                $"Failed to parse player statistics from sheet '{sheetName}': {ex.Message}");
        }
    }

    /// <summary>
    /// Optimized version: Parses player statistics from a pre-loaded Excel worksheet
    /// Avoids stream exhaustion by using an existing worksheet instead of creating new ExcelPackage
    /// </summary>
    public async Task<ServiceResult<IEnumerable<PlayerStatisticsData>>> ParsePlayerStatisticsFromWorksheetAsync(
        ExcelWorksheet worksheet, int matchId, CancellationToken cancellationToken = default)
    {
        try
        {
            // CRITICAL FIX: Use pre-loaded worksheet directly - no stream recreation needed
            return await Task.Run(() =>
            {
                var sheetName = worksheet.Name;
                
                // Perform inline validation using the existing worksheet
                var validationData = ValidatePlayerStatisticsSheet(worksheet, sheetName);
                _logger.LogInformation("Sheet {SheetName} validation: Columns={ColumnCount}, PlayerRows={PlayerRowCount}, IsValidStructure={IsValidStructure}", 
                    sheetName, validationData.ColumnCount, validationData.PlayerRowCount, validationData.IsValidStructure);
                
                if (!validationData.IsValidStructure)
                {
                    var errorMessages = validationData.ValidationErrors.Select(e => e.ErrorMessage).ToList();
                    _logger.LogWarning("Sheet {SheetName} has structure issues but proceeding anyway. Errors: {Errors}", 
                        sheetName, string.Join("; ", errorMessages));
                    
                    // Continue with parsing to see what we can extract, rather than failing completely
                }

                var playerStatistics = new List<PlayerStatisticsData>();
                var mapper = new PlayerStatisticsMapper(_logger, _transformationService);

                // Dynamically find where player data starts
                var startRow = FindPlayerStatsDataStartRow(worksheet);
                var maxRow = Math.Min(worksheet.Dimension?.Rows ?? 0, 
                                      startRow + ExcelColumnMappings.MAX_PLAYERS_PER_MATCH);

                _logger.LogInformation("Sheet {SheetName}: StartRow={StartRow}, MaxRow={MaxRow}, TotalRows={TotalRows}, TotalColumns={TotalColumns}", 
                    sheetName, startRow, maxRow, worksheet.Dimension?.Rows, worksheet.Dimension?.Columns);

                for (int row = startRow; row <= maxRow; row++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    // Extract row data efficiently using bulk reading
                    var rowData = new object?[ExcelColumnMappings.TOTAL_COLUMNS];
                    for (int col = 0; col < ExcelColumnMappings.TOTAL_COLUMNS && col < (worksheet.Dimension?.Columns ?? 0); col++)
                    {
                        rowData[col] = worksheet.Cells[row, col + 1].Value; // Convert to 1-based
                    }

                    // Check if this row has valid player data
                    if (!ExcelColumnMappings.IsValidPlayerDataRow(rowData))
                    {
                        // Check if it's an invalid player name that we're specifically filtering out
                        var potentialPlayerName = rowData[ExcelColumnMappings.PlayerInfo.PLAYER_NAME]?.ToString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(potentialPlayerName) && !ExcelColumnMappings.IsValidPlayerName(potentialPlayerName))
                        {
                            _logger.LogInformation("🚫 Skipping invalid player entry '{PlayerName}' from row {Row} in sheet '{SheetName}' - template placeholder or summary row", 
                                potentialPlayerName, row, sheetName);
                        }
                        continue; // Skip empty or invalid rows
                    }

                    // Extract player information
                    var playerName = rowData[ExcelColumnMappings.PlayerInfo.PLAYER_NAME]?.ToString()?.Trim();
                    if (string.IsNullOrWhiteSpace(playerName))
                    {
                        continue; // Skip rows without player names
                    }

                    // Parse jersey number (from # column)
                    var jerseyNumber = ExtractJerseyNumber(rowData[ExcelColumnMappings.PlayerInfo.JERSEY_NUMBER]);

                    // Create player statistics data
                    var playerStatsData = new PlayerStatisticsData
                    {
                        PlayerName = playerName,
                        JerseyNumber = jerseyNumber,
                        RowNumber = row,
                        SheetName = sheetName,
                        PlayerId = 0 // Will be resolved during processing
                    };

                    // Map statistics using the mapper
                    var mapResult = mapper.MapToEntity(rowData, matchId, 0, row, sheetName);
                    if (mapResult.IsSuccess && mapResult.Data != null)
                    {
                        playerStatsData.Statistics = mapResult.Data;
                        playerStatsData.ValidationErrors = mapResult.ValidationErrors;
                    }
                    else
                    {
                        playerStatsData.ValidationErrors = mapResult.ValidationErrors;
                    }

                    playerStatistics.Add(playerStatsData);
                }

                _logger.LogInformation("Parsed {PlayerCount} player statistics from sheet {SheetName} using optimized worksheet method", 
                    playerStatistics.Count, sheetName);

                return ServiceResult<IEnumerable<PlayerStatisticsData>>.Success(playerStatistics);
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Player statistics parsing cancelled for sheet {SheetName}", worksheet.Name);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse player statistics from worksheet {SheetName}", worksheet.Name);
            return ServiceResult<IEnumerable<PlayerStatisticsData>>.Failed(
                $"Failed to parse player statistics from sheet '{worksheet.Name}': {ex.Message}");
        }
    }

    /// <summary>
    /// Validates player statistics sheet structure and data
    /// </summary>
    /// <summary>
    /// Synchronous validation that works with an existing worksheet to avoid stream conflicts
    /// </summary>
    private PlayerStatsValidationResult ValidatePlayerStatisticsSheet(ExcelWorksheet worksheet, string sheetName)
    {
        var result = new PlayerStatsValidationResult();
        var errors = new List<ValidationError>();

        // Check basic dimensions
        if (worksheet.Dimension == null)
        {
            errors.Add(new ValidationError
            {
                SheetName = sheetName,
                ErrorType = EtlErrorTypes.SHEET_STRUCTURE,
                ErrorMessage = "Sheet is empty or has no data",
                SuggestedFix = "Ensure sheet contains player statistics data"
            });
            
            result.ValidationErrors = errors;
            return result;
        }

        result.ColumnCount = worksheet.Dimension.Columns;
        
        // Validate minimum columns
        if (result.ColumnCount < ExcelColumnMappings.MIN_REQUIRED_COLUMNS)
        {
            errors.Add(new ValidationError
            {
                SheetName = sheetName,
                ErrorType = EtlErrorTypes.SHEET_STRUCTURE,
                ErrorMessage = $"Sheet has insufficient columns: {result.ColumnCount}. Expected at least {ExcelColumnMappings.MIN_REQUIRED_COLUMNS}",
                SuggestedFix = "Ensure all required columns are present in the Excel sheet"
            });
        }

        // Count player rows (simplified validation)
        var startRow = FindPlayerStatsDataStartRow(worksheet);
        var maxRow = Math.Min(worksheet.Dimension.Rows, startRow + ExcelColumnMappings.MAX_PLAYERS_PER_MATCH);
        
        int playerRowCount = 0;
        for (int row = startRow; row <= maxRow; row++)
        {
            var rowData = new object?[ExcelColumnMappings.TOTAL_COLUMNS];
            for (int col = 0; col < Math.Min(ExcelColumnMappings.TOTAL_COLUMNS, worksheet.Dimension.Columns); col++)
            {
                rowData[col] = worksheet.Cells[row, col + 1].Value;
            }

            if (ExcelColumnMappings.IsValidPlayerDataRow(rowData))
            {
                playerRowCount++;
            }
        }

        result.PlayerRowCount = playerRowCount;
        result.ValidationErrors = errors;
        result.IsValidStructure = errors.Count == 0 && playerRowCount > 0;

        return result;
    }

    public async Task<ServiceResult<PlayerStatsValidationResult>> ValidatePlayerStatisticsSheetAsync(
        Stream fileStream, string sheetName)
    {
        try
        {
            // CRITICAL FIX: Wrap synchronous Excel operations in Task.Run to prevent thread pool blocking
            return await Task.Run(() =>
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                
                using var package = new ExcelPackage(fileStream);
                var worksheet = package.Workbook.Worksheets[sheetName];
                
                if (worksheet == null)
                {
                    return ServiceResult<PlayerStatsValidationResult>.Failed($"Worksheet '{sheetName}' not found");
                }

                var result = ValidatePlayerStatisticsSheet(worksheet, sheetName);
                return ServiceResult<PlayerStatsValidationResult>.Success(result);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return ServiceResult<PlayerStatsValidationResult>.Failed($"Validation failed: {ex.Message}");
        }
    }

    private int? ExtractJerseyNumber(object? cellValue)
    {
        var stringValue = cellValue?.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(stringValue))
            return null;

        // Try parsing as integer first (direct jersey number)
        if (int.TryParse(stringValue, out var intValue))
        {
            if (intValue >= GaaConstants.MIN_JERSEY_NUMBER && intValue <= GaaConstants.MAX_JERSEY_NUMBER)
            {
                return intValue;
            }
        }

        // If it's a larger number, it might be minutes played, not jersey number
        // In this case, return null and let jersey number be derived from player position
        return null;
    }

    // KPI parsing methods temporarily removed to fix compilation errors

    #endregion
}
