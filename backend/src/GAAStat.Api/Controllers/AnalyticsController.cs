using GAAStat.Services.Interfaces;
using GAAStat.Services.Models;
using Microsoft.AspNetCore.Mvc;

namespace GAAStat.Api.Controllers;

/// <summary>
/// Controller for comprehensive GAA statistics analytics and reporting
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IMatchAnalyticsService _matchAnalytics;
    private readonly IPlayerAnalyticsService _playerAnalytics;
    private readonly ITeamAnalyticsService _teamAnalytics;
    private readonly ISeasonAnalyticsService _seasonAnalytics;
    private readonly IPositionalAnalysisService _positionalAnalysis;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        IMatchAnalyticsService matchAnalytics,
        IPlayerAnalyticsService playerAnalytics,
        ITeamAnalyticsService teamAnalytics,
        ISeasonAnalyticsService seasonAnalytics,
        IPositionalAnalysisService positionalAnalysis,
        ILogger<AnalyticsController> logger)
    {
        _matchAnalytics = matchAnalytics;
        _playerAnalytics = playerAnalytics;
        _teamAnalytics = teamAnalytics;
        _seasonAnalytics = seasonAnalytics;
        _positionalAnalysis = positionalAnalysis;
        _logger = logger;
    }

    #region Match Analytics Endpoints

    /// <summary>
    /// Gets comprehensive match summary with team and player statistics
    /// </summary>
    /// <param name="matchId">Match identifier</param>
    /// <returns>Complete match analysis</returns>
    [HttpGet("match/{matchId}/summary")]
    public async Task<IActionResult> GetMatchSummary(int matchId)
    {
        var result = await _matchAnalytics.GetMatchSummaryAsync(matchId);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets detailed team performance comparison for a match
    /// </summary>
    /// <param name="matchId">Match identifier</param>
    /// <returns>Side-by-side team performance comparison</returns>
    [HttpGet("match/{matchId}/team-comparison")]
    public async Task<IActionResult> GetMatchTeamComparison(int matchId)
    {
        var result = await _matchAnalytics.GetMatchTeamComparisonAsync(matchId);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets kickout analysis for a specific match
    /// </summary>
    /// <param name="matchId">Match identifier</param>
    /// <returns>Comprehensive kickout statistics and analysis</returns>
    [HttpGet("match/{matchId}/kickout-analysis")]
    public async Task<IActionResult> GetKickoutAnalysis(int matchId)
    {
        var result = await _matchAnalytics.GetKickoutAnalysisAsync(matchId);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets shot analysis including conversion rates and efficiency
    /// </summary>
    /// <param name="matchId">Match identifier</param>
    /// <returns>Detailed shot analysis and scoring patterns</returns>
    [HttpGet("match/{matchId}/shot-analysis")]
    public async Task<IActionResult> GetShotAnalysis(int matchId)
    {
        var result = await _matchAnalytics.GetShotAnalysisAsync(matchId);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets match momentum analysis showing performance trends
    /// </summary>
    /// <param name="matchId">Match identifier</param>
    /// <returns>Match momentum and performance trend data</returns>
    [HttpGet("match/{matchId}/momentum")]
    public async Task<IActionResult> GetMatchMomentumAnalysis(int matchId)
    {
        var result = await _matchAnalytics.GetMatchMomentumAnalysisAsync(matchId);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets top performers in a specific match across all categories
    /// </summary>
    /// <param name="matchId">Match identifier</param>
    /// <param name="limit">Maximum number of players to return per category (default: 5)</param>
    /// <returns>Top performers in various statistical categories</returns>
    [HttpGet("match/{matchId}/top-performers")]
    public async Task<IActionResult> GetMatchTopPerformers(int matchId, [FromQuery] int limit = 5)
    {
        if (limit < 1 || limit > 20)
        {
            return BadRequest(ApiResponse.Error("Limit must be between 1 and 20"));
        }

        var result = await _matchAnalytics.GetMatchTopPerformersAsync(matchId, limit);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    #endregion

    #region Player Analytics Endpoints

    /// <summary>
    /// Gets comprehensive performance analysis for a player across a season
    /// </summary>
    /// <param name="playerName">Player name as stored in database</param>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Complete player performance metrics and trends</returns>
    [HttpGet("player/{playerName}/season/{seasonId}")]
    public async Task<IActionResult> GetPlayerSeasonPerformance(string playerName, int seasonId)
    {
        var result = await _playerAnalytics.GetPlayerSeasonPerformanceAsync(playerName, seasonId);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets player efficiency rating and detailed metrics
    /// </summary>
    /// <param name="playerName">Player name</param>
    /// <param name="seasonId">Optional season filter</param>
    /// <returns>Comprehensive efficiency metrics and ratings</returns>
    [HttpGet("player/{playerName}/efficiency")]
    public async Task<IActionResult> GetPlayerEfficiencyRating(string playerName, [FromQuery] int? seasonId = null)
    {
        var result = await _playerAnalytics.GetPlayerEfficiencyRatingAsync(playerName, seasonId);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets player performance comparison against team averages
    /// </summary>
    /// <param name="playerName">Player name</param>
    /// <param name="teamId">Team identifier</param>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Player vs team performance comparison</returns>
    [HttpGet("player/{playerName}/team-comparison")]
    public async Task<IActionResult> GetPlayerTeamComparison(string playerName, [FromQuery] int teamId, [FromQuery] int seasonId)
    {
        var result = await _playerAnalytics.GetPlayerTeamComparisonAsync(playerName, teamId, seasonId);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets player performance trends over time
    /// </summary>
    /// <param name="playerName">Player name</param>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Performance trends and progression analysis</returns>
    [HttpGet("player/{playerName}/trends")]
    public async Task<IActionResult> GetPlayerTrends(string playerName, [FromQuery] int seasonId)
    {
        var result = await _playerAnalytics.GetPlayerTrendsAsync(playerName, seasonId);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets player performance against different opposition teams
    /// </summary>
    /// <param name="playerName">Player name</param>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Performance breakdown by opposition</returns>
    [HttpGet("player/{playerName}/opposition-analysis")]
    public async Task<IActionResult> GetPlayerOppositionAnalysis(string playerName, [FromQuery] int seasonId)
    {
        var result = await _playerAnalytics.GetPlayerOppositionAnalysisAsync(playerName, seasonId);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets player performance comparison between home and away matches
    /// </summary>
    /// <param name="playerName">Player name</param>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Home vs away performance analysis</returns>
    [HttpGet("player/{playerName}/venue-analysis")]
    public async Task<IActionResult> GetPlayerVenueAnalysis(string playerName, [FromQuery] int seasonId)
    {
        var result = await _playerAnalytics.GetPlayerVenueAnalysisAsync(playerName, seasonId);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets cumulative season statistics for a player
    /// </summary>
    /// <param name="playerName">Player name</param>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Cumulative statistics across all matches in season</returns>
    [HttpGet("player/{playerName}/cumulative-stats")]
    public async Task<IActionResult> GetPlayerCumulativeStats(string playerName, [FromQuery] int seasonId)
    {
        var result = await _playerAnalytics.GetPlayerCumulativeStatsAsync(playerName, seasonId);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    #endregion

    #region Team Analytics Endpoints

    /// <summary>
    /// Gets comprehensive team statistics for a season
    /// </summary>
    /// <param name="teamId">Team identifier</param>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Complete team performance metrics and averages</returns>
    [HttpGet("team/{teamId}/season/{seasonId}")]
    public async Task<IActionResult> GetTeamSeasonStatistics(int teamId, int seasonId)
    {
        var result = await _teamAnalytics.GetTeamSeasonStatisticsAsync(teamId, seasonId);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets team performance comparison against a specific opponent
    /// </summary>
    /// <param name="teamId">Team identifier</param>
    /// <param name="opponentId">Opponent team identifier</param>
    /// <param name="seasonId">Optional season filter</param>
    /// <returns>Head-to-head team performance analysis</returns>
    [HttpGet("team/{teamId}/comparison/{opponentId}")]
    public async Task<IActionResult> GetTeamComparison(int teamId, int opponentId, [FromQuery] int? seasonId = null)
    {
        var result = await _teamAnalytics.GetTeamComparisonAsync(teamId, opponentId, seasonId);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets team offensive efficiency metrics
    /// </summary>
    /// <param name="teamId">Team identifier</param>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Detailed offensive performance analysis</returns>
    [HttpGet("team/{teamId}/offensive-stats")]
    public async Task<IActionResult> GetTeamOffensiveStats(int teamId, [FromQuery] int seasonId)
    {
        var result = await _teamAnalytics.GetTeamOffensiveStatsAsync(teamId, seasonId);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets team defensive efficiency metrics
    /// </summary>
    /// <param name="teamId">Team identifier</param>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Detailed defensive performance analysis</returns>
    [HttpGet("team/{teamId}/defensive-stats")]
    public async Task<IActionResult> GetTeamDefensiveStats(int teamId, [FromQuery] int seasonId)
    {
        var result = await _teamAnalytics.GetTeamDefensiveStatsAsync(teamId, seasonId);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets team possession statistics and control metrics
    /// </summary>
    /// <param name="teamId">Team identifier</param>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Possession and ball control analysis</returns>
    [HttpGet("team/{teamId}/possession-stats")]
    public async Task<IActionResult> GetTeamPossessionStats(int teamId, [FromQuery] int seasonId)
    {
        var result = await _teamAnalytics.GetTeamPossessionStatsAsync(teamId, seasonId);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets team performance by venue (home vs away)
    /// </summary>
    /// <param name="teamId">Team identifier</param>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Home vs away performance breakdown</returns>
    [HttpGet("team/{teamId}/venue-analysis")]
    public async Task<IActionResult> GetTeamVenueAnalysis(int teamId, [FromQuery] int seasonId)
    {
        var result = await _teamAnalytics.GetTeamVenueAnalysisAsync(teamId, seasonId);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets team roster efficiency and player contribution analysis
    /// </summary>
    /// <param name="teamId">Team identifier</param>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Player contribution and roster depth analysis</returns>
    [HttpGet("team/{teamId}/roster-analysis")]
    public async Task<IActionResult> GetTeamRosterAnalysis(int teamId, [FromQuery] int seasonId)
    {
        var result = await _teamAnalytics.GetTeamRosterAnalysisAsync(teamId, seasonId);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets team performance trends throughout the season
    /// </summary>
    /// <param name="teamId">Team identifier</param>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Team performance progression and trends</returns>
    [HttpGet("team/{teamId}/trends")]
    public async Task<IActionResult> GetTeamTrends(int teamId, [FromQuery] int seasonId)
    {
        var result = await _teamAnalytics.GetTeamTrendsAsync(teamId, seasonId);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    #endregion

    #region Season Analytics Endpoints

    /// <summary>
    /// Gets comprehensive season summary with key statistics
    /// </summary>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Complete season overview with top performers and trends</returns>
    [HttpGet("season/{seasonId}/summary")]
    public async Task<IActionResult> GetSeasonSummary(int seasonId)
    {
        var result = await _seasonAnalytics.GetSeasonSummaryAsync(seasonId);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets cumulative statistics for all players and teams in a season
    /// </summary>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Season-wide cumulative statistics</returns>
    [HttpGet("season/{seasonId}/cumulative")]
    public async Task<IActionResult> GetSeasonCumulativeStatistics(int seasonId)
    {
        var result = await _seasonAnalytics.GetSeasonCumulativeStatisticsAsync(seasonId);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets top scorers across the season with various filters
    /// </summary>
    /// <param name="seasonId">Season identifier</param>
    /// <param name="position">Optional position filter</param>
    /// <param name="teamId">Optional team filter</param>
    /// <param name="limit">Maximum number of players to return (default: 20)</param>
    /// <returns>Top scoring players with detailed statistics</returns>
    [HttpGet("season/{seasonId}/top-scorers")]
    public async Task<IActionResult> GetTopScorers(
        int seasonId, 
        [FromQuery] string? position = null, 
        [FromQuery] int? teamId = null, 
        [FromQuery] int limit = 20)
    {
        if (limit < 1 || limit > 50)
        {
            return BadRequest(ApiResponse.Error("Limit must be between 1 and 50"));
        }

        var result = await _seasonAnalytics.GetTopScorersAsync(seasonId, position, teamId, limit);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets PSR (Performance Success Rate) leaders across the season
    /// </summary>
    /// <param name="seasonId">Season identifier</param>
    /// <param name="position">Optional position filter</param>
    /// <param name="teamId">Optional team filter</param>
    /// <param name="minMatches">Minimum matches played to qualify (default: 3)</param>
    /// <param name="limit">Maximum number of players to return (default: 20)</param>
    /// <returns>Top PSR performers with trend analysis</returns>
    [HttpGet("season/{seasonId}/psr-leaders")]
    public async Task<IActionResult> GetPsrLeaders(
        int seasonId, 
        [FromQuery] string? position = null, 
        [FromQuery] int? teamId = null, 
        [FromQuery] int minMatches = 3, 
        [FromQuery] int limit = 20)
    {
        if (limit < 1 || limit > 50)
        {
            return BadRequest(ApiResponse.Error("Limit must be between 1 and 50"));
        }

        if (minMatches < 1 || minMatches > 20)
        {
            return BadRequest(ApiResponse.Error("Minimum matches must be between 1 and 20"));
        }

        var result = await _seasonAnalytics.GetPsrLeadersAsync(seasonId, position, teamId, minMatches, limit);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets season-wide performance trends and patterns
    /// </summary>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Statistical trends and patterns analysis</returns>
    [HttpGet("season/{seasonId}/trends")]
    public async Task<IActionResult> GetSeasonTrends(int seasonId)
    {
        var result = await _seasonAnalytics.GetSeasonTrendsAsync(seasonId);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets comparative analysis across multiple seasons
    /// </summary>
    /// <param name="seasonIds">Comma-separated list of season identifiers</param>
    /// <returns>Cross-season comparison and trend analysis</returns>
    [HttpGet("seasons/comparison")]
    public async Task<IActionResult> GetMultiSeasonComparison([FromQuery] string seasonIds)
    {
        if (string.IsNullOrEmpty(seasonIds))
        {
            return BadRequest(ApiResponse.Error("Season IDs parameter is required"));
        }

        var seasonIdsList = new List<int>();
        foreach (var idStr in seasonIds.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(idStr.Trim(), out var id))
            {
                seasonIdsList.Add(id);
            }
        }

        if (seasonIdsList.Count < 2)
        {
            return BadRequest(ApiResponse.Error("At least 2 valid season IDs are required for comparison"));
        }

        if (seasonIdsList.Count > 10)
        {
            return BadRequest(ApiResponse.Error("Maximum of 10 seasons can be compared at once"));
        }

        var result = await _seasonAnalytics.GetMultiSeasonComparisonAsync(seasonIdsList);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : BadRequest(ApiResponse.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets team rankings and league table for a season
    /// </summary>
    /// <param name="seasonId">Season identifier</param>
    /// <param name="competitionId">Optional competition filter</param>
    /// <returns>Team rankings based on performance metrics</returns>
    [HttpGet("season/{seasonId}/league-table")]
    public async Task<IActionResult> GetSeasonLeagueTable(int seasonId, [FromQuery] int? competitionId = null)
    {
        var result = await _seasonAnalytics.GetSeasonLeagueTableAsync(seasonId, competitionId);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets statistical leaders across all categories for a season
    /// </summary>
    /// <param name="seasonId">Season identifier</param>
    /// <param name="limit">Number of leaders per category (default: 10)</param>
    /// <returns>Leaders in all major statistical categories</returns>
    [HttpGet("season/{seasonId}/statistical-leaders")]
    public async Task<IActionResult> GetSeasonStatisticalLeaders(int seasonId, [FromQuery] int limit = 10)
    {
        if (limit < 1 || limit > 25)
        {
            return BadRequest(ApiResponse.Error("Limit must be between 1 and 25"));
        }

        var result = await _seasonAnalytics.GetSeasonStatisticalLeadersAsync(seasonId, limit);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    #endregion

    #region Positional Analysis Endpoints

    /// <summary>
    /// Gets performance analysis for all players in a specific position
    /// </summary>
    /// <param name="position">Position name (e.g., "Goalkeeper", "Full Back", "Midfielder")</param>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Position-specific performance metrics and rankings</returns>
    [HttpGet("position/{position}/season/{seasonId}")]
    public async Task<IActionResult> GetPositionalPerformance(string position, int seasonId)
    {
        var result = await _positionalAnalysis.GetPositionalPerformanceAsync(position, seasonId);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets comparative analysis between different positions
    /// </summary>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Cross-positional performance comparison</returns>
    [HttpGet("positions/comparison/{seasonId}")]
    public async Task<IActionResult> GetPositionalComparison(int seasonId)
    {
        var result = await _positionalAnalysis.GetPositionalComparisonAsync(seasonId);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets goalkeeper-specific advanced metrics and analysis
    /// </summary>
    /// <param name="seasonId">Season identifier</param>
    /// <param name="teamId">Optional team filter</param>
    /// <returns>Specialized goalkeeper performance metrics</returns>
    [HttpGet("goalkeepers/analysis/{seasonId}")]
    public async Task<IActionResult> GetGoalkeeperAnalysis(int seasonId, [FromQuery] int? teamId = null)
    {
        var result = await _positionalAnalysis.GetGoalkeeperAnalysisAsync(seasonId, teamId);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets defender-specific metrics including tackles and defensive actions
    /// </summary>
    /// <param name="seasonId">Season identifier</param>
    /// <param name="teamId">Optional team filter</param>
    /// <returns>Specialized defender performance metrics</returns>
    [HttpGet("defenders/analysis/{seasonId}")]
    public async Task<IActionResult> GetDefenderAnalysis(int seasonId, [FromQuery] int? teamId = null)
    {
        var result = await _positionalAnalysis.GetDefenderAnalysisAsync(seasonId, teamId);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets midfielder-specific metrics including distribution and possession
    /// </summary>
    /// <param name="seasonId">Season identifier</param>
    /// <param name="teamId">Optional team filter</param>
    /// <returns>Specialized midfielder performance metrics</returns>
    [HttpGet("midfielders/analysis/{seasonId}")]
    public async Task<IActionResult> GetMidfielderAnalysis(int seasonId, [FromQuery] int? teamId = null)
    {
        var result = await _positionalAnalysis.GetMidfielderAnalysisAsync(seasonId, teamId);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets forward-specific metrics including scoring and attacking plays
    /// </summary>
    /// <param name="seasonId">Season identifier</param>
    /// <param name="teamId">Optional team filter</param>
    /// <returns>Specialized forward performance metrics</returns>
    [HttpGet("forwards/analysis/{seasonId}")]
    public async Task<IActionResult> GetForwardAnalysis(int seasonId, [FromQuery] int? teamId = null)
    {
        var result = await _positionalAnalysis.GetForwardAnalysisAsync(seasonId, teamId);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets position-specific PSR benchmarks and averages
    /// </summary>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>PSR benchmarks by position</returns>
    [HttpGet("positions/psr-benchmarks/{seasonId}")]
    public async Task<IActionResult> GetPositionalPsrBenchmarks(int seasonId)
    {
        var result = await _positionalAnalysis.GetPositionalPsrBenchmarksAsync(seasonId);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets optimal formation analysis based on player performance
    /// </summary>
    /// <param name="teamId">Team identifier</param>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Formation recommendations based on player strengths</returns>
    [HttpGet("team/{teamId}/formation-analysis/{seasonId}")]
    public async Task<IActionResult> GetOptimalFormationAnalysis(int teamId, int seasonId)
    {
        var result = await _positionalAnalysis.GetOptimalFormationAnalysisAsync(teamId, seasonId);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error(result.ErrorMessage));
    }

    #endregion

    #region System Health Endpoints

    /// <summary>
    /// Gets system health check for analytics services
    /// </summary>
    /// <returns>Health status of all analytics components</returns>
    [HttpGet("health")]
    public IActionResult GetSystemHealth()
    {
        var health = new
        {
            Status = "Healthy",
            Services = new
            {
                MatchAnalytics = "Online",
                PlayerAnalytics = "Online",
                TeamAnalytics = "Online",
                SeasonAnalytics = "Online",
                PositionalAnalysis = "Online"
            },
            Timestamp = DateTime.UtcNow,
            Version = "3.0.0"
        };

        return Ok(ApiResponse.Success(health));
    }

    #endregion
}

/// <summary>
/// Standard API response wrapper for consistent response format
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public IEnumerable<string> Errors { get; set; } = [];
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Helper class for creating API responses
/// </summary>
public static class ApiResponse
{
    public static ApiResponse<T> Success<T>(T data, string? message = null) => 
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<object> Success(string message) => 
        new() { Success = true, Message = message };

    public static ApiResponse<object> Error(string message, IEnumerable<string>? errors = null) => 
        new() { Success = false, Message = message, Errors = errors ?? [] };
}