using FluentAssertions;
using GAAStat.Services.ETL.Loaders;
using GAAStat.Services.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace GAAStat.Services.Tests.ETL;

/// <summary>
/// Unit tests for MatchDataLoader
/// Tests database loading, upsert logic, and transaction management
/// </summary>
public class MatchDataLoaderTests : IDisposable
{
    private readonly Mock<ILogger<MatchDataLoader>> _mockLogger;
    private readonly GAAStat.Dal.Contexts.GAAStatDbContext _dbContext;
    private readonly MatchDataLoader _loader;

    public MatchDataLoaderTests()
    {
        _mockLogger = new Mock<ILogger<MatchDataLoader>>();
        _dbContext = InMemoryDbContextFactory.CreateWithSeedData();
        _loader = new MatchDataLoader(_dbContext, _mockLogger.Object);
    }

    [Fact]
    public async Task LoadMatchDataAsync_NewMatch_InsertsSuccessfully()
    {
        // Arrange
        var matchSheets = new List<GAAStat.Services.ETL.Models.MatchSheetData>
        {
            TestDataBuilder.CreateValidMatchSheetData()
        };

        // Act
        var result = await _loader.LoadMatchDataAsync(matchSheets);

        // Assert
        result.Success.Should().BeTrue();
        result.MatchesProcessed.Should().Be(1);
        result.TeamStatisticsCreated.Should().Be(6);
        result.Errors.Should().BeEmpty();

        var matches = await _dbContext.Matches.ToListAsync();
        matches.Should().HaveCount(1);

        var teamStats = await _dbContext.MatchTeamStatistics.ToListAsync();
        teamStats.Should().HaveCount(6);
    }

    [Fact]
    public async Task LoadMatchDataAsync_MultipleMatches_InsertsAll()
    {
        // Arrange
        var matchSheets = new List<GAAStat.Services.ETL.Models.MatchSheetData>
        {
            TestDataBuilder.CreateValidMatchSheetData(1),
            TestDataBuilder.CreateValidMatchSheetData(2),
            TestDataBuilder.CreateValidMatchSheetData(3)
        };

        // Act
        var result = await _loader.LoadMatchDataAsync(matchSheets);

        // Assert
        result.Success.Should().BeTrue();
        result.MatchesProcessed.Should().Be(3);
        result.TeamStatisticsCreated.Should().Be(18); // 3 matches × 6 records

        var matches = await _dbContext.Matches.ToListAsync();
        matches.Should().HaveCount(3);
    }

    [Fact]
    public async Task LoadMatchDataAsync_NewSeason_CreatesSeasonRecord()
    {
        // Arrange
        var matchSheets = new List<GAAStat.Services.ETL.Models.MatchSheetData>
        {
            TestDataBuilder.CreateValidMatchSheetData(matchDate: new DateTime(2025, 8, 15))
        };

        // Act
        await _loader.LoadMatchDataAsync(matchSheets);

        // Assert
        var seasons = await _dbContext.Seasons.ToListAsync();
        seasons.Should().Contain(s => s.Year == 2025);
    }

    [Fact]
    public async Task LoadMatchDataAsync_ExistingSeason_DoesNotDuplicate()
    {
        // Arrange
        var season = InMemoryDbContextFactory.CreateTestSeason(_dbContext, 2025);

        var matchSheets = new List<GAAStat.Services.ETL.Models.MatchSheetData>
        {
            TestDataBuilder.CreateValidMatchSheetData(matchDate: new DateTime(2025, 8, 15))
        };

        // Act
        await _loader.LoadMatchDataAsync(matchSheets);

        // Assert
        var seasons = await _dbContext.Seasons.Where(s => s.Year == 2025).ToListAsync();
        seasons.Should().HaveCount(1);
        seasons.First().SeasonId.Should().Be(season.SeasonId);
    }

    [Fact]
    public async Task LoadMatchDataAsync_NewCompetition_CreatesCompetitionRecord()
    {
        // Arrange
        var matchSheets = new List<GAAStat.Services.ETL.Models.MatchSheetData>
        {
            TestDataBuilder.CreateValidMatchSheetData(competition: "Championship")
        };

        // Act
        await _loader.LoadMatchDataAsync(matchSheets);

        // Assert
        var competitions = await _dbContext.Competitions.ToListAsync();
        competitions.Should().Contain(c => c.Name == "Championship");
    }

    [Fact]
    public async Task LoadMatchDataAsync_ExistingCompetition_DoesNotDuplicate()
    {
        // Arrange
        var season = InMemoryDbContextFactory.CreateTestSeason(_dbContext, 2025);
        var competition = InMemoryDbContextFactory.CreateTestCompetition(_dbContext, season.SeasonId, "Championship");

        var matchSheets = new List<GAAStat.Services.ETL.Models.MatchSheetData>
        {
            TestDataBuilder.CreateValidMatchSheetData(
                matchDate: new DateTime(2025, 8, 15),
                competition: "Championship")
        };

        // Act
        await _loader.LoadMatchDataAsync(matchSheets);

        // Assert
        var competitions = await _dbContext.Competitions
            .Where(c => c.SeasonId == season.SeasonId && c.Name == "Championship")
            .ToListAsync();
        competitions.Should().HaveCount(1);
        competitions.First().CompetitionId.Should().Be(competition.CompetitionId);
    }

    [Fact]
    public async Task LoadMatchDataAsync_NewTeams_CreatesTeamRecords()
    {
        // Arrange
        var matchSheets = new List<GAAStat.Services.ETL.Models.MatchSheetData>
        {
            TestDataBuilder.CreateValidMatchSheetData(opposition: "Slaughtmanus")
        };

        // Act
        await _loader.LoadMatchDataAsync(matchSheets);

        // Assert
        var teams = await _dbContext.Teams.ToListAsync();
        teams.Should().Contain(t => t.Name == "Drum" && t.IsDrum);
        teams.Should().Contain(t => t.Name == "Slaughtmanus" && !t.IsDrum);
    }

    [Fact]
    public async Task LoadMatchDataAsync_ExistingTeams_DoesNotDuplicate()
    {
        // Arrange
        var drumTeam = InMemoryDbContextFactory.CreateTestTeam(_dbContext, "Drum", true);
        var awayTeam = InMemoryDbContextFactory.CreateTestTeam(_dbContext, "Slaughtmanus", false);

        var matchSheets = new List<GAAStat.Services.ETL.Models.MatchSheetData>
        {
            TestDataBuilder.CreateValidMatchSheetData(opposition: "Slaughtmanus")
        };

        // Act
        await _loader.LoadMatchDataAsync(matchSheets);

        // Assert
        var teams = await _dbContext.Teams.ToListAsync();
        teams.Should().HaveCount(2);
        teams.Should().Contain(t => t.TeamId == drumTeam.TeamId);
        teams.Should().Contain(t => t.TeamId == awayTeam.TeamId);
    }

    [Fact]
    public async Task LoadMatchDataAsync_DuplicateMatch_ThrowsAndRollsBack()
    {
        // Arrange
        var matchSheets = new List<GAAStat.Services.ETL.Models.MatchSheetData>
        {
            TestDataBuilder.CreateValidMatchSheetData(matchNumber: 1)
        };

        // Load first time
        await _loader.LoadMatchDataAsync(matchSheets);

        // Act - try to load again
        var result = await _loader.LoadMatchDataAsync(matchSheets);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors.First().Message.Should().Contain("already exists");

        // Verify no duplicate records
        var matches = await _dbContext.Matches.ToListAsync();
        matches.Should().HaveCount(1);
    }

    [Fact]
    public async Task LoadMatchDataAsync_InsertAllScoreFields_StoresCorrectly()
    {
        // Arrange
        var matchSheets = new List<GAAStat.Services.ETL.Models.MatchSheetData>
        {
            TestDataBuilder.CreateValidMatchSheetData()
        };

        // Act
        await _loader.LoadMatchDataAsync(matchSheets);

        // Assert
        var match = await _dbContext.Matches.FirstAsync();
        match.HomeScoreFirstHalf.Should().Be("0-05");
        match.HomeScoreSecondHalf.Should().Be("1-07");
        match.HomeScoreFullTime.Should().Be("1-12");
        match.AwayScoreFirstHalf.Should().Be("0-04");
        match.AwayScoreSecondHalf.Should().Be("0-06");
        match.AwayScoreFullTime.Should().Be("0-10");
    }

    [Fact]
    public async Task LoadMatchDataAsync_InsertTeamStatistics_StoresAllFields()
    {
        // Arrange
        var matchSheets = new List<GAAStat.Services.ETL.Models.MatchSheetData>
        {
            TestDataBuilder.CreateValidMatchSheetData()
        };

        // Act
        await _loader.LoadMatchDataAsync(matchSheets);

        // Assert
        var teamStats = await _dbContext.MatchTeamStatistics.ToListAsync();
        teamStats.Should().HaveCount(6);

        var drumFirst = teamStats.First(s => s.Period == "1st");
        drumFirst.TotalPossession.Should().NotBeNull();
        drumFirst.ScoreSourceKickoutLong.Should().BeGreaterOrEqualTo(0);
        drumFirst.ShotSourceKickoutLong.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task LoadMatchDataAsync_PartialFailure_RollsBackFailedMatch()
    {
        // Arrange
        var validMatch = TestDataBuilder.CreateValidMatchSheetData(1);
        var invalidMatch = TestDataBuilder.CreateValidMatchSheetData(2);
        invalidMatch.TeamStatistics.Clear(); // This will cause failure

        var matchSheets = new List<GAAStat.Services.ETL.Models.MatchSheetData>
        {
            validMatch,
            invalidMatch
        };

        // Act
        var result = await _loader.LoadMatchDataAsync(matchSheets);

        // Assert
        result.Success.Should().BeFalse();
        result.MatchesProcessed.Should().Be(1); // Only first match succeeded
        result.Errors.Should().HaveCount(1);

        var matches = await _dbContext.Matches.ToListAsync();
        matches.Should().HaveCount(1); // Second match rolled back
    }

    [Fact]
    public async Task LoadMatchDataAsync_TransactionRollback_NoPartialData()
    {
        // Arrange
        var matchData = TestDataBuilder.CreateValidMatchSheetData();
        matchData.TeamStatistics.Clear(); // This will cause failure during insert

        var matchSheets = new List<GAAStat.Services.ETL.Models.MatchSheetData> { matchData };

        // Act
        var result = await _loader.LoadMatchDataAsync(matchSheets);

        // Assert
        result.Success.Should().BeFalse();
        result.MatchesProcessed.Should().Be(0);

        // Verify nothing was inserted
        var matches = await _dbContext.Matches.ToListAsync();
        matches.Should().BeEmpty();

        var teamStats = await _dbContext.MatchTeamStatistics.ToListAsync();
        teamStats.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadMatchDataAsync_CreatesAuditFields_SetsTimestamps()
    {
        // Arrange
        var matchSheets = new List<GAAStat.Services.ETL.Models.MatchSheetData>
        {
            TestDataBuilder.CreateValidMatchSheetData()
        };

        var beforeLoad = DateTime.UtcNow.AddSeconds(-1);

        // Act
        await _loader.LoadMatchDataAsync(matchSheets);

        var afterLoad = DateTime.UtcNow.AddSeconds(1);

        // Assert
        var match = await _dbContext.Matches.FirstAsync();
        match.CreatedAt.Should().BeAfter(beforeLoad);
        match.CreatedAt.Should().BeBefore(afterLoad);
        match.UpdatedAt.Should().BeAfter(beforeLoad);
        match.UpdatedAt.Should().BeBefore(afterLoad);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}
