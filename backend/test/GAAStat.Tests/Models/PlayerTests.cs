using GAAStat.Dal.Models.Application;

namespace GAAStat.Tests.Models;

public class PlayerTests
{
    [Fact]
    public void Player_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var player = new Player();

        // Assert
        Assert.NotNull(player.MatchStatistics);
        Assert.Empty(player.MatchStatistics);
        Assert.Equal(string.Empty, player.FirstName);
        Assert.Equal(string.Empty, player.LastName);
        Assert.Equal(string.Empty, player.FullName);
        Assert.False(player.IsActive);
    }

    [Fact]
    public void Player_Properties_CanBeSet()
    {
        // Arrange
        var player = new Player
        {
            PlayerId = 1,
            JerseyNumber = 10,
            FirstName = "Cahair",
            LastName = "O Kane",
            FullName = "Cahair O Kane",
            PositionId = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(1, player.PlayerId);
        Assert.Equal(10, player.JerseyNumber);
        Assert.Equal("Cahair", player.FirstName);
        Assert.Equal("O Kane", player.LastName);
        Assert.Equal("Cahair O Kane", player.FullName);
        Assert.Equal(1, player.PositionId);
        Assert.True(player.IsActive);
    }

    [Theory]
    [InlineData(1, "Cahair", "O Kane")]
    [InlineData(2, "Seamus", "O Kane")]
    [InlineData(3, "Alex", "Moore")]
    public void Player_ValidJerseyNumbers_CanBeAssigned(int jerseyNumber, string firstName, string lastName)
    {
        // Arrange & Act
        var player = new Player
        {
            JerseyNumber = jerseyNumber,
            FirstName = firstName,
            LastName = lastName,
            FullName = $"{firstName} {lastName}"
        };

        // Assert
        Assert.Equal(jerseyNumber, player.JerseyNumber);
        Assert.Equal($"{firstName} {lastName}", player.FullName);
    }

    [Fact]
    public void Player_Position_NavigationProperty_CanBeSet()
    {
        // Arrange
        var position = new Position { PositionId = 1, Name = "Goalkeeper", Code = "GK" };
        var player = new Player
        {
            PlayerId = 1,
            JerseyNumber = 1,
            FullName = "Test Player",
            PositionId = 1,
            Position = position
        };

        // Assert
        Assert.NotNull(player.Position);
        Assert.Equal("Goalkeeper", player.Position.Name);
        Assert.Equal("GK", player.Position.Code);
    }

    [Fact]
    public void Player_MatchStatistics_NavigationProperty_Works()
    {
        // Arrange
        var player = new Player { PlayerId = 1, JerseyNumber = 1, FullName = "Test Player" };
        var stats = new PlayerMatchStatistics
        {
            PlayerMatchStatId = 1,
            PlayerId = 1,
            MatchId = 1,
            MinutesPlayed = 63
        };

        // Act
        player.MatchStatistics.Add(stats);

        // Assert
        Assert.Single(player.MatchStatistics);
        Assert.Equal(63, player.MatchStatistics.First().MinutesPlayed);
    }
}
