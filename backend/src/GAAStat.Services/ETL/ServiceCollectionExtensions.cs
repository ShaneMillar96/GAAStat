using GAAStat.Services.ETL.Interfaces;
using GAAStat.Services.ETL.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GAAStat.Services.ETL;

/// <summary>
/// Extension methods for registering Player Statistics ETL services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Player Statistics ETL services with dependency injection.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddPlayerStatisticsEtl(this IServiceCollection services)
    {
        // Register main ETL service
        services.AddScoped<IPlayerStatisticsEtlService, PlayerStatisticsEtlService>();

        // Register supporting services
        services.AddScoped<PlayerDataLoader>();
        services.AddScoped<PlayerRosterService>();
        services.AddScoped<PositionDetectionService>();

        return services;
    }
}
