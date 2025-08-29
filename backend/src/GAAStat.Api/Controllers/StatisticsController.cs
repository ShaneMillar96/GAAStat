using GAAStat.Services.Interfaces;
using GAAStat.Services.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace GAAStat.Api.Controllers;

/// <summary>
/// Controller for core GAA statistics operations and calculations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsCalculationService _statisticsService;
    private readonly IPlayerAnalyticsService _playerAnalytics;
    private readonly ITeamAnalyticsService _teamAnalytics;
    private readonly ISeasonAnalyticsService _seasonAnalytics;
    private readonly ILogger<StatisticsController> _logger;

    public StatisticsController(
        IStatisticsCalculationService statisticsService,
        IPlayerAnalyticsService playerAnalytics,
        ITeamAnalyticsService teamAnalytics,
        ISeasonAnalyticsService seasonAnalytics,
        ILogger<StatisticsController> logger)
    {
        _statisticsService = statisticsService;
        _playerAnalytics = playerAnalytics;
        _teamAnalytics = teamAnalytics;
        _seasonAnalytics = seasonAnalytics;
        _logger = logger;
    }

    /// <summary>
    /// Gets player statistics for a specific player across seasons or matches
    /// </summary>
    /// <param name="playerName">Player name as stored in database</param>
    /// <param name="seasonId">Optional season filter</param>
    /// <param name="matchIds">Optional comma-separated list of specific match IDs</param>
    /// <returns>Comprehensive player statistics</returns>
    [HttpGet("player/{playerName}")]
    public async Task<IActionResult> GetPlayerStatistics(
        string playerName, 
        [FromQuery] int? seasonId = null,
        [FromQuery] string? matchIds = null)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return BadRequest(ApiResponse.Error("Player name is required"));

        try
        {
            if (seasonId.HasValue)
            {
                var result = await _playerAnalytics.GetPlayerSeasonPerformanceAsync(playerName, seasonId.Value);
                return result.IsSuccess
                    ? Ok(ApiResponse.Success(result.Data))
                    : NotFound(ApiResponse.Error(result.ErrorMessage));
            }

            if (!string.IsNullOrEmpty(matchIds))
            {
                var matchIdList = ParseMatchIds(matchIds);
                if (matchIdList.Any())
                {
                    // This would call a service method for specific matches
                    var response = new PlayerStatisticsResponse
                    {
                        PlayerName = playerName,
                        Statistics = new PlayerStatisticsDto()
                    };
                    return Ok(ApiResponse.Success(response));
                }
            }

            // Return overall player statistics if no specific filters
            var overallResult = await _playerAnalytics.GetPlayerEfficiencyRatingAsync(playerName);
            return overallResult.IsSuccess
                ? Ok(ApiResponse.Success(overallResult.Data))
                : NotFound(ApiResponse.Error(overallResult.ErrorMessage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while retrieving statistics for player {PlayerName}", playerName);
            return StatusCode(500, ApiResponse.Error("An error occurred while retrieving player statistics"));
        }
    }

    /// <summary>
    /// Gets team statistics for a specific team and season
    /// </summary>
    /// <param name="teamId">Team identifier</param>
    /// <param name="seasonId">Season identifier</param>
    /// <param name="includePlayerBreakdown">Whether to include individual player statistics</param>
    /// <returns>Comprehensive team statistics</returns>
    [HttpGet("team/{teamId}")]
    public async Task<IActionResult> GetTeamStatistics(
        int teamId, 
        [FromQuery, Required] int seasonId,
        [FromQuery] bool includePlayerBreakdown = false)
    {
        if (teamId <= 0)
            return BadRequest(ApiResponse.Error("Invalid team ID"));

        if (seasonId <= 0)
            return BadRequest(ApiResponse.Error("Invalid season ID"));

        try
        {
            var result = await _teamAnalytics.GetTeamSeasonStatisticsAsync(teamId, seasonId);
            
            if (!result.IsSuccess)
                return NotFound(ApiResponse.Error(result.ErrorMessage));

            var response = new TeamStatisticsResponse
            {
                TeamId = teamId,
                SeasonId = seasonId,
                Statistics = result.Data,
                IncludesPlayerBreakdown = includePlayerBreakdown
            };

            if (includePlayerBreakdown)
            {
                // This would call additional service methods to get player breakdown
                response.PlayerBreakdown = new List<PlayerStatisticsDto>();
            }

            return Ok(ApiResponse.Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while retrieving statistics for team {TeamId}", teamId);
            return StatusCode(500, ApiResponse.Error("An error occurred while retrieving team statistics"));
        }
    }

    /// <summary>
    /// Gets season-wide statistical leaders across all categories
    /// </summary>
    /// <param name="seasonId">Season identifier</param>
    /// <param name="category">Optional statistical category filter (scoring, defensive, possession, etc.)</param>
    /// <param name="limit">Maximum number of leaders per category (default: 10)</param>
    /// <param name="minMatches">Minimum matches played to qualify (default: 3)</param>
    /// <returns>Statistical leaders in various categories</returns>
    [HttpGet("leaders/season/{seasonId}")]
    public async Task<IActionResult> GetSeasonLeaders(
        int seasonId,
        [FromQuery] string? category = null,
        [FromQuery] int limit = 10,
        [FromQuery] int minMatches = 3)
    {
        if (seasonId <= 0)
            return BadRequest(ApiResponse.Error("Invalid season ID"));

        if (limit <= 0 || limit > 50)
            return BadRequest(ApiResponse.Error("Limit must be between 1 and 50"));

        if (minMatches < 1 || minMatches > 20)
            return BadRequest(ApiResponse.Error("Minimum matches must be between 1 and 20"));

        try
        {
            var result = await _seasonAnalytics.GetSeasonStatisticalLeadersAsync(seasonId, limit);
            return result.IsSuccess
                ? Ok(ApiResponse.Success(result.Data))
                : NotFound(ApiResponse.Error(result.ErrorMessage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while retrieving season leaders for season {SeasonId}", seasonId);
            return StatusCode(500, ApiResponse.Error("An error occurred while retrieving season leaders"));
        }
    }

    /// <summary>
    /// Gets PSR (Performance Success Rate) rankings for players
    /// </summary>
    /// <param name="seasonId">Season identifier</param>
    /// <param name="position">Optional position filter</param>
    /// <param name="teamId">Optional team filter</param>
    /// <param name="minMatches">Minimum matches played to qualify (default: 3)</param>
    /// <param name="limit">Maximum number of players to return (default: 20)</param>
    /// <returns>PSR rankings with detailed metrics</returns>
    [HttpGet("psr-rankings/season/{seasonId}")]
    public async Task<IActionResult> GetPsrRankings(
        int seasonId,
        [FromQuery] string? position = null,
        [FromQuery] int? teamId = null,
        [FromQuery] int minMatches = 3,
        [FromQuery] int limit = 20)
    {
        if (seasonId <= 0)
            return BadRequest(ApiResponse.Error("Invalid season ID"));

        if (limit <= 0 || limit > 50)
            return BadRequest(ApiResponse.Error("Limit must be between 1 and 50"));

        if (minMatches < 1 || minMatches > 20)
            return BadRequest(ApiResponse.Error("Minimum matches must be between 1 and 20"));

        try
        {
            var result = await _seasonAnalytics.GetPsrLeadersAsync(seasonId, position, teamId, minMatches, limit);
            return result.IsSuccess
                ? Ok(ApiResponse.Success(result.Data))
                : NotFound(ApiResponse.Error(result.ErrorMessage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while retrieving PSR rankings for season {SeasonId}", seasonId);
            return StatusCode(500, ApiResponse.Error("An error occurred while retrieving PSR rankings"));
        }
    }

    /// <summary>
    /// Gets scoring statistics and leaders
    /// </summary>
    /// <param name="seasonId">Season identifier</param>
    /// <param name="position">Optional position filter</param>
    /// <param name="teamId">Optional team filter</param>
    /// <param name="limit">Maximum number of players to return (default: 20)</param>
    /// <returns>Top scoring players with detailed statistics</returns>
    [HttpGet("scoring/season/{seasonId}")]
    public async Task<IActionResult> GetScoringStatistics(
        int seasonId,
        [FromQuery] string? position = null,
        [FromQuery] int? teamId = null,
        [FromQuery] int limit = 20)
    {
        if (seasonId <= 0)
            return BadRequest(ApiResponse.Error("Invalid season ID"));

        if (limit <= 0 || limit > 50)
            return BadRequest(ApiResponse.Error("Limit must be between 1 and 50"));

        try
        {
            var result = await _seasonAnalytics.GetTopScorersAsync(seasonId, position, teamId, limit);
            return result.IsSuccess
                ? Ok(ApiResponse.Success(result.Data))
                : NotFound(ApiResponse.Error(result.ErrorMessage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while retrieving scoring statistics for season {SeasonId}", seasonId);
            return StatusCode(500, ApiResponse.Error("An error occurred while retrieving scoring statistics"));
        }
    }

    /// <summary>
    /// Gets performance comparison between two players
    /// </summary>
    /// <param name="request">Player comparison request with player names and filters</param>
    /// <returns>Side-by-side performance comparison</returns>
    [HttpPost("compare/players")]
    public async Task<IActionResult> ComparePlayerPerformance([FromBody] PlayerComparisonRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Player1Name) || string.IsNullOrWhiteSpace(request.Player2Name))
            return BadRequest(ApiResponse.Error("Both player names are required"));

        if (request.Player1Name.Equals(request.Player2Name, StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse.Error("Cannot compare a player with themselves"));

        try
        {
            // This would call a service method to compare players
            var response = new PlayerComparisonResponse
            {
                Player1Name = request.Player1Name,
                Player2Name = request.Player2Name,
                SeasonId = request.SeasonId,
                ComparisonMetrics = new Dictionary<string, PlayerComparisonMetric>()
            };

            return Ok(ApiResponse.Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while comparing players {Player1} and {Player2}", 
                request.Player1Name, request.Player2Name);
            return StatusCode(500, ApiResponse.Error("An error occurred while comparing players"));
        }
    }

    /// <summary>
    /// Gets performance comparison between two teams
    /// </summary>
    /// <param name="team1Id">First team identifier</param>
    /// <param name="team2Id">Second team identifier</param>
    /// <param name="seasonId">Optional season filter</param>
    /// <returns>Head-to-head team performance comparison</returns>
    [HttpGet("compare/teams")]
    public async Task<IActionResult> CompareTeamPerformance(
        [FromQuery, Required] int team1Id,
        [FromQuery, Required] int team2Id,
        [FromQuery] int? seasonId = null)
    {
        if (team1Id <= 0 || team2Id <= 0)
            return BadRequest(ApiResponse.Error("Valid team IDs are required"));

        if (team1Id == team2Id)
            return BadRequest(ApiResponse.Error("Cannot compare a team with itself"));

        try
        {
            var result = await _teamAnalytics.GetTeamComparisonAsync(team1Id, team2Id, seasonId);
            return result.IsSuccess
                ? Ok(ApiResponse.Success(result.Data))
                : NotFound(ApiResponse.Error(result.ErrorMessage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while comparing teams {Team1Id} and {Team2Id}", team1Id, team2Id);
            return StatusCode(500, ApiResponse.Error("An error occurred while comparing teams"));
        }
    }

    /// <summary>
    /// Calculates and returns comprehensive match statistics
    /// </summary>
    /// <param name="matchId">Match identifier</param>
    /// <param name="recalculate">Whether to recalculate statistics (default: false)</param>
    /// <returns>Comprehensive match statistics</returns>
    [HttpPost("calculate/match/{matchId}")]
    public async Task<IActionResult> CalculateMatchStatistics(
        int matchId, 
        [FromQuery] bool recalculate = false)
    {
        if (matchId <= 0)
            return BadRequest(ApiResponse.Error("Invalid match ID"));

        try
        {
            // This would call the statistics calculation service
            var response = new MatchStatisticsCalculationResponse
            {
                MatchId = matchId,
                CalculatedAt = DateTime.UtcNow,
                WasRecalculated = recalculate
            };

            return Ok(ApiResponse.Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while calculating statistics for match {MatchId}", matchId);
            return StatusCode(500, ApiResponse.Error("An error occurred while calculating match statistics"));
        }
    }

    #region Private Helper Methods

    private static List<int> ParseMatchIds(string matchIds)
    {
        var ids = new List<int>();
        foreach (var idStr in matchIds.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(idStr.Trim(), out var id) && id > 0)
            {
                ids.Add(id);
            }
        }
        return ids;
    }

    #endregion
}

#region Request/Response DTOs

/// <summary>
/// Response model for player statistics
/// </summary>
public class PlayerStatisticsResponse
{
    public string PlayerName { get; set; } = string.Empty;
    public int? SeasonId { get; set; }
    public PlayerStatisticsDto Statistics { get; set; } = new();
}

/// <summary>
/// Player statistics data transfer object
/// </summary>
public class PlayerStatisticsDto
{
    public string PlayerName { get; set; } = string.Empty;
    public int? JerseyNumber { get; set; }
    public string Position { get; set; } = string.Empty;
    public int MatchesPlayed { get; set; }
    public int TotalMinutes { get; set; }
    public decimal PerformanceSuccessRate { get; set; }
    public int TotalPossessions { get; set; }
    public int Goals { get; set; }
    public int Points { get; set; }
    public int TotalScore { get; set; }
    public int TotalEvents { get; set; }
    public Dictionary<string, decimal> AdditionalMetrics { get; set; } = new();
}

/// <summary>
/// Response model for team statistics
/// </summary>
public class TeamStatisticsResponse
{
    public int TeamId { get; set; }
    public int SeasonId { get; set; }
    public object? Statistics { get; set; }
    public bool IncludesPlayerBreakdown { get; set; }
    public IEnumerable<PlayerStatisticsDto> PlayerBreakdown { get; set; } = new List<PlayerStatisticsDto>();
}

/// <summary>
/// Request model for player comparison
/// </summary>
public class PlayerComparisonRequest
{
    [Required]
    public string Player1Name { get; set; } = string.Empty;

    [Required]
    public string Player2Name { get; set; } = string.Empty;

    public int? SeasonId { get; set; }

    public string? Position { get; set; }
}

/// <summary>
/// Response model for player comparison
/// </summary>
public class PlayerComparisonResponse
{
    public string Player1Name { get; set; } = string.Empty;
    public string Player2Name { get; set; } = string.Empty;
    public int? SeasonId { get; set; }
    public DateTime ComparedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, PlayerComparisonMetric> ComparisonMetrics { get; set; } = new();
}

/// <summary>
/// Comparison metric for two players
/// </summary>
public class PlayerComparisonMetric
{
    public string MetricName { get; set; } = string.Empty;
    public decimal Player1Value { get; set; }
    public decimal Player2Value { get; set; }
    public decimal Difference { get; set; }
    public string BetterPlayer { get; set; } = string.Empty;
}

/// <summary>
/// Response model for match statistics calculation
/// </summary>
public class MatchStatisticsCalculationResponse
{
    public int MatchId { get; set; }
    public DateTime CalculatedAt { get; set; }
    public bool WasRecalculated { get; set; }
    public int PlayerStatisticsCalculated { get; set; }
    public int TeamStatisticsCalculated { get; set; }
    public TimeSpan CalculationDuration { get; set; }
}

#endregion