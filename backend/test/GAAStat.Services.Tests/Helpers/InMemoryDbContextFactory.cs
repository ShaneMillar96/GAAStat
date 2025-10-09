using GAAStat.Dal.Contexts;
using GAAStat.Dal.Models;
using GAAStat.Dal.Models.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GAAStat.Services.Tests.Helpers;

/// <summary>
/// Factory for creating in-memory database contexts for testing
/// </summary>
public static class InMemoryDbContextFactory
{
    /// <summary>
    /// Creates a new in-memory database context with a unique database name
    /// </summary>
    public static GAAStatDbContext Create()
    {
        var options = new DbContextOptionsBuilder<GAAStatDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        var context = new GAAStatDbContext(options);

        // Ensure database is created
        context.Database.EnsureCreated();

        return context;
    }

    /// <summary>
    /// Creates a new in-memory database context with seed data
    /// </summary>
    public static GAAStatDbContext CreateWithSeedData()
    {
        var context = Create();
        SeedReferenceData(context);
        return context;
    }

    /// <summary>
    /// Seeds reference data (positions) into the database
    /// </summary>
    public static void SeedReferenceData(GAAStatDbContext context)
    {
        // Seed positions
        if (!context.Positions.Any())
        {
            context.Positions.AddRange(
                new Position { Name = "Goalkeeper", Code = "GK", DisplayOrder = 1 },
                new Position { Name = "Defender", Code = "DEF", DisplayOrder = 2 },
                new Position { Name = "Midfielder", Code = "MID", DisplayOrder = 3 },
                new Position { Name = "Forward", Code = "FWD", DisplayOrder = 4 }
            );
            context.SaveChanges();
        }
    }

    /// <summary>
    /// Creates a season for testing
    /// </summary>
    public static Season CreateTestSeason(GAAStatDbContext context, int year = 2025)
    {
        var season = new Season
        {
            Year = year,
            Name = $"{year} Season",
            IsCurrent = year == DateTime.Now.Year
        };
        context.Seasons.Add(season);
        context.SaveChanges();
        return season;
    }

    /// <summary>
    /// Creates a competition for testing
    /// </summary>
    public static Competition CreateTestCompetition(GAAStatDbContext context, int seasonId, string name = "Championship")
    {
        var competition = new Competition
        {
            SeasonId = seasonId,
            Name = name,
            Type = name
        };
        context.Competitions.Add(competition);
        context.SaveChanges();
        return competition;
    }

    /// <summary>
    /// Creates a team for testing
    /// </summary>
    public static Team CreateTestTeam(GAAStatDbContext context, string name = "TestTeam", bool isDrum = false)
    {
        var team = new Team
        {
            Name = name,
            IsDrum = isDrum,
            IsActive = true
        };
        context.Teams.Add(team);
        context.SaveChanges();
        return team;
    }
}
