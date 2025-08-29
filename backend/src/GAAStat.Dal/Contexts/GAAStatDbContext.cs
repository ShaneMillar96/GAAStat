using Microsoft.EntityFrameworkCore;
using GAAStat.Dal.Models.application;
using GAAStat.Dal.Interfaces;

namespace GAAStat.Dal.Contexts;

public class GAAStatDbContext : DbContext, IGAAStatDbContext
{
    public GAAStatDbContext(DbContextOptions<GAAStatDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Team> Teams { get; set; }
    public DbSet<Player> Players { get; set; }
    public DbSet<Match> Matches { get; set; }
    public DbSet<PlayerStat> PlayerStats { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure constraints and relationships
        modelBuilder.Entity<Match>()
            .HasOne(m => m.HomeTeam)
            .WithMany()
            .HasForeignKey(m => m.HomeTeamId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Match>()
            .HasOne(m => m.AwayTeam)
            .WithMany()
            .HasForeignKey(m => m.AwayTeamId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure table names to match database schema
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Team>().ToTable("teams");
        modelBuilder.Entity<Player>().ToTable("players");
        modelBuilder.Entity<Match>().ToTable("matches");
        modelBuilder.Entity<PlayerStat>().ToTable("player_stats");

        // Configure column names to match snake_case convention
        ConfigureSnakeCaseNaming(modelBuilder);
    }

    private void ConfigureSnakeCaseNaming(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }
        }
    }

    private string ToSnakeCase(string name)
    {
        return string.Concat(name.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + c.ToString().ToLower() : c.ToString().ToLower()));
    }
}