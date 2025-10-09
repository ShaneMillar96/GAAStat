using GAAStat.Dal.Models.Application;

namespace GAAStat.Tests.Models;

public class TeamTests
{
    [Fact]
    public void Team_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var team = new Team();

        // Assert
        Assert.NotNull(team.HomeMatches);
        Assert.NotNull(team.AwayMatches);
        Assert.NotNull(team.TeamStatistics);
        Assert.Empty(team.HomeMatches);
        Assert.Empty(team.AwayMatches);
        Assert.Empty(team.TeamStatistics);
        Assert.Equal(string.Empty, team.Name);
        Assert.False(team.IsDrum);
        Assert.False(team.IsActive);
    }

    [Fact]
    public void Team_Properties_CanBeSet()
    {
        // Arrange
        var team = new Team
        {
            TeamId = 1,
            Name = "Drum",
            Abbreviation = "DRM",
            IsDrum = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(1, team.TeamId);
        Assert.Equal("Drum", team.Name);
        Assert.Equal("DRM", team.Abbreviation);
        Assert.True(team.IsDrum);
        Assert.True(team.IsActive);
    }

    [Fact]
    public void Team_DrumTeam_CanBeIdentified()
    {
        // Arrange
        var drumTeam = new Team { Name = "Drum", IsDrum = true };
        var opponentTeam = new Team { Name = "Slaughtmanus", IsDrum = false };

        // Assert
        Assert.True(drumTeam.IsDrum);
        Assert.False(opponentTeam.IsDrum);
    }

    [Fact]
    public void Team_Abbreviation_CanBeNull()
    {
        // Arrange & Act
        var team = new Team
        {
            Name = "Test Team",
            Abbreviation = null
        };

        // Assert
        Assert.Null(team.Abbreviation);
    }

    [Fact]
    public void Team_HomeMatches_NavigationProperty_Works()
    {
        // Arrange
        var team = new Team { TeamId = 1, Name = "Drum" };
        var match = new Match
        {
            MatchId = 1,
            HomeTeamId = 1,
            AwayTeamId = 2,
            MatchDate = DateTime.UtcNow
        };

        // Act
        team.HomeMatches.Add(match);

        // Assert
        Assert.Single(team.HomeMatches);
        Assert.Equal(match, team.HomeMatches.First());
    }

    [Fact]
    public void Team_AwayMatches_NavigationProperty_Works()
    {
        // Arrange
        var team = new Team { TeamId = 1, Name = "Drum" };
        var match = new Match
        {
            MatchId = 1,
            HomeTeamId = 2,
            AwayTeamId = 1,
            MatchDate = DateTime.UtcNow
        };

        // Act
        team.AwayMatches.Add(match);

        // Assert
        Assert.Single(team.AwayMatches);
        Assert.Equal(match, team.AwayMatches.First());
    }
}
