namespace GAAStat.Services.Models;

/// <summary>
/// Result of seeding all reference data tables
/// </summary>
public class ReferenceDataSeedResult
{
    public int PositionsSeeded { get; set; }
    public int TimePeriodsSeeded { get; set; }
    public int TeamTypesSeeded { get; set; }
    public int KickoutTypesSeeded { get; set; }
    public int ShotTypesSeeded { get; set; }
    public int ShotOutcomesSeeded { get; set; }
    public int PositionAreasSeeded { get; set; }
    public int FreeTypesSeeded { get; set; }
    public int MetricCategoriesSeeded { get; set; }
    public int KpiDefinitionsSeeded { get; set; }
    
    public int TotalSeeded => PositionsSeeded + TimePeriodsSeeded + TeamTypesSeeded + 
                             KickoutTypesSeeded + ShotTypesSeeded + ShotOutcomesSeeded + 
                             PositionAreasSeeded + FreeTypesSeeded + MetricCategoriesSeeded + 
                             KpiDefinitionsSeeded;
    
    public List<string> WarningMessages { get; set; } = new();
}

/// <summary>
/// Result of validating reference data completeness
/// </summary>
public class ReferenceDataValidationResult
{
    public bool IsComplete { get; set; }
    public List<string> MissingTables { get; set; } = new();
    public Dictionary<string, int> TableCounts { get; set; } = new();
    public List<string> ValidationErrors { get; set; } = new();
}

/// <summary>
/// Defines the expected reference data for GAA statistics
/// </summary>
public static class ReferenceDataConstants
{
    /// <summary>
    /// Standard GAA playing positions
    /// </summary>
    public static readonly Dictionary<string, string> Positions = new()
    {
        { "Goalkeeper", "Primary goalkeeper position" },
        { "Defender", "Defensive field player" },
        { "Midfielder", "Midfield player" },
        { "Forward", "Forward/attacking player" }
    };

    /// <summary>
    /// Match time periods for statistics
    /// </summary>
    public static readonly Dictionary<string, string> TimePeriods = new()
    {
        { "First Half", "First half of match" },
        { "Second Half", "Second half of match" },
        { "Full Game", "Complete match statistics" }
    };

    /// <summary>
    /// Team type designations
    /// </summary>
    public static readonly Dictionary<string, string> TeamTypes = new()
    {
        { "Drum", "Home team (Drum)" },
        { "Opposition", "Away/opposing team" }
    };

    /// <summary>
    /// Kickout type classifications
    /// </summary>
    public static readonly Dictionary<string, string> KickoutTypes = new()
    {
        { "Long", "Long kickout attempt" },
        { "Short", "Short kickout attempt" }
    };

    /// <summary>
    /// Shot type classifications
    /// </summary>
    public static readonly Dictionary<string, string> ShotTypes = new()
    {
        { "From Play", "Shot during open play" },
        { "Free Kick", "Shot from a free kick" },
        { "Penalty", "Penalty shot" }
    };

    /// <summary>
    /// Shot outcome classifications
    /// </summary>
    public static readonly Dictionary<string, (string Description, bool IsScore)> ShotOutcomes = new()
    {
        { "Goal", ("Ball goes under the crossbar", true) },
        { "Point", ("Ball goes over the crossbar and between posts", true) },
        { "2 Pointer", ("Two-point score", true) },
        { "Wide", ("Ball goes wide of the posts", false) },
        { "Short", ("Ball falls short of the goals", false) },
        { "Save", ("Goalkeeper saves the attempt", false) },
        { "Block", ("Shot is blocked by a defender", false) },
        { "45", ("Ball goes over the end line, resulting in a 45m free", false) },
        { "Woodwork", ("Ball hits the goalposts or crossbar", false) }
    };

    /// <summary>
    /// Field position areas for shot analysis
    /// </summary>
    public static readonly Dictionary<string, string> PositionAreas = new()
    {
        { "Attacking Third", "Attacking third of the field" },
        { "Middle Third", "Middle third of the field" },
        { "Defensive Third", "Defensive third of the field" }
    };

    /// <summary>
    /// Free kick type classifications
    /// </summary>
    public static readonly Dictionary<string, string> FreeTypes = new()
    {
        { "Standard", "Standard free kick taken normally" },
        { "Quick", "Quick free kick taken rapidly" }
    };

    /// <summary>
    /// Metric category groupings for team statistics
    /// </summary>
    public static readonly Dictionary<string, string> MetricCategories = new()
    {
        { "Possession", "Possession and ball retention statistics" },
        { "Attacking", "Attacking and scoring statistics" },
        { "Defensive", "Defensive and tackle statistics" },
        { "Kickouts", "Kickout performance statistics" },
        { "Shooting", "Shot accuracy and conversion statistics" },
        { "Disciplinary", "Cards and foul statistics" },
        { "Goalkeeping", "Goalkeeper specific statistics" },
        { "Score Source", "Source of scoring opportunities" },
        { "General", "General match statistics" }
    };

    /// <summary>
    /// Default KPI definitions for GAA statistics when Excel file doesn't contain KPI sheet
    /// </summary>
    public static readonly List<(string KpiCode, string KpiName, string Description, string? PositionRelevance, double? PsrValue)> DefaultKpiDefinitions = new()
    {
        ("1.0_won_clean", "Kickout Won Clean", "Successfully won kickout without contest", "Home", 1.0),
        ("1.0_lost_clean", "Kickout Lost Clean", "Kickout lost without defensive challenge", "Home", 0.0),
        ("1.0_won_contested", "Kickout Won Contested", "Won kickout after aerial contest", "Home", 1.0),
        ("1.0_lost_contested", "Kickout Lost Contested", "Lost kickout after aerial contest", "Home", 0.0),
        ("2.0_successful", "Attack Successful", "Attack resulted in score or scoring opportunity", "Both", 1.0),
        ("2.0_breakdown", "Attack Breakdown", "Attack broke down without scoring chance", "Both", 0.0),
        ("3.0_tackle_won", "Tackle Won", "Successfully won possession through tackle", "Both", 1.0),
        ("3.0_tackle_lost", "Tackle Lost", "Failed tackle attempt, opponent retained possession", "Both", 0.0),
        ("4.0_turnover_forced", "Turnover Forced", "Forced opponent to lose possession", "Both", 1.0),
        ("4.0_turnover_conceded", "Turnover Conceded", "Lost possession through opponent action", "Both", 0.0),
        ("5.0_shot_goal", "Shot Goal", "Shot attempt resulting in goal", "Both", 1.0),
        ("5.0_shot_point", "Shot Point", "Shot attempt resulting in point", "Both", 1.0),
        ("5.0_shot_miss", "Shot Miss", "Shot attempt that missed target", "Both", 0.0),
        ("6.0_pass_completed", "Pass Completed", "Successful pass to teammate", "Both", 1.0),
        ("6.0_pass_intercepted", "Pass Intercepted", "Pass intercepted by opposition", "Both", 0.0)
    };
}