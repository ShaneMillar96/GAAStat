using GAAStat.Dal.Models.Application;

namespace GAAStat.Tests.Models;

public class MatchTests
{
    [Fact]
    public void Match_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var match = new Match();

        // Assert
        Assert.NotNull(match.TeamStatistics);
        Assert.NotNull(match.PlayerStatistics);
        Assert.Empty(match.TeamStatistics);
        Assert.Empty(match.PlayerStatistics);
        Assert.Equal(string.Empty, match.Venue);
    }

    [Fact]
    public void Match_Properties_CanBeSet()
    {
        // Arrange
        var matchDate = new DateTime(2025, 9, 26);
        var match = new Match
        {
            MatchId = 1,
            CompetitionId = 1,
            MatchNumber = 9,
            HomeTeamId = 1,
            AwayTeamId = 2,
            MatchDate = matchDate,
            Venue = "Home",
            HomeScoreFirstHalf = "0-04",
            HomeScoreSecondHalf = "1-07",
            HomeScoreFullTime = "1-11",
            AwayScoreFirstHalf = "0-10",
            AwayScoreSecondHalf = "0-06",
            AwayScoreFullTime = "0-16",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(1, match.MatchId);
        Assert.Equal(9, match.MatchNumber);
        Assert.Equal("Home", match.Venue);
        Assert.Equal("1-11", match.HomeScoreFullTime);
        Assert.Equal("0-16", match.AwayScoreFullTime);
        Assert.Equal(matchDate, match.MatchDate);
    }

    [Theory]
    [InlineData("Home")]
    [InlineData("Away")]
    [InlineData("Neutral")]
    public void Match_ValidVenues_CanBeSet(string venue)
    {
        // Arrange & Act
        var match = new Match
        {
            Venue = venue,
            MatchDate = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(venue, match.Venue);
    }

    [Fact]
    public void Match_GAAScoreNotation_IsStored()
    {
        // Arrange & Act
        var match = new Match
        {
            HomeScoreFirstHalf = "0-04",  // 0 goals, 4 points
            HomeScoreSecondHalf = "1-07", // 1 goal, 7 points
            HomeScoreFullTime = "1-11"    // 1 goal, 11 points total
        };

        // Assert
        Assert.Equal("0-04", match.HomeScoreFirstHalf);
        Assert.Equal("1-07", match.HomeScoreSecondHalf);
        Assert.Equal("1-11", match.HomeScoreFullTime);
    }

    [Fact]
    public void Match_Scores_CanBeNull()
    {
        // Arrange & Act
        var match = new Match
        {
            MatchDate = DateTime.UtcNow,
            Venue = "Home"
        };

        // Assert
        Assert.Null(match.HomeScoreFirstHalf);
        Assert.Null(match.HomeScoreSecondHalf);
        Assert.Null(match.HomeScoreFullTime);
        Assert.Null(match.AwayScoreFirstHalf);
        Assert.Null(match.AwayScoreSecondHalf);
        Assert.Null(match.AwayScoreFullTime);
    }

    [Fact]
    public void Match_TeamStatistics_NavigationProperty_Works()
    {
        // Arrange
        var match = new Match { MatchId = 1, MatchDate = DateTime.UtcNow };
        var stats = new MatchTeamStatistics
        {
            MatchTeamStatId = 1,
            MatchId = 1,
            TeamId = 1,
            Period = "Full",
            TotalPossession = 0.55m
        };

        // Act
        match.TeamStatistics.Add(stats);

        // Assert
        Assert.Single(match.TeamStatistics);
        Assert.Equal(0.55m, match.TeamStatistics.First().TotalPossession);
    }

    [Fact]
    public void Match_PlayerStatistics_NavigationProperty_Works()
    {
        // Arrange
        var match = new Match { MatchId = 1, MatchDate = DateTime.UtcNow };
        var stats = new PlayerMatchStatistics
        {
            PlayerMatchStatId = 1,
            MatchId = 1,
            PlayerId = 1,
            MinutesPlayed = 63
        };

        // Act
        match.PlayerStatistics.Add(stats);

        // Assert
        Assert.Single(match.PlayerStatistics);
        Assert.Equal(63, match.PlayerStatistics.First().MinutesPlayed);
    }
}
