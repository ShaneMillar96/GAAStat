using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GAAStat.Dal.Contexts;
using GAAStat.Dal.Models.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GAAStat.Services.ETL.Services;

/// <summary>
/// Manages player roster operations with upsert logic.
/// Handles exact matching, fuzzy matching, and player creation.
/// </summary>
public class PlayerRosterService
{
    private readonly GAAStatDbContext _dbContext;
    private readonly ILogger<PlayerRosterService> _logger;

    // Levenshtein distance threshold for fuzzy matching
    private const int FuzzyMatchThreshold = 3;

    public PlayerRosterService(
        GAAStatDbContext dbContext,
        ILogger<PlayerRosterService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets or creates player using upsert logic.
    /// Strategy: Exact match → Fuzzy match (Levenshtein ≤3) → Create new
    /// </summary>
    /// <param name="jerseyNumber">Jersey number</param>
    /// <param name="playerName">Full player name</param>
    /// <param name="positionId">Position ID (GK/DEF/MID/FWD)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Existing or newly created player</returns>
    public async Task<Player> GetOrCreatePlayerAsync(
        int jerseyNumber,
        string playerName,
        int positionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            throw new ArgumentException("Player name cannot be empty", nameof(playerName));

        if (jerseyNumber <= 0)
            throw new ArgumentException("Jersey number must be positive", nameof(jerseyNumber));

        // Step 1: Try exact match on jersey number and name
        var exactMatch = await FindExactMatchAsync(jerseyNumber, playerName, cancellationToken);
        if (exactMatch != null)
        {
            _logger.LogDebug("Found exact match for player #{JerseyNumber} '{PlayerName}'",
                jerseyNumber, playerName);
            return exactMatch;
        }

        // Step 2: Try fuzzy match on name (in case of typos)
        var fuzzyMatch = await FindFuzzyMatchAsync(jerseyNumber, playerName, cancellationToken);
        if (fuzzyMatch != null)
        {
            _logger.LogInformation(
                "Found fuzzy match for player #{JerseyNumber} '{PlayerName}' → existing player '{ExistingName}'",
                jerseyNumber, playerName, fuzzyMatch.FullName);
            return fuzzyMatch;
        }

        // Step 3: No match found - create new player
        var newPlayer = await CreatePlayerAsync(jerseyNumber, playerName, positionId, cancellationToken);
        _logger.LogInformation("Created new player #{JerseyNumber} '{PlayerName}' (ID: {PlayerId})",
            jerseyNumber, playerName, newPlayer.PlayerId);

        return newPlayer;
    }

    /// <summary>
    /// Finds player by exact jersey number and name match.
    /// </summary>
    private async Task<Player?> FindExactMatchAsync(
        int jerseyNumber,
        string playerName,
        CancellationToken cancellationToken)
    {
        var normalizedName = NormalizeName(playerName);

        return await _dbContext.Players
            .Where(p => p.JerseyNumber == jerseyNumber &&
                       p.FullName.ToLower() == normalizedName)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Finds player by jersey number and fuzzy name match (Levenshtein distance ≤3).
    /// </summary>
    private async Task<Player?> FindFuzzyMatchAsync(
        int jerseyNumber,
        string playerName,
        CancellationToken cancellationToken)
    {
        var normalizedName = NormalizeName(playerName);

        // Get all players with same jersey number
        var candidatePlayers = await _dbContext.Players
            .Where(p => p.JerseyNumber == jerseyNumber)
            .ToListAsync(cancellationToken);

        // Find best fuzzy match using Levenshtein distance
        Player? bestMatch = null;
        int bestDistance = int.MaxValue;

        foreach (var candidate in candidatePlayers)
        {
            var distance = CalculateLevenshteinDistance(
                normalizedName,
                NormalizeName(candidate.FullName));

            if (distance <= FuzzyMatchThreshold && distance < bestDistance)
            {
                bestMatch = candidate;
                bestDistance = distance;
            }
        }

        return bestMatch;
    }

    /// <summary>
    /// Creates new player record.
    /// </summary>
    private async Task<Player> CreatePlayerAsync(
        int jerseyNumber,
        string playerName,
        int positionId,
        CancellationToken cancellationToken)
    {
        var (firstName, lastName) = ParsePlayerName(playerName);

        var player = new Player
        {
            JerseyNumber = jerseyNumber,
            FirstName = firstName,
            LastName = lastName,
            FullName = playerName,
            PositionId = positionId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Players.Add(player);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return player;
    }

    /// <summary>
    /// Updates player position if it changed.
    /// </summary>
    /// <param name="player">Player to update</param>
    /// <param name="newPositionId">New position ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task UpdatePlayerPositionAsync(
        Player player,
        int newPositionId,
        CancellationToken cancellationToken = default)
    {
        if (player.PositionId != newPositionId)
        {
            _logger.LogInformation(
                "Updating position for player #{JerseyNumber} '{PlayerName}' from {OldPosition} to {NewPosition}",
                player.JerseyNumber, player.FullName, player.PositionId, newPositionId);

            player.PositionId = newPositionId;
            player.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Gets all active players for caching.
    /// </summary>
    public async Task<List<Player>> GetAllActivePlayersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Players
            .Where(p => p.IsActive)
            .Include(p => p.Position)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Normalizes player name for comparison (lowercase, trim).
    /// </summary>
    private string NormalizeName(string name)
    {
        return name.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Parses full player name into first name and last name.
    /// Simple heuristic: First word = first name, rest = last name.
    /// </summary>
    private (string FirstName, string LastName) ParsePlayerName(string fullName)
    {
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            return (string.Empty, string.Empty);
        }
        else if (parts.Length == 1)
        {
            // Single name - use as both first and last
            return (parts[0], parts[0]);
        }
        else
        {
            // Multiple words - first word is first name, rest is last name
            var firstName = parts[0];
            var lastName = string.Join(" ", parts.Skip(1));
            return (firstName, lastName);
        }
    }

    /// <summary>
    /// Calculates Levenshtein distance between two strings.
    /// Used for fuzzy name matching.
    /// </summary>
    private int CalculateLevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
            return target?.Length ?? 0;

        if (string.IsNullOrEmpty(target))
            return source.Length;

        var sourceLength = source.Length;
        var targetLength = target.Length;

        // Create distance matrix
        var distance = new int[sourceLength + 1, targetLength + 1];

        // Initialize first column and row
        for (int i = 0; i <= sourceLength; i++)
            distance[i, 0] = i;

        for (int j = 0; j <= targetLength; j++)
            distance[0, j] = j;

        // Calculate distances
        for (int i = 1; i <= sourceLength; i++)
        {
            for (int j = 1; j <= targetLength; j++)
            {
                var cost = (target[j - 1] == source[i - 1]) ? 0 : 1;

                distance[i, j] = Math.Min(
                    Math.Min(
                        distance[i - 1, j] + 1,      // Deletion
                        distance[i, j - 1] + 1),     // Insertion
                    distance[i - 1, j - 1] + cost);  // Substitution
            }
        }

        return distance[sourceLength, targetLength];
    }
}
