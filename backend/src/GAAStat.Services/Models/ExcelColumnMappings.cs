namespace GAAStat.Services.Models;

/// <summary>
/// Excel column mappings for GAA match player statistics
/// Based on the 85-column Excel structure from Drum Analysis 2025.xlsx
/// </summary>
public static class ExcelColumnMappings
{
    /// <summary>
    /// Column indices for player identification (0-based)
    /// </summary>
    public static class PlayerInfo
    {
        public const int PLAYER_NAME = 0;        // Column A - Player
        public const int JERSEY_NUMBER = 1;      // Column B - Min
        public const int TOTAL_ENGAGEMENTS = 2;  // Column C - TE  
    }
    
    /// <summary>
    /// Column indices for possession statistics
    /// </summary>
    public static class Possession
    {
        public const int TOTAL_POSSESSIONS = 3;         // Column D
        public const int TURNOVERS_WON = 4;             // Column E
        public const int INTERCEPTIONS = 5;             // Column F
        public const int POSSESSION_SUCCESS_RATE = 6;    // Column G
        public const int POSSESSIONS_PER_TE = 7;        // Column H
    }
    
    /// <summary>
    /// Column indices for attacking statistics
    /// </summary>
    public static class Attacking
    {
        public const int TOTAL_ATTACKS = 8;         // Column I
        public const int KICK_RETAINED = 9;         // Column J
        public const int KICK_LOST = 10;            // Column K
        public const int CARRY_RETAINED = 11;       // Column L
        public const int CARRY_LOST = 12;           // Column M
    }
    
    /// <summary>
    /// Column indices for shooting statistics
    /// </summary>
    public static class Shooting
    {
        public const int SHOTS_TOTAL = 13;          // Column N
        public const int GOALS = 14;                // Column O
        public const int POINTS = 15;               // Column P
        public const int WIDES = 16;                // Column Q
        public const int CONVERSION_RATE = 17;      // Column R
        public const int SCORES = 18;               // Column S - Combined score display
    }
    
    /// <summary>
    /// Column indices for defensive statistics
    /// </summary>
    public static class Defensive
    {
        public const int TACKLES_TOTAL = 19;        // Column T
        public const int TACKLES_CONTACT = 20;      // Column U
        public const int TACKLES_MISSED = 21;       // Column V
        public const int TACKLE_PERCENTAGE = 22;    // Column W
        public const int FREES_CONCEDED_TOTAL = 23; // Column X
    }
    
    /// <summary>
    /// Column indices for disciplinary statistics
    /// </summary>
    public static class Disciplinary
    {
        public const int YELLOW_CARDS = 24;         // Column Y
        public const int BLACK_CARDS = 25;          // Column Z
        public const int RED_CARDS = 26;            // Column AA
    }
    
    /// <summary>
    /// Column indices for goalkeeper statistics
    /// </summary>
    public static class Goalkeeper
    {
        public const int KICKOUTS_TOTAL = 27;       // Column AB
        public const int KICKOUTS_RETAINED = 28;    // Column AC
        public const int KICKOUTS_LOST = 29;        // Column AD
        public const int KICKOUT_PERCENTAGE = 30;   // Column AE
        public const int SAVES = 31;                // Column AF
    }
    
    /// <summary>
    /// Expected Excel column headers for validation
    /// </summary>
    public static readonly Dictionary<int, string> ExpectedHeaders = new()
    {
        { PlayerInfo.PLAYER_NAME, "Player" },
        { PlayerInfo.JERSEY_NUMBER, "Min" },
        { PlayerInfo.TOTAL_ENGAGEMENTS, "TE" },
        { Possession.TOTAL_POSSESSIONS, "Poss" },
        { Possession.TURNOVERS_WON, "TO Won" },
        { Possession.INTERCEPTIONS, "Int" },
        { Possession.POSSESSION_SUCCESS_RATE, "Poss %" },
        { Possession.POSSESSIONS_PER_TE, "Poss/TE" },
        { Attacking.TOTAL_ATTACKS, "Att" },
        { Attacking.KICK_RETAINED, "KR" },
        { Attacking.KICK_LOST, "KL" },
        { Attacking.CARRY_RETAINED, "CR" },
        { Attacking.CARRY_LOST, "CL" },
        { Shooting.SHOTS_TOTAL, "Shots" },
        { Shooting.GOALS, "Goals" },
        { Shooting.POINTS, "Points" },
        { Shooting.WIDES, "Wides" },
        { Shooting.CONVERSION_RATE, "Conv %" },
        { Shooting.SCORES, "Scores" },
        { Defensive.TACKLES_TOTAL, "Tack" },
        { Defensive.TACKLES_CONTACT, "TC" },
        { Defensive.TACKLES_MISSED, "TM" },
        { Defensive.TACKLE_PERCENTAGE, "Tack %" },
        { Defensive.FREES_CONCEDED_TOTAL, "FC" },
        { Disciplinary.YELLOW_CARDS, "YC" },
        { Disciplinary.BLACK_CARDS, "BC" },
        { Disciplinary.RED_CARDS, "RC" },
        { Goalkeeper.KICKOUTS_TOTAL, "KO" },
        { Goalkeeper.KICKOUTS_RETAINED, "KOR" },
        { Goalkeeper.KICKOUTS_LOST, "KOL" },
        { Goalkeeper.KICKOUT_PERCENTAGE, "KO %" },
        { Goalkeeper.SAVES, "Saves" }
    };
    
    /// <summary>
    /// Total number of expected columns in Excel sheet
    /// </summary>
    public const int TOTAL_COLUMNS = 85;
    
    /// <summary>
    /// Minimum required columns for valid player statistics
    /// </summary>
    public const int MIN_REQUIRED_COLUMNS = 32;
    
    /// <summary>
    /// Expected row where player statistics begin (0-based)
    /// </summary>
    public const int PLAYER_STATS_START_ROW = 4; // Row 5 in Excel (1-based)
    
    /// <summary>
    /// Expected row where headers are located (0-based)
    /// </summary>
    public const int HEADER_ROW = 3; // Row 4 in Excel (1-based)
    
    /// <summary>
    /// Maximum number of players expected in a match
    /// </summary>
    public const int MAX_PLAYERS_PER_MATCH = 30;
    
    /// <summary>
    /// Validates if a column index represents a required field
    /// </summary>
    public static bool IsRequiredColumn(int columnIndex)
    {
        return columnIndex <= MIN_REQUIRED_COLUMNS;
    }
    
    /// <summary>
    /// Gets the expected header name for a column index
    /// </summary>
    public static string? GetExpectedHeader(int columnIndex)
    {
        return ExpectedHeaders.GetValueOrDefault(columnIndex);
    }
    
    /// <summary>
    /// Validates if a row appears to contain valid player data
    /// </summary>
    public static bool IsValidPlayerDataRow(object?[] rowData)
    {
        if (rowData.Length < MIN_REQUIRED_COLUMNS)
            return false;
            
        // Must have player name
        if (string.IsNullOrWhiteSpace(rowData[PlayerInfo.PLAYER_NAME]?.ToString()))
            return false;
            
        // Must have some statistics (not all zeros)
        var hasStats = rowData.Skip(2).Take(10).Any(cell => 
        {
            if (int.TryParse(cell?.ToString(), out var value))
                return value > 0;
            return false;
        });
        
        return hasStats;
    }
}