using GAAStat.Services.Models;

namespace GAAStat.Services.Interfaces;

/// <summary>
/// Service for position-specific performance metrics and analysis
/// </summary>
public interface IPositionalAnalysisService
{
    /// <summary>
    /// Gets performance analysis for all players in a specific position
    /// </summary>
    /// <param name="position">Position name (e.g., "Goalkeeper", "Full Back", "Midfielder")</param>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Position-specific performance metrics and rankings</returns>
    Task<ServiceResult<PositionalPerformanceDto>> GetPositionalPerformanceAsync(string position, int seasonId);
    
    /// <summary>
    /// Gets comparative analysis between different positions
    /// </summary>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Cross-positional performance comparison</returns>
    Task<ServiceResult<PositionalComparisonDto>> GetPositionalComparisonAsync(int seasonId);
    
    /// <summary>
    /// Gets goalkeeper-specific advanced metrics and analysis
    /// </summary>
    /// <param name="seasonId">Season identifier</param>
    /// <param name="teamId">Optional team filter</param>
    /// <returns>Specialized goalkeeper performance metrics</returns>
    Task<ServiceResult<GoalkeeperAnalysisDto>> GetGoalkeeperAnalysisAsync(int seasonId, int? teamId = null);
    
    /// <summary>
    /// Gets defender-specific metrics including tackles and defensive actions
    /// </summary>
    /// <param name="seasonId">Season identifier</param>
    /// <param name="teamId">Optional team filter</param>
    /// <returns>Specialized defender performance metrics</returns>
    Task<ServiceResult<DefenderAnalysisDto>> GetDefenderAnalysisAsync(int seasonId, int? teamId = null);
    
    /// <summary>
    /// Gets midfielder-specific metrics including distribution and possession
    /// </summary>
    /// <param name="seasonId">Season identifier</param>
    /// <param name="teamId">Optional team filter</param>
    /// <returns>Specialized midfielder performance metrics</returns>
    Task<ServiceResult<MidfielderAnalysisDto>> GetMidfielderAnalysisAsync(int seasonId, int? teamId = null);
    
    /// <summary>
    /// Gets forward-specific metrics including scoring and attacking plays
    /// </summary>
    /// <param name="seasonId">Season identifier</param>
    /// <param name="teamId">Optional team filter</param>
    /// <returns>Specialized forward performance metrics</returns>
    Task<ServiceResult<ForwardAnalysisDto>> GetForwardAnalysisAsync(int seasonId, int? teamId = null);
    
    /// <summary>
    /// Gets position-specific PSR benchmarks and averages
    /// </summary>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>PSR benchmarks by position</returns>
    Task<ServiceResult<PositionalPsrBenchmarksDto>> GetPositionalPsrBenchmarksAsync(int seasonId);
    
    /// <summary>
    /// Gets optimal formation analysis based on player performance
    /// </summary>
    /// <param name="teamId">Team identifier</param>
    /// <param name="seasonId">Season identifier</param>
    /// <returns>Formation recommendations based on player strengths</returns>
    Task<ServiceResult<FormationAnalysisDto>> GetOptimalFormationAnalysisAsync(int teamId, int seasonId);
}