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
    
    public int TotalSeeded => PositionsSeeded + TimePeriodsSeeded + TeamTypesSeeded + 
                             KickoutTypesSeeded + ShotTypesSeeded + ShotOutcomesSeeded + 
                             PositionAreasSeeded + FreeTypesSeeded + MetricCategoriesSeeded;
    
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
}