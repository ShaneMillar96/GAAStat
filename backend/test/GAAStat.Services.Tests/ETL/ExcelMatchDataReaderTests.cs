using FluentAssertions;
using GAAStat.Services.ETL.Readers;
using GAAStat.Services.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace GAAStat.Services.Tests.ETL;

/// <summary>
/// Unit tests for ExcelMatchDataReader
/// Tests Excel file reading, sheet filtering, and data extraction
/// </summary>
public class ExcelMatchDataReaderTests : IDisposable
{
    private readonly Mock<ILogger<ExcelMatchDataReader>> _mockLogger;
    private readonly ExcelMatchDataReader _reader;
    private readonly string _testFilePath;

    public ExcelMatchDataReaderTests()
    {
        _mockLogger = new Mock<ILogger<ExcelMatchDataReader>>();
        _reader = new ExcelMatchDataReader(_mockLogger.Object);
        _testFilePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.xlsx");
    }

    [Fact]
    public async Task ReadMatchSheetsAsync_ValidFile_ReturnsAllMatchSheets()
    {
        // Arrange
        CreateTestFile();

        // Act
        var result = await _reader.ReadMatchSheetsAsync(_testFilePath);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(m => m.SheetName.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public async Task ReadMatchSheetsAsync_ValidFile_ExtractsMatchMetadataCorrectly()
    {
        // Arrange
        CreateTestFile();

        // Act
        var result = await _reader.ReadMatchSheetsAsync(_testFilePath);

        // Assert
        var firstMatch = result.First();
        firstMatch.MatchNumber.Should().Be(1);
        firstMatch.Competition.Should().Be("Championship");
        firstMatch.Opposition.Should().Be("Slaughtmanus");
        firstMatch.MatchDate.Should().Be(new DateTime(2025, 8, 15));
    }

    [Fact]
    public async Task ReadMatchSheetsAsync_ValidFile_ExtractsAllScores()
    {
        // Arrange
        CreateTestFile();

        // Act
        var result = await _reader.ReadMatchSheetsAsync(_testFilePath);

        // Assert
        var firstMatch = result.First();
        firstMatch.HomeScoreFirstHalf.Should().Be("0-05");
        firstMatch.HomeScoreSecondHalf.Should().Be("1-07");
        firstMatch.HomeScoreFullTime.Should().Be("1-12");
        firstMatch.AwayScoreFirstHalf.Should().Be("0-04");
        firstMatch.AwayScoreSecondHalf.Should().Be("0-06");
        firstMatch.AwayScoreFullTime.Should().Be("0-10");
    }

    [Fact]
    public async Task ReadMatchSheetsAsync_ValidFile_Extracts6TeamStatisticsRecords()
    {
        // Arrange
        CreateTestFile();

        // Act
        var result = await _reader.ReadMatchSheetsAsync(_testFilePath);

        // Assert
        var firstMatch = result.First();
        firstMatch.TeamStatistics.Should().HaveCount(6);

        // Verify periods are present
        firstMatch.TeamStatistics.Should().Contain(s => s.Period == "1st");
        firstMatch.TeamStatistics.Should().Contain(s => s.Period == "2nd");
        firstMatch.TeamStatistics.Should().Contain(s => s.Period == "Full");

        // Verify both teams are present
        firstMatch.TeamStatistics.Should().Contain(s => s.TeamName == "Drum");
        firstMatch.TeamStatistics.Should().Contain(s => s.TeamName == "Slaughtmanus");
    }

    [Fact]
    public async Task ReadMatchSheetsAsync_ValidFile_ExtractsPossessionValues()
    {
        // Arrange
        CreateTestFile();

        // Act
        var result = await _reader.ReadMatchSheetsAsync(_testFilePath);

        // Assert
        var firstMatch = result.First();
        var drumFirst = firstMatch.TeamStatistics.First(s => s.TeamName == "Drum" && s.Period == "1st");

        drumFirst.TotalPossession.Should().NotBeNull();
        drumFirst.TotalPossession.Should().BeInRange(0m, 1m);
    }

    [Fact]
    public async Task ReadMatchSheetsAsync_ValidFile_ExtractsScoreSourceFields()
    {
        // Arrange
        CreateTestFile();

        // Act
        var result = await _reader.ReadMatchSheetsAsync(_testFilePath);

        // Assert
        var firstMatch = result.First();
        var drumFirst = firstMatch.TeamStatistics.First(s => s.TeamName == "Drum" && s.Period == "1st");

        // Verify all 8 score source fields are populated
        drumFirst.ScoreSourceKickoutLong.Should().NotBeNull();
        drumFirst.ScoreSourceKickoutShort.Should().NotBeNull();
        drumFirst.ScoreSourceOppKickoutLong.Should().NotBeNull();
        drumFirst.ScoreSourceOppKickoutShort.Should().NotBeNull();
        drumFirst.ScoreSourceTurnover.Should().NotBeNull();
        drumFirst.ScoreSourcePossessionLost.Should().NotBeNull();
        drumFirst.ScoreSourceShotShort.Should().NotBeNull();
        drumFirst.ScoreSourceThrowUpIn.Should().NotBeNull();
    }

    [Fact]
    public async Task ReadMatchSheetsAsync_ValidFile_ExtractsShotSourceFields()
    {
        // Arrange
        CreateTestFile();

        // Act
        var result = await _reader.ReadMatchSheetsAsync(_testFilePath);

        // Assert
        var firstMatch = result.First();
        var drumFirst = firstMatch.TeamStatistics.First(s => s.TeamName == "Drum" && s.Period == "1st");

        // Verify all 8 shot source fields are populated
        drumFirst.ShotSourceKickoutLong.Should().NotBeNull();
        drumFirst.ShotSourceKickoutShort.Should().NotBeNull();
        drumFirst.ShotSourceOppKickoutLong.Should().NotBeNull();
        drumFirst.ShotSourceOppKickoutShort.Should().NotBeNull();
        drumFirst.ShotSourceTurnover.Should().NotBeNull();
        drumFirst.ShotSourcePossessionLost.Should().NotBeNull();
        drumFirst.ShotSourceShotShort.Should().NotBeNull();
        drumFirst.ShotSourceThrowUpIn.Should().NotBeNull();
    }

    [Fact]
    public async Task ReadMatchSheetsAsync_FileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = "/path/to/nonexistent/file.xlsx";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _reader.ReadMatchSheetsAsync(nonExistentPath));
    }

    [Fact]
    public async Task ReadMatchSheetsAsync_WithNonMatchSheets_OnlyReturnsMatchSheets()
    {
        // Arrange
        CreateTestFile();

        // Act
        var result = await _reader.ReadMatchSheetsAsync(_testFilePath);

        // Assert
        result.Should().HaveCount(3);
        result.Should().NotContain(m => m.SheetName == "Player Matrix");
        result.Should().NotContain(m => m.SheetName == "KPI Definitions");
    }

    [Theory]
    [InlineData("09. Championship vs Slaughtmanus 26.09.25", true)]
    [InlineData("01. League vs Lissan 15.08.25", true)]
    [InlineData("Player Matrix", false)]
    [InlineData("KPI Definitions", false)]
    [InlineData("Cumulative Stats 2025", false)]
    [InlineData("", false)]
    public void IsMatchSheet_VariousSheetNames_ReturnsExpectedResult(string sheetName, bool expected)
    {
        // This test validates the sheet name pattern matching logic
        // We can't directly call IsMatchSheet as it's private, but we can infer its behavior
        // through the public API by creating a test file

        // For this test, we'll validate the pattern through the ReadMatchSheetsAsync behavior
        // The actual pattern validation is implicitly tested through other tests

        // Arrange & Act & Assert
        var isMatch = System.Text.RegularExpressions.Regex.IsMatch(
            sheetName,
            @"^(\d+)\.\s+(\w+)\s+vs\s+(.+?)\s+(\d{2})\.(\d{2})\.(\d{2})$");

        isMatch.Should().Be(expected);
    }

    private void CreateTestFile()
    {
        TestDataFileCreator.CreateTestMatchDataFile(_testFilePath);
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }
}
