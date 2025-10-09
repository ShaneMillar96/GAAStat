using GAAStat.Services.ETL.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace GAAStat.Services.ETL.Transformers;

/// <summary>
/// Transforms and validates match data extracted from Excel.
/// Performs data validation, format checking, and business rule enforcement.
/// </summary>
public class MatchDataTransformer
{
    private readonly ILogger<MatchDataTransformer> _logger;
    private static readonly Regex ScorePattern = new(@"^(\d+)-(\d+)$", RegexOptions.Compiled);

    public MatchDataTransformer(ILogger<MatchDataTransformer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates a list of match sheets
    /// </summary>
    /// <param name="matchSheets">List of match sheet data to validate</param>
    /// <returns>True if all validations pass</returns>
    public bool ValidateMatchData(List<MatchSheetData> matchSheets)
    {
        bool isValid = true;

        foreach (var matchData in matchSheets)
        {
            try
            {
                ValidateSingleMatch(matchData);
            }
            catch (ValidationException ex)
            {
                _logger.LogError(ex, "Validation failed for match {MatchNumber}: {Message}",
                    matchData.MatchNumber, ex.Message);
                isValid = false;
            }
        }

        return isValid;
    }

    /// <summary>
    /// Validates a single match sheet
    /// </summary>
    private void ValidateSingleMatch(MatchSheetData matchData)
    {
        // Validate metadata
        ValidateMatchMetadata(matchData);

        // Validate scores
        ValidateScores(matchData);

        // Validate team statistics
        ValidateTeamStatistics(matchData);

        _logger.LogDebug("Validation passed for match {MatchNumber}", matchData.MatchNumber);
    }

    /// <summary>
    /// Validates match metadata (date, competition, teams)
    /// </summary>
    private void ValidateMatchMetadata(MatchSheetData matchData)
    {
        if (matchData.MatchNumber <= 0)
        {
            throw new ValidationException($"Invalid match number: {matchData.MatchNumber}");
        }

        if (string.IsNullOrWhiteSpace(matchData.Competition))
        {
            throw new ValidationException("Competition name cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(matchData.Opposition))
        {
            throw new ValidationException("Opposition team name cannot be empty");
        }

        if (matchData.MatchDate < new DateTime(2000, 1, 1) || matchData.MatchDate > DateTime.Now.AddYears(1))
        {
            throw new ValidationException($"Invalid match date: {matchData.MatchDate:yyyy-MM-dd}");
        }
    }

    /// <summary>
    /// Validates score format and consistency
    /// </summary>
    private void ValidateScores(MatchSheetData matchData)
    {
        // Validate score format
        ValidateScoreFormat(matchData.HomeScoreFirstHalf, "Home 1st Half");
        ValidateScoreFormat(matchData.HomeScoreSecondHalf, "Home 2nd Half");
        ValidateScoreFormat(matchData.HomeScoreFullTime, "Home Full Time");
        ValidateScoreFormat(matchData.AwayScoreFirstHalf, "Away 1st Half");
        ValidateScoreFormat(matchData.AwayScoreSecondHalf, "Away 2nd Half");
        ValidateScoreFormat(matchData.AwayScoreFullTime, "Away Full Time");

        // Validate period totals (with tolerance for corrections)
        ValidatePeriodTotals(
            matchData.HomeScoreFirstHalf,
            matchData.HomeScoreSecondHalf,
            matchData.HomeScoreFullTime,
            "Home");

        ValidatePeriodTotals(
            matchData.AwayScoreFirstHalf,
            matchData.AwayScoreSecondHalf,
            matchData.AwayScoreFullTime,
            "Away");
    }

    /// <summary>
    /// Validates score format (G-PP pattern)
    /// </summary>
    private void ValidateScoreFormat(string? score, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(score))
        {
            _logger.LogWarning("Missing score for {FieldName}", fieldName);
            return;
        }

        if (!ScorePattern.IsMatch(score))
        {
            throw new ValidationException($"Invalid score format for {fieldName}: {score}. Expected format: G-PP");
        }

        var match = ScorePattern.Match(score);
        var goals = int.Parse(match.Groups[1].Value);
        var points = int.Parse(match.Groups[2].Value);

        if (goals < 0 || goals > 10)
        {
            throw new ValidationException($"Unrealistic goals value for {fieldName}: {goals}");
        }

        if (points < 0 || points > 30)
        {
            throw new ValidationException($"Unrealistic points value for {fieldName}: {points}");
        }
    }

    /// <summary>
    /// Validates that full-time score approximately equals 1st + 2nd half (with 10% tolerance)
    /// </summary>
    private void ValidatePeriodTotals(string? first, string? second, string? fullTime, string team)
    {
        if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(second) || string.IsNullOrWhiteSpace(fullTime))
        {
            return; // Skip validation if any score is missing
        }

        var (g1, p1) = ParseScore(first);
        var (g2, p2) = ParseScore(second);
        var (gF, pF) = ParseScore(fullTime);

        var totalGoals = g1 + g2;
        var totalPoints = p1 + p2;

        // Allow 10% tolerance for rounding/correction errors
        var goalTolerance = Math.Max(1, (int)(gF * 0.1));
        var pointTolerance = Math.Max(1, (int)(pF * 0.1));

        if (Math.Abs(totalGoals - gF) > goalTolerance)
        {
            _logger.LogWarning("{Team} goals: {G1}+{G2}≠{GF} (tolerance: {Tol})",
                team, g1, g2, gF, goalTolerance);
        }

        if (Math.Abs(totalPoints - pF) > pointTolerance)
        {
            _logger.LogWarning("{Team} points: {P1}+{P2}≠{PF} (tolerance: {Tol})",
                team, p1, p2, pF, pointTolerance);
        }
    }

    /// <summary>
    /// Parses GAA score notation (G-PP)
    /// </summary>
    private (int Goals, int Points) ParseScore(string score)
    {
        var match = ScorePattern.Match(score);
        if (!match.Success)
        {
            return (0, 0);
        }

        var goals = int.Parse(match.Groups[1].Value);
        var points = int.Parse(match.Groups[2].Value);

        return (goals, points);
    }

    /// <summary>
    /// Validates team statistics
    /// </summary>
    private void ValidateTeamStatistics(MatchSheetData matchData)
    {
        // Validate that we have exactly 6 statistics records (3 periods × 2 teams)
        if (matchData.TeamStatistics.Count != 6)
        {
            throw new ValidationException($"Expected 6 team statistics records, found {matchData.TeamStatistics.Count}");
        }

        foreach (var stats in matchData.TeamStatistics)
        {
            ValidateSingleTeamStatistics(stats);
        }

        // Validate possession sums for each period
        ValidatePossessionSums(matchData);
    }

    /// <summary>
    /// Validates a single team statistics record
    /// </summary>
    private void ValidateSingleTeamStatistics(TeamStatisticsData stats)
    {
        // Validate period
        if (!new[] { "1st", "2nd", "Full" }.Contains(stats.Period))
        {
            throw new ValidationException($"Invalid period: {stats.Period}");
        }

        // Validate possession percentage (0-1 range)
        if (stats.TotalPossession.HasValue)
        {
            if (stats.TotalPossession.Value < 0 || stats.TotalPossession.Value > 1)
            {
                throw new ValidationException($"Possession out of range: {stats.TotalPossession.Value}");
            }
        }

        // Validate all count fields are non-negative
        ValidateNonNegative(stats.ScoreSourceKickoutLong, "ScoreSourceKickoutLong");
        ValidateNonNegative(stats.ScoreSourceKickoutShort, "ScoreSourceKickoutShort");
        ValidateNonNegative(stats.ScoreSourceOppKickoutLong, "ScoreSourceOppKickoutLong");
        ValidateNonNegative(stats.ScoreSourceOppKickoutShort, "ScoreSourceOppKickoutShort");
        ValidateNonNegative(stats.ScoreSourceTurnover, "ScoreSourceTurnover");
        ValidateNonNegative(stats.ScoreSourcePossessionLost, "ScoreSourcePossessionLost");
        ValidateNonNegative(stats.ScoreSourceShotShort, "ScoreSourceShotShort");
        ValidateNonNegative(stats.ScoreSourceThrowUpIn, "ScoreSourceThrowUpIn");

        ValidateNonNegative(stats.ShotSourceKickoutLong, "ShotSourceKickoutLong");
        ValidateNonNegative(stats.ShotSourceKickoutShort, "ShotSourceKickoutShort");
        ValidateNonNegative(stats.ShotSourceOppKickoutLong, "ShotSourceOppKickoutLong");
        ValidateNonNegative(stats.ShotSourceOppKickoutShort, "ShotSourceOppKickoutShort");
        ValidateNonNegative(stats.ShotSourceTurnover, "ShotSourceTurnover");
        ValidateNonNegative(stats.ShotSourcePossessionLost, "ShotSourcePossessionLost");
        ValidateNonNegative(stats.ShotSourceShotShort, "ShotSourceShotShort");
        ValidateNonNegative(stats.ShotSourceThrowUpIn, "ShotSourceThrowUpIn");
    }

    /// <summary>
    /// Validates that a value is non-negative
    /// </summary>
    private void ValidateNonNegative(int? value, string fieldName)
    {
        if (value.HasValue && value.Value < 0)
        {
            throw new ValidationException($"Negative value not allowed for {fieldName}: {value.Value}");
        }
    }

    /// <summary>
    /// Validates that possession percentages sum to approximately 1.0 (with 5% tolerance)
    /// </summary>
    private void ValidatePossessionSums(MatchSheetData matchData)
    {
        var periods = new[] { "1st", "2nd", "Full" };

        foreach (var period in periods)
        {
            var drumStats = matchData.TeamStatistics.FirstOrDefault(s => s.TeamName == "Drum" && s.Period == period);
            var oppStats = matchData.TeamStatistics.FirstOrDefault(s => s.TeamName != "Drum" && s.Period == period);

            if (drumStats?.TotalPossession == null || oppStats?.TotalPossession == null)
            {
                continue;
            }

            var sum = drumStats.TotalPossession.Value + oppStats.TotalPossession.Value;

            if (Math.Abs(sum - 1.0m) > 0.05m)
            {
                _logger.LogWarning("Possession sum for {Period} period: {Sum:F4} (expected ≈1.0)",
                    period, sum);
            }
        }
    }
}

/// <summary>
/// Exception thrown when data validation fails
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
    public ValidationException(string message, Exception innerException) : base(message, innerException) { }
}
