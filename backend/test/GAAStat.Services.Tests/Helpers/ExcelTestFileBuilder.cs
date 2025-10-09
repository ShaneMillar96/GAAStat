using OfficeOpenXml;

namespace GAAStat.Services.Tests.Helpers;

/// <summary>
/// Helper class for creating test Excel files
/// </summary>
public class ExcelTestFileBuilder : IDisposable
{
    private readonly ExcelPackage _package;
    private readonly string _filePath;

    public ExcelTestFileBuilder(string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        _filePath = filePath;
        _package = new ExcelPackage();
    }

    /// <summary>
    /// Adds a match sheet with standard structure
    /// </summary>
    public ExcelTestFileBuilder AddMatchSheet(
        int matchNumber,
        string competition,
        string opposition,
        DateTime matchDate,
        string homeScoreFirstHalf = "0-05",
        string homeScoreSecondHalf = "1-07",
        string homeScoreFullTime = "1-12",
        string awayScoreFirstHalf = "0-04",
        string awayScoreSecondHalf = "0-06",
        string awayScoreFullTime = "0-10")
    {
        var sheetName = $"{matchNumber:D2}. {competition} vs {opposition} {matchDate:dd.MM.yy}";
        var worksheet = _package.Workbook.Worksheets.Add(sheetName);

        // Row 1: Title
        worksheet.Cells[1, 1].Value = sheetName;

        // Row 2: Team names
        worksheet.Cells[2, 2].Value = "Drum";
        worksheet.Cells[2, 5].Value = opposition;

        // Row 3: Period headers
        worksheet.Cells[3, 2].Value = "1st";
        worksheet.Cells[3, 3].Value = "2nd";
        worksheet.Cells[3, 4].Value = "Full";
        worksheet.Cells[3, 5].Value = "1st";
        worksheet.Cells[3, 6].Value = "2nd";
        worksheet.Cells[3, 7].Value = "Full";

        // Row 4: Scores
        worksheet.Cells[4, 2].Value = homeScoreFirstHalf;
        worksheet.Cells[4, 3].Value = homeScoreSecondHalf;
        worksheet.Cells[4, 4].Value = homeScoreFullTime;
        worksheet.Cells[4, 5].Value = awayScoreFirstHalf;
        worksheet.Cells[4, 6].Value = awayScoreSecondHalf;
        worksheet.Cells[4, 7].Value = awayScoreFullTime;

        // Row 5: Total Possession
        worksheet.Cells[5, 2].Value = 0.52; // Drum 1st
        worksheet.Cells[5, 3].Value = 0.51; // Drum 2nd
        worksheet.Cells[5, 4].Value = 0.515; // Drum Full
        worksheet.Cells[5, 5].Value = 0.48; // Away 1st
        worksheet.Cells[5, 6].Value = 0.49; // Away 2nd
        worksheet.Cells[5, 7].Value = 0.485; // Away Full

        // Rows 7-14: Score sources
        AddStatisticsRow(worksheet, 7, 2, 1, 3, 2); // Kickout Long
        AddStatisticsRow(worksheet, 8, 1, 2, 1, 2); // Kickout Short
        AddStatisticsRow(worksheet, 9, 3, 2, 1, 3); // Opp Kickout Long
        AddStatisticsRow(worksheet, 10, 1, 1, 2, 1); // Opp Kickout Short
        AddStatisticsRow(worksheet, 11, 2, 3, 1, 2); // Turnover
        AddStatisticsRow(worksheet, 12, 1, 1, 1, 1); // Possession Lost
        AddStatisticsRow(worksheet, 13, 0, 0, 0, 0); // Shot Short
        AddStatisticsRow(worksheet, 14, 1, 2, 1, 1); // Throw Up/In

        // Rows 16-23: Shot sources
        AddStatisticsRow(worksheet, 16, 3, 4, 2, 3); // Kickout Long
        AddStatisticsRow(worksheet, 17, 2, 3, 1, 2); // Kickout Short
        AddStatisticsRow(worksheet, 18, 4, 5, 3, 4); // Opp Kickout Long
        AddStatisticsRow(worksheet, 19, 2, 2, 2, 2); // Opp Kickout Short
        AddStatisticsRow(worksheet, 20, 3, 4, 2, 3); // Turnover
        AddStatisticsRow(worksheet, 21, 2, 2, 1, 2); // Possession Lost
        AddStatisticsRow(worksheet, 22, 1, 1, 0, 1); // Shot Short
        AddStatisticsRow(worksheet, 23, 2, 3, 1, 2); // Throw Up/In

        return this;
    }

    /// <summary>
    /// Adds a statistics row with 6 values (3 periods × 2 teams)
    /// </summary>
    private void AddStatisticsRow(ExcelWorksheet worksheet, int row, int drumFirst, int drumSecond, int awayFirst, int awaySecond)
    {
        worksheet.Cells[row, 2].Value = drumFirst;
        worksheet.Cells[row, 3].Value = drumSecond;
        worksheet.Cells[row, 4].Value = drumFirst + drumSecond; // Full
        worksheet.Cells[row, 5].Value = awayFirst;
        worksheet.Cells[row, 6].Value = awaySecond;
        worksheet.Cells[row, 7].Value = awayFirst + awaySecond; // Full
    }

    /// <summary>
    /// Adds a non-match sheet (should be ignored by ETL)
    /// </summary>
    public ExcelTestFileBuilder AddNonMatchSheet(string sheetName)
    {
        var worksheet = _package.Workbook.Worksheets.Add(sheetName);
        worksheet.Cells[1, 1].Value = "This is not a match sheet";
        return this;
    }

    /// <summary>
    /// Saves the Excel file
    /// </summary>
    public void Save()
    {
        var fileInfo = new FileInfo(_filePath);
        if (fileInfo.Directory != null && !fileInfo.Directory.Exists)
        {
            fileInfo.Directory.Create();
        }

        _package.SaveAs(fileInfo);
    }

    public void Dispose()
    {
        _package?.Dispose();
    }
}
