using GAAStat.Services.Interfaces;
using GAAStat.Services.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace GAAStat.Api.Controllers;

/// <summary>
/// Controller for match data management and Excel import operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MatchesController : ControllerBase
{
    private readonly IExcelImportService _importService;
    private readonly IMatchAnalyticsService _matchAnalytics;
    private readonly IStatisticsCalculationService _statisticsService;
    private readonly ILogger<MatchesController> _logger;

    public MatchesController(
        IExcelImportService importService,
        IMatchAnalyticsService matchAnalytics,
        IStatisticsCalculationService statisticsService,
        ILogger<MatchesController> logger)
    {
        _importService = importService;
        _matchAnalytics = matchAnalytics;
        _statisticsService = statisticsService;
        _logger = logger;
    }

    /// <summary>
    /// Uploads and processes an Excel file containing GAA match statistics
    /// </summary>
    /// <param name="request">File upload request containing Excel file and configuration</param>
    /// <returns>Import operation result with comprehensive summary</returns>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadMatchFile([FromForm] UploadMatchFileRequest request)
    {
        if (request.File == null || request.File.Length == 0)
            return BadRequest(ApiResponse.Error("No file provided"));

        if (!IsValidExcelFile(request.File))
            return BadRequest(ApiResponse.Error("Invalid file type. Only Excel files (.xlsx, .xls) are supported"));

        if (request.File.Length > GetMaxFileSizeBytes())
            return BadRequest(ApiResponse.Error($"File size exceeds maximum limit of {GetMaxFileSizeMB()}MB"));

        try
        {
            using var stream = request.File.OpenReadStream();
            
            var config = new BulkOperationConfig
            {
                BatchSize = request.BatchSize ?? 1000,
                EnableBulkInsert = request.EnableBulkInsert ?? true,
                EnableParallelProcessing = request.EnableParallelProcessing ?? true,
                MaxConcurrentBatches = request.MaxConcurrentBatches ?? 4
            };

            var result = await _importService.ImportMatchDataAsync(
                stream, 
                request.File.FileName, 
                config);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Excel file {FileName} imported successfully with {MatchCount} matches", 
                    request.File.FileName, result.Data?.MatchesImported);

                return Ok(ApiResponse.Success(result.Data, "File imported successfully"));
            }

            _logger.LogWarning("Excel file {FileName} import failed: {Error}", 
                request.File.FileName, result.ErrorMessage);

            return BadRequest(ApiResponse.Error(result.ErrorMessage ?? "Import failed", result.ValidationErrors));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during Excel file import for {FileName}", request.File?.FileName);
            return StatusCode(500, ApiResponse.Error("An error occurred during file processing"));
        }
    }

    /// <summary>
    /// Validates an Excel file without importing the data
    /// </summary>
    /// <param name="file">Excel file to validate</param>
    /// <param name="performCrossSheetValidation">Whether to perform cross-sheet validation (default: true)</param>
    /// <returns>Validation results with detailed analysis</returns>
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateExcelFile(
        IFormFile file, 
        [FromQuery] bool performCrossSheetValidation = true)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse.Error("No file provided"));

        if (!IsValidExcelFile(file))
            return BadRequest(ApiResponse.Error("Invalid file type. Only Excel files (.xlsx, .xls) are supported"));

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _importService.ValidateExcelFileAsync(stream, file.FileName, performCrossSheetValidation);

            return result.IsSuccess 
                ? Ok(ApiResponse.Success(result.Data))
                : BadRequest(ApiResponse.Error(result.ErrorMessage ?? "Validation failed", result.ValidationErrors));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during Excel file validation for {FileName}", file.FileName);
            return StatusCode(500, ApiResponse.Error("An error occurred during file validation"));
        }
    }

    /// <summary>
    /// Gets comprehensive statistics for a specific match
    /// </summary>
    /// <param name="matchId">Match identifier</param>
    /// <returns>Complete match statistics including team and player data</returns>
    [HttpGet("{matchId}/statistics")]
    public async Task<IActionResult> GetMatchStatistics(int matchId)
    {
        if (matchId <= 0)
            return BadRequest(ApiResponse.Error("Invalid match ID"));

        try
        {
            var result = await _matchAnalytics.GetMatchSummaryAsync(matchId);
            return result.IsSuccess 
                ? Ok(ApiResponse.Success(result.Data))
                : NotFound(ApiResponse.Error($"Match with ID {matchId} not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while retrieving statistics for match {MatchId}", matchId);
            return StatusCode(500, ApiResponse.Error("An error occurred while retrieving match statistics"));
        }
    }

    /// <summary>
    /// Gets a list of all matches with basic information
    /// </summary>
    /// <param name="seasonId">Optional season filter</param>
    /// <param name="teamId">Optional team filter</param>
    /// <param name="limit">Maximum number of matches to return (default: 50)</param>
    /// <param name="offset">Number of matches to skip for pagination (default: 0)</param>
    /// <returns>List of matches with basic information</returns>
    [HttpGet]
    public async Task<IActionResult> GetMatches(
        [FromQuery] int? seasonId = null,
        [FromQuery] int? teamId = null,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        if (limit <= 0 || limit > 100)
            return BadRequest(ApiResponse.Error("Limit must be between 1 and 100"));

        if (offset < 0)
            return BadRequest(ApiResponse.Error("Offset must be non-negative"));

        try
        {
            // This would typically call a service method to get match list
            // For now, return a placeholder response
            var response = new MatchListResponse
            {
                Matches = new List<MatchSummaryDto>(),
                TotalCount = 0,
                Limit = limit,
                Offset = offset
            };

            return Ok(ApiResponse.Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while retrieving matches");
            return StatusCode(500, ApiResponse.Error("An error occurred while retrieving matches"));
        }
    }

    /// <summary>
    /// Gets team comparison data for a specific match
    /// </summary>
    /// <param name="matchId">Match identifier</param>
    /// <returns>Side-by-side team performance comparison</returns>
    [HttpGet("{matchId}/team-comparison")]
    public async Task<IActionResult> GetMatchTeamComparison(int matchId)
    {
        if (matchId <= 0)
            return BadRequest(ApiResponse.Error("Invalid match ID"));

        try
        {
            var result = await _matchAnalytics.GetMatchTeamComparisonAsync(matchId);
            return result.IsSuccess 
                ? Ok(ApiResponse.Success(result.Data))
                : NotFound(ApiResponse.Error($"Match with ID {matchId} not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while retrieving team comparison for match {MatchId}", matchId);
            return StatusCode(500, ApiResponse.Error("An error occurred while retrieving team comparison"));
        }
    }

    /// <summary>
    /// Gets player performance data for a specific match
    /// </summary>
    /// <param name="matchId">Match identifier</param>
    /// <param name="teamId">Optional team filter to show only players from specific team</param>
    /// <param name="position">Optional position filter</param>
    /// <param name="limit">Maximum number of players to return (default: 30)</param>
    /// <returns>Player performance data for the match</returns>
    [HttpGet("{matchId}/players")]
    public async Task<IActionResult> GetMatchPlayers(
        int matchId,
        [FromQuery] int? teamId = null,
        [FromQuery] string? position = null,
        [FromQuery] int limit = 30)
    {
        if (matchId <= 0)
            return BadRequest(ApiResponse.Error("Invalid match ID"));

        if (limit <= 0 || limit > 50)
            return BadRequest(ApiResponse.Error("Limit must be between 1 and 50"));

        try
        {
            // This would call a service method to get player data for the match
            // For now, return a placeholder response
            var response = new MatchPlayersResponse
            {
                MatchId = matchId,
                Players = new List<PlayerMatchStatisticsDto>()
            };

            return Ok(ApiResponse.Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while retrieving players for match {MatchId}", matchId);
            return StatusCode(500, ApiResponse.Error("An error occurred while retrieving match players"));
        }
    }

    /// <summary>
    /// Gets kickout analysis for a specific match
    /// </summary>
    /// <param name="matchId">Match identifier</param>
    /// <returns>Comprehensive kickout statistics and analysis</returns>
    [HttpGet("{matchId}/kickout-analysis")]
    public async Task<IActionResult> GetKickoutAnalysis(int matchId)
    {
        if (matchId <= 0)
            return BadRequest(ApiResponse.Error("Invalid match ID"));

        try
        {
            var result = await _matchAnalytics.GetKickoutAnalysisAsync(matchId);
            return result.IsSuccess 
                ? Ok(ApiResponse.Success(result.Data))
                : NotFound(ApiResponse.Error($"Match with ID {matchId} not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while retrieving kickout analysis for match {MatchId}", matchId);
            return StatusCode(500, ApiResponse.Error("An error occurred while retrieving kickout analysis"));
        }
    }

    #region Private Helper Methods

    private static bool IsValidExcelFile(IFormFile file)
    {
        if (file == null) return false;

        var allowedExtensions = new[] { ".xlsx", ".xls" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        return allowedExtensions.Contains(extension) && 
               (file.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" ||
                file.ContentType == "application/vnd.ms-excel");
    }

    private static long GetMaxFileSizeBytes()
    {
        return GetMaxFileSizeMB() * 1024 * 1024;
    }

    private static int GetMaxFileSizeMB()
    {
        return int.TryParse(Environment.GetEnvironmentVariable("MAX_FILE_SIZE_MB"), out var size) ? size : 50;
    }

    #endregion
}

#region Request/Response DTOs

/// <summary>
/// Request model for Excel file upload
/// </summary>
public class UploadMatchFileRequest
{
    [Required]
    public IFormFile File { get; set; } = null!;

    [Range(100, 5000)]
    public int? BatchSize { get; set; }

    public bool? EnableBulkInsert { get; set; }

    public bool? EnableParallelProcessing { get; set; }

    [Range(1, 8)]
    public int? MaxConcurrentBatches { get; set; }
}

/// <summary>
/// Response model for match list endpoint
/// </summary>
public class MatchListResponse
{
    public IEnumerable<MatchSummaryDto> Matches { get; set; } = new List<MatchSummaryDto>();
    public int TotalCount { get; set; }
    public int Limit { get; set; }
    public int Offset { get; set; }
}

/// <summary>
/// Basic match information for list display
/// </summary>
public class MatchSummaryDto
{
    public int Id { get; set; }
    public string Competition { get; set; } = string.Empty;
    public string HomeTeam { get; set; } = string.Empty;
    public string AwayTeam { get; set; } = string.Empty;
    public DateTime MatchDate { get; set; }
    public string? Venue { get; set; }
    public string? HomeScore { get; set; }
    public string? AwayScore { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Response model for match players endpoint
/// </summary>
public class MatchPlayersResponse
{
    public int MatchId { get; set; }
    public IEnumerable<PlayerMatchStatisticsDto> Players { get; set; } = new List<PlayerMatchStatisticsDto>();
}

/// <summary>
/// Player statistics for a specific match
/// </summary>
public class PlayerMatchStatisticsDto
{
    public string PlayerName { get; set; } = string.Empty;
    public int? JerseyNumber { get; set; }
    public string Position { get; set; } = string.Empty;
    public int? MinutesPlayed { get; set; }
    public decimal? PerformanceSuccessRate { get; set; }
    public int? TotalPossessions { get; set; }
    public string? Score { get; set; }
    public int? TotalEvents { get; set; }
    public string TeamName { get; set; } = string.Empty;
}

#endregion