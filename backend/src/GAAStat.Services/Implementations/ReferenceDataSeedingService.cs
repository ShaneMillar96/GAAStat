using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GAAStat.Dal.Contexts;
using GAAStat.Dal.Models.Application;
using GAAStat.Services.Interfaces;
using GAAStat.Services.Models;

namespace GAAStat.Services.Implementations;

/// <summary>
/// Service for seeding reference data (lookup tables) required for ETL processing
/// </summary>
public class ReferenceDataSeedingService : IReferenceDataSeedingService
{
    private readonly GAAStatDbContext _context;
    private readonly ILogger<ReferenceDataSeedingService> _logger;

    public ReferenceDataSeedingService(GAAStatDbContext context, ILogger<ReferenceDataSeedingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds all required lookup tables with default GAA values
    /// </summary>
    public async Task<ServiceResult<ReferenceDataSeedResult>> SeedAllReferenceDataAsync()
    {
        try
        {
            _logger.LogInformation("Starting reference data seeding process");

            var result = new ReferenceDataSeedResult();

            // Seed all lookup tables in dependency order
            var positionsResult = await SeedPositionsAsync();
            if (positionsResult.IsSuccess)
                result.PositionsSeeded = positionsResult.Data;
            else
                result.WarningMessages.Add($"Positions seeding failed: {positionsResult.ErrorMessage}");

            var timePeriodsResult = await SeedTimePeriodsAsync();
            if (timePeriodsResult.IsSuccess)
                result.TimePeriodsSeeded = timePeriodsResult.Data;
            else
                result.WarningMessages.Add($"Time periods seeding failed: {timePeriodsResult.ErrorMessage}");

            var teamTypesResult = await SeedTeamTypesAsync();
            if (teamTypesResult.IsSuccess)
                result.TeamTypesSeeded = teamTypesResult.Data;
            else
                result.WarningMessages.Add($"Team types seeding failed: {teamTypesResult.ErrorMessage}");

            var kickoutTypesResult = await SeedKickoutTypesAsync();
            if (kickoutTypesResult.IsSuccess)
                result.KickoutTypesSeeded = kickoutTypesResult.Data;
            else
                result.WarningMessages.Add($"Kickout types seeding failed: {kickoutTypesResult.ErrorMessage}");

            var shotTypesResult = await SeedShotTypesAsync();
            if (shotTypesResult.IsSuccess)
                result.ShotTypesSeeded = shotTypesResult.Data;
            else
                result.WarningMessages.Add($"Shot types seeding failed: {shotTypesResult.ErrorMessage}");

            var shotOutcomesResult = await SeedShotOutcomesAsync();
            if (shotOutcomesResult.IsSuccess)
                result.ShotOutcomesSeeded = shotOutcomesResult.Data;
            else
                result.WarningMessages.Add($"Shot outcomes seeding failed: {shotOutcomesResult.ErrorMessage}");

            var positionAreasResult = await SeedPositionAreasAsync();
            if (positionAreasResult.IsSuccess)
                result.PositionAreasSeeded = positionAreasResult.Data;
            else
                result.WarningMessages.Add($"Position areas seeding failed: {positionAreasResult.ErrorMessage}");

            var freeTypesResult = await SeedFreeTypesAsync();
            if (freeTypesResult.IsSuccess)
                result.FreeTypesSeeded = freeTypesResult.Data;
            else
                result.WarningMessages.Add($"Free types seeding failed: {freeTypesResult.ErrorMessage}");

            var metricCategoriesResult = await SeedMetricCategoriesAsync();
            if (metricCategoriesResult.IsSuccess)
                result.MetricCategoriesSeeded = metricCategoriesResult.Data;
            else
                result.WarningMessages.Add($"Metric categories seeding failed: {metricCategoriesResult.ErrorMessage}");

            _logger.LogInformation("Reference data seeding completed. Total records seeded: {TotalSeeded}", 
                result.TotalSeeded);

            if (result.WarningMessages.Any())
            {
                _logger.LogWarning("Reference data seeding completed with warnings: {Warnings}", 
                    string.Join("; ", result.WarningMessages));
            }

            return ServiceResult<ReferenceDataSeedResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed reference data");
            return ServiceResult<ReferenceDataSeedResult>.Failed("Reference data seeding failed");
        }
    }

    /// <summary>
    /// Seeds position lookup data
    /// </summary>
    public async Task<ServiceResult<int>> SeedPositionsAsync()
    {
        try
        {
            var existingCount = await _context.Positions.CountAsync();
            if (existingCount >= ReferenceDataConstants.Positions.Count)
            {
                _logger.LogDebug("Positions already seeded ({Count} records)", existingCount);
                return ServiceResult<int>.Success(0);
            }

            var seededCount = 0;
            foreach (var (positionName, description) in ReferenceDataConstants.Positions)
            {
                var existing = await _context.Positions
                    .FirstOrDefaultAsync(p => p.PositionName == positionName);

                if (existing == null)
                {
                    var position = new Position
                    {
                        PositionName = positionName,
                        PositionCategory = GetPositionCategory(positionName),
                        Description = description
                    };

                    await _context.Positions.AddAsync(position);
                    seededCount++;
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} positions", seededCount);
            return ServiceResult<int>.Success(seededCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed positions");
            return ServiceResult<int>.Failed("Failed to seed positions");
        }
    }

    /// <summary>
    /// Seeds time period lookup data
    /// </summary>
    public async Task<ServiceResult<int>> SeedTimePeriodsAsync()
    {
        try
        {
            var existingCount = await _context.TimePeriods.CountAsync();
            if (existingCount >= ReferenceDataConstants.TimePeriods.Count)
            {
                _logger.LogDebug("Time periods already seeded ({Count} records)", existingCount);
                return ServiceResult<int>.Success(0);
            }

            var seededCount = 0;
            foreach (var (periodName, description) in ReferenceDataConstants.TimePeriods)
            {
                var existing = await _context.TimePeriods
                    .FirstOrDefaultAsync(tp => tp.PeriodName == periodName);

                if (existing == null)
                {
                    var timePeriod = new TimePeriod
                    {
                        PeriodName = periodName,
                        Description = description
                    };

                    await _context.TimePeriods.AddAsync(timePeriod);
                    seededCount++;
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} time periods", seededCount);
            return ServiceResult<int>.Success(seededCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed time periods");
            return ServiceResult<int>.Failed("Failed to seed time periods");
        }
    }

    /// <summary>
    /// Seeds team type lookup data
    /// </summary>
    public async Task<ServiceResult<int>> SeedTeamTypesAsync()
    {
        try
        {
            var existingCount = await _context.TeamTypes.CountAsync();
            if (existingCount >= ReferenceDataConstants.TeamTypes.Count)
            {
                _logger.LogDebug("Team types already seeded ({Count} records)", existingCount);
                return ServiceResult<int>.Success(0);
            }

            var seededCount = 0;
            foreach (var (typeName, description) in ReferenceDataConstants.TeamTypes)
            {
                var existing = await _context.TeamTypes
                    .FirstOrDefaultAsync(tt => tt.TypeName == typeName);

                if (existing == null)
                {
                    var teamType = new TeamType
                    {
                        TypeName = typeName,
                        Description = description
                    };

                    await _context.TeamTypes.AddAsync(teamType);
                    seededCount++;
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} team types", seededCount);
            return ServiceResult<int>.Success(seededCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed team types");
            return ServiceResult<int>.Failed("Failed to seed team types");
        }
    }

    /// <summary>
    /// Seeds kickout type lookup data
    /// </summary>
    public async Task<ServiceResult<int>> SeedKickoutTypesAsync()
    {
        try
        {
            var existingCount = await _context.KickoutTypes.CountAsync();
            if (existingCount >= ReferenceDataConstants.KickoutTypes.Count)
            {
                _logger.LogDebug("Kickout types already seeded ({Count} records)", existingCount);
                return ServiceResult<int>.Success(0);
            }

            var seededCount = 0;
            foreach (var (typeName, description) in ReferenceDataConstants.KickoutTypes)
            {
                var existing = await _context.KickoutTypes
                    .FirstOrDefaultAsync(kt => kt.TypeName == typeName);

                if (existing == null)
                {
                    var kickoutType = new KickoutType
                    {
                        TypeName = typeName,
                        Description = description
                    };

                    await _context.KickoutTypes.AddAsync(kickoutType);
                    seededCount++;
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} kickout types", seededCount);
            return ServiceResult<int>.Success(seededCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed kickout types");
            return ServiceResult<int>.Failed("Failed to seed kickout types");
        }
    }

    /// <summary>
    /// Seeds shot type lookup data
    /// </summary>
    public async Task<ServiceResult<int>> SeedShotTypesAsync()
    {
        try
        {
            var existingCount = await _context.ShotTypes.CountAsync();
            if (existingCount >= ReferenceDataConstants.ShotTypes.Count)
            {
                _logger.LogDebug("Shot types already seeded ({Count} records)", existingCount);
                return ServiceResult<int>.Success(0);
            }

            var seededCount = 0;
            foreach (var (typeName, description) in ReferenceDataConstants.ShotTypes)
            {
                var existing = await _context.ShotTypes
                    .FirstOrDefaultAsync(st => st.TypeName == typeName);

                if (existing == null)
                {
                    var shotType = new ShotType
                    {
                        TypeName = typeName,
                        Description = description
                    };

                    await _context.ShotTypes.AddAsync(shotType);
                    seededCount++;
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} shot types", seededCount);
            return ServiceResult<int>.Success(seededCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed shot types");
            return ServiceResult<int>.Failed("Failed to seed shot types");
        }
    }

    /// <summary>
    /// Seeds shot outcome lookup data
    /// </summary>
    public async Task<ServiceResult<int>> SeedShotOutcomesAsync()
    {
        try
        {
            var existingCount = await _context.ShotOutcomes.CountAsync();
            if (existingCount >= ReferenceDataConstants.ShotOutcomes.Count)
            {
                _logger.LogDebug("Shot outcomes already seeded ({Count} records)", existingCount);
                return ServiceResult<int>.Success(0);
            }

            var seededCount = 0;
            foreach (var (outcomeName, (description, isScore)) in ReferenceDataConstants.ShotOutcomes)
            {
                var existing = await _context.ShotOutcomes
                    .FirstOrDefaultAsync(so => so.OutcomeName == outcomeName);

                if (existing == null)
                {
                    var shotOutcome = new ShotOutcome
                    {
                        OutcomeName = outcomeName,
                        Description = description,
                        IsScore = isScore
                    };

                    await _context.ShotOutcomes.AddAsync(shotOutcome);
                    seededCount++;
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} shot outcomes", seededCount);
            return ServiceResult<int>.Success(seededCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed shot outcomes");
            return ServiceResult<int>.Failed("Failed to seed shot outcomes");
        }
    }

    /// <summary>
    /// Seeds position area lookup data
    /// </summary>
    public async Task<ServiceResult<int>> SeedPositionAreasAsync()
    {
        try
        {
            var existingCount = await _context.PositionAreas.CountAsync();
            if (existingCount >= ReferenceDataConstants.PositionAreas.Count)
            {
                _logger.LogDebug("Position areas already seeded ({Count} records)", existingCount);
                return ServiceResult<int>.Success(0);
            }

            var seededCount = 0;
            foreach (var (areaName, description) in ReferenceDataConstants.PositionAreas)
            {
                var existing = await _context.PositionAreas
                    .FirstOrDefaultAsync(pa => pa.AreaName == areaName);

                if (existing == null)
                {
                    var positionArea = new PositionArea
                    {
                        AreaName = areaName,
                        Description = description
                    };

                    await _context.PositionAreas.AddAsync(positionArea);
                    seededCount++;
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} position areas", seededCount);
            return ServiceResult<int>.Success(seededCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed position areas");
            return ServiceResult<int>.Failed("Failed to seed position areas");
        }
    }

    /// <summary>
    /// Seeds free type lookup data
    /// </summary>
    public async Task<ServiceResult<int>> SeedFreeTypesAsync()
    {
        try
        {
            var existingCount = await _context.FreeTypes.CountAsync();
            if (existingCount >= ReferenceDataConstants.FreeTypes.Count)
            {
                _logger.LogDebug("Free types already seeded ({Count} records)", existingCount);
                return ServiceResult<int>.Success(0);
            }

            var seededCount = 0;
            foreach (var (typeName, description) in ReferenceDataConstants.FreeTypes)
            {
                var existing = await _context.FreeTypes
                    .FirstOrDefaultAsync(ft => ft.TypeName == typeName);

                if (existing == null)
                {
                    var freeType = new FreeType
                    {
                        TypeName = typeName,
                        Description = description
                    };

                    await _context.FreeTypes.AddAsync(freeType);
                    seededCount++;
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} free types", seededCount);
            return ServiceResult<int>.Success(seededCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed free types");
            return ServiceResult<int>.Failed("Failed to seed free types");
        }
    }

    /// <summary>
    /// Seeds metric category lookup data
    /// </summary>
    public async Task<ServiceResult<int>> SeedMetricCategoriesAsync()
    {
        try
        {
            var existingCount = await _context.MetricCategories.CountAsync();
            if (existingCount >= ReferenceDataConstants.MetricCategories.Count)
            {
                _logger.LogDebug("Metric categories already seeded ({Count} records)", existingCount);
                return ServiceResult<int>.Success(0);
            }

            var seededCount = 0;
            foreach (var (categoryName, description) in ReferenceDataConstants.MetricCategories)
            {
                var existing = await _context.MetricCategories
                    .FirstOrDefaultAsync(mc => mc.CategoryName == categoryName);

                if (existing == null)
                {
                    var metricCategory = new MetricCategory
                    {
                        CategoryName = categoryName,
                        Description = description
                    };

                    await _context.MetricCategories.AddAsync(metricCategory);
                    seededCount++;
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} metric categories", seededCount);
            return ServiceResult<int>.Success(seededCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed metric categories");
            return ServiceResult<int>.Failed("Failed to seed metric categories");
        }
    }

    /// <summary>
    /// Validates that all required reference data exists
    /// </summary>
    public async Task<ServiceResult<ReferenceDataValidationResult>> ValidateReferenceDataAsync()
    {
        try
        {
            var result = new ReferenceDataValidationResult();

            // Check each table
            var tableCounts = new Dictionary<string, int>
            {
                ["positions"] = await _context.Positions.CountAsync(),
                ["time_periods"] = await _context.TimePeriods.CountAsync(),
                ["team_types"] = await _context.TeamTypes.CountAsync(),
                ["kickout_types"] = await _context.KickoutTypes.CountAsync(),
                ["shot_types"] = await _context.ShotTypes.CountAsync(),
                ["shot_outcomes"] = await _context.ShotOutcomes.CountAsync(),
                ["position_areas"] = await _context.PositionAreas.CountAsync(),
                ["free_types"] = await _context.FreeTypes.CountAsync(),
                ["metric_categories"] = await _context.MetricCategories.CountAsync()
            };

            result.TableCounts = tableCounts;

            // Check for missing data
            var expectedCounts = new Dictionary<string, int>
            {
                ["positions"] = ReferenceDataConstants.Positions.Count,
                ["time_periods"] = ReferenceDataConstants.TimePeriods.Count,
                ["team_types"] = ReferenceDataConstants.TeamTypes.Count,
                ["kickout_types"] = ReferenceDataConstants.KickoutTypes.Count,
                ["shot_types"] = ReferenceDataConstants.ShotTypes.Count,
                ["shot_outcomes"] = ReferenceDataConstants.ShotOutcomes.Count,
                ["position_areas"] = ReferenceDataConstants.PositionAreas.Count,
                ["free_types"] = ReferenceDataConstants.FreeTypes.Count,
                ["metric_categories"] = ReferenceDataConstants.MetricCategories.Count
            };

            foreach (var (table, expectedCount) in expectedCounts)
            {
                if (tableCounts[table] < expectedCount)
                {
                    result.MissingTables.Add(table);
                    result.ValidationErrors.Add($"{table} has {tableCounts[table]} records, expected at least {expectedCount}");
                }
            }

            result.IsComplete = !result.MissingTables.Any();

            return ServiceResult<ReferenceDataValidationResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate reference data");
            return ServiceResult<ReferenceDataValidationResult>.Failed("Reference data validation failed");
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Gets the position category for a given position name
    /// </summary>
    private static string GetPositionCategory(string positionName)
    {
        return positionName switch
        {
            "Goalkeeper" => "Goalkeeper",
            "Defender" => "Defender",
            "Midfielder" => "Midfielder", 
            "Forward" => "Forward",
            _ => "Field Player"
        };
    }

    #endregion
}