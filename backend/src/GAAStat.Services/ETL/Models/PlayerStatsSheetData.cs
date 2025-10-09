using System;
using System.Collections.Generic;

namespace GAAStat.Services.ETL.Models;

/// <summary>
/// Represents data extracted from a single player stats sheet.
/// Contains metadata and list of player statistics.
/// </summary>
public class PlayerStatsSheetData
{
    /// <summary>
    /// Excel sheet name
    /// </summary>
    public string SheetName { get; set; } = string.Empty;

    /// <summary>
    /// Match number (from sheet name)
    /// </summary>
    public int MatchNumber { get; set; }

    /// <summary>
    /// Opposition team name
    /// </summary>
    public string Opposition { get; set; } = string.Empty;

    /// <summary>
    /// Match date
    /// </summary>
    public DateTime MatchDate { get; set; }

    /// <summary>
    /// Field map: abbreviation → column index
    /// </summary>
    public Dictionary<string, int> FieldMap { get; set; } = new();

    /// <summary>
    /// List of player statistics extracted from this sheet
    /// </summary>
    public List<PlayerStatisticsData> Players { get; set; } = new();
}
