using GAAStat.Dal.Models.Application;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Tests.Integration;

public class GAAStatDbContextTests : IDisposable
{
    private readonly GAAStat.Dal.Contexts.GAAStatDbContext _context;

    public GAAStatDbContextTests()
    {
        _context = DbContextTestHelper.CreateUniqueInMemoryContext();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task DbContext_CanConnect_Successfully()
    {
        // Act
        var canConnect = await _context.Database.CanConnectAsync();

        // Assert
        Assert.True(canConnect);
    }

    [Fact]
    public void DbContext_HasAllDbSets_Configured()
    {
        // Assert
        Assert.NotNull(_context.Seasons);
        Assert.NotNull(_context.Positions);
        Assert.NotNull(_context.Teams);
        Assert.NotNull(_context.Competitions);
        Assert.NotNull(_context.Players);
        Assert.NotNull(_context.Matches);
        Assert.NotNull(_context.MatchTeamStatistics);
        Assert.NotNull(_context.PlayerMatchStatistics);
        Assert.NotNull(_context.KpiDefinitions);
    }

    [Fact]
    public async Task Season_CanBeCreated_AndRetrieved()
    {
        // Arrange
        var season = new Season
        {
            Year = 2025,
            Name = "2025 Season",
            IsCurrent = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        _context.Seasons.Add(season);
        await _context.SaveChangesAsync();

        // Assert
        var retrievedSeason = await _context.Seasons.FirstOrDefaultAsync(s => s.Year == 2025);
        Assert.NotNull(retrievedSeason);
        Assert.Equal("2025 Season", retrievedSeason.Name);
        Assert.True(retrievedSeason.IsCurrent);
    }

    [Fact]
    public async Task Position_CanBeCreated_AndRetrieved()
    {
        // Arrange
        var position = new Position
        {
            Name = "Goalkeeper",
            Code = "GK",
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _context.Positions.Add(position);
        await _context.SaveChangesAsync();

        // Assert
        var retrievedPosition = await _context.Positions.FirstOrDefaultAsync(p => p.Code == "GK");
        Assert.NotNull(retrievedPosition);
        Assert.Equal("Goalkeeper", retrievedPosition.Name);
        Assert.Equal(1, retrievedPosition.DisplayOrder);
    }

    [Fact]
    public async Task Team_CanBeCreated_AndRetrieved()
    {
        // Arrange
        var team = new Team
        {
            Name = "Drum",
            Abbreviation = "DRM",
            IsDrum = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        _context.Teams.Add(team);
        await _context.SaveChangesAsync();

        // Assert
        var retrievedTeam = await _context.Teams.FirstOrDefaultAsync(t => t.Name == "Drum");
        Assert.NotNull(retrievedTeam);
        Assert.True(retrievedTeam.IsDrum);
        Assert.Equal("DRM", retrievedTeam.Abbreviation);
    }

    [Fact]
    public async Task Competition_WithSeason_CanBeCreated()
    {
        // Arrange
        var season = new Season { Year = 2025, Name = "2025 Season", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _context.Seasons.Add(season);
        await _context.SaveChangesAsync();

        var competition = new Competition
        {
            SeasonId = season.SeasonId,
            Name = "Championship",
            Type = "Championship",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        _context.Competitions.Add(competition);
        await _context.SaveChangesAsync();

        // Assert
        var retrievedCompetition = await _context.Competitions
            .Include(c => c.Season)
            .FirstOrDefaultAsync(c => c.Name == "Championship");

        Assert.NotNull(retrievedCompetition);
        Assert.Equal("Championship", retrievedCompetition.Type);
        Assert.NotNull(retrievedCompetition.Season);
        Assert.Equal(2025, retrievedCompetition.Season.Year);
    }

    [Fact]
    public async Task Player_WithPosition_CanBeCreated()
    {
        // Arrange
        var position = new Position { Name = "Goalkeeper", Code = "GK", DisplayOrder = 1, CreatedAt = DateTime.UtcNow };
        _context.Positions.Add(position);
        await _context.SaveChangesAsync();

        var player = new Player
        {
            JerseyNumber = 1,
            FirstName = "Cahair",
            LastName = "O Kane",
            FullName = "Cahair O Kane",
            PositionId = position.PositionId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        _context.Players.Add(player);
        await _context.SaveChangesAsync();

        // Assert
        var retrievedPlayer = await _context.Players
            .Include(p => p.Position)
            .FirstOrDefaultAsync(p => p.JerseyNumber == 1);

        Assert.NotNull(retrievedPlayer);
        Assert.Equal("Cahair O Kane", retrievedPlayer.FullName);
        Assert.NotNull(retrievedPlayer.Position);
        Assert.Equal("GK", retrievedPlayer.Position.Code);
    }

    [Fact]
    public async Task Match_WithAllRelationships_CanBeCreated()
    {
        // Arrange - Create dependencies
        var season = new Season { Year = 2025, Name = "2025 Season", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var homeTeam = new Team { Name = "Drum", IsDrum = true, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var awayTeam = new Team { Name = "Slaughtmanus", IsDrum = false, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        _context.Seasons.Add(season);
        _context.Teams.AddRange(homeTeam, awayTeam);
        await _context.SaveChangesAsync();

        var competition = new Competition
        {
            SeasonId = season.SeasonId,
            Name = "Championship",
            Type = "Championship",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Competitions.Add(competition);
        await _context.SaveChangesAsync();

        var match = new Match
        {
            CompetitionId = competition.CompetitionId,
            MatchNumber = 9,
            HomeTeamId = homeTeam.TeamId,
            AwayTeamId = awayTeam.TeamId,
            MatchDate = new DateTime(2025, 9, 26),
            Venue = "Home",
            HomeScoreFullTime = "1-11",
            AwayScoreFullTime = "0-16",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        _context.Matches.Add(match);
        await _context.SaveChangesAsync();

        // Assert
        var retrievedMatch = await _context.Matches
            .Include(m => m.Competition)
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .FirstOrDefaultAsync(m => m.MatchNumber == 9);

        Assert.NotNull(retrievedMatch);
        Assert.Equal("Home", retrievedMatch.Venue);
        Assert.Equal("1-11", retrievedMatch.HomeScoreFullTime);
        Assert.NotNull(retrievedMatch.HomeTeam);
        Assert.Equal("Drum", retrievedMatch.HomeTeam.Name);
        Assert.NotNull(retrievedMatch.AwayTeam);
        Assert.Equal("Slaughtmanus", retrievedMatch.AwayTeam.Name);
    }

    [Fact]
    public async Task MatchTeamStatistics_CanBeCreated()
    {
        // Arrange - Create dependencies
        var season = new Season { Year = 2025, Name = "2025 Season", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var team = new Team { Name = "Drum", IsDrum = true, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _context.Seasons.Add(season);
        _context.Teams.Add(team);
        await _context.SaveChangesAsync();

        var competition = new Competition
        {
            SeasonId = season.SeasonId,
            Name = "Championship",
            Type = "Championship",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Competitions.Add(competition);
        await _context.SaveChangesAsync();

        var match = new Match
        {
            CompetitionId = competition.CompetitionId,
            MatchNumber = 1,
            HomeTeamId = team.TeamId,
            AwayTeamId = team.TeamId,
            MatchDate = DateTime.UtcNow,
            Venue = "Home",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Matches.Add(match);
        await _context.SaveChangesAsync();

        var teamStats = new MatchTeamStatistics
        {
            MatchId = match.MatchId,
            TeamId = team.TeamId,
            Period = "Full",
            Scoreline = "1-11",
            TotalPossession = 0.55m,
            ScoreSourceKickoutLong = 2,
            ScoreSourceTurnover = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        _context.MatchTeamStatistics.Add(teamStats);
        await _context.SaveChangesAsync();

        // Assert
        var retrievedStats = await _context.MatchTeamStatistics
            .Include(s => s.Match)
            .Include(s => s.Team)
            .FirstOrDefaultAsync(s => s.Period == "Full");

        Assert.NotNull(retrievedStats);
        Assert.Equal(0.55m, retrievedStats.TotalPossession);
        Assert.Equal(2, retrievedStats.ScoreSourceKickoutLong);
        Assert.NotNull(retrievedStats.Team);
    }

    [Fact]
    public async Task PlayerMatchStatistics_WithAllFields_CanBeCreated()
    {
        // Arrange - Create dependencies
        var season = new Season { Year = 2025, Name = "2025 Season", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var position = new Position { Name = "Forward", Code = "FWD", DisplayOrder = 4, CreatedAt = DateTime.UtcNow };
        var team = new Team { Name = "Drum", IsDrum = true, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        _context.Seasons.Add(season);
        _context.Positions.Add(position);
        _context.Teams.Add(team);
        await _context.SaveChangesAsync();

        var player = new Player
        {
            JerseyNumber = 10,
            FirstName = "Test",
            LastName = "Player",
            FullName = "Test Player",
            PositionId = position.PositionId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Players.Add(player);
        await _context.SaveChangesAsync();

        var competition = new Competition
        {
            SeasonId = season.SeasonId,
            Name = "Championship",
            Type = "Championship",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Competitions.Add(competition);
        await _context.SaveChangesAsync();

        var match = new Match
        {
            CompetitionId = competition.CompetitionId,
            MatchNumber = 1,
            HomeTeamId = team.TeamId,
            AwayTeamId = team.TeamId,
            MatchDate = DateTime.UtcNow,
            Venue = "Home",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Matches.Add(match);
        await _context.SaveChangesAsync();

        var playerStats = new PlayerMatchStatistics
        {
            MatchId = match.MatchId,
            PlayerId = player.PlayerId,
            MinutesPlayed = 63,
            TotalEngagements = 25,
            Psr = 13,
            ShotsPlayTotal = 7,
            ShotsPlayPoints = 4,
            ShotsPlayGoals = 1,
            TotalShots = 10,
            AssistsTotal = 2,
            TacklesTotal = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        _context.PlayerMatchStatistics.Add(playerStats);
        await _context.SaveChangesAsync();

        // Assert
        var retrievedStats = await _context.PlayerMatchStatistics
            .Include(s => s.Player)
            .Include(s => s.Match)
            .FirstOrDefaultAsync(s => s.MinutesPlayed == 63);

        Assert.NotNull(retrievedStats);
        Assert.Equal(63, retrievedStats.MinutesPlayed);
        Assert.Equal(7, retrievedStats.ShotsPlayTotal);
        Assert.NotNull(retrievedStats.Player);
        Assert.Equal("Test Player", retrievedStats.Player.FullName);
    }

    [Fact]
    public async Task KpiDefinition_CanBeCreated_AndRetrieved()
    {
        // Arrange
        var kpi = new KpiDefinition
        {
            EventNumber = 1,
            EventName = "Kickout",
            Outcome = "Won clean",
            TeamAssignment = "Home",
            PsrValue = 1.0m,
            Definition = "A kickout won clean in the air by a home player",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _context.KpiDefinitions.Add(kpi);
        await _context.SaveChangesAsync();

        // Assert
        var retrievedKpi = await _context.KpiDefinitions
            .FirstOrDefaultAsync(k => k.EventName == "Kickout");

        Assert.NotNull(retrievedKpi);
        Assert.Equal("Won clean", retrievedKpi.Outcome);
        Assert.Equal(1.0m, retrievedKpi.PsrValue);
    }

    [Fact]
    public async Task MultipleEntities_CanBeQueried_WithComplexIncludes()
    {
        // Arrange - Create full hierarchy
        var season = new Season { Year = 2025, Name = "2025 Season", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var position = new Position { Name = "Forward", Code = "FWD", DisplayOrder = 4, CreatedAt = DateTime.UtcNow };
        var drumTeam = new Team { Name = "Drum", IsDrum = true, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var oppTeam = new Team { Name = "Opposition", IsDrum = false, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        _context.Seasons.Add(season);
        _context.Positions.Add(position);
        _context.Teams.AddRange(drumTeam, oppTeam);
        await _context.SaveChangesAsync();

        var competition = new Competition
        {
            SeasonId = season.SeasonId,
            Name = "Championship",
            Type = "Championship",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Competitions.Add(competition);
        await _context.SaveChangesAsync();

        var player = new Player
        {
            JerseyNumber = 10,
            FullName = "Test Player",
            FirstName = "Test",
            LastName = "Player",
            PositionId = position.PositionId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Players.Add(player);
        await _context.SaveChangesAsync();

        var match = new Match
        {
            CompetitionId = competition.CompetitionId,
            MatchNumber = 1,
            HomeTeamId = drumTeam.TeamId,
            AwayTeamId = oppTeam.TeamId,
            MatchDate = DateTime.UtcNow,
            Venue = "Home",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Matches.Add(match);
        await _context.SaveChangesAsync();

        // Act - Complex query with multiple includes
        var result = await _context.Matches
            .Include(m => m.Competition)
                .ThenInclude(c => c.Season)
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .FirstOrDefaultAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Competition);
        Assert.NotNull(result.Competition.Season);
        Assert.NotNull(result.HomeTeam);
        Assert.NotNull(result.AwayTeam);
        Assert.Equal(2025, result.Competition.Season.Year);
        Assert.True(result.HomeTeam.IsDrum);
        Assert.False(result.AwayTeam.IsDrum);
    }
}
