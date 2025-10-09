using FluentAssertions;
using GAAStat.Services.ETL.Loaders;
using GAAStat.Services.ETL.Readers;
using GAAStat.Services.ETL.Services;
using GAAStat.Services.ETL.Transformers;
using GAAStat.Services.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace GAAStat.Services.Tests.Integration;

/// <summary>
/// Integration tests for the complete ETL pipeline
/// Tests end-to-end workflows, idempotency, and error scenarios
/// </summary>
public class MatchStatisticsEtlIntegrationTests : IDisposable
{
    private readonly GAAStat.Dal.Contexts.GAAStatDbContext _dbContext;
    private readonly MatchStatisticsEtlService _etlService;
    private readonly string _testFilePath;

    public MatchStatisticsEtlIntegrationTests()
    {
        _dbContext = InMemoryDbContextFactory.CreateWithSeedData();
        _testFilePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.xlsx");

        // Create service with real dependencies and mock loggers
        var reader = new ExcelMatchDataReader(Mock.Of<ILogger<ExcelMatchDataReader>>());
        var transformer = new MatchDataTransformer(Mock.Of<ILogger<MatchDataTransformer>>());
        var loader = new MatchDataLoader(_dbContext, Mock.Of<ILogger<MatchDataLoader>>());

        _etlService = new MatchStatisticsEtlService(
            reader,
            transformer,
            loader,
            Mock.Of<ILogger<MatchStatisticsEtlService>>());
    }

    [Fact]
    public async Task EndToEnd_ValidFile_LoadsAllDataCorrectly()
    {
        // Arrange
        CreateTestFile();

        // Act
        var result = await _etlService.ProcessMatchStatisticsAsync(_testFilePath);

        // Assert - ETL Result
        result.Success.Should().BeTrue();
        result.MatchesProcessed.Should().Be(3);
        result.TeamStatisticsCreated.Should().Be(18);

        // Assert - Database State
        var matches = await _dbContext.Matches
            .Include(m => m.Competition)
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .ToListAsync();

        matches.Should().HaveCount(3);

        // Verify first match details
        var firstMatch = matches.OrderBy(m => m.MatchNumber).First();
        firstMatch.MatchNumber.Should().Be(1);
        firstMatch.HomeTeam.Name.Should().Be("Drum");
        firstMatch.AwayTeam.Name.Should().Be("Slaughtmanus");
        firstMatch.HomeScoreFullTime.Should().Be("1-12");
    }

    [Fact]
    public async Task EndToEnd_ValidFile_CreatesReferenceDataHierarchy()
    {
        // Arrange
        CreateTestFile();

        // Act
        await _etlService.ProcessMatchStatisticsAsync(_testFilePath);

        // Assert - Verify data hierarchy
        var seasons = await _dbContext.Seasons.ToListAsync();
        seasons.Should().Contain(s => s.Year == 2025);

        var season = seasons.First(s => s.Year == 2025);
        var competitions = await _dbContext.Competitions
            .Where(c => c.SeasonId == season.SeasonId)
            .ToListAsync();

        competitions.Should().Contain(c => c.Name == "Championship");
        competitions.Should().Contain(c => c.Name == "League");
    }

    [Fact]
    public async Task EndToEnd_ValidFile_CreatesTeamStatisticsForAllPeriods()
    {
        // Arrange
        CreateTestFile();

        // Act
        await _etlService.ProcessMatchStatisticsAsync(_testFilePath);

        // Assert
        var match = await _dbContext.Matches.FirstAsync();
        var teamStats = await _dbContext.MatchTeamStatistics
            .Where(ts => ts.MatchId == match.MatchId)
            .ToListAsync();

        teamStats.Should().HaveCount(6);

        // Verify all periods present
        teamStats.Should().Contain(s => s.Period == "1st");
        teamStats.Should().Contain(s => s.Period == "2nd");
        teamStats.Should().Contain(s => s.Period == "Full");

        // Verify both teams present
        var drumTeam = await _dbContext.Teams.FirstAsync(t => t.IsDrum);
        var awayTeam = await _dbContext.Teams.FirstAsync(t => !t.IsDrum);

        teamStats.Should().Contain(s => s.TeamId == drumTeam.TeamId);
        teamStats.Should().Contain(s => s.TeamId == awayTeam.TeamId);
    }

    [Fact]
    public async Task Idempotency_RunTwice_DoesNotDuplicateReferenceData()
    {
        // Arrange
        CreateTestFile();

        // Act - Run ETL twice
        await _etlService.ProcessMatchStatisticsAsync(_testFilePath);

        // Delete matches to allow second run (they have duplicate check)
        _dbContext.Matches.RemoveRange(_dbContext.Matches);
        _dbContext.MatchTeamStatistics.RemoveRange(_dbContext.MatchTeamStatistics);
        await _dbContext.SaveChangesAsync();

        var result2 = await _etlService.ProcessMatchStatisticsAsync(_testFilePath);

        // Assert - Reference data not duplicated
        var seasons = await _dbContext.Seasons.Where(s => s.Year == 2025).ToListAsync();
        seasons.Should().HaveCount(1);

        var teams = await _dbContext.Teams.Where(t => t.Name == "Drum").ToListAsync();
        teams.Should().HaveCount(1);

        result2.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Idempotency_DuplicateMatch_PreventsDoubleInsertion()
    {
        // Arrange
        CreateTestFile();

        // Act - Run ETL twice
        var result1 = await _etlService.ProcessMatchStatisticsAsync(_testFilePath);
        var result2 = await _etlService.ProcessMatchStatisticsAsync(_testFilePath);

        // Assert
        result1.Success.Should().BeTrue();
        result2.Success.Should().BeFalse();
        result2.Errors.Should().HaveCountGreaterThan(0);

        // Verify no duplicate matches
        var matches = await _dbContext.Matches.ToListAsync();
        matches.Should().HaveCount(3);

        var teamStats = await _dbContext.MatchTeamStatistics.ToListAsync();
        teamStats.Should().HaveCount(18);
    }

    [Fact]
    public async Task ErrorRecovery_PartialFailure_CommitsSuccessfulMatches()
    {
        // Arrange - Create file with one valid match
        using var builder = new ExcelTestFileBuilder(_testFilePath);
        builder.AddMatchSheet(1, "Championship", "TestTeam", new DateTime(2025, 8, 15));
        builder.Save();

        // Act
        var result = await _etlService.ProcessMatchStatisticsAsync(_testFilePath);

        // Assert
        result.Success.Should().BeTrue();
        result.MatchesProcessed.Should().Be(1);

        var matches = await _dbContext.Matches.ToListAsync();
        matches.Should().HaveCount(1);
    }

    [Fact]
    public async Task DataIntegrity_AllForeignKeys_AreValid()
    {
        // Arrange
        CreateTestFile();

        // Act
        await _etlService.ProcessMatchStatisticsAsync(_testFilePath);

        // Assert - Verify FK relationships
        var matches = await _dbContext.Matches
            .Include(m => m.Competition)
            .ThenInclude(c => c.Season)
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .ToListAsync();

        matches.Should().AllSatisfy(m =>
        {
            m.Competition.Should().NotBeNull();
            m.Competition.Season.Should().NotBeNull();
            m.HomeTeam.Should().NotBeNull();
            m.AwayTeam.Should().NotBeNull();
        });

        var teamStats = await _dbContext.MatchTeamStatistics
            .Include(ts => ts.Match)
            .Include(ts => ts.Team)
            .ToListAsync();

        teamStats.Should().AllSatisfy(ts =>
        {
            ts.Match.Should().NotBeNull();
            ts.Team.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task Performance_ThreeMatches_CompletesInReasonableTime()
    {
        // Arrange
        CreateTestFile();
        var maxDuration = TimeSpan.FromSeconds(10);

        // Act
        var startTime = DateTime.UtcNow;
        var result = await _etlService.ProcessMatchStatisticsAsync(_testFilePath);
        var actualDuration = DateTime.UtcNow - startTime;

        // Assert
        result.Success.Should().BeTrue();
        actualDuration.Should().BeLessThan(maxDuration);
        result.Duration.Should().BeLessThan(maxDuration);
    }

    [Fact]
    public async Task DataConsistency_TeamStatisticsCounts_Match6PerMatch()
    {
        // Arrange
        CreateTestFile();

        // Act
        await _etlService.ProcessMatchStatisticsAsync(_testFilePath);

        // Assert
        var matches = await _dbContext.Matches.ToListAsync();

        foreach (var match in matches)
        {
            var statsCount = await _dbContext.MatchTeamStatistics
                .Where(ts => ts.MatchId == match.MatchId)
                .CountAsync();

            statsCount.Should().Be(6, $"Match {match.MatchNumber} should have 6 team statistics records");
        }
    }

    [Fact]
    public async Task DataValidation_PossessionValues_AreWithinRange()
    {
        // Arrange
        CreateTestFile();

        // Act
        await _etlService.ProcessMatchStatisticsAsync(_testFilePath);

        // Assert
        var teamStats = await _dbContext.MatchTeamStatistics.ToListAsync();

        teamStats.Should().AllSatisfy(ts =>
        {
            if (ts.TotalPossession.HasValue)
            {
                ts.TotalPossession.Value.Should().BeGreaterOrEqualTo(0);
                ts.TotalPossession.Value.Should().BeLessOrEqualTo(1);
            }
        });
    }

    [Fact]
    public async Task TransactionIntegrity_FailedMatch_LeavesNoPartialData()
    {
        // Arrange - Create minimal test file
        CreateTestFile();

        // Get initial counts
        var initialMatchCount = await _dbContext.Matches.CountAsync();
        var initialStatsCount = await _dbContext.MatchTeamStatistics.CountAsync();

        // Act - Process normally first time
        var result = await _etlService.ProcessMatchStatisticsAsync(_testFilePath);
        result.Success.Should().BeTrue();

        var afterFirstRun = await _dbContext.Matches.CountAsync();
        afterFirstRun.Should().BeGreaterThan(initialMatchCount);

        // Try to run again (should fail due to duplicates)
        var result2 = await _etlService.ProcessMatchStatisticsAsync(_testFilePath);

        // Assert - No additional records created
        var finalMatchCount = await _dbContext.Matches.CountAsync();
        var finalStatsCount = await _dbContext.MatchTeamStatistics.CountAsync();

        finalMatchCount.Should().Be(afterFirstRun);
        result2.Success.Should().BeFalse();
    }

    private void CreateTestFile()
    {
        TestDataFileCreator.CreateTestMatchDataFile(_testFilePath);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }
}
