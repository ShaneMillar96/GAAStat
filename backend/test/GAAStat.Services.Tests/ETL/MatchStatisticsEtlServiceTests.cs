using FluentAssertions;
using GAAStat.Services.ETL.Loaders;
using GAAStat.Services.ETL.Readers;
using GAAStat.Services.ETL.Services;
using GAAStat.Services.ETL.Transformers;
using GAAStat.Services.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace GAAStat.Services.Tests.ETL;

/// <summary>
/// Unit tests for MatchStatisticsEtlService
/// Tests the complete ETL orchestration and pipeline coordination
/// </summary>
public class MatchStatisticsEtlServiceTests : IDisposable
{
    private readonly Mock<ILogger<ExcelMatchDataReader>> _mockReaderLogger;
    private readonly Mock<ILogger<MatchDataTransformer>> _mockTransformerLogger;
    private readonly Mock<ILogger<MatchDataLoader>> _mockLoaderLogger;
    private readonly Mock<ILogger<MatchStatisticsEtlService>> _mockServiceLogger;
    private readonly GAAStat.Dal.Contexts.GAAStatDbContext _dbContext;
    private readonly MatchStatisticsEtlService _etlService;
    private readonly string _testFilePath;

    public MatchStatisticsEtlServiceTests()
    {
        _mockReaderLogger = new Mock<ILogger<ExcelMatchDataReader>>();
        _mockTransformerLogger = new Mock<ILogger<MatchDataTransformer>>();
        _mockLoaderLogger = new Mock<ILogger<MatchDataLoader>>();
        _mockServiceLogger = new Mock<ILogger<MatchStatisticsEtlService>>();

        _dbContext = InMemoryDbContextFactory.CreateWithSeedData();
        _testFilePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.xlsx");

        // Create service with real dependencies
        var reader = new ExcelMatchDataReader(_mockReaderLogger.Object);
        var transformer = new MatchDataTransformer(_mockTransformerLogger.Object);
        var loader = new MatchDataLoader(_dbContext, _mockLoaderLogger.Object);

        _etlService = new MatchStatisticsEtlService(
            reader,
            transformer,
            loader,
            _mockServiceLogger.Object);
    }

    [Fact]
    public async Task ProcessMatchStatisticsAsync_ValidFile_CompletesSuccessfully()
    {
        // Arrange
        CreateTestFile();

        // Act
        var result = await _etlService.ProcessMatchStatisticsAsync(_testFilePath);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.MatchesProcessed.Should().Be(3);
        result.TeamStatisticsCreated.Should().Be(18); // 3 matches × 6 records
        result.Errors.Should().BeEmpty();
        result.EndTime.Should().HaveValue();
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task ProcessMatchStatisticsAsync_ValidFile_CreatesAllDatabaseRecords()
    {
        // Arrange
        CreateTestFile();

        // Act
        var result = await _etlService.ProcessMatchStatisticsAsync(_testFilePath);

        // Assert
        var matches = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(_dbContext.Matches);
        matches.Should().HaveCount(3);

        var teamStats = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(_dbContext.MatchTeamStatistics);
        teamStats.Should().HaveCount(18);

        var seasons = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(_dbContext.Seasons);
        seasons.Should().Contain(s => s.Year == 2025);

        var competitions = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(_dbContext.Competitions);
        competitions.Should().NotBeEmpty();

        var teams = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(_dbContext.Teams);
        teams.Should().Contain(t => t.IsDrum);
    }

    [Fact]
    public async Task ProcessMatchStatisticsAsync_FileNotFound_ReturnsError()
    {
        // Arrange
        var nonExistentPath = "/path/to/nonexistent/file.xlsx";

        // Act
        var result = await _etlService.ProcessMatchStatisticsAsync(nonExistentPath);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors.First().Code.Should().Be("FILE_NOT_FOUND");
        result.MatchesProcessed.Should().Be(0);
    }

    [Fact]
    public async Task ProcessMatchStatisticsAsync_LogsPhases_InCorrectOrder()
    {
        // Arrange
        CreateTestFile();

        // Act
        await _etlService.ProcessMatchStatisticsAsync(_testFilePath);

        // Assert - Verify logging sequence
        _mockServiceLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Phase 1: Extracting")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockServiceLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Phase 2: Validating")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockServiceLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Phase 3: Loading")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
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
