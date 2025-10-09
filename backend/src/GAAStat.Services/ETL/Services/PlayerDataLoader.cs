using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GAAStat.Dal.Contexts;
using GAAStat.Dal.Models;
using GAAStat.Dal.Models.Application;
using GAAStat.Services.ETL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GAAStat.Services.ETL.Services;

/// <summary>
/// Loads player statistics into database.
/// Handles bulk insertion and duplicate detection.
/// </summary>
public class PlayerDataLoader
{
    private readonly GAAStatDbContext _dbContext;
    private readonly PlayerRosterService _playerRosterService;
    private readonly PositionDetectionService _positionDetectionService;
    private readonly ILogger<PlayerDataLoader> _logger;

    public PlayerDataLoader(
        GAAStatDbContext dbContext,
        PlayerRosterService playerRosterService,
        PositionDetectionService positionDetectionService,
        ILogger<PlayerDataLoader> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _playerRosterService = playerRosterService ?? throw new ArgumentNullException(nameof(playerRosterService));
        _positionDetectionService = positionDetectionService ?? throw new ArgumentNullException(nameof(positionDetectionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Loads player statistics for a single match sheet.
    /// Uses transaction for atomicity.
    /// </summary>
    /// <param name="sheet">Player stats sheet data</param>
    /// <param name="matchId">Match ID to link statistics to</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple of (created count, updated count, skipped count)</returns>
    public async Task<(int Created, int Updated, int Skipped)> LoadPlayerStatisticsAsync(
        PlayerStatsSheetData sheet,
        int matchId,
        CancellationToken cancellationToken = default)
    {
        if (sheet == null)
            throw new ArgumentNullException(nameof(sheet));

        if (matchId <= 0)
            throw new ArgumentException("Match ID must be positive", nameof(matchId));

        int created = 0;
        int updated = 0;
        int skipped = 0;

        // Start transaction for atomicity
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            _logger.LogInformation(
                "Loading statistics for {PlayerCount} players in match {MatchId} (vs {Opposition})",
                sheet.Players.Count, matchId, sheet.Opposition);

            foreach (var playerData in sheet.Players)
            {
                try
                {
                    // Get position ID for player
                    var positionId = await _positionDetectionService.GetPositionIdAsync(
                        playerData.PositionCode,
                        cancellationToken);

                    // Get or create player in roster
                    var player = await _playerRosterService.GetOrCreatePlayerAsync(
                        playerData.JerseyNumber,
                        playerData.PlayerName,
                        positionId,
                        cancellationToken);

                    // Check if statistics already exist for this player+match
                    var existingStats = await _dbContext.PlayerMatchStatistics
                        .Where(pms => pms.MatchId == matchId && pms.PlayerId == player.PlayerId)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (existingStats != null)
                    {
                        _logger.LogWarning(
                            "Statistics already exist for player #{JerseyNumber} '{PlayerName}' in match {MatchId}. Skipping.",
                            playerData.JerseyNumber, playerData.PlayerName, matchId);
                        skipped++;
                        continue;
                    }

                    // Create player match statistics record
                    var statistics = MapToPlayerMatchStatistics(playerData, player.PlayerId, matchId);

                    _dbContext.PlayerMatchStatistics.Add(statistics);
                    created++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error loading statistics for player #{JerseyNumber} '{PlayerName}' in match {MatchId}",
                        playerData.JerseyNumber, playerData.PlayerName, matchId);
                    skipped++;
                }
            }

            // Save all statistics in batch
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Loaded player statistics for match {MatchId}: {Created} created, {Updated} updated, {Skipped} skipped",
                matchId, created, updated, skipped);

            return (created, updated, skipped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading player statistics for match {MatchId}. Rolling back transaction.", matchId);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Checks if statistics already exist for a match.
    /// </summary>
    /// <param name="matchId">Match ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if statistics exist</returns>
    public async Task<bool> StatisticsExistForMatchAsync(
        int matchId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PlayerMatchStatistics
            .AnyAsync(pms => pms.MatchId == matchId, cancellationToken);
    }

    /// <summary>
    /// Deletes statistics for a match (for re-processing).
    /// </summary>
    /// <param name="matchId">Match ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of records deleted</returns>
    public async Task<int> DeleteStatisticsForMatchAsync(
        int matchId,
        CancellationToken cancellationToken = default)
    {
        var statistics = await _dbContext.PlayerMatchStatistics
            .Where(pms => pms.MatchId == matchId)
            .ToListAsync(cancellationToken);

        if (statistics.Any())
        {
            _dbContext.PlayerMatchStatistics.RemoveRange(statistics);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted {Count} player statistics records for match {MatchId}",
                statistics.Count, matchId);
        }

        return statistics.Count;
    }

    /// <summary>
    /// Maps ETL player data to database entity.
    /// </summary>
    private PlayerMatchStatistics MapToPlayerMatchStatistics(
        PlayerStatisticsData source,
        int playerId,
        int matchId)
    {
        return new PlayerMatchStatistics
        {
            PlayerId = playerId,
            MatchId = matchId,

            // Summary Statistics
            MinutesPlayed = source.MinutesPlayed,
            TotalEngagements = source.TotalEngagements,
            TePerPsr = source.TePerPsr,
            Scores = source.Scores,
            Psr = source.Psr,
            PsrPerTp = source.PsrPerTp,

            // Possession Play
            Tp = source.Tp,
            Tow = source.Tow,
            Interceptions = source.Interceptions,
            Tpl = source.Tpl,
            Kp = source.Kp,
            Hp = source.Hp,
            Ha = source.Ha,
            Turnovers = source.Turnovers,
            Ineffective = source.Ineffective,
            ShotShort = source.ShotShort,
            ShotSave = source.ShotSave,
            Fouled = source.Fouled,
            Woodwork = source.Woodwork,

            // Kickout Analysis - Drum
            KoDrumKow = source.KoDrumKow,
            KoDrumWc = source.KoDrumWc,
            KoDrumBw = source.KoDrumBw,
            KoDrumSw = source.KoDrumSw,

            // Kickout Analysis - Opposition
            KoOppKow = source.KoOppKow,
            KoOppWc = source.KoOppWc,
            KoOppBw = source.KoOppBw,
            KoOppSw = source.KoOppSw,

            // Attacking Play
            Ta = source.Ta,
            Kr = source.Kr,
            Kl = source.Kl,
            Cr = source.Cr,
            Cl = source.Cl,

            // Shots from Play
            ShotsPlayTotal = source.ShotsPlayTotal,
            ShotsPlayPoints = source.ShotsPlayPoints,
            ShotsPlay2Points = source.ShotsPlay2Points,
            ShotsPlayGoals = source.ShotsPlayGoals,
            ShotsPlayWide = source.ShotsPlayWide,
            ShotsPlayShort = source.ShotsPlayShort,
            ShotsPlaySave = source.ShotsPlaySave,
            ShotsPlayWoodwork = source.ShotsPlayWoodwork,
            ShotsPlayBlocked = source.ShotsPlayBlocked,
            ShotsPlay45 = source.ShotsPlay45,
            ShotsPlayPercentage = source.ShotsPlayPercentage,

            // Scoreable Frees
            FreesTotal = source.FreesTotal,
            FreesPoints = source.FreesPoints,
            Frees2Points = source.Frees2Points,
            FreesGoals = source.FreesGoals,
            FreesWide = source.FreesWide,
            FreesShort = source.FreesShort,
            FreesSave = source.FreesSave,
            FreesWoodwork = source.FreesWoodwork,
            Frees45 = source.Frees45,
            FreesQf = source.FreesQf,
            FreesPercentage = source.FreesPercentage,

            // Total Shots
            TotalShots = source.TotalShots,
            TotalShotsPercentage = source.TotalShotsPercentage,

            // Assists
            AssistsTotal = source.AssistsTotal,
            AssistsPoint = source.AssistsPoint,
            AssistsGoal = source.AssistsGoal,

            // Tackles
            TacklesTotal = source.TacklesTotal,
            TacklesContested = source.TacklesContested,
            TacklesMissed = source.TacklesMissed,
            TacklesPercentage = source.TacklesPercentage,

            // Frees Conceded
            FreesConcededTotal = source.FreesConcededTotal,
            FreesConcededAttack = source.FreesConcededAttack,
            FreesConcededMidfield = source.FreesConcededMidfield,
            FreesConcededDefense = source.FreesConcededDefense,
            FreesConcededPenalty = source.FreesConcededPenalty,

            // 50m Free Conceded
            Frees50MTotal = source.Frees50mTotal,
            Frees50MDelay = source.Frees50mDelay,
            Frees50MDissent = source.Frees50mDissent,
            Frees50M3V3 = source.Frees50m3v3,

            // Bookings
            YellowCards = source.YellowCards,
            BlackCards = source.BlackCards,
            RedCards = source.RedCards,

            // Throw Up
            ThrowUpWon = source.ThrowUpWon,
            ThrowUpLost = source.ThrowUpLost,

            // Goalkeeper Stats
            GkTotalKickouts = source.GkTotalKickouts,
            GkKickoutRetained = source.GkKickoutRetained,
            GkKickoutLost = source.GkKickoutLost,
            GkKickoutPercentage = source.GkKickoutPercentage,
            GkSaves = source.GkSaves,

            // Metadata
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
