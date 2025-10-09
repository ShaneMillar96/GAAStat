using FluentAssertions;
using GAAStat.Services.ETL.Models;
using GAAStat.Services.ETL.Transformers;
using GAAStat.Services.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace GAAStat.Services.Tests.ETL;

/// <summary>
/// Unit tests for MatchDataTransformer
/// Tests validation logic, score parsing, and data transformation
/// </summary>
public class MatchDataTransformerTests
{
    private readonly Mock<ILogger<MatchDataTransformer>> _mockLogger;
    private readonly MatchDataTransformer _transformer;

    public MatchDataTransformerTests()
    {
        _mockLogger = new Mock<ILogger<MatchDataTransformer>>();
        _transformer = new MatchDataTransformer(_mockLogger.Object);
    }

    [Fact]
    public void ValidateMatchData_ValidData_ReturnsTrue()
    {
        // Arrange
        var matchSheets = new List<MatchSheetData>
        {
            TestDataBuilder.CreateValidMatchSheetData()
        };

        // Act
        var result = _transformer.ValidateMatchData(matchSheets);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateMatchData_InvalidScoreFormat_ReturnsFalse()
    {
        // Arrange
        var matchSheets = new List<MatchSheetData>
        {
            TestDataBuilder.CreateInvalidScoreFormatData()
        };

        // Act
        var result = _transformer.ValidateMatchData(matchSheets);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateMatchData_InvalidPossession_ReturnsFalse()
    {
        // Arrange
        var matchSheets = new List<MatchSheetData>
        {
            TestDataBuilder.CreateInvalidPossessionData()
        };

        // Act
        var result = _transformer.ValidateMatchData(matchSheets);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateMatchData_NegativeStatistics_ReturnsFalse()
    {
        // Arrange
        var matchSheets = new List<MatchSheetData>
        {
            TestDataBuilder.CreateNegativeStatisticsData()
        };

        // Act
        var result = _transformer.ValidateMatchData(matchSheets);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateMatchData_MissingTeamStatistics_ReturnsFalse()
    {
        // Arrange
        var matchSheets = new List<MatchSheetData>
        {
            TestDataBuilder.CreateMissingTeamStatisticsData()
        };

        // Act
        var result = _transformer.ValidateMatchData(matchSheets);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("0-05", true)]
    [InlineData("1-12", true)]
    [InlineData("5-23", true)]
    [InlineData("0-00", true)]
    [InlineData("INVALID", false)]
    [InlineData("1-", false)]
    [InlineData("-05", false)]
    [InlineData("1:05", false)]
    [InlineData("", false)]
    public void ValidateScoreFormat_VariousFormats_ValidatesCorrectly(string score, bool shouldBeValid)
    {
        // Arrange
        var matchData = TestDataBuilder.CreateValidMatchSheetData();
        matchData.HomeScoreFullTime = score;

        var matchSheets = new List<MatchSheetData> { matchData };

        // Act
        var result = _transformer.ValidateMatchData(matchSheets);

        // Assert
        result.Should().Be(shouldBeValid);
    }

    [Theory]
    [InlineData(0, 5, true)]
    [InlineData(3, 15, true)]
    [InlineData(10, 30, true)] // Maximum realistic values
    [InlineData(-1, 5, false)] // Negative goals
    [InlineData(5, -1, false)] // Negative points
    [InlineData(15, 5, false)] // Unrealistic goals (>10)
    [InlineData(5, 35, false)] // Unrealistic points (>30)
    public void ValidateScoreFormat_ScoreRanges_ValidatesCorrectly(int goals, int points, bool shouldBeValid)
    {
        // Arrange
        var matchData = TestDataBuilder.CreateValidMatchSheetData();
        matchData.HomeScoreFullTime = $"{goals}-{points:D2}";

        var matchSheets = new List<MatchSheetData> { matchData };

        // Act
        var result = _transformer.ValidateMatchData(matchSheets);

        // Assert
        result.Should().Be(shouldBeValid);
    }

    [Theory]
    [InlineData(0.0, true)]
    [InlineData(0.5, true)]
    [InlineData(1.0, true)]
    [InlineData(-0.1, false)]
    [InlineData(1.1, false)]
    [InlineData(2.0, false)]
    public void ValidatePossession_VariousValues_ValidatesCorrectly(decimal possession, bool shouldBeValid)
    {
        // Arrange
        var matchData = TestDataBuilder.CreateValidMatchSheetData();
        matchData.TeamStatistics[0].TotalPossession = possession;

        var matchSheets = new List<MatchSheetData> { matchData };

        // Act
        var result = _transformer.ValidateMatchData(matchSheets);

        // Assert
        result.Should().Be(shouldBeValid);
    }

    [Fact]
    public void ValidateMatchData_InvalidMatchNumber_ReturnsFalse()
    {
        // Arrange
        var matchData = TestDataBuilder.CreateValidMatchSheetData(matchNumber: 0);
        var matchSheets = new List<MatchSheetData> { matchData };

        // Act
        var result = _transformer.ValidateMatchData(matchSheets);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateMatchData_EmptyCompetition_ReturnsFalse()
    {
        // Arrange
        var matchData = TestDataBuilder.CreateValidMatchSheetData(competition: "");
        var matchSheets = new List<MatchSheetData> { matchData };

        // Act
        var result = _transformer.ValidateMatchData(matchSheets);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateMatchData_EmptyOpposition_ReturnsFalse()
    {
        // Arrange
        var matchData = TestDataBuilder.CreateValidMatchSheetData(opposition: "");
        var matchSheets = new List<MatchSheetData> { matchData };

        // Act
        var result = _transformer.ValidateMatchData(matchSheets);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("1999-12-31", false)] // Before 2000
    [InlineData("2000-01-01", true)] // Minimum valid date
    [InlineData("2025-08-15", true)] // Normal date
    [InlineData("2026-12-31", true)] // Future within 1 year
    public void ValidateMatchDate_VariousDates_ValidatesCorrectly(string dateString, bool shouldBeValid)
    {
        // Arrange
        var matchData = TestDataBuilder.CreateValidMatchSheetData(matchDate: DateTime.Parse(dateString));
        var matchSheets = new List<MatchSheetData> { matchData };

        // Act
        var result = _transformer.ValidateMatchData(matchSheets);

        // Assert
        result.Should().Be(shouldBeValid);
    }

    [Fact]
    public void ValidateTeamStatistics_Exactly6Records_IsValid()
    {
        // Arrange
        var matchData = TestDataBuilder.CreateValidMatchSheetData();
        var matchSheets = new List<MatchSheetData> { matchData };

        // Act
        var result = _transformer.ValidateMatchData(matchSheets);

        // Assert
        result.Should().BeTrue();
        matchData.TeamStatistics.Should().HaveCount(6);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(12)]
    public void ValidateTeamStatistics_IncorrectRecordCount_ReturnsFalse(int recordCount)
    {
        // Arrange
        var matchData = TestDataBuilder.CreateValidMatchSheetData();
        matchData.TeamStatistics.Clear();

        for (int i = 0; i < recordCount; i++)
        {
            matchData.TeamStatistics.Add(TestDataBuilder.CreateValidTeamStatistics("Drum", "1st"));
        }

        var matchSheets = new List<MatchSheetData> { matchData };

        // Act
        var result = _transformer.ValidateMatchData(matchSheets);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("1st", true)]
    [InlineData("2nd", true)]
    [InlineData("Full", true)]
    [InlineData("3rd", false)]
    [InlineData("First", false)]
    [InlineData("", false)]
    public void ValidatePeriod_VariousPeriods_ValidatesCorrectly(string period, bool shouldBeValid)
    {
        // Arrange
        var matchData = TestDataBuilder.CreateValidMatchSheetData();
        matchData.TeamStatistics[0].Period = period;

        var matchSheets = new List<MatchSheetData> { matchData };

        // Act
        var result = _transformer.ValidateMatchData(matchSheets);

        // Assert
        result.Should().Be(shouldBeValid);
    }

    [Fact]
    public void ValidatePossessionSums_SumEqualsOne_LogsNoWarning()
    {
        // Arrange
        var matchData = TestDataBuilder.CreateValidMatchSheetData();
        matchData.TeamStatistics[0].TotalPossession = 0.50m; // Drum 1st half
        matchData.TeamStatistics[1].TotalPossession = 0.50m; // Opposition 1st half

        var matchSheets = new List<MatchSheetData> { matchData };

        // Act
        var result = _transformer.ValidateMatchData(matchSheets);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidatePossessionSums_SumOutsideTolerance_LogsWarning()
    {
        // Arrange
        var matchData = TestDataBuilder.CreatePossessionSumWarningData();
        var matchSheets = new List<MatchSheetData> { matchData };

        // Act
        var result = _transformer.ValidateMatchData(matchSheets);

        // Assert
        result.Should().BeTrue(); // Still valid, just logs warning
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Possession sum")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void ValidateMatchData_MultipleMatches_ValidatesAll()
    {
        // Arrange
        var matchSheets = new List<MatchSheetData>
        {
            TestDataBuilder.CreateValidMatchSheetData(1),
            TestDataBuilder.CreateValidMatchSheetData(2),
            TestDataBuilder.CreateValidMatchSheetData(3)
        };

        // Act
        var result = _transformer.ValidateMatchData(matchSheets);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateMatchData_MixOfValidAndInvalid_ReturnsFalse()
    {
        // Arrange
        var matchSheets = new List<MatchSheetData>
        {
            TestDataBuilder.CreateValidMatchSheetData(1),
            TestDataBuilder.CreateInvalidScoreFormatData(), // Invalid
            TestDataBuilder.CreateValidMatchSheetData(3)
        };

        // Act
        var result = _transformer.ValidateMatchData(matchSheets);

        // Assert
        result.Should().BeFalse();
    }
}
