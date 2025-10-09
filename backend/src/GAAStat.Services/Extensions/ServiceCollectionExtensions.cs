using GAAStat.Services.ETL;
using GAAStat.Services.ETL.Interfaces;
using GAAStat.Services.ETL.Loaders;
using GAAStat.Services.ETL.Readers;
using GAAStat.Services.ETL.Services;
using GAAStat.Services.ETL.Transformers;
using Microsoft.Extensions.DependencyInjection;

namespace GAAStat.Services.Extensions;

/// <summary>
/// Extension methods for registering GAAStat services with dependency injection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all ETL services for GAA statistics processing (match and player statistics)
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddGAAStatisticsEtlServices(this IServiceCollection services)
    {
        // Match statistics ETL services
        services.AddScoped<IMatchStatisticsEtlService, MatchStatisticsEtlService>();
        services.AddScoped<ExcelMatchDataReader>();
        services.AddScoped<MatchDataTransformer>();
        services.AddScoped<MatchDataLoader>();

        // Player statistics ETL services
        services.AddScoped<IPlayerStatisticsEtlService, PlayerStatisticsEtlService>();
        services.AddScoped<PlayerDataLoader>();
        services.AddScoped<PlayerRosterService>();
        services.AddScoped<PositionDetectionService>();

        return services;
    }
}
