namespace GAAStat.Services.Tests.Helpers;

/// <summary>
/// Helper class to create test Excel file
/// Run this manually to regenerate test data file
/// </summary>
public class TestDataFileCreator
{
    public static void CreateTestMatchDataFile(string filePath)
    {
        using var builder = new ExcelTestFileBuilder(filePath);

        // Match 1: Championship vs Slaughtmanus
        builder.AddMatchSheet(
            matchNumber: 1,
            competition: "Championship",
            opposition: "Slaughtmanus",
            matchDate: new DateTime(2025, 8, 15),
            homeScoreFirstHalf: "0-05",
            homeScoreSecondHalf: "1-07",
            homeScoreFullTime: "1-12",
            awayScoreFirstHalf: "0-04",
            awayScoreSecondHalf: "0-06",
            awayScoreFullTime: "0-10"
        );

        // Match 2: League vs Magilligan
        builder.AddMatchSheet(
            matchNumber: 2,
            competition: "League",
            opposition: "Magilligan",
            matchDate: new DateTime(2025, 8, 22),
            homeScoreFirstHalf: "1-03",
            homeScoreSecondHalf: "0-08",
            homeScoreFullTime: "1-11",
            awayScoreFirstHalf: "0-05",
            awayScoreSecondHalf: "1-04",
            awayScoreFullTime: "1-09"
        );

        // Match 3: Championship vs Lissan
        builder.AddMatchSheet(
            matchNumber: 3,
            competition: "Championship",
            opposition: "Lissan",
            matchDate: new DateTime(2025, 9, 5),
            homeScoreFirstHalf: "0-06",
            homeScoreSecondHalf: "2-05",
            homeScoreFullTime: "2-11",
            awayScoreFirstHalf: "1-02",
            awayScoreSecondHalf: "0-07",
            awayScoreFullTime: "1-09"
        );

        // Add a non-match sheet to test filtering
        builder.AddNonMatchSheet("Player Matrix");
        builder.AddNonMatchSheet("KPI Definitions");

        builder.Save();
    }
}
