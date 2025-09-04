namespace GAAStat.Services.Models;

/// <summary>
/// Excel column mappings for GAA match player statistics
/// Based on the 85-column Excel structure from Drum Analysis 2025.xlsx
/// </summary>
public static class ExcelColumnMappings
{
    /// <summary>
    /// Invalid player name patterns that should be excluded from processing
    /// These represent template placeholders and summary rows, not actual players
    /// </summary>
    public static class InvalidPlayerPatterns
    {
        public static readonly string[] EXCLUDED_NAMES = 
        {
            "Team Average",
            "Player 18",
            "Player 19",
            "Player 20",
            "Player 21",
            "Player 22",
            "Player 23",
            "Player 24",
            "Player 25"
        };
        
        public const string TEAM_AVERAGE = "Team Average";
        public const string PLAYER_PLACEHOLDER_PATTERN = "Player ";
    }
    
    /// <summary>
    /// Column indices for player identification (0-based)
    /// </summary>
    public static class PlayerInfo
    {
        public const int JERSEY_NUMBER = 0;      // Column A - # (Jersey Number)
        public const int PLAYER_NAME = 1;        // Column B - Player Name
        public const int MINUTES_PLAYED = 2;     // Column C - Min (Minutes Played)
        public const int TOTAL_ENGAGEMENTS = 3;  // Column D - TE (Total Engagements)
    }
    
    /// <summary>
    /// Column indices for possession statistics
    /// </summary>
    public static class Possession
    {
        public const int TE_PSR_RATIO = 4;              // Column E - TE/PSR
        public const int SCORES = 5;                    // Column F - Scores (like "0-03(2f)", "2-00")
        public const int POSSESSION_SUCCESS_RATE = 6;   // Column G - PSR  
        public const int PSR_TP_RATIO = 7;              // Column H - PSR/TP
        public const int TOTAL_POSSESSIONS = 8;         // Column I - TP (Total Possessions)
        public const int TURNOVERS_WON = 9;             // Column J - ToW
        public const int INTERCEPTIONS = 10;            // Column K - Int
    }
    
    /// <summary>
    /// Column indices for attacking statistics
    /// </summary>
    public static class Attacking
    {
        public const int TOTAL_ATTACKS = 29;        // Column AD (30th column, 0-based = 29) - TA
        public const int KICK_RETAINED = 30;        // Column AE - KR
        public const int KICK_LOST = 31;            // Column AF - KL
        public const int CARRY_RETAINED = 32;       // Column AG - CR
        public const int CARRY_LOST = 33;           // Column AH - CL
    }
    
    /// <summary>
    /// Column indices for shooting statistics
    /// </summary>
    public static class Shooting
    {
        public const int SHOTS_TOTAL = 34;          // Column AI - Tot (Total Shots)
        public const int POINTS = 35;               // Column AJ - Pts
        public const int TWO_POINTS = 36;           // Column AK - 2 Pts
        public const int GOALS = 37;                // Column AL - Gls
        public const int WIDES = 38;                // Column AM - Wid
        public const int SCORES = 5;                // Column F - Scores (Combined score display)
    }
    
    /// <summary>
    /// Column indices for defensive statistics
    /// </summary>
    public static class Defensive
    {
        public const int TACKLES_TOTAL = 62;        // Column BK (63rd column, 0-based = 62) - Tot
        public const int TACKLES_CONTACT = 63;      // Column BL - Con
        public const int TACKLES_MISSED = 64;       // Column BM - Mis
        public const int TACKLE_PERCENTAGE = 65;    // Column BN - %
        public const int FREES_CONCEDED_TOTAL = 67; // Column BO - Tot (Frees Conceded)
    }
    
    /// <summary>
    /// Column indices for disciplinary statistics
    /// </summary>
    public static class Disciplinary
    {
        public const int YELLOW_CARDS = 75;         // Column BX - Yel
        public const int BLACK_CARDS = 76;          // Column BY - Bla
        public const int RED_CARDS = 77;            // Column BZ - Red
    }
    
    /// <summary>
    /// Column indices for goalkeeper statistics
    /// </summary>
    public static class Goalkeeper
    {
        public const int KICKOUTS_TOTAL = 80;       // Column CA - TKo
        public const int KICKOUTS_RETAINED = 81;    // Column CB - KoR
        public const int KICKOUTS_LOST = 82;        // Column CC - KoL
        public const int KICKOUT_PERCENTAGE = 83;   // Column CD - %
        public const int SAVES = 84;                // Column CE - Saves
    }
    
    /// <summary>
    /// Expected Excel column headers for validation
    /// </summary>
    public static readonly Dictionary<int, string> ExpectedHeaders = new()
    {
        { PlayerInfo.JERSEY_NUMBER, "#" },
        { PlayerInfo.PLAYER_NAME, "Player Name" },
        { PlayerInfo.MINUTES_PLAYED, "Min" },
        { PlayerInfo.TOTAL_ENGAGEMENTS, "TE" },
        { Possession.TE_PSR_RATIO, "TE/PSR" },
        { Possession.SCORES, "Scores" },
        { Possession.POSSESSION_SUCCESS_RATE, "PSR" },
        { Possession.PSR_TP_RATIO, "PSR/TP" },
        { Possession.TOTAL_POSSESSIONS, "TP" },
        { Possession.TURNOVERS_WON, "ToW" },
        { Possession.INTERCEPTIONS, "Int" },
        { Attacking.TOTAL_ATTACKS, "TA" },
        { Attacking.KICK_RETAINED, "KR" },
        { Attacking.KICK_LOST, "KL" },
        { Attacking.CARRY_RETAINED, "CR" },
        { Attacking.CARRY_LOST, "CL" },
        { Shooting.SHOTS_TOTAL, "Tot" },
        { Shooting.POINTS, "Pts" },
        { Shooting.TWO_POINTS, "2 Pts" },
        { Shooting.GOALS, "Gls" },
        { Shooting.WIDES, "Wid" },
        { Defensive.TACKLES_TOTAL, "Tot" },
        { Defensive.TACKLES_CONTACT, "Con" },
        { Defensive.TACKLES_MISSED, "Mis" },
        { Defensive.TACKLE_PERCENTAGE, "%" },
        { Defensive.FREES_CONCEDED_TOTAL, "Tot" },
        { Disciplinary.YELLOW_CARDS, "Yel" },
        { Disciplinary.BLACK_CARDS, "Bla" },
        { Disciplinary.RED_CARDS, "Red" },
        { Goalkeeper.KICKOUTS_TOTAL, "TKo" },
        { Goalkeeper.KICKOUTS_RETAINED, "KoR" },
        { Goalkeeper.KICKOUTS_LOST, "KoL" },
        { Goalkeeper.KICKOUT_PERCENTAGE, "%" },
        { Goalkeeper.SAVES, "Saves" }
    };
    
    /// <summary>
    /// Total number of expected columns in Excel sheet
    /// </summary>
    public const int TOTAL_COLUMNS = 85;
    
    /// <summary>
    /// Minimum required columns for valid player statistics
    /// </summary>
    public const int MIN_REQUIRED_COLUMNS = 85;
    
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
    /// Checks if a player name is valid (not a template placeholder or summary row)
    /// </summary>
    public static bool IsValidPlayerName(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return false;
            
        var trimmedName = playerName.Trim();
        
        // Check exact matches against excluded names
        if (InvalidPlayerPatterns.EXCLUDED_NAMES.Contains(trimmedName, StringComparer.OrdinalIgnoreCase))
            return false;
            
        // Check for "Player X" pattern (template placeholders)
        if (trimmedName.StartsWith(InvalidPlayerPatterns.PLAYER_PLACEHOLDER_PATTERN, StringComparison.OrdinalIgnoreCase))
            return false;
            
        // Check for "Team Average" or variations
        if (trimmedName.Contains("Average", StringComparison.OrdinalIgnoreCase) && 
            trimmedName.Contains("Team", StringComparison.OrdinalIgnoreCase))
            return false;
            
        return true;
    }
    
    /// <summary>
    /// Validates if a row appears to contain valid player data
    /// </summary>
    public static bool IsValidPlayerDataRow(object?[] rowData)
    {
        if (rowData.Length < 40)  // At least need basic columns
            return false;
            
        // Must have player name
        var playerName = rowData[PlayerInfo.PLAYER_NAME]?.ToString();
        if (string.IsNullOrWhiteSpace(playerName))
            return false;
            
        // Check if the player name is valid (not a template placeholder or summary row)
        if (!IsValidPlayerName(playerName))
            return false;
            
        // Must have some statistics (check total engagements, possessions, or minutes)
        var hasMinutes = double.TryParse(rowData[PlayerInfo.MINUTES_PLAYED]?.ToString(), out var minutes) && minutes > 0;
        var hasEngagements = double.TryParse(rowData[PlayerInfo.TOTAL_ENGAGEMENTS]?.ToString(), out var engagements) && engagements > 0;
        var hasPossessions = double.TryParse(rowData[Possession.TOTAL_POSSESSIONS]?.ToString(), out var possessions) && possessions > 0;
        
        return hasMinutes || hasEngagements || hasPossessions;
    }
}