using GAAStat.Dal.Models.Application;

namespace GAAStat.Tests.Models;

public class PlayerMatchStatisticsTests
{
    [Fact]
    public void PlayerMatchStatistics_DefaultValues_AreZero()
    {
        // Arrange & Act
        var stats = new PlayerMatchStatistics();

        // Assert - Summary Statistics
        Assert.Equal(0, stats.MinutesPlayed);
        Assert.Equal(0, stats.TotalEngagements);
        Assert.Equal(0, stats.Psr);

        // Assert - Possession Play
        Assert.Equal(0, stats.Tp);
        Assert.Equal(0, stats.Tow);
        Assert.Equal(0, stats.Interceptions);

        // Assert - Shots from Play
        Assert.Equal(0, stats.ShotsPlayTotal);
        Assert.Equal(0, stats.ShotsPlayPoints);
        Assert.Equal(0, stats.ShotsPlayGoals);

        // Assert - Goalkeeper Stats
        Assert.Equal(0, stats.GkTotalKickouts);
        Assert.Equal(0, stats.GkSaves);
    }

    [Fact]
    public void PlayerMatchStatistics_AllProperties_CanBeSet()
    {
        // Arrange & Act
        var stats = new PlayerMatchStatistics
        {
            PlayerMatchStatId = 1,
            MatchId = 1,
            PlayerId = 1,
            MinutesPlayed = 63,
            TotalEngagements = 25,
            TePerPsr = 0.52m,
            Scores = "1-03(1f)",
            Psr = 13,
            PsrPerTp = 13.0m
        };

        // Assert
        Assert.Equal(1, stats.PlayerMatchStatId);
        Assert.Equal(63, stats.MinutesPlayed);
        Assert.Equal(25, stats.TotalEngagements);
        Assert.Equal(0.52m, stats.TePerPsr);
        Assert.Equal("1-03(1f)", stats.Scores);
    }

    [Fact]
    public void PlayerMatchStatistics_PossessionPlay_PropertiesWork()
    {
        // Arrange & Act
        var stats = new PlayerMatchStatistics
        {
            Tp = 15,
            Tow = 3,
            Interceptions = 2,
            Tpl = 1,
            Kp = 5,
            Hp = 8,
            Ha = 10,
            Turnovers = 2,
            Ineffective = 1
        };

        // Assert
        Assert.Equal(15, stats.Tp);
        Assert.Equal(3, stats.Tow);
        Assert.Equal(2, stats.Interceptions);
        Assert.Equal(5, stats.Kp);
        Assert.Equal(8, stats.Hp);
    }

    [Fact]
    public void PlayerMatchStatistics_KickoutAnalysis_DrumStats_Work()
    {
        // Arrange & Act
        var stats = new PlayerMatchStatistics
        {
            KoDrumKow = 5,
            KoDrumWc = 3,
            KoDrumBw = 1,
            KoDrumSw = 1
        };

        // Assert
        Assert.Equal(5, stats.KoDrumKow);
        Assert.Equal(3, stats.KoDrumWc);
        Assert.Equal(1, stats.KoDrumBw);
        Assert.Equal(1, stats.KoDrumSw);
    }

    [Fact]
    public void PlayerMatchStatistics_KickoutAnalysis_OppositionStats_Work()
    {
        // Arrange & Act
        var stats = new PlayerMatchStatistics
        {
            KoOppKow = 4,
            KoOppWc = 2,
            KoOppBw = 1,
            KoOppSw = 1
        };

        // Assert
        Assert.Equal(4, stats.KoOppKow);
        Assert.Equal(2, stats.KoOppWc);
    }

    [Fact]
    public void PlayerMatchStatistics_ShotsFromPlay_AllFields_Work()
    {
        // Arrange & Act
        var stats = new PlayerMatchStatistics
        {
            ShotsPlayTotal = 10,
            ShotsPlayPoints = 4,
            ShotsPlay2Points = 2,
            ShotsPlayGoals = 1,
            ShotsPlayWide = 2,
            ShotsPlayShort = 0,
            ShotsPlaySave = 1,
            ShotsPlayWoodwork = 0,
            ShotsPlayBlocked = 0,
            ShotsPlay45 = 0,
            ShotsPlayPercentage = 0.7m
        };

        // Assert
        Assert.Equal(10, stats.ShotsPlayTotal);
        Assert.Equal(4, stats.ShotsPlayPoints);
        Assert.Equal(2, stats.ShotsPlay2Points);
        Assert.Equal(1, stats.ShotsPlayGoals);
        Assert.Equal(0.7m, stats.ShotsPlayPercentage);
    }

    [Fact]
    public void PlayerMatchStatistics_ScoreableFrees_AllFields_Work()
    {
        // Arrange & Act
        var stats = new PlayerMatchStatistics
        {
            FreesTotal = 5,
            FreesPoints = 3,
            Frees2Points = 0,
            FreesGoals = 0,
            FreesWide = 2,
            FreesShort = 0,
            FreesSave = 0,
            FreesWoodwork = 0,
            Frees45 = 0,
            FreesQf = 1,
            FreesPercentage = 0.6m
        };

        // Assert
        Assert.Equal(5, stats.FreesTotal);
        Assert.Equal(3, stats.FreesPoints);
        Assert.Equal(2, stats.FreesWide);
        Assert.Equal(0.6m, stats.FreesPercentage);
    }

    [Fact]
    public void PlayerMatchStatistics_TotalShots_Works()
    {
        // Arrange & Act
        var stats = new PlayerMatchStatistics
        {
            TotalShots = 15,
            TotalShotsPercentage = 0.6667m
        };

        // Assert
        Assert.Equal(15, stats.TotalShots);
        Assert.Equal(0.6667m, stats.TotalShotsPercentage);
    }

    [Fact]
    public void PlayerMatchStatistics_Assists_AllFields_Work()
    {
        // Arrange & Act
        var stats = new PlayerMatchStatistics
        {
            AssistsTotal = 3,
            AssistsPoint = 2,
            AssistsGoal = 1
        };

        // Assert
        Assert.Equal(3, stats.AssistsTotal);
        Assert.Equal(2, stats.AssistsPoint);
        Assert.Equal(1, stats.AssistsGoal);
    }

    [Fact]
    public void PlayerMatchStatistics_Tackles_AllFields_Work()
    {
        // Arrange & Act
        var stats = new PlayerMatchStatistics
        {
            TacklesTotal = 10,
            TacklesContested = 8,
            TacklesMissed = 2,
            TacklesPercentage = 0.8m
        };

        // Assert
        Assert.Equal(10, stats.TacklesTotal);
        Assert.Equal(8, stats.TacklesContested);
        Assert.Equal(2, stats.TacklesMissed);
        Assert.Equal(0.8m, stats.TacklesPercentage);
    }

    [Fact]
    public void PlayerMatchStatistics_FreesConceded_AllFields_Work()
    {
        // Arrange & Act
        var stats = new PlayerMatchStatistics
        {
            FreesConcededTotal = 4,
            FreesConcededAttack = 1,
            FreesConcededMidfield = 2,
            FreesConcededDefense = 1,
            FreesConcededPenalty = 0
        };

        // Assert
        Assert.Equal(4, stats.FreesConcededTotal);
        Assert.Equal(1, stats.FreesConcededAttack);
        Assert.Equal(2, stats.FreesConcededMidfield);
    }

    [Fact]
    public void PlayerMatchStatistics_Frees50m_AllFields_Work()
    {
        // Arrange & Act
        var stats = new PlayerMatchStatistics
        {
            Frees50MTotal = 2,
            Frees50MDelay = 1,
            Frees50MDissent = 1,
            Frees50M3V3 = 0
        };

        // Assert
        Assert.Equal(2, stats.Frees50MTotal);
        Assert.Equal(1, stats.Frees50MDelay);
        Assert.Equal(1, stats.Frees50MDissent);
    }

    [Fact]
    public void PlayerMatchStatistics_Bookings_AllFields_Work()
    {
        // Arrange & Act
        var stats = new PlayerMatchStatistics
        {
            YellowCards = 1,
            BlackCards = 0,
            RedCards = 0
        };

        // Assert
        Assert.Equal(1, stats.YellowCards);
        Assert.Equal(0, stats.BlackCards);
        Assert.Equal(0, stats.RedCards);
    }

    [Fact]
    public void PlayerMatchStatistics_ThrowUp_Works()
    {
        // Arrange & Act
        var stats = new PlayerMatchStatistics
        {
            ThrowUpWon = 5,
            ThrowUpLost = 2
        };

        // Assert
        Assert.Equal(5, stats.ThrowUpWon);
        Assert.Equal(2, stats.ThrowUpLost);
    }

    [Fact]
    public void PlayerMatchStatistics_GoalkeeperStats_AllFields_Work()
    {
        // Arrange & Act
        var stats = new PlayerMatchStatistics
        {
            GkTotalKickouts = 24,
            GkKickoutRetained = 18,
            GkKickoutLost = 6,
            GkKickoutPercentage = 0.75m,
            GkSaves = 3
        };

        // Assert
        Assert.Equal(24, stats.GkTotalKickouts);
        Assert.Equal(18, stats.GkKickoutRetained);
        Assert.Equal(6, stats.GkKickoutLost);
        Assert.Equal(0.75m, stats.GkKickoutPercentage);
        Assert.Equal(3, stats.GkSaves);
    }

    [Fact]
    public void PlayerMatchStatistics_Scores_GAANotation_IsStored()
    {
        // Arrange & Act
        var stats = new PlayerMatchStatistics
        {
            Scores = "2-05(2f)" // 2 goals, 5 points, 2 from frees
        };

        // Assert
        Assert.Equal("2-05(2f)", stats.Scores);
    }

    [Fact]
    public void PlayerMatchStatistics_NullableFields_CanBeNull()
    {
        // Arrange & Act
        var stats = new PlayerMatchStatistics();

        // Assert
        Assert.Null(stats.Scores);
        Assert.Null(stats.TePerPsr);
        Assert.Null(stats.PsrPerTp);
        Assert.Null(stats.ShotsPlayPercentage);
        Assert.Null(stats.FreesPercentage);
        Assert.Null(stats.TacklesPercentage);
    }
}
