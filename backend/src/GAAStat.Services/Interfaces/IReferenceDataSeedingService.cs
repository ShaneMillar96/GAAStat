using GAAStat.Services.Models;

namespace GAAStat.Services.Interfaces;

/// <summary>
/// Service for seeding reference data (lookup tables) required for ETL processing
/// Ensures all necessary lookup values exist before processing match data
/// </summary>
public interface IReferenceDataSeedingService
{
    /// <summary>
    /// Seeds all required lookup tables with default GAA values
    /// This must be called before processing any match data to ensure referential integrity
    /// </summary>
    /// <returns>Result indicating success/failure with counts of seeded records</returns>
    Task<ServiceResult<ReferenceDataSeedResult>> SeedAllReferenceDataAsync();

    /// <summary>
    /// Seeds position lookup data (Goalkeeper, Defender, Midfielder, Forward)
    /// </summary>
    Task<ServiceResult<int>> SeedPositionsAsync();

    /// <summary>
    /// Seeds time period lookup data (First Half, Second Half, Full Game)
    /// </summary>
    Task<ServiceResult<int>> SeedTimePeriodsAsync();

    /// <summary>
    /// Seeds team type lookup data (Drum, Opposition)
    /// </summary>
    Task<ServiceResult<int>> SeedTeamTypesAsync();

    /// <summary>
    /// Seeds kickout type lookup data (Long, Short)
    /// </summary>
    Task<ServiceResult<int>> SeedKickoutTypesAsync();

    /// <summary>
    /// Seeds shot type lookup data (From Play, Free Kick, Penalty)
    /// </summary>
    Task<ServiceResult<int>> SeedShotTypesAsync();

    /// <summary>
    /// Seeds shot outcome lookup data (Goal, Point, Wide, Save, Block, etc.)
    /// </summary>
    Task<ServiceResult<int>> SeedShotOutcomesAsync();

    /// <summary>
    /// Seeds position area lookup data (Attacking Third, Middle Third, Defensive Third)
    /// </summary>
    Task<ServiceResult<int>> SeedPositionAreasAsync();

    /// <summary>
    /// Seeds free type lookup data (Standard, Quick)
    /// </summary>
    Task<ServiceResult<int>> SeedFreeTypesAsync();

    /// <summary>
    /// Seeds metric category lookup data (Possession, Attacking, Defensive, etc.)
    /// </summary>
    Task<ServiceResult<int>> SeedMetricCategoriesAsync();

    /// <summary>
    /// Checks if all required reference data exists
    /// </summary>
    /// <returns>Validation result indicating missing reference data</returns>
    Task<ServiceResult<ReferenceDataValidationResult>> ValidateReferenceDataAsync();
}