using GAAStat.Dal.Models.Application;

namespace GAAStat.Tests.Models;

public class SeasonTests
{
    [Fact]
    public void Season_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var season = new Season();

        // Assert
        Assert.NotNull(season.Competitions);
        Assert.Empty(season.Competitions);
        Assert.Equal(string.Empty, season.Name);
        Assert.False(season.IsCurrent);
    }

    [Fact]
    public void Season_Properties_CanBeSet()
    {
        // Arrange
        var season = new Season
        {
            SeasonId = 1,
            Year = 2025,
            Name = "2025 Season",
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 12, 31),
            IsCurrent = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(1, season.SeasonId);
        Assert.Equal(2025, season.Year);
        Assert.Equal("2025 Season", season.Name);
        Assert.NotNull(season.StartDate);
        Assert.NotNull(season.EndDate);
        Assert.True(season.IsCurrent);
    }

    [Fact]
    public void Season_Competitions_NavigationProperty_Works()
    {
        // Arrange
        var season = new Season { SeasonId = 1, Year = 2025, Name = "2025 Season" };
        var competition = new Competition
        {
            CompetitionId = 1,
            SeasonId = 1,
            Name = "Championship",
            Type = "Championship"
        };

        // Act
        season.Competitions.Add(competition);

        // Assert
        Assert.Single(season.Competitions);
        Assert.Equal(competition, season.Competitions.First());
    }

    [Fact]
    public void Season_DateRange_CanBeNull()
    {
        // Arrange & Act
        var season = new Season
        {
            Year = 2025,
            Name = "2025 Season",
            StartDate = null,
            EndDate = null
        };

        // Assert
        Assert.Null(season.StartDate);
        Assert.Null(season.EndDate);
    }
}
