using Microsoft.EntityFrameworkCore;
using GAAStat.Dal.Models.application;

namespace GAAStat.Dal.Interfaces;

public interface IGAAStatDbContext
{
    DbSet<Competition> Competitions { get; set; }
    DbSet<EventOutcome> EventOutcomes { get; set; }
    DbSet<EventType> EventTypes { get; set; }
    DbSet<ImportHistory> ImportHistories { get; set; }
    DbSet<ImportSnapshot> ImportSnapshots { get; set; }
    DbSet<Match> Matches { get; set; }
    DbSet<MatchKickoutStat> MatchKickoutStats { get; set; }
    DbSet<MatchPlayerStat> MatchPlayerStats { get; set; }
    DbSet<MatchSourceAnalysis> MatchSourceAnalyses { get; set; }
    DbSet<Season> Seasons { get; set; }
    DbSet<Team> Teams { get; set; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    int SaveChanges();
}