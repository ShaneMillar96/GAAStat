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
    /// Registers all ETL services for match statistics processing
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddMatchStatisticsEtlServices(this IServiceCollection services)
    {
        // Main ETL orchestrator
        services.AddScoped<IMatchStatisticsEtlService, MatchStatisticsEtlService>();

        // ETL pipeline components
        services.AddScoped<ExcelMatchDataReader>();
        services.AddScoped<MatchDataTransformer>();
        services.AddScoped<MatchDataLoader>();

        return services;
    }
}
