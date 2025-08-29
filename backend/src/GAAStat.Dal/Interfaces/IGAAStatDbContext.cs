using Microsoft.EntityFrameworkCore;
using GAAStat.Dal.Models.application;

namespace GAAStat.Dal.Interfaces;

public interface IGAAStatDbContext
{
    DbSet<User> Users { get; set; }
    DbSet<Team> Teams { get; set; }
    DbSet<Player> Players { get; set; }
    DbSet<Match> Matches { get; set; }
    DbSet<PlayerStat> PlayerStats { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    int SaveChanges();
}