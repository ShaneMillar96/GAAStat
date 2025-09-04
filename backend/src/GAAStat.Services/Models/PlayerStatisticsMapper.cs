using GAAStat.Dal.Models.Application;
using GAAStat.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace GAAStat.Services.Models;

/// <summary>
/// Maps Excel row data to MatchPlayerStatistics entity
/// Handles data transformation, validation, and error collection
/// </summary>
public class PlayerStatisticsMapper
{
    private readonly ILogger _logger;
    private readonly IDataTransformationService _transformationService;

    public PlayerStatisticsMapper(
        ILogger logger,
        IDataTransformationService transformationService)
    {
        _logger = logger;
        _transformationService = transformationService;
    }

    /// <summary>
    /// Maps Excel row data to MatchPlayerStatistics entity
    /// </summary>
    public MapResult<MatchPlayerStatistics> MapToEntity(
        object?[] rowData, 
        int matchId, 
        int playerId, 
        int rowNumber, 
        string sheetName)
    {
        var errors = new List<ValidationError>();
        
        try
        {
            var playerStats = new MatchPlayerStatistics
            {
                MatchId = matchId,
                PlayerId = playerId
            };

            // Map basic engagement data
            playerStats.MinutesPlayed = ParseIntValue(rowData, ExcelColumnMappings.PlayerInfo.MINUTES_PLAYED, 0, 70, 
                "Minutes played", rowNumber, sheetName, errors);
            playerStats.TotalEngagements = ParseIntValue(rowData, ExcelColumnMappings.PlayerInfo.TOTAL_ENGAGEMENTS, 0, 200, 
                "Total engagements", rowNumber, sheetName, errors);

            // Map possession statistics
            playerStats.TotalPossessions = ParseIntValue(rowData, ExcelColumnMappings.Possession.TOTAL_POSSESSIONS, 0, 100, 
                "Total possessions", rowNumber, sheetName, errors);
            playerStats.TurnoversWon = ParseIntValue(rowData, ExcelColumnMappings.Possession.TURNOVERS_WON, 0, 50, 
                "Turnovers won", rowNumber, sheetName, errors);
            playerStats.Interceptions = ParseIntValue(rowData, ExcelColumnMappings.Possession.INTERCEPTIONS, 0, 30, 
                "Interceptions", rowNumber, sheetName, errors);

            // Map attacking statistics
            playerStats.TotalAttacks = ParseIntValue(rowData, ExcelColumnMappings.Attacking.TOTAL_ATTACKS, 0, 50, 
                "Total attacks", rowNumber, sheetName, errors);
            playerStats.KickRetained = ParseIntValue(rowData, ExcelColumnMappings.Attacking.KICK_RETAINED, 0, 30, 
                "Kicks retained", rowNumber, sheetName, errors);
            playerStats.KickLost = ParseIntValue(rowData, ExcelColumnMappings.Attacking.KICK_LOST, 0, 30, 
                "Kicks lost", rowNumber, sheetName, errors);
            playerStats.CarryRetained = ParseIntValue(rowData, ExcelColumnMappings.Attacking.CARRY_RETAINED, 0, 30, 
                "Carries retained", rowNumber, sheetName, errors);
            playerStats.CarryLost = ParseIntValue(rowData, ExcelColumnMappings.Attacking.CARRY_LOST, 0, 30, 
                "Carries lost", rowNumber, sheetName, errors);

            // Map shooting statistics
            playerStats.ShotsTotal = ParseIntValue(rowData, ExcelColumnMappings.Shooting.SHOTS_TOTAL, 0, 20, 
                "Total shots", rowNumber, sheetName, errors);

            // CRITICAL FIX: Read Goals and Points from individual columns for better accuracy
            // These are the actual source columns that contain the correct values
            var directGoals = ParseIntValue(rowData, ExcelColumnMappings.Shooting.GOALS, 0, 10, 
                "Goals", rowNumber, sheetName, errors);
            var directPoints = ParseIntValue(rowData, ExcelColumnMappings.Shooting.POINTS, 0, 20, 
                "Points", rowNumber, sheetName, errors);
            
            // Also parse from the Scores column (format: "G-PP" or "G-PP(Nf)") for validation
            var scoresText = ParseStringValue(rowData, ExcelColumnMappings.Possession.SCORES, 
                "Scores", rowNumber, sheetName, errors);
            
            if (!string.IsNullOrEmpty(scoresText))
            {
                var (parsedGoals, parsedPoints, frees) = _transformationService.ParsePlayerScore(scoresText);
                
                // Use direct column values as primary source, but validate against parsed scores
                playerStats.Goals = directGoals;
                playerStats.Points = directPoints;
                playerStats.Scores = scoresText; // Keep original format from Excel
                
                // Log discrepancies between direct columns and parsed scores
                if (directGoals != parsedGoals || directPoints != parsedPoints)
                {
                    _logger.LogWarning("📊 SCORE DISCREPANCY for {PlayerName} in {SheetName} row {Row}: " +
                        "Direct columns: {DirectGoals}-{DirectPoints}, Parsed from '{ScoreText}': {ParsedGoals}-{ParsedPoints}",
                        ParseStringValue(rowData, ExcelColumnMappings.PlayerInfo.PLAYER_NAME, "Player name", rowNumber, sheetName, errors),
                        sheetName, rowNumber, directGoals, directPoints, scoresText, parsedGoals, parsedPoints);
                        
                    errors.Add(new ValidationError
                    {
                        SheetName = sheetName,
                        RowNumber = rowNumber,
                        ErrorType = "score_discrepancy", 
                        ErrorMessage = $"Score mismatch: Direct columns ({directGoals}-{directPoints}) vs Parsed ({parsedGoals}-{parsedPoints}) from '{scoresText}'",
                        SuggestedFix = "Verify Goals and Points columns match Scores column format"
                    });
                }
                
                // Log if frees were detected (for analysis but not stored in main table)
                if (frees > 0)
                {
                    _logger.LogDebug("Player scored {Frees} free kicks in {Scores} for {SheetName} row {Row}", 
                        frees, scoresText, sheetName, rowNumber);
                }
            }
            else
            {
                // Use direct column values when no score text is available
                playerStats.Goals = directGoals;
                playerStats.Points = directPoints;
                playerStats.Scores = directGoals > 0 || directPoints > 0 ? $"{directGoals}-{directPoints:00}" : string.Empty;
            }

            playerStats.Wides = ParseIntValue(rowData, ExcelColumnMappings.Shooting.WIDES, 0, 15, 
                "Wides", rowNumber, sheetName, errors);

            // Map defensive statistics
            playerStats.TacklesTotal = ParseIntValue(rowData, ExcelColumnMappings.Defensive.TACKLES_TOTAL, 0, 50, 
                "Total tackles", rowNumber, sheetName, errors);
            playerStats.TacklesContact = ParseIntValue(rowData, ExcelColumnMappings.Defensive.TACKLES_CONTACT, 0, 50, 
                "Contact tackles", rowNumber, sheetName, errors);
            playerStats.TacklesMissed = ParseIntValue(rowData, ExcelColumnMappings.Defensive.TACKLES_MISSED, 0, 20, 
                "Missed tackles", rowNumber, sheetName, errors);
            playerStats.FreesConcededTotal = ParseIntValue(rowData, ExcelColumnMappings.Defensive.FREES_CONCEDED_TOTAL, 0, 20, 
                "Frees conceded", rowNumber, sheetName, errors);

            // Map disciplinary statistics
            playerStats.YellowCards = ParseIntValue(rowData, ExcelColumnMappings.Disciplinary.YELLOW_CARDS, 0, 2, 
                "Yellow cards", rowNumber, sheetName, errors);
            playerStats.BlackCards = ParseIntValue(rowData, ExcelColumnMappings.Disciplinary.BLACK_CARDS, 0, 1, 
                "Black cards", rowNumber, sheetName, errors);
            playerStats.RedCards = ParseIntValue(rowData, ExcelColumnMappings.Disciplinary.RED_CARDS, 0, 1, 
                "Red cards", rowNumber, sheetName, errors);

            // Map goalkeeper statistics (usually only for goalkeeper)
            playerStats.KickoutsTotal = ParseIntValue(rowData, ExcelColumnMappings.Goalkeeper.KICKOUTS_TOTAL, 0, 50, 
                "Total kickouts", rowNumber, sheetName, errors);
            playerStats.KickoutsRetained = ParseIntValue(rowData, ExcelColumnMappings.Goalkeeper.KICKOUTS_RETAINED, 0, 50, 
                "Kickouts retained", rowNumber, sheetName, errors);
            playerStats.KickoutsLost = ParseIntValue(rowData, ExcelColumnMappings.Goalkeeper.KICKOUTS_LOST, 0, 50, 
                "Kickouts lost", rowNumber, sheetName, errors);
            playerStats.Saves = ParseIntValue(rowData, ExcelColumnMappings.Goalkeeper.SAVES, 0, 20, 
                "Saves", rowNumber, sheetName, errors);

            // Calculate percentage fields
            CalculatePercentages(playerStats, rowData, rowNumber, sheetName, errors);

            _logger.LogDebug("Successfully mapped player statistics for player {PlayerId} in match {MatchId}", 
                playerId, matchId);

            return new MapResult<MatchPlayerStatistics>
            {
                IsSuccess = true,
                Data = playerStats,
                ValidationErrors = errors
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to map player statistics for player {PlayerId} in match {MatchId}", 
                playerId, matchId);
                
            errors.Add(new ValidationError
            {
                SheetName = sheetName,
                RowNumber = rowNumber,
                ErrorType = EtlErrorTypes.MISSING_DATA,
                ErrorMessage = $"Failed to parse player statistics: {ex.Message}",
                SuggestedFix = "Check Excel data format and values"
            });

            return new MapResult<MatchPlayerStatistics>
            {
                IsSuccess = false,
                Data = null,
                ValidationErrors = errors
            };
        }
    }

    /// <summary>
    /// Parses integer value from Excel cell with validation
    /// </summary>
    private int ParseIntValue(
        object?[] rowData, 
        int columnIndex, 
        int minValue, 
        int maxValue,
        string fieldName,
        int rowNumber,
        string sheetName,
        List<ValidationError> errors)
    {
        if (columnIndex >= rowData.Length)
        {
            return 0; // Default to 0 for missing columns
        }

        var cellValue = rowData[columnIndex]?.ToString();
        if (string.IsNullOrWhiteSpace(cellValue))
        {
            return 0; // Default to 0 for empty cells
        }

        if (int.TryParse(cellValue, out var intValue))
        {
            if (intValue < minValue || intValue > maxValue)
            {
                errors.Add(new ValidationError
                {
                    SheetName = sheetName,
                    RowNumber = rowNumber,
                    ColumnName = ExcelColumnMappings.GetExpectedHeader(columnIndex),
                    ErrorType = "validation_warning",
                    ErrorMessage = $"{fieldName} value {intValue} is outside expected range ({minValue}-{maxValue})",
                    SuggestedFix = $"Verify {fieldName} value is correct"
                });
            }
            return intValue;
        }

        errors.Add(new ValidationError
        {
            SheetName = sheetName,
            RowNumber = rowNumber,
            ColumnName = ExcelColumnMappings.GetExpectedHeader(columnIndex),
            ErrorType = EtlErrorTypes.MISSING_DATA,
            ErrorMessage = $"Invalid {fieldName} value: '{cellValue}'. Expected integer.",
            SuggestedFix = $"Enter valid integer for {fieldName}"
        });

        return 0; // Default to 0 for invalid values
    }

    /// <summary>
    /// Parses string value from Excel cell
    /// </summary>
    private string ParseStringValue(
        object?[] rowData, 
        int columnIndex, 
        string fieldName,
        int rowNumber,
        string sheetName,
        List<ValidationError> errors)
    {
        if (columnIndex >= rowData.Length)
        {
            return string.Empty;
        }

        var cellValue = rowData[columnIndex]?.ToString();
        if (string.IsNullOrWhiteSpace(cellValue))
        {
            return string.Empty;
        }

        return cellValue.Trim();
    }

    /// <summary>
    /// Parses decimal percentage value from Excel cell
    /// </summary>
    private decimal? ParseDecimalValue(
        object?[] rowData, 
        int columnIndex, 
        string fieldName,
        int rowNumber,
        string sheetName,
        List<ValidationError> errors)
    {
        if (columnIndex >= rowData.Length)
        {
            return null;
        }

        var cellValue = rowData[columnIndex]?.ToString();
        if (string.IsNullOrWhiteSpace(cellValue))
        {
            return null;
        }

        // Handle percentage values (e.g., "85.5%" or "0.855")
        var cleanValue = cellValue.Replace("%", "").Trim();
        
        if (decimal.TryParse(cleanValue, out var decimalValue))
        {
            // If value is greater than 1, assume it's a percentage and convert
            if (decimalValue > 1)
            {
                decimalValue = decimalValue / 100;
            }
            
            // Validate percentage range
            if (decimalValue < 0 || decimalValue > 1)
            {
                errors.Add(new ValidationError
                {
                    SheetName = sheetName,
                    RowNumber = rowNumber,
                    ColumnName = ExcelColumnMappings.GetExpectedHeader(columnIndex),
                    ErrorType = "validation_warning",
                    ErrorMessage = $"{fieldName} percentage {decimalValue:P} is outside valid range (0-100%)",
                    SuggestedFix = $"Verify {fieldName} percentage is correct"
                });
            }
            
            return Math.Round(decimalValue, 4);
        }

        errors.Add(new ValidationError
        {
            SheetName = sheetName,
            RowNumber = rowNumber,
            ColumnName = ExcelColumnMappings.GetExpectedHeader(columnIndex),
            ErrorType = EtlErrorTypes.MISSING_DATA,
            ErrorMessage = $"Invalid {fieldName} percentage: '{cellValue}'. Expected decimal or percentage.",
            SuggestedFix = $"Enter valid percentage for {fieldName}"
        });

        return null;
    }

    /// <summary>
    /// Parses engagement efficiency value directly from Excel without percentage conversion
    /// Engagement efficiency is a ratio (TP + ToW + Int) / TE and can legitimately exceed 1.0
    /// </summary>
    private decimal? ParseEngagementEfficiency(
        object?[] rowData, 
        int columnIndex, 
        string fieldName,
        int rowNumber,
        string sheetName,
        List<ValidationError> errors)
    {
        if (columnIndex >= rowData.Length)
        {
            return null;
        }

        var cellValue = rowData[columnIndex]?.ToString();
        if (string.IsNullOrWhiteSpace(cellValue))
        {
            return null;
        }

        // Parse the raw decimal value without any percentage conversion
        if (decimal.TryParse(cellValue, out var decimalValue))
        {
            // Validate engagement efficiency is within reasonable range (0.0 to 2.5)
            // Higher values are possible but unusual
            if (decimalValue < 0m || decimalValue > 2.5m)
            {
                errors.Add(new ValidationError
                {
                    SheetName = sheetName,
                    RowNumber = rowNumber,
                    ColumnName = ExcelColumnMappings.GetExpectedHeader(columnIndex),
                    ErrorType = "validation_warning",
                    ErrorMessage = $"{fieldName} value {decimalValue:F4} is outside normal range (0.0-2.5)",
                    SuggestedFix = $"Verify {fieldName} Excel calculation or data entry"
                });
            }
            
            return Math.Round(decimalValue, 4);
        }

        errors.Add(new ValidationError
        {
            SheetName = sheetName,
            RowNumber = rowNumber,
            ColumnName = ExcelColumnMappings.GetExpectedHeader(columnIndex),
            ErrorType = EtlErrorTypes.MISSING_DATA,
            ErrorMessage = $"Invalid {fieldName} value: '{cellValue}'. Expected decimal number.",
            SuggestedFix = $"Enter valid decimal number for {fieldName}"
        });

        return null;
    }

    /// <summary>
    /// Calculates percentage fields based on raw statistics
    /// </summary>
    private void CalculatePercentages(
        MatchPlayerStatistics playerStats, 
        object?[] rowData, 
        int rowNumber, 
        string sheetName, 
        List<ValidationError> errors)
    {
        // CRITICAL FIX: Read engagement efficiency directly from Excel Column E (TE/PSR) as raw ratio value
        // Excel contains the correct engagement efficiency values as ratios that can exceed 1.0
        playerStats.EngagementEfficiency = ParseEngagementEfficiency(rowData, ExcelColumnMappings.Possession.TE_PSR_RATIO, 
            "Engagement efficiency", rowNumber, sheetName, errors);

        // Parse possession success rate from Excel if available, otherwise calculate
        playerStats.PossessionSuccessRate = ParseDecimalValue(rowData, ExcelColumnMappings.Possession.POSSESSION_SUCCESS_RATE, 
            "Possession success rate", rowNumber, sheetName, errors);

        // Calculate possessions per TE
        if (playerStats.TotalEngagements > 0)
        {
            playerStats.PossessionsPerTe = Math.Round((decimal)playerStats.TotalPossessions / playerStats.TotalEngagements, 4);
        }

        // Calculate conversion rate (goals + points) / total shots
        if (playerStats.ShotsTotal > 0)
        {
            var successfulShots = playerStats.Goals + playerStats.Points;
            playerStats.ConversionRate = Math.Round((decimal)successfulShots / playerStats.ShotsTotal, 4);
        }

        // Calculate tackle percentage (successful tackles / total tackles)
        if (playerStats.TacklesTotal > 0)
        {
            var successfulTackles = playerStats.TacklesTotal - playerStats.TacklesMissed;
            playerStats.TacklePercentage = Math.Round((decimal)successfulTackles / playerStats.TacklesTotal, 4);
        }

        // Calculate kickout percentage (retained / total)
        if (playerStats.KickoutsTotal > 0)
        {
            playerStats.KickoutPercentage = Math.Round((decimal)playerStats.KickoutsRetained / playerStats.KickoutsTotal, 4);
        }
    }
}

/// <summary>
/// Result of mapping operation
/// </summary>
public class MapResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public List<ValidationError> ValidationErrors { get; set; } = new();
}