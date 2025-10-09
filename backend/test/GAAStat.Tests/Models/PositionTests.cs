using GAAStat.Dal.Models.Application;

namespace GAAStat.Tests.Models;

public class PositionTests
{
    [Fact]
    public void Position_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var position = new Position();

        // Assert
        Assert.NotNull(position.Players);
        Assert.Empty(position.Players);
        Assert.Equal(string.Empty, position.Name);
        Assert.Equal(string.Empty, position.Code);
    }

    [Fact]
    public void Position_Properties_CanBeSet()
    {
        // Arrange
        var position = new Position
        {
            PositionId = 1,
            Name = "Goalkeeper",
            Code = "GK",
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(1, position.PositionId);
        Assert.Equal("Goalkeeper", position.Name);
        Assert.Equal("GK", position.Code);
        Assert.Equal(1, position.DisplayOrder);
    }

    [Theory]
    [InlineData("Goalkeeper", "GK", 1)]
    [InlineData("Defender", "DEF", 2)]
    [InlineData("Midfielder", "MID", 3)]
    [InlineData("Forward", "FWD", 4)]
    public void Position_ValidPositions_CanBeCreated(string name, string code, int displayOrder)
    {
        // Arrange & Act
        var position = new Position
        {
            Name = name,
            Code = code,
            DisplayOrder = displayOrder
        };

        // Assert
        Assert.Equal(name, position.Name);
        Assert.Equal(code, position.Code);
        Assert.Equal(displayOrder, position.DisplayOrder);
    }

    [Fact]
    public void Position_Players_NavigationProperty_Works()
    {
        // Arrange
        var position = new Position { PositionId = 1, Name = "Goalkeeper", Code = "GK" };
        var player = new Player
        {
            PlayerId = 1,
            JerseyNumber = 1,
            FullName = "Test Player",
            PositionId = 1
        };

        // Act
        position.Players.Add(player);

        // Assert
        Assert.Single(position.Players);
        Assert.Equal(player, position.Players.First());
    }
}
