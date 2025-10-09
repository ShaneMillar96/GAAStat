using GAAStat.Dal.Contexts;
using GAAStat.Dal.Models.Application;
using GAAStat.Services.ETL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GAAStat.Services.ETL.Loaders;

/// <summary>
/// Loads match data from Excel into the database.
/// Handles reference data upsert, match creation, and team statistics insertion.
/// </summary>
public class MatchDataLoader
{
    private readonly GAAStatDbContext _dbContext;
    private readonly ILogger<MatchDataLoader> _logger;

    public MatchDataLoader(GAAStatDbContext dbContext, ILogger<MatchDataLoader> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Loads all match data into the database
    /// </summary>
    /// <param name="matchSheets">List of match sheet data to load</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ETL result with statistics</returns>
    public async Task<EtlResult> LoadMatchDataAsync(
        List<MatchSheetData> matchSheets,
        CancellationToken cancellationToken = default)
    {
        var result = EtlResult.CreateSuccess();

        foreach (var matchData in matchSheets)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                _logger.LogInformation("Loading match {MatchNumber}: {SheetName}",
                    matchData.MatchNumber, matchData.SheetName);

                // Phase 1: Upsert reference data
                var season = await UpsertSeasonAsync(matchData.MatchDate.Year, cancellationToken);
                var competition = await UpsertCompetitionAsync(season.SeasonId, matchData.Competition, cancellationToken);
                var drumTeam = await UpsertTeamAsync("Drum", true, cancellationToken);
                var awayTeam = await UpsertTeamAsync(matchData.Opposition, false, cancellationToken);

                // Phase 2: Insert match
                var match = await InsertMatchAsync(
                    competition.CompetitionId,
                    drumTeam.TeamId,
                    awayTeam.TeamId,
                    matchData,
                    cancellationToken);

                // Phase 3: Insert team statistics (6 records)
                await InsertTeamStatisticsAsync(match.MatchId, drumTeam.TeamId, awayTeam.TeamId, matchData, cancellationToken);

                // Commit transaction
                await transaction.CommitAsync(cancellationToken);

                result.MatchesProcessed++;
                result.TeamStatisticsCreated += 6;

                _logger.LogInformation("Successfully loaded match {MatchNumber}", matchData.MatchNumber);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to load match {MatchNumber}: {Message}",
                    matchData.MatchNumber, ex.Message);
                result.AddError($"MATCH_{matchData.MatchNumber}", ex.Message, matchData.SheetName);
            }
        }

        result.EndTime = DateTime.UtcNow;
        return result;
    }

    /// <summary>
    /// Upserts a season (creates if not exists)
    /// </summary>
    private async Task<Season> UpsertSeasonAsync(int year, CancellationToken cancellationToken)
    {
        var season = await _dbContext.Seasons
            .FirstOrDefaultAsync(s => s.Year == year, cancellationToken);

        if (season == null)
        {
            season = new Season
            {
                Year = year,
                Name = $"{year} Season",
                IsCurrent = year == DateTime.Now.Year,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Seasons.Add(season);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created season {Year}", year);
        }

        return season;
    }

    /// <summary>
    /// Upserts a competition (creates if not exists)
    /// </summary>
    private async Task<Competition> UpsertCompetitionAsync(
        int seasonId,
        string competitionName,
        CancellationToken cancellationToken)
    {
        var competition = await _dbContext.Competitions
            .FirstOrDefaultAsync(c => c.SeasonId == seasonId && c.Name == competitionName, cancellationToken);

        if (competition == null)
        {
            competition = new Competition
            {
                SeasonId = seasonId,
                Name = competitionName,
                Type = competitionName, // Type matches Name for our use case
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Competitions.Add(competition);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created competition {Name} for season {SeasonId}", competitionName, seasonId);
        }

        return competition;
    }

    /// <summary>
    /// Upserts a team (creates if not exists)
    /// </summary>
    private async Task<Team> UpsertTeamAsync(
        string teamName,
        bool isDrum,
        CancellationToken cancellationToken)
    {
        var team = await _dbContext.Teams
            .FirstOrDefaultAsync(t => t.Name == teamName, cancellationToken);

        if (team == null)
        {
            team = new Team
            {
                Name = teamName,
                Abbreviation = GenerateAbbreviation(teamName),
                IsDrum = isDrum,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Teams.Add(team);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created team {Name} (IsDrum: {IsDrum})", teamName, isDrum);
        }

        return team;
    }

    /// <summary>
    /// Generates a 3-letter abbreviation from team name
    /// </summary>
    private string GenerateAbbreviation(string teamName)
    {
        if (teamName.Length <= 3)
            return teamName.ToUpper();

        return teamName.Substring(0, 3).ToUpper();
    }

    /// <summary>
    /// Inserts a new match record
    /// </summary>
    private async Task<Match> InsertMatchAsync(
        int competitionId,
        int homeTeamId,
        int awayTeamId,
        MatchSheetData matchData,
        CancellationToken cancellationToken)
    {
        // Check for existing match (idempotency)
        var existingMatch = await _dbContext.Matches
            .FirstOrDefaultAsync(m => m.CompetitionId == competitionId && m.MatchNumber == matchData.MatchNumber,
                cancellationToken);

        if (existingMatch != null)
        {
            _logger.LogWarning("Match {MatchNumber} already exists, skipping", matchData.MatchNumber);
            throw new InvalidOperationException($"Match {matchData.MatchNumber} already exists in competition {competitionId}");
        }

        var match = new Match
        {
            CompetitionId = competitionId,
            MatchNumber = matchData.MatchNumber,
            HomeTeamId = homeTeamId,
            AwayTeamId = awayTeamId,
            MatchDate = matchData.MatchDate,
            Venue = matchData.Venue,
            HomeScoreFirstHalf = matchData.HomeScoreFirstHalf,
            HomeScoreSecondHalf = matchData.HomeScoreSecondHalf,
            HomeScoreFullTime = matchData.HomeScoreFullTime,
            AwayScoreFirstHalf = matchData.AwayScoreFirstHalf,
            AwayScoreSecondHalf = matchData.AwayScoreSecondHalf,
            AwayScoreFullTime = matchData.AwayScoreFullTime,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Matches.Add(match);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Inserted match {MatchId} for match number {MatchNumber}",
            match.MatchId, matchData.MatchNumber);

        return match;
    }

    /// <summary>
    /// Inserts team statistics for a match (6 records: 3 periods × 2 teams)
    /// </summary>
    private async Task InsertTeamStatisticsAsync(
        int matchId,
        int drumTeamId,
        int awayTeamId,
        MatchSheetData matchData,
        CancellationToken cancellationToken)
    {
        var statsToInsert = new List<MatchTeamStatistics>();

        foreach (var stats in matchData.TeamStatistics)
        {
            var teamId = stats.TeamName == "Drum" ? drumTeamId : awayTeamId;

            var teamStats = new MatchTeamStatistics
            {
                MatchId = matchId,
                TeamId = teamId,
                Period = stats.Period,
                Scoreline = stats.Scoreline,
                TotalPossession = stats.TotalPossession,

                // Score sources
                ScoreSourceKickoutLong = stats.ScoreSourceKickoutLong ?? 0,
                ScoreSourceKickoutShort = stats.ScoreSourceKickoutShort ?? 0,
                ScoreSourceOppKickoutLong = stats.ScoreSourceOppKickoutLong ?? 0,
                ScoreSourceOppKickoutShort = stats.ScoreSourceOppKickoutShort ?? 0,
                ScoreSourceTurnover = stats.ScoreSourceTurnover ?? 0,
                ScoreSourcePossessionLost = stats.ScoreSourcePossessionLost ?? 0,
                ScoreSourceShotShort = stats.ScoreSourceShotShort ?? 0,
                ScoreSourceThrowUpIn = stats.ScoreSourceThrowUpIn ?? 0,

                // Shot sources
                ShotSourceKickoutLong = stats.ShotSourceKickoutLong ?? 0,
                ShotSourceKickoutShort = stats.ShotSourceKickoutShort ?? 0,
                ShotSourceOppKickoutLong = stats.ShotSourceOppKickoutLong ?? 0,
                ShotSourceOppKickoutShort = stats.ShotSourceOppKickoutShort ?? 0,
                ShotSourceTurnover = stats.ShotSourceTurnover ?? 0,
                ShotSourcePossessionLost = stats.ShotSourcePossessionLost ?? 0,
                ShotSourceShotShort = stats.ShotSourceShotShort ?? 0,
                ShotSourceThrowUpIn = stats.ShotSourceThrowUpIn ?? 0,

                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            statsToInsert.Add(teamStats);
        }

        // Validate we have exactly 6 statistics records
        if (statsToInsert.Count != 6)
        {
            throw new InvalidOperationException($"Expected 6 team statistics records, found {statsToInsert.Count}");
        }

        // Bulk insert all 6 records
        await _dbContext.MatchTeamStatistics.AddRangeAsync(statsToInsert, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Inserted {Count} team statistics records for match {MatchId}",
            statsToInsert.Count, matchId);
    }
}
