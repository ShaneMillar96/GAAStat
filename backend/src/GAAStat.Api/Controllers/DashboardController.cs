using GAAStat.Api.Models;
using GAAStat.Services.Dashboard.Interfaces;
using GAAStat.Services.Dashboard.Models;
using Microsoft.AspNetCore.Mvc;

namespace GAAStat.Api.Controllers;

/// <summary>
/// API controller for dashboard statistics and insights
/// Provides aggregated team and player performance metrics
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDashboardService dashboardService,
        ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Get team performance overview with season totals and averages
    /// </summary>
    /// <param name="seasonId">Season identifier (optional - uses current season if not provided)</param>
    /// <param name="competitionType">Filter by competition type: "League", "Championship", "Cup" (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Team overview with wins, losses, draws, scoring, and possession statistics</returns>
    /// <response code="200">Successfully retrieved team overview</response>
    /// <response code="404">Season or team not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("team-overview")]
    [ProducesResponseType(typeof(DashboardResponseDto<TeamOverviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DashboardResponseDto<TeamOverviewDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DashboardResponseDto<TeamOverviewDto>>> GetTeamOverview(
        [FromQuery] int? seasonId = null,
        [FromQuery] string? competitionType = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "GetTeamOverview called. SeasonId: {SeasonId}, Competition: {Competition}",
                seasonId,
                competitionType);

            var result = await _dashboardService.GetTeamOverviewAsync(
                seasonId,
                competitionType,
                cancellationToken);

            if (!result.Success)
            {
                var statusCode = DetermineStatusCode(result.Errors.FirstOrDefault()?.Code);
                return StatusCode(statusCode, MapToResponseDto(result));
            }

            return Ok(MapToResponseDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetTeamOverview");
            return StatusCode(500, new DashboardResponseDto<TeamOverviewDto>
            {
                Success = false,
                Errors = new List<ErrorDto>
                {
                    new ErrorDto { Code = "INTERNAL_ERROR", Message = "An unexpected error occurred" }
                }
            });
        }
    }

    /// <summary>
    /// Get top performers by metric (scoring, PSR, tackles, assists, possession, interceptions)
    /// </summary>
    /// <param name="metricType">Metric to rank by: "scoring", "psr", "tackles", "assists", "possession", "interceptions"</param>
    /// <param name="seasonId">Season identifier (optional)</param>
    /// <param name="competitionType">Filter by competition type (optional)</param>
    /// <param name="topCount">Number of top performers to return (default: 10)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of top performers ordered by metric value</returns>
    /// <response code="200">Successfully retrieved top performers</response>
    /// <response code="400">Invalid metric type</response>
    /// <response code="404">No data found</response>
    [HttpGet("top-performers")]
    [ProducesResponseType(typeof(DashboardResponseDto<List<TopPerformerDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DashboardResponseDto<List<TopPerformerDto>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(DashboardResponseDto<List<TopPerformerDto>>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DashboardResponseDto<List<TopPerformerDto>>>> GetTopPerformers(
        [FromQuery] string metricType,
        [FromQuery] int? seasonId = null,
        [FromQuery] string? competitionType = null,
        [FromQuery] int topCount = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(metricType))
            {
                return BadRequest(new DashboardResponseDto<List<TopPerformerDto>>
                {
                    Success = false,
                    Errors = new List<ErrorDto>
                    {
                        new ErrorDto { Code = "INVALID_METRIC", Message = "Metric type is required" }
                    }
                });
            }

            _logger.LogInformation(
                "GetTopPerformers called. Metric: {Metric}, SeasonId: {SeasonId}, TopCount: {Count}",
                metricType,
                seasonId,
                topCount);

            var result = await _dashboardService.GetTopPerformersAsync(
                metricType,
                seasonId,
                competitionType,
                topCount,
                cancellationToken);

            if (!result.Success)
            {
                var statusCode = DetermineStatusCode(result.Errors.FirstOrDefault()?.Code);
                return StatusCode(statusCode, MapToResponseDto(result));
            }

            return Ok(MapToResponseDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetTopPerformers");
            return StatusCode(500, new DashboardResponseDto<List<TopPerformerDto>>
            {
                Success = false,
                Errors = new List<ErrorDto>
                {
                    new ErrorDto { Code = "INTERNAL_ERROR", Message = "An unexpected error occurred" }
                }
            });
        }
    }

    /// <summary>
    /// Get recent match results with scores and outcomes
    /// </summary>
    /// <param name="seasonId">Season identifier (optional)</param>
    /// <param name="competitionType">Filter by competition type (optional)</param>
    /// <param name="matchCount">Number of recent matches to return (default: 5)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of recent matches ordered by date descending</returns>
    /// <response code="200">Successfully retrieved recent matches</response>
    /// <response code="404">No matches found</response>
    [HttpGet("recent-matches")]
    [ProducesResponseType(typeof(DashboardResponseDto<List<RecentMatchDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DashboardResponseDto<List<RecentMatchDto>>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DashboardResponseDto<List<RecentMatchDto>>>> GetRecentMatches(
        [FromQuery] int? seasonId = null,
        [FromQuery] string? competitionType = null,
        [FromQuery] int matchCount = 5,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "GetRecentMatches called. SeasonId: {SeasonId}, MatchCount: {Count}",
                seasonId,
                matchCount);

            var result = await _dashboardService.GetRecentMatchesAsync(
                seasonId,
                competitionType,
                matchCount,
                cancellationToken);

            if (!result.Success)
            {
                var statusCode = DetermineStatusCode(result.Errors.FirstOrDefault()?.Code);
                return StatusCode(statusCode, MapToResponseDto(result));
            }

            return Ok(MapToResponseDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetRecentMatches");
            return StatusCode(500, new DashboardResponseDto<List<RecentMatchDto>>
            {
                Success = false,
                Errors = new List<ErrorDto>
                {
                    new ErrorDto { Code = "INTERNAL_ERROR", Message = "An unexpected error occurred" }
                }
            });
        }
    }

    /// <summary>
    /// Get aggregated player season statistics
    /// </summary>
    /// <param name="seasonId">Season identifier (optional)</param>
    /// <param name="competitionType">Filter by competition type (optional)</param>
    /// <param name="positionCode">Filter by position: "GK", "DEF", "MID", "FWD" (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of player statistics aggregated across all matches</returns>
    /// <response code="200">Successfully retrieved player statistics</response>
    /// <response code="404">No statistics found</response>
    [HttpGet("player-season-stats")]
    [ProducesResponseType(typeof(DashboardResponseDto<List<PlayerSeasonStatsDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DashboardResponseDto<List<PlayerSeasonStatsDto>>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DashboardResponseDto<List<PlayerSeasonStatsDto>>>> GetPlayerSeasonStatistics(
        [FromQuery] int? seasonId = null,
        [FromQuery] string? competitionType = null,
        [FromQuery] string? positionCode = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "GetPlayerSeasonStatistics called. SeasonId: {SeasonId}, Position: {Position}",
                seasonId,
                positionCode);

            var result = await _dashboardService.GetPlayerSeasonStatisticsAsync(
                seasonId,
                competitionType,
                positionCode,
                cancellationToken);

            if (!result.Success)
            {
                var statusCode = DetermineStatusCode(result.Errors.FirstOrDefault()?.Code);
                return StatusCode(statusCode, MapToResponseDto(result));
            }

            return Ok(MapToResponseDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetPlayerSeasonStatistics");
            return StatusCode(500, new DashboardResponseDto<List<PlayerSeasonStatsDto>>
            {
                Success = false,
                Errors = new List<ErrorDto>
                {
                    new ErrorDto { Code = "INTERNAL_ERROR", Message = "An unexpected error occurred" }
                }
            });
        }
    }

    /// <summary>
    /// Get team form (win/loss/draw record for recent matches)
    /// </summary>
    /// <param name="seasonId">Season identifier (optional)</param>
    /// <param name="competitionType">Filter by competition type (optional)</param>
    /// <param name="matchCount">Number of recent matches for form calculation (default: 5)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Team form with win/loss/draw counts and form string (e.g., "WWLDW")</returns>
    /// <response code="200">Successfully retrieved team form</response>
    /// <response code="404">No matches found for form calculation</response>
    [HttpGet("team-form")]
    [ProducesResponseType(typeof(DashboardResponseDto<TeamFormDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DashboardResponseDto<TeamFormDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DashboardResponseDto<TeamFormDto>>> GetTeamForm(
        [FromQuery] int? seasonId = null,
        [FromQuery] string? competitionType = null,
        [FromQuery] int matchCount = 5,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "GetTeamForm called. SeasonId: {SeasonId}, MatchCount: {Count}",
                seasonId,
                matchCount);

            var result = await _dashboardService.GetTeamFormAsync(
                seasonId,
                competitionType,
                matchCount,
                cancellationToken);

            if (!result.Success)
            {
                var statusCode = DetermineStatusCode(result.Errors.FirstOrDefault()?.Code);
                return StatusCode(statusCode, MapToResponseDto(result));
            }

            return Ok(MapToResponseDto(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetTeamForm");
            return StatusCode(500, new DashboardResponseDto<TeamFormDto>
            {
                Success = false,
                Errors = new List<ErrorDto>
                {
                    new ErrorDto { Code = "INTERNAL_ERROR", Message = "An unexpected error occurred" }
                }
            });
        }
    }

    // =========================================================================
    // Private Helper Methods
    // =========================================================================

    /// <summary>
    /// Determines HTTP status code from error code
    /// </summary>
    private static int DetermineStatusCode(string? errorCode)
    {
        return errorCode switch
        {
            "NO_SEASON" => StatusCodes.Status404NotFound,
            "NO_DRUM_TEAM" => StatusCodes.Status404NotFound,
            "NO_MATCHES" => StatusCodes.Status404NotFound,
            "NO_STATS" => StatusCodes.Status404NotFound,
            "INVALID_METRIC" => StatusCodes.Status400BadRequest,
            "CACHE_ERROR" => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    /// <summary>
    /// Maps service result to API response DTO
    /// </summary>
    private static DashboardResponseDto<T> MapToResponseDto<T>(
        DashboardResult<T> result)
    {
        return new DashboardResponseDto<T>
        {
            Success = result.Success,
            Data = result.Data,
            DurationMs = result.Duration.TotalMilliseconds,
            Errors = result.Errors.Select(e => new ErrorDto
            {
                Code = e.Code,
                Message = e.Message
            }).ToList(),
            Warnings = result.Warnings.Select(w => new WarningDto
            {
                Code = w.Code,
                Message = w.Message
            }).ToList()
        };
    }
}
