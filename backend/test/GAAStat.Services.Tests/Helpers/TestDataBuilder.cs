using GAAStat.Services.ETL.Models;

namespace GAAStat.Services.Tests.Helpers;

/// <summary>
/// Builder for creating test data objects
/// </summary>
public static class TestDataBuilder
{
    /// <summary>
    /// Creates a valid match sheet data object for testing
    /// </summary>
    public static MatchSheetData CreateValidMatchSheetData(
        int matchNumber = 1,
        string competition = "Championship",
        string opposition = "TestOpposition",
        DateTime? matchDate = null)
    {
        matchDate ??= new DateTime(2025, 8, 15);

        var data = new MatchSheetData
        {
            SheetName = $"{matchNumber:D2}. {competition} vs {opposition} {matchDate.Value:dd.MM.yy}",
            MatchNumber = matchNumber,
            Competition = competition,
            Opposition = opposition,
            MatchDate = matchDate.Value,
            Venue = "Home",
            HomeScoreFirstHalf = "0-05",
            HomeScoreSecondHalf = "1-07",
            HomeScoreFullTime = "1-12",
            AwayScoreFirstHalf = "0-04",
            AwayScoreSecondHalf = "0-06",
            AwayScoreFullTime = "0-10"
        };

        // Add team statistics (6 records: 3 periods × 2 teams)
        var periods = new[] { "1st", "2nd", "Full" };
        var teams = new[] { "Drum", opposition };

        foreach (var period in periods)
        {
            foreach (var team in teams)
            {
                data.TeamStatistics.Add(CreateValidTeamStatistics(team, period));
            }
        }

        return data;
    }

    /// <summary>
    /// Creates valid team statistics data for testing
    /// </summary>
    public static TeamStatisticsData CreateValidTeamStatistics(string teamName, string period)
    {
        return new TeamStatisticsData
        {
            TeamName = teamName,
            Period = period,
            Scoreline = period == "Full" ? "1-12" : (period == "1st" ? "0-05" : "1-07"),
            TotalPossession = teamName == "Drum" ? 0.52m : 0.48m,
            ScoreSourceKickoutLong = 2,
            ScoreSourceKickoutShort = 1,
            ScoreSourceOppKickoutLong = 3,
            ScoreSourceOppKickoutShort = 1,
            ScoreSourceTurnover = 2,
            ScoreSourcePossessionLost = 1,
            ScoreSourceShotShort = 0,
            ScoreSourceThrowUpIn = 1,
            ShotSourceKickoutLong = 3,
            ShotSourceKickoutShort = 2,
            ShotSourceOppKickoutLong = 4,
            ShotSourceOppKickoutShort = 2,
            ShotSourceTurnover = 3,
            ShotSourcePossessionLost = 2,
            ShotSourceShotShort = 1,
            ShotSourceThrowUpIn = 2
        };
    }

    /// <summary>
    /// Creates match sheet data with invalid score format
    /// </summary>
    public static MatchSheetData CreateInvalidScoreFormatData()
    {
        var data = CreateValidMatchSheetData();
        data.HomeScoreFullTime = "INVALID"; // Invalid format
        return data;
    }

    /// <summary>
    /// Creates match sheet data with invalid possession values
    /// </summary>
    public static MatchSheetData CreateInvalidPossessionData()
    {
        var data = CreateValidMatchSheetData();
        data.TeamStatistics[0].TotalPossession = 1.5m; // Out of range
        return data;
    }

    /// <summary>
    /// Creates match sheet data with negative statistics
    /// </summary>
    public static MatchSheetData CreateNegativeStatisticsData()
    {
        var data = CreateValidMatchSheetData();
        data.TeamStatistics[0].ScoreSourceKickoutLong = -1; // Negative value
        return data;
    }

    /// <summary>
    /// Creates match sheet data with missing team statistics
    /// </summary>
    public static MatchSheetData CreateMissingTeamStatisticsData()
    {
        var data = CreateValidMatchSheetData();
        data.TeamStatistics.Clear(); // No team statistics
        return data;
    }

    /// <summary>
    /// Creates match sheet data with possession sum not equal to 1.0
    /// </summary>
    public static MatchSheetData CreatePossessionSumWarningData()
    {
        var data = CreateValidMatchSheetData();
        // Set possession to 0.55 and 0.48 (sum = 1.03, should trigger warning)
        data.TeamStatistics[0].TotalPossession = 0.55m;
        data.TeamStatistics[1].TotalPossession = 0.48m;
        return data;
    }
}
