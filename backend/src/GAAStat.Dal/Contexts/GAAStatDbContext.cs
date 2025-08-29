using Microsoft.EntityFrameworkCore;
using GAAStat.Dal.Interfaces;
using GAAStat.Dal.Models.application;

namespace GAAStat.Dal.Contexts;

public partial class GAAStatDbContext : DbContext, IGAAStatDbContext
{
    public GAAStatDbContext(DbContextOptions<GAAStatDbContext> options) : base(options)
    {
    }

    public virtual DbSet<Competition> Competitions { get; set; }
    public virtual DbSet<EventOutcome> EventOutcomes { get; set; }
    public virtual DbSet<EventType> EventTypes { get; set; }
    public virtual DbSet<ImportHistory> ImportHistories { get; set; }
    public virtual DbSet<ImportSnapshot> ImportSnapshots { get; set; }
    public virtual DbSet<Match> Matches { get; set; }
    public virtual DbSet<MatchKickoutStat> MatchKickoutStats { get; set; }
    public virtual DbSet<MatchPlayerStat> MatchPlayerStats { get; set; }
    public virtual DbSet<MatchSourceAnalysis> MatchSourceAnalyses { get; set; }
    public virtual DbSet<Season> Seasons { get; set; }
    public virtual DbSet<Team> Teams { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureCompetitionEntity(modelBuilder);
        ConfigureEventOutcomeEntity(modelBuilder);
        ConfigureEventTypeEntity(modelBuilder);
        ConfigureImportHistoryEntity(modelBuilder);
        ConfigureImportSnapshotEntity(modelBuilder);
        ConfigureMatchEntity(modelBuilder);
        ConfigureMatchKickoutStatEntity(modelBuilder);
        ConfigureMatchPlayerStatEntity(modelBuilder);
        ConfigureMatchSourceAnalysisEntity(modelBuilder);
        ConfigureSeasonEntity(modelBuilder);
        ConfigureTeamEntity(modelBuilder);

        OnModelCreatingPartial(modelBuilder);
    }

    private void ConfigureCompetitionEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Competition>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("competitions_pkey");
            entity.ToTable("competitions", tb => tb.HasComment("Competitions within seasons (League, Championship, Cup formats)"));
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne(d => d.Season).WithMany(p => p.Competitions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("competitions_season_id_fkey");
        });
    }

    private void ConfigureEventOutcomeEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventOutcome>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("event_outcomes_pkey");
            entity.ToTable("event_outcomes", tb => tb.HasComment("Possible outcomes for events with assigned PSR values (-3 to +3)"));
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.PsrValue).HasComment("Performance Success Rate value assigned to this outcome");
            entity.HasOne(d => d.EventType).WithMany(p => p.EventOutcomes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("event_outcomes_event_type_id_fkey");
        });
    }

    private void ConfigureEventTypeEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("event_types_pkey");
            entity.ToTable("event_types", tb => tb.HasComment("KPI event type definitions with default PSR values"));
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.DefaultPsrValue).HasDefaultValue(0);
        });
    }

    private void ConfigureImportHistoryEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ImportHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("import_history_pkey");
            entity.ToTable("import_history", tb => tb.HasComment("Audit trail of all Excel import operations"));
            entity.Property(e => e.EventsCreated).HasDefaultValue(0);
            entity.Property(e => e.ImportStartedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ImportStatus).HasDefaultValueSql("'in_progress'::character varying");
            entity.Property(e => e.ImportType).HasDefaultValueSql("'excel_full_reload'::character varying");
            entity.Property(e => e.MatchesImported).HasDefaultValue(0);
            entity.Property(e => e.PlayersProcessed).HasDefaultValue(0);
        });
    }

    private void ConfigureImportSnapshotEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ImportSnapshot>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("import_snapshots_pkey");
            entity.ToTable("import_snapshots", tb => tb.HasComment("Compressed data snapshots for rollback capability"));
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.TotalMatches).HasDefaultValue(0);
            entity.Property(e => e.TotalPlayerRecords).HasDefaultValue(0);
        });
    }

    private void ConfigureMatchEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Match>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("matches_pkey");
            entity.ToTable("matches", tb => tb.HasComment("Match records cleared and reloaded on each Excel import"));
            entity.Property(e => e.AwayScoreGoals).HasDefaultValue(0);
            entity.Property(e => e.AwayScorePoints).HasDefaultValue(0);
            entity.Property(e => e.ExcelSheetName).HasComment("Source Excel sheet name for import traceability");
            entity.Property(e => e.HomeScoreGoals).HasDefaultValue(0);
            entity.Property(e => e.HomeScorePoints).HasDefaultValue(0);
            entity.Property(e => e.ImportedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.AwayTeam).WithMany(p => p.MatchAwayTeams)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("matches_away_team_id_fkey");
            entity.HasOne(d => d.Competition).WithMany(p => p.Matches)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("matches_competition_id_fkey");
            entity.HasOne(d => d.HomeTeam).WithMany(p => p.MatchHomeTeams)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("matches_home_team_id_fkey");
        });
    }

    private void ConfigureMatchKickoutStatEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MatchKickoutStat>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("match_kickout_stats_pkey");
            entity.ToTable("match_kickout_stats", tb => tb.HasComment("Specialized kickout analysis data for goalkeepers and field players"));
            entity.Property(e => e.ImportedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.KickoutsDownMiddle).HasDefaultValue(0);
            entity.Property(e => e.KickoutsLostBreak).HasDefaultValue(0);
            entity.Property(e => e.KickoutsLostClean).HasDefaultValue(0);
            entity.Property(e => e.KickoutsLostShort).HasDefaultValue(0);
            entity.Property(e => e.KickoutsTaken).HasDefaultValue(0);
            entity.Property(e => e.KickoutsToLeft).HasDefaultValue(0);
            entity.Property(e => e.KickoutsToRight).HasDefaultValue(0);
            entity.Property(e => e.KickoutsWonBreak).HasDefaultValue(0);
            entity.Property(e => e.KickoutsWonClean).HasDefaultValue(0);
            entity.Property(e => e.KickoutsWonShort).HasDefaultValue(0);
            entity.Property(e => e.Saves).HasDefaultValue(0);

            entity.HasOne(d => d.MatchPlayerStat).WithMany(p => p.MatchKickoutStats)
                .HasConstraintName("match_kickout_stats_match_player_stat_id_fkey");
        });
    }

    private void ConfigureMatchPlayerStatEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MatchPlayerStat>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("match_player_stats_pkey");
            entity.ToTable("match_player_stats", tb => tb.HasComment("Comprehensive player statistics (85+ columns) cleared on import"));
            
            // Configure default values for all numeric statistics
            ConfigurePlayerStatDefaults(entity);

            entity.Property(e => e.ImportedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.PerformanceSuccessRate).HasComment("PSR value calculation result (-3.0 to +3.0 range)");
            entity.Property(e => e.PlayerName).HasComment("Player name stored exactly as appears in Excel import");

            entity.HasOne(d => d.Match).WithMany(p => p.MatchPlayerStats)
                .HasConstraintName("match_player_stats_match_id_fkey");
            entity.HasOne(d => d.Team).WithMany(p => p.MatchPlayerStats)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("match_player_stats_team_id_fkey");
        });
    }

    private void ConfigurePlayerStatDefaults(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<MatchPlayerStat> entity)
    {
        entity.Property(e => e.AerialContestsWon).HasDefaultValue(0);
        entity.Property(e => e.AttackingPlays).HasDefaultValue(0);
        entity.Property(e => e.Captain).HasDefaultValue(false);
        entity.Property(e => e.CardsBlack).HasDefaultValue(0);
        entity.Property(e => e.CardsRed).HasDefaultValue(0);
        entity.Property(e => e.CardsYellow).HasDefaultValue(0);
        entity.Property(e => e.CleanCatches).HasDefaultValue(0);
        entity.Property(e => e.DefensiveActions).HasDefaultValue(0);
        entity.Property(e => e.FreesConceded).HasDefaultValue(0);
        entity.Property(e => e.FreesSaved).HasDefaultValue(0);
        entity.Property(e => e.FreesShort).HasDefaultValue(0);
        entity.Property(e => e.FreesWide).HasDefaultValue(0);
        entity.Property(e => e.FreesWon).HasDefaultValue(0);
        entity.Property(e => e.Fumbles).HasDefaultValue(0);
        entity.Property(e => e.GoalsFromFrees).HasDefaultValue(0);
        entity.Property(e => e.GoalsFromPlay).HasDefaultValue(0);
        entity.Property(e => e.GroundBallWins).HasDefaultValue(0);
        entity.Property(e => e.HandPasses).HasDefaultValue(0);
        entity.Property(e => e.Interceptions).HasDefaultValue(0);
        entity.Property(e => e.KickPasses).HasDefaultValue(0);
        entity.Property(e => e.MinutesPlayed).HasDefaultValue(0);
        entity.Property(e => e.PointsFromFrees).HasDefaultValue(0);
        entity.Property(e => e.PointsFromPlay).HasDefaultValue(0);
        entity.Property(e => e.PossessionsLost).HasDefaultValue(0);
        entity.Property(e => e.ScoreAssistsGoals).HasDefaultValue(0);
        entity.Property(e => e.ScoreAssistsPoints).HasDefaultValue(0);
        entity.Property(e => e.ShotsBlocked).HasDefaultValue(0);
        entity.Property(e => e.ShotsSaved).HasDefaultValue(0);
        entity.Property(e => e.ShotsShort).HasDefaultValue(0);
        entity.Property(e => e.ShotsWide).HasDefaultValue(0);
        entity.Property(e => e.ShotsWoodwork).HasDefaultValue(0);
        entity.Property(e => e.TacklesMade).HasDefaultValue(0);
        entity.Property(e => e.TacklesMissed).HasDefaultValue(0);
        entity.Property(e => e.TotalEvents).HasDefaultValue(0);
        entity.Property(e => e.TotalPossessions).HasDefaultValue(0);
        entity.Property(e => e.TurnoversWon).HasDefaultValue(0);
        entity.Property(e => e.TwoPointersFromPlay).HasDefaultValue(0);
    }

    private void ConfigureMatchSourceAnalysisEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MatchSourceAnalysis>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("match_source_analysis_pkey");
            entity.ToTable("match_source_analysis", tb => tb.HasComment("Attack/shot source analysis for tactical insights"));
            entity.Property(e => e.ImportedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.SuccessfulCount).HasDefaultValue(0);
            entity.Property(e => e.TotalCount).HasDefaultValue(0);

            entity.HasOne(d => d.Match).WithMany(p => p.MatchSourceAnalyses)
                .HasConstraintName("match_source_analysis_match_id_fkey");
            entity.HasOne(d => d.Team).WithMany(p => p.MatchSourceAnalyses)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("match_source_analysis_team_id_fkey");
        });
    }

    private void ConfigureSeasonEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Season>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("seasons_pkey");
            entity.ToTable("seasons", tb => tb.HasComment("Season definitions for multi-year data management"));
            entity.HasIndex(e => e.IsCurrent, "unique_current_season")
                .IsUnique()
                .HasFilter("(is_current = true)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsCurrent).HasDefaultValue(false);
        });
    }

    private void ConfigureTeamEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("teams_pkey");
            entity.ToTable("teams", tb => tb.HasComment("GAA teams participating in matches - reference data not cleared on import"));
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}