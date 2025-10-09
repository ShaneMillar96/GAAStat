using Microsoft.EntityFrameworkCore;
using GAAStat.Dal.Models.Application;

namespace GAAStat.Dal.Interfaces;

public interface IGAAStatDbContext
{
    // DbSet properties for all 9 tables
    DbSet<Season> Seasons { get; set; }
    DbSet<Position> Positions { get; set; }
    DbSet<Team> Teams { get; set; }
    DbSet<Competition> Competitions { get; set; }
    DbSet<Player> Players { get; set; }
    DbSet<Match> Matches { get; set; }
    DbSet<MatchTeamStatistics> MatchTeamStatistics { get; set; }
    DbSet<PlayerMatchStatistics> PlayerMatchStatistics { get; set; }
    DbSet<KpiDefinition> KpiDefinitions { get; set; }

    // SaveChanges methods
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    int SaveChanges();
}