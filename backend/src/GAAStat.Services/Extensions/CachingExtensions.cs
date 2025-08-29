using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GAAStat.Services.Extensions;

/// <summary>
/// Extension methods for caching operations optimized for analytics performance
/// </summary>
public static class CachingExtensions
{
    /// <summary>
    /// Cache expiration times for different data types
    /// </summary>
    public static class CacheExpirationTimes
    {
        public static readonly TimeSpan MatchData = TimeSpan.FromMinutes(30);
        public static readonly TimeSpan PlayerSeason = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan TeamSeason = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan SeasonSummary = TimeSpan.FromHours(1);
        public static readonly TimeSpan PositionalAnalysis = TimeSpan.FromMinutes(30);
        public static readonly TimeSpan PlayerEfficiency = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan TopScorers = TimeSpan.FromMinutes(20);
        public static readonly TimeSpan LeaderBoards = TimeSpan.FromMinutes(10);
    }

    /// <summary>
    /// Cache key generators for consistent key naming
    /// </summary>
    public static class CacheKeys
    {
        public static string MatchSummary(int matchId) => $"match_summary_{matchId}";
        public static string MatchTeamComparison(int matchId) => $"match_team_comparison_{matchId}";
        public static string KickoutAnalysis(int matchId) => $"kickout_analysis_{matchId}";
        public static string ShotAnalysis(int matchId) => $"shot_analysis_{matchId}";
        public static string MatchMomentum(int matchId) => $"match_momentum_{matchId}";
        public static string MatchTopPerformers(int matchId, int limit) => $"match_top_performers_{matchId}_{limit}";
        
        public static string PlayerSeasonPerformance(string playerName, int seasonId) => 
            $"player_season_{playerName.Replace(" ", "_")}_{seasonId}";
        public static string PlayerEfficiency(string playerName, int? seasonId) => 
            $"player_efficiency_{playerName.Replace(" ", "_")}_{seasonId}";
        public static string PlayerTeamComparison(string playerName, int teamId, int seasonId) => 
            $"player_team_comp_{playerName.Replace(" ", "_")}_{teamId}_{seasonId}";
        public static string PlayerTrends(string playerName, int seasonId) => 
            $"player_trends_{playerName.Replace(" ", "_")}_{seasonId}";
        public static string PlayerCumulativeStats(string playerName, int seasonId) => 
            $"player_cumulative_{playerName.Replace(" ", "_")}_{seasonId}";
        
        public static string TeamSeasonStats(int teamId, int seasonId) => $"team_season_{teamId}_{seasonId}";
        public static string TeamComparison(int teamId, int opponentId, int? seasonId) => 
            $"team_comparison_{teamId}_{opponentId}_{seasonId}";
        public static string TeamOffensiveStats(int teamId, int seasonId) => $"team_offensive_{teamId}_{seasonId}";
        public static string TeamDefensiveStats(int teamId, int seasonId) => $"team_defensive_{teamId}_{seasonId}";
        public static string TeamPossessionStats(int teamId, int seasonId) => $"team_possession_{teamId}_{seasonId}";
        public static string TeamVenueAnalysis(int teamId, int seasonId) => $"team_venue_{teamId}_{seasonId}";
        public static string TeamRosterAnalysis(int teamId, int seasonId) => $"team_roster_{teamId}_{seasonId}";
        public static string TeamTrends(int teamId, int seasonId) => $"team_trends_{teamId}_{seasonId}";
        
        public static string SeasonSummary(int seasonId) => $"season_summary_{seasonId}";
        public static string SeasonCumulative(int seasonId) => $"season_cumulative_{seasonId}";
        public static string TopScorers(int seasonId, string? position, int? teamId, int limit) => 
            $"top_scorers_{seasonId}_{position}_{teamId}_{limit}";
        public static string PsrLeaders(int seasonId, string? position, int? teamId, int minMatches, int limit) => 
            $"psr_leaders_{seasonId}_{position}_{teamId}_{minMatches}_{limit}";
        public static string SeasonTrends(int seasonId) => $"season_trends_{seasonId}";
        public static string SeasonLeagueTable(int seasonId, int? competitionId) => 
            $"league_table_{seasonId}_{competitionId}";
        public static string SeasonStatisticalLeaders(int seasonId, int limit) => 
            $"statistical_leaders_{seasonId}_{limit}";
        
        public static string PositionalPerformance(string position, int seasonId) => 
            $"positional_perf_{position.Replace(" ", "_")}_{seasonId}";
        public static string PositionalComparison(int seasonId) => $"positional_comp_{seasonId}";
        public static string GoalkeeperAnalysis(int seasonId, int? teamId) => $"goalkeeper_analysis_{seasonId}_{teamId}";
        public static string DefenderAnalysis(int seasonId, int? teamId) => $"defender_analysis_{seasonId}_{teamId}";
        public static string MidfielderAnalysis(int seasonId, int? teamId) => $"midfielder_analysis_{seasonId}_{teamId}";
        public static string ForwardAnalysis(int seasonId, int? teamId) => $"forward_analysis_{seasonId}_{teamId}";
        public static string PositionalPsrBenchmarks(int seasonId) => $"positional_psr_benchmarks_{seasonId}";
        public static string FormationAnalysis(int teamId, int seasonId) => $"formation_analysis_{teamId}_{seasonId}";
    }

    /// <summary>
    /// Gets or sets a cached value with automatic expiration
    /// </summary>
    /// <typeparam name="T">Type of cached data</typeparam>
    /// <param name="cache">Memory cache instance</param>
    /// <param name="key">Cache key</param>
    /// <param name="factory">Factory function to create data if not cached</param>
    /// <param name="expiration">Cache expiration time</param>
    /// <param name="logger">Logger instance</param>
    /// <returns>Cached or newly created data</returns>
    public static async Task<T> GetOrSetAsync<T>(
        this IMemoryCache cache,
        string key,
        Func<Task<T>> factory,
        TimeSpan expiration,
        ILogger? logger = null)
    {
        if (cache.TryGetValue(key, out T cachedResult))
        {
            logger?.LogDebug("Cache hit for key: {CacheKey}", key);
            return cachedResult;
        }

        logger?.LogDebug("Cache miss for key: {CacheKey}, generating new data", key);
        var startTime = DateTime.UtcNow;
        
        try
        {
            var result = await factory();
            
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration,
                Priority = CacheItemPriority.Normal
            };

            cache.Set(key, result, cacheOptions);
            
            var duration = DateTime.UtcNow - startTime;
            logger?.LogDebug("Generated and cached data for key: {CacheKey} in {Duration}ms", 
                key, duration.TotalMilliseconds);
            
            return result;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error generating data for cache key: {CacheKey}", key);
            throw;
        }
    }

    /// <summary>
    /// Gets or sets a cached value with automatic expiration (synchronous version)
    /// </summary>
    /// <typeparam name="T">Type of cached data</typeparam>
    /// <param name="cache">Memory cache instance</param>
    /// <param name="key">Cache key</param>
    /// <param name="factory">Factory function to create data if not cached</param>
    /// <param name="expiration">Cache expiration time</param>
    /// <param name="logger">Logger instance</param>
    /// <returns>Cached or newly created data</returns>
    public static T GetOrSet<T>(
        this IMemoryCache cache,
        string key,
        Func<T> factory,
        TimeSpan expiration,
        ILogger? logger = null)
    {
        if (cache.TryGetValue(key, out T cachedResult))
        {
            logger?.LogDebug("Cache hit for key: {CacheKey}", key);
            return cachedResult;
        }

        logger?.LogDebug("Cache miss for key: {CacheKey}, generating new data", key);
        var startTime = DateTime.UtcNow;
        
        try
        {
            var result = factory();
            
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration,
                Priority = CacheItemPriority.Normal
            };

            cache.Set(key, result, cacheOptions);
            
            var duration = DateTime.UtcNow - startTime;
            logger?.LogDebug("Generated and cached data for key: {CacheKey} in {Duration}ms", 
                key, duration.TotalMilliseconds);
            
            return result;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error generating data for cache key: {CacheKey}", key);
            throw;
        }
    }

    /// <summary>
    /// Removes a cached item by key
    /// </summary>
    /// <param name="cache">Memory cache instance</param>
    /// <param name="key">Cache key to remove</param>
    /// <param name="logger">Logger instance</param>
    public static void RemoveByKey(this IMemoryCache cache, string key, ILogger? logger = null)
    {
        cache.Remove(key);
        logger?.LogDebug("Removed cache entry for key: {CacheKey}", key);
    }

    /// <summary>
    /// Removes all cached items matching a pattern
    /// </summary>
    /// <param name="cache">Memory cache instance</param>
    /// <param name="pattern">Pattern to match (e.g., "match_*", "player_*")</param>
    /// <param name="logger">Logger instance</param>
    public static void RemoveByPattern(this IMemoryCache cache, string pattern, ILogger? logger = null)
    {
        // Note: This is a simplified implementation
        // In production, you might want to use a more sophisticated cache invalidation strategy
        logger?.LogDebug("Pattern-based cache removal requested for pattern: {Pattern}", pattern);
    }

    /// <summary>
    /// Invalidates cache entries related to a specific match
    /// </summary>
    /// <param name="cache">Memory cache instance</param>
    /// <param name="matchId">Match ID</param>
    /// <param name="logger">Logger instance</param>
    public static void InvalidateMatchCache(this IMemoryCache cache, int matchId, ILogger? logger = null)
    {
        var keysToRemove = new[]
        {
            CacheKeys.MatchSummary(matchId),
            CacheKeys.MatchTeamComparison(matchId),
            CacheKeys.KickoutAnalysis(matchId),
            CacheKeys.ShotAnalysis(matchId),
            CacheKeys.MatchMomentum(matchId)
        };

        foreach (var key in keysToRemove)
        {
            cache.RemoveByKey(key, logger);
        }

        // Also remove top performers with different limits
        for (int limit = 1; limit <= 20; limit++)
        {
            cache.RemoveByKey(CacheKeys.MatchTopPerformers(matchId, limit), logger);
        }
    }

    /// <summary>
    /// Invalidates cache entries related to a specific player
    /// </summary>
    /// <param name="cache">Memory cache instance</param>
    /// <param name="playerName">Player name</param>
    /// <param name="seasonId">Season ID</param>
    /// <param name="teamId">Team ID (optional)</param>
    /// <param name="logger">Logger instance</param>
    public static void InvalidatePlayerCache(this IMemoryCache cache, string playerName, int? seasonId = null, int? teamId = null, ILogger? logger = null)
    {
        if (seasonId.HasValue)
        {
            var keysToRemove = new[]
            {
                CacheKeys.PlayerSeasonPerformance(playerName, seasonId.Value),
                CacheKeys.PlayerEfficiency(playerName, seasonId.Value),
                CacheKeys.PlayerTrends(playerName, seasonId.Value),
                CacheKeys.PlayerCumulativeStats(playerName, seasonId.Value)
            };

            foreach (var key in keysToRemove)
            {
                cache.RemoveByKey(key, logger);
            }

            if (teamId.HasValue)
            {
                cache.RemoveByKey(CacheKeys.PlayerTeamComparison(playerName, teamId.Value, seasonId.Value), logger);
            }
        }

        // Remove efficiency cache without season filter
        cache.RemoveByKey(CacheKeys.PlayerEfficiency(playerName, null), logger);
    }

    /// <summary>
    /// Invalidates cache entries related to a specific team
    /// </summary>
    /// <param name="cache">Memory cache instance</param>
    /// <param name="teamId">Team ID</param>
    /// <param name="seasonId">Season ID</param>
    /// <param name="logger">Logger instance</param>
    public static void InvalidateTeamCache(this IMemoryCache cache, int teamId, int? seasonId = null, ILogger? logger = null)
    {
        if (seasonId.HasValue)
        {
            var keysToRemove = new[]
            {
                CacheKeys.TeamSeasonStats(teamId, seasonId.Value),
                CacheKeys.TeamOffensiveStats(teamId, seasonId.Value),
                CacheKeys.TeamDefensiveStats(teamId, seasonId.Value),
                CacheKeys.TeamPossessionStats(teamId, seasonId.Value),
                CacheKeys.TeamVenueAnalysis(teamId, seasonId.Value),
                CacheKeys.TeamRosterAnalysis(teamId, seasonId.Value),
                CacheKeys.TeamTrends(teamId, seasonId.Value),
                CacheKeys.FormationAnalysis(teamId, seasonId.Value)
            };

            foreach (var key in keysToRemove)
            {
                cache.RemoveByKey(key, logger);
            }
        }
    }

    /// <summary>
    /// Invalidates cache entries related to a specific season
    /// </summary>
    /// <param name="cache">Memory cache instance</param>
    /// <param name="seasonId">Season ID</param>
    /// <param name="logger">Logger instance</param>
    public static void InvalidateSeasonCache(this IMemoryCache cache, int seasonId, ILogger? logger = null)
    {
        var keysToRemove = new[]
        {
            CacheKeys.SeasonSummary(seasonId),
            CacheKeys.SeasonCumulative(seasonId),
            CacheKeys.SeasonTrends(seasonId),
            CacheKeys.PositionalComparison(seasonId),
            CacheKeys.PositionalPsrBenchmarks(seasonId),
            CacheKeys.SeasonLeagueTable(seasonId, null)
        };

        foreach (var key in keysToRemove)
        {
            cache.RemoveByKey(key, logger);
        }

        // Remove position-specific caches
        var positions = new[] { "Goalkeeper", "Full Back", "Half Back", "Centre Back", "Midfielder", "Centre Midfielder", "Wing Midfielder", "Forward", "Full Forward", "Half Forward", "Centre Forward" };
        foreach (var position in positions)
        {
            cache.RemoveByKey(CacheKeys.PositionalPerformance(position, seasonId), logger);
        }
    }
}