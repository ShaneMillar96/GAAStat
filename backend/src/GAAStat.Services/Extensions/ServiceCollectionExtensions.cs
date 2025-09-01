using GAAStat.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GAAStat.Services.Extensions;

/// <summary>
/// Extension methods for registering GAA Statistics services with dependency injection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all GAA Statistics services with the service collection
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddGAAStatServices(this IServiceCollection services)
    {
        // Core import services
        services.AddScoped<IExcelImportService, ExcelImportService>();
        services.AddScoped<IStatisticsCalculationService, StatisticsCalculationService>();
        services.AddScoped<IImportSnapshotService, NullImportSnapshotService>(); // Using null implementation for core ETL
        
        // Phase 2 Enhanced services
        services.AddScoped<IBulkOperationsService, BulkOperationsService>();
        services.AddScoped<IAdvancedExcelProcessorService, AdvancedExcelProcessorService>();

        // Phase 3 Analytics services
        services.AddScoped<IMatchAnalyticsService, MatchAnalyticsService>();
        services.AddScoped<IPlayerAnalyticsService, PlayerAnalyticsService>();
        services.AddScoped<ITeamAnalyticsService, TeamAnalyticsService>();
        services.AddScoped<ISeasonAnalyticsService, SeasonAnalyticsService>();
        services.AddScoped<IPositionalAnalysisService, PositionalAnalysisService>();

        return services;
    }

    /// <summary>
    /// Registers Excel import services specifically
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddExcelImportServices(this IServiceCollection services)
    {
        services.AddScoped<IExcelImportService, ExcelImportService>();
        services.AddScoped<IStatisticsCalculationService, StatisticsCalculationService>();
        services.AddScoped<IImportSnapshotService, NullImportSnapshotService>(); // Using null implementation for core ETL

        return services;
    }

    /// <summary>
    /// Registers statistics calculation services
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddStatisticsServices(this IServiceCollection services)
    {
        services.AddScoped<IStatisticsCalculationService, StatisticsCalculationService>();

        return services;
    }

    /// <summary>
    /// Registers Phase 2 enhanced services for robust import operations
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddPhase2EnhancedServices(this IServiceCollection services)
    {
        // Enhanced snapshot management
        services.AddScoped<IImportSnapshotService, NullImportSnapshotService>(); // Using null implementation for core ETL
        
        // High-performance bulk operations
        services.AddScoped<IBulkOperationsService, BulkOperationsService>();
        
        // Advanced Excel processing (31 sheet types)
        services.AddScoped<IAdvancedExcelProcessorService, AdvancedExcelProcessorService>();

        return services;
    }

    /// <summary>
    /// Registers bulk operations services for high-performance database operations
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddBulkOperationsServices(this IServiceCollection services)
    {
        services.AddScoped<IBulkOperationsService, BulkOperationsService>();
        
        return services;
    }

    /// <summary>
    /// Registers advanced Excel processing services
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddAdvancedExcelServices(this IServiceCollection services)
    {
        services.AddScoped<IAdvancedExcelProcessorService, AdvancedExcelProcessorService>();
        services.AddScoped<IBulkOperationsService, BulkOperationsService>();
        services.AddScoped<IImportSnapshotService, NullImportSnapshotService>(); // Using null implementation for core ETL
        
        return services;
    }

    /// <summary>
    /// Registers Phase 3 comprehensive analytics services with caching support
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddAnalyticsServices(this IServiceCollection services)
    {
        // Core analytics services
        services.AddScoped<IMatchAnalyticsService, MatchAnalyticsService>();
        services.AddScoped<IPlayerAnalyticsService, PlayerAnalyticsService>();
        services.AddScoped<ITeamAnalyticsService, TeamAnalyticsService>();
        services.AddScoped<ISeasonAnalyticsService, SeasonAnalyticsService>();
        services.AddScoped<IPositionalAnalysisService, PositionalAnalysisService>();

        // Add memory caching for performance optimization
        services.AddMemoryCache();

        return services;
    }

    /// <summary>
    /// Registers all services required for a complete GAA Statistics application
    /// including import, processing, and analytics capabilities
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddCompleteGAAStatServices(this IServiceCollection services)
    {
        // Phase 1: Core import services
        services.AddExcelImportServices();
        services.AddStatisticsServices();

        // Phase 2: Enhanced processing services
        services.AddPhase2EnhancedServices();

        // Phase 3: Comprehensive analytics services
        services.AddAnalyticsServices();

        return services;
    }
}