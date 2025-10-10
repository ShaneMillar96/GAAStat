namespace GAAStat.Services.Dashboard.Utilities;

/// <summary>
/// Utility class for parsing and calculating GAA scores
/// Handles notation like "1-03" (1 goal, 3 points) = 6 total points
/// </summary>
public class ScoreCalculator
{
    /// <summary>
    /// Parses GAA score notation to total points
    /// Format: "G-PP" where G=goals (3 pts), PP=points (1 pt)
    /// </summary>
    /// <param name="scoreNotation">Score in GAA format (e.g., "1-03", "0-15")</param>
    /// <returns>Total points (goals × 3 + points)</returns>
    public int ParseGaaScore(string? scoreNotation)
    {
        if (string.IsNullOrWhiteSpace(scoreNotation))
        {
            return 0;
        }

        var parts = scoreNotation.Split('-');
        if (parts.Length != 2)
        {
            return 0;
        }

        if (!int.TryParse(parts[0], out int goals))
        {
            goals = 0;
        }

        if (!int.TryParse(parts[1], out int points))
        {
            points = 0;
        }

        return (goals * 3) + points;
    }

    /// <summary>
    /// Formats total points as GAA notation
    /// </summary>
    /// <param name="totalPoints">Total points</param>
    /// <returns>GAA notation (e.g., "1-03" for 6 points)</returns>
    public string FormatGaaScore(int totalPoints)
    {
        int goals = totalPoints / 3;
        int points = totalPoints % 3;
        return $"{goals}-{points:D2}";
    }
}
