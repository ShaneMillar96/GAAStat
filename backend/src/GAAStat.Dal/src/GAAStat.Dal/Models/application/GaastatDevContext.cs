using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.src.GAAStat.Dal.Models.application;

public partial class GaastatDevContext : DbContext
{
    public GaastatDevContext()
    {
    }

    public GaastatDevContext(DbContextOptions<GaastatDevContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Competition> Competitions { get; set; }

    public virtual DbSet<CompetitionType> CompetitionTypes { get; set; }

    public virtual DbSet<EtlJob> EtlJobs { get; set; }

    public virtual DbSet<EtlJobProgress> EtlJobProgresses { get; set; }

    public virtual DbSet<EtlValidationError> EtlValidationErrors { get; set; }

    public virtual DbSet<FlywaySchemaHistory> FlywaySchemaHistories { get; set; }

    public virtual DbSet<FreeType> FreeTypes { get; set; }

    public virtual DbSet<KickoutAnalysis> KickoutAnalyses { get; set; }

    public virtual DbSet<KickoutType> KickoutTypes { get; set; }

    public virtual DbSet<KpiDefinition> KpiDefinitions { get; set; }

    public virtual DbSet<Match> Matches { get; set; }

    public virtual DbSet<MatchPlayerStatistic> MatchPlayerStatistics { get; set; }

    public virtual DbSet<MatchResult> MatchResults { get; set; }

    public virtual DbSet<MatchTeamStatistic> MatchTeamStatistics { get; set; }

    public virtual DbSet<MetricCategory> MetricCategories { get; set; }

    public virtual DbSet<MetricDefinition> MetricDefinitions { get; set; }

    public virtual DbSet<Player> Players { get; set; }

    public virtual DbSet<Position> Positions { get; set; }

    public virtual DbSet<PositionArea> PositionAreas { get; set; }

    public virtual DbSet<PositionAverage> PositionAverages { get; set; }

    public virtual DbSet<PositionalAnalysis> PositionalAnalyses { get; set; }

    public virtual DbSet<ScoreableFreeAnalysis> ScoreableFreeAnalyses { get; set; }

    public virtual DbSet<Season> Seasons { get; set; }

    public virtual DbSet<SeasonPlayerTotal> SeasonPlayerTotals { get; set; }

    public virtual DbSet<ShotAnalysis> ShotAnalyses { get; set; }

    public virtual DbSet<ShotOutcome> ShotOutcomes { get; set; }

    public virtual DbSet<ShotType> ShotTypes { get; set; }

    public virtual DbSet<Team> Teams { get; set; }

    public virtual DbSet<TeamType> TeamTypes { get; set; }

    public virtual DbSet<TimePeriod> TimePeriods { get; set; }

    public virtual DbSet<Venue> Venues { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Database=gaastat-dev;Username=gaastat;Password=password1;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Competition>(entity =>
        {
            entity.HasKey(e => e.CompetitionId).HasName("competitions_pkey");

            entity.ToTable("competitions", tb => tb.HasComment("Competition master data"));

            entity.HasOne(d => d.CompetitionType).WithMany(p => p.Competitions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("competitions_competition_type_id_fkey");
        });

        modelBuilder.Entity<CompetitionType>(entity =>
        {
            entity.HasKey(e => e.CompetitionTypeId).HasName("competition_types_pkey");

            entity.ToTable("competition_types", tb => tb.HasComment("Classification of competition types (League, Championship, Cup)"));
        });

        modelBuilder.Entity<EtlJob>(entity =>
        {
            entity.HasKey(e => e.JobId).HasName("etl_jobs_pkey");

            entity.ToTable("etl_jobs", tb => tb.HasComment("Master table tracking Excel ETL job execution status and metadata"));

            entity.Property(e => e.JobId).HasComment("Unique identifier for ETL job");
            entity.Property(e => e.CompletedAt).HasComment("Timestamp when job finished (success or failure)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.CreatedBy).HasComment("User or system that initiated the ETL job");
            entity.Property(e => e.ErrorSummary).HasComment("High-level summary of errors if job failed");
            entity.Property(e => e.FileName).HasComment("Original name of uploaded Excel file");
            entity.Property(e => e.FileSizeBytes).HasComment("Size of uploaded file in bytes");
            entity.Property(e => e.StartedAt).HasComment("Timestamp when job processing began");
            entity.Property(e => e.Status).HasComment("Current job status: pending, processing, completed, failed");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<EtlJobProgress>(entity =>
        {
            entity.HasKey(e => e.ProgressId).HasName("etl_job_progress_pkey");

            entity.ToTable("etl_job_progress", tb => tb.HasComment("Stage-by-stage progress tracking for ETL jobs with detailed status updates"));

            entity.Property(e => e.ProgressId).HasComment("Unique identifier for progress entry");
            entity.Property(e => e.CompletedSteps).HasComment("Number of steps completed in this stage");
            entity.Property(e => e.JobId).HasComment("Reference to parent ETL job");
            entity.Property(e => e.Message).HasComment("Detailed progress message for user feedback");
            entity.Property(e => e.Stage).HasComment("Current processing stage (e.g., \"Parsing Excel\", \"Validating Data\")");
            entity.Property(e => e.Status).HasComment("Status of current stage");
            entity.Property(e => e.TotalSteps).HasComment("Total number of steps for this stage");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Job).WithMany(p => p.EtlJobProgresses).HasConstraintName("etl_job_progress_job_id_fkey");
        });

        modelBuilder.Entity<EtlValidationError>(entity =>
        {
            entity.HasKey(e => e.ErrorId).HasName("etl_validation_errors_pkey");

            entity.ToTable("etl_validation_errors", tb => tb.HasComment("Detailed error tracking for data validation issues during ETL processing"));

            entity.Property(e => e.ErrorId).HasComment("Unique identifier for validation error");
            entity.Property(e => e.ColumnName).HasComment("Column name where error occurred");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ErrorMessage).HasComment("Detailed error description");
            entity.Property(e => e.ErrorType).HasComment("Category of validation error");
            entity.Property(e => e.JobId).HasComment("Reference to ETL job that encountered this error");
            entity.Property(e => e.RowNumber).HasComment("Row number in sheet where error was found");
            entity.Property(e => e.SheetName).HasComment("Excel sheet name where error occurred");
            entity.Property(e => e.SuggestedFix).HasComment("Recommended action to resolve the error");

            entity.HasOne(d => d.Job).WithMany(p => p.EtlValidationErrors).HasConstraintName("etl_validation_errors_job_id_fkey");
        });

        modelBuilder.Entity<FlywaySchemaHistory>(entity =>
        {
            entity.HasKey(e => e.InstalledRank).HasName("flyway_schema_history_pk");

            entity.Property(e => e.InstalledRank).ValueGeneratedNever();
            entity.Property(e => e.InstalledOn).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<FreeType>(entity =>
        {
            entity.HasKey(e => e.FreeTypeId).HasName("free_types_pkey");

            entity.ToTable("free_types", tb => tb.HasComment("Free kick types (Standard, Quick)"));
        });

        modelBuilder.Entity<KickoutAnalysis>(entity =>
        {
            entity.HasKey(e => e.KickoutAnalysisId).HasName("kickout_analysis_pkey");

            entity.ToTable("kickout_analysis", tb => tb.HasComment("Detailed kickout performance tracking by type and period"));

            entity.HasIndex(e => e.OutcomeBreakdown, "idx_kickout_outcome_breakdown").HasMethod("gin");

            entity.HasIndex(e => new { e.MatchId, e.SuccessRate }, "idx_successful_kickouts").HasFilter("(success_rate > 0.5)");

            entity.Property(e => e.OutcomeBreakdown).HasComment("JSONB object containing detailed outcome statistics");
            entity.Property(e => e.Successful).HasDefaultValue(0);
            entity.Property(e => e.TotalAttempts).HasDefaultValue(0);

            entity.HasOne(d => d.KickoutType).WithMany(p => p.KickoutAnalyses).HasConstraintName("kickout_analysis_kickout_type_id_fkey");

            entity.HasOne(d => d.Match).WithMany(p => p.KickoutAnalyses).HasConstraintName("kickout_analysis_match_id_fkey");

            entity.HasOne(d => d.TeamType).WithMany(p => p.KickoutAnalyses).HasConstraintName("kickout_analysis_team_type_id_fkey");

            entity.HasOne(d => d.TimePeriod).WithMany(p => p.KickoutAnalyses).HasConstraintName("kickout_analysis_time_period_id_fkey");
        });

        modelBuilder.Entity<KickoutType>(entity =>
        {
            entity.HasKey(e => e.KickoutTypeId).HasName("kickout_types_pkey");

            entity.ToTable("kickout_types", tb => tb.HasComment("Kickout classifications (Long, Short)"));
        });

        modelBuilder.Entity<KpiDefinition>(entity =>
        {
            entity.HasKey(e => e.KpiId).HasName("kpi_definitions_pkey");

            entity.ToTable("kpi_definitions", tb => tb.HasComment("KPI definitions with formulas and benchmark values"));

            entity.HasIndex(e => e.BenchmarkValues, "idx_kpi_definitions_benchmarks").HasMethod("gin");
        });

        modelBuilder.Entity<Match>(entity =>
        {
            entity.HasKey(e => e.MatchId).HasName("matches_pkey");

            entity.ToTable("matches", tb => tb.HasComment("Match information, results, and basic statistics"));

            entity.Property(e => e.DrumGoals)
                .HasDefaultValue(0)
                .HasComment("Number of goals scored by home team");
            entity.Property(e => e.DrumPoints)
                .HasDefaultValue(0)
                .HasComment("Number of points scored by home team");
            entity.Property(e => e.DrumScore).HasComment("Formatted score string for home team (e.g., \"2-12\")");
            entity.Property(e => e.MatchNumber).HasComment("Sequential match number for the season");
            entity.Property(e => e.OppositionGoals)
                .HasDefaultValue(0)
                .HasComment("Number of goals scored by opposition");
            entity.Property(e => e.OppositionPoints)
                .HasDefaultValue(0)
                .HasComment("Number of points scored by opposition");
            entity.Property(e => e.OppositionScore).HasComment("Formatted score string for opposition (e.g., \"1-08\")");
            entity.Property(e => e.PointDifference).HasComment("Total point difference (positive = home win)");

            entity.HasOne(d => d.Competition).WithMany(p => p.Matches).HasConstraintName("matches_competition_id_fkey");

            entity.HasOne(d => d.MatchResult).WithMany(p => p.Matches).HasConstraintName("matches_match_result_id_fkey");

            entity.HasOne(d => d.Opposition).WithMany(p => p.Matches).HasConstraintName("matches_opposition_id_fkey");

            entity.HasOne(d => d.Season).WithMany(p => p.Matches).HasConstraintName("matches_season_id_fkey");

            entity.HasOne(d => d.Venue).WithMany(p => p.Matches).HasConstraintName("matches_venue_id_fkey");
        });

        modelBuilder.Entity<MatchPlayerStatistic>(entity =>
        {
            entity.HasKey(e => e.MatchPlayerStatId).HasName("match_player_statistics_pkey");

            entity.ToTable("match_player_statistics", tb => tb.HasComment("Individual player performance data (80+ fields per player per match)"));

            entity.HasIndex(e => e.Goals, "idx_match_player_stats_goals").HasFilter("(goals > 0)");

            entity.HasIndex(e => e.Points, "idx_match_player_stats_points").HasFilter("(points > 0)");

            entity.HasIndex(e => e.EngagementEfficiency, "idx_player_efficiency")
                .IsDescending()
                .HasFilter("(engagement_efficiency IS NOT NULL)");

            entity.HasIndex(e => new { e.Goals, e.Points }, "idx_top_scorers")
                .IsDescending()
                .HasFilter("((goals > 0) OR (points > 0))");

            entity.Property(e => e.BlackCards).HasDefaultValue(0);
            entity.Property(e => e.CarryLost).HasDefaultValue(0);
            entity.Property(e => e.CarryRetained).HasDefaultValue(0);
            entity.Property(e => e.EngagementEfficiency).HasComment("Player engagement efficiency rate (0-1)");
            entity.Property(e => e.FreesConcededTotal).HasDefaultValue(0);
            entity.Property(e => e.Goals).HasDefaultValue(0);
            entity.Property(e => e.Interceptions).HasDefaultValue(0);
            entity.Property(e => e.KickLost).HasDefaultValue(0);
            entity.Property(e => e.KickRetained).HasDefaultValue(0);
            entity.Property(e => e.KickoutsLost).HasDefaultValue(0);
            entity.Property(e => e.KickoutsRetained).HasDefaultValue(0);
            entity.Property(e => e.KickoutsTotal).HasDefaultValue(0);
            entity.Property(e => e.MinutesPlayed).HasDefaultValue(0);
            entity.Property(e => e.Points).HasDefaultValue(0);
            entity.Property(e => e.PossessionSuccessRate).HasComment("Success rate for possessions (0-1)");
            entity.Property(e => e.PossessionsPerTe).HasComment("Possessions per total engagement");
            entity.Property(e => e.RedCards).HasDefaultValue(0);
            entity.Property(e => e.Saves).HasDefaultValue(0);
            entity.Property(e => e.ShotsTotal).HasDefaultValue(0);
            entity.Property(e => e.TacklesContact).HasDefaultValue(0);
            entity.Property(e => e.TacklesMissed).HasDefaultValue(0);
            entity.Property(e => e.TacklesTotal).HasDefaultValue(0);
            entity.Property(e => e.TotalAttacks).HasDefaultValue(0);
            entity.Property(e => e.TotalEngagements).HasDefaultValue(0);
            entity.Property(e => e.TotalPossessions).HasDefaultValue(0);
            entity.Property(e => e.TurnoversWon).HasDefaultValue(0);
            entity.Property(e => e.Wides).HasDefaultValue(0);
            entity.Property(e => e.YellowCards).HasDefaultValue(0);

            entity.HasOne(d => d.Match).WithMany(p => p.MatchPlayerStatistics).HasConstraintName("match_player_statistics_match_id_fkey");

            entity.HasOne(d => d.Player).WithMany(p => p.MatchPlayerStatistics)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("match_player_statistics_player_id_fkey");
        });

        modelBuilder.Entity<MatchResult>(entity =>
        {
            entity.HasKey(e => e.MatchResultId).HasName("match_results_pkey");

            entity.ToTable("match_results", tb => tb.HasComment("Match outcome types (Win, Loss, Draw)"));
        });

        modelBuilder.Entity<MatchTeamStatistic>(entity =>
        {
            entity.HasKey(e => e.MatchTeamStatId).HasName("match_team_statistics_pkey");

            entity.ToTable("match_team_statistics", tb => tb.HasComment("Detailed team-level performance metrics (235+ data points per match)"));

            entity.Property(e => e.DrumFirstHalf).HasComment("Home team metric value for first half");
            entity.Property(e => e.DrumFullGame).HasComment("Home team metric value for full game");
            entity.Property(e => e.DrumSecondHalf).HasComment("Home team metric value for second half");

            entity.HasOne(d => d.Match).WithMany(p => p.MatchTeamStatistics).HasConstraintName("match_team_statistics_match_id_fkey");

            entity.HasOne(d => d.MetricDefinition).WithMany(p => p.MatchTeamStatistics)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("match_team_statistics_metric_definition_id_fkey");
        });

        modelBuilder.Entity<MetricCategory>(entity =>
        {
            entity.HasKey(e => e.MetricCategoryId).HasName("metric_categories_pkey");

            entity.ToTable("metric_categories", tb => tb.HasComment("Statistical metric category groupings"));
        });

        modelBuilder.Entity<MetricDefinition>(entity =>
        {
            entity.HasKey(e => e.MetricId).HasName("metric_definitions_pkey");

            entity.ToTable("metric_definitions", tb => tb.HasComment("Statistical metric explanations and calculation methods"));

            entity.HasOne(d => d.MetricCategory).WithMany(p => p.MetricDefinitions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("metric_definitions_metric_category_id_fkey");
        });

        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasKey(e => e.PlayerId).HasName("players_pkey");

            entity.ToTable("players", tb => tb.HasComment("Player master data and position information"));

            entity.HasIndex(e => e.PositionId, "idx_active_players_by_position").HasFilter("(is_active = true)");

            entity.HasIndex(e => e.IsActive, "idx_players_active").HasFilter("(is_active = true)");

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasComment("Whether the player is currently active");
            entity.Property(e => e.JerseyNumber).HasComment("Jersey number (1-99)");
            entity.Property(e => e.PlayerName).HasComment("Full name of the player");

            entity.HasOne(d => d.Position).WithMany(p => p.Players).HasConstraintName("players_position_id_fkey");
        });

        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasKey(e => e.PositionId).HasName("positions_pkey");

            entity.ToTable("positions", tb => tb.HasComment("Playing position definitions and categories"));
        });

        modelBuilder.Entity<PositionArea>(entity =>
        {
            entity.HasKey(e => e.PositionAreaId).HasName("position_areas_pkey");

            entity.ToTable("position_areas", tb => tb.HasComment("Field position areas (Attacking Third, Middle Third, Defensive Third)"));
        });

        modelBuilder.Entity<PositionAverage>(entity =>
        {
            entity.HasKey(e => e.PositionAvgId).HasName("position_averages_pkey");

            entity.ToTable("position_averages", tb => tb.HasComment("Position-based benchmark comparisons"));

            entity.Property(e => e.AvgPossessionsPerGame).HasComment("Average possessions per game for this position");
            entity.Property(e => e.AvgScoresPerGame).HasComment("Average scores per game for this position");
            entity.Property(e => e.AvgTacklesPerGame).HasComment("Average tackles per game for this position");

            entity.HasOne(d => d.Position).WithMany(p => p.PositionAverages).HasConstraintName("position_averages_position_id_fkey");

            entity.HasOne(d => d.Season).WithMany(p => p.PositionAverages).HasConstraintName("position_averages_season_id_fkey");
        });

        modelBuilder.Entity<PositionalAnalysis>(entity =>
        {
            entity.HasKey(e => e.PositionalAnalysisId).HasName("positional_analysis_pkey");

            entity.ToTable("positional_analysis", tb => tb.HasComment("Position-based aggregated statistics per match"));

            entity.Property(e => e.TotalPossessions).HasDefaultValue(0);
            entity.Property(e => e.TotalScores).HasDefaultValue(0);
            entity.Property(e => e.TotalTackles).HasDefaultValue(0);

            entity.HasOne(d => d.Match).WithMany(p => p.PositionalAnalyses).HasConstraintName("positional_analysis_match_id_fkey");

            entity.HasOne(d => d.Position).WithMany(p => p.PositionalAnalyses)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("positional_analysis_position_id_fkey");
        });

        modelBuilder.Entity<ScoreableFreeAnalysis>(entity =>
        {
            entity.HasKey(e => e.ScoreableFreeId).HasName("scoreable_free_analysis_pkey");

            entity.ToTable("scoreable_free_analysis", tb => tb.HasComment("Free kick performance with distance and success tracking"));

            entity.Property(e => e.Distance).HasComment("Distance description of the free kick");
            entity.Property(e => e.Success).HasComment("Whether the free kick was successful");

            entity.HasOne(d => d.FreeType).WithMany(p => p.ScoreableFreeAnalyses).HasConstraintName("scoreable_free_analysis_free_type_id_fkey");

            entity.HasOne(d => d.Match).WithMany(p => p.ScoreableFreeAnalyses).HasConstraintName("scoreable_free_analysis_match_id_fkey");

            entity.HasOne(d => d.Player).WithMany(p => p.ScoreableFreeAnalyses).HasConstraintName("scoreable_free_analysis_player_id_fkey");

            entity.HasOne(d => d.ShotOutcome).WithMany(p => p.ScoreableFreeAnalyses).HasConstraintName("scoreable_free_analysis_shot_outcome_id_fkey");
        });

        modelBuilder.Entity<Season>(entity =>
        {
            entity.HasKey(e => e.SeasonId).HasName("seasons_pkey");

            entity.ToTable("seasons", tb => tb.HasComment("Season definitions and date ranges"));

            entity.HasIndex(e => e.IsCurrent, "idx_seasons_current").HasFilter("(is_current = true)");

            entity.Property(e => e.IsCurrent).HasDefaultValue(false);
        });

        modelBuilder.Entity<SeasonPlayerTotal>(entity =>
        {
            entity.HasKey(e => e.SeasonTotalId).HasName("season_player_totals_pkey");

            entity.ToTable("season_player_totals", tb => tb.HasComment("Player season statistics and averages"));

            entity.Property(e => e.AvgEngagementEfficiency).HasComment("Average engagement efficiency across all games");
            entity.Property(e => e.GamesPlayed)
                .HasDefaultValue(0)
                .HasComment("Number of games played in the season");
            entity.Property(e => e.TotalGoals).HasDefaultValue(0);
            entity.Property(e => e.TotalInterceptions).HasDefaultValue(0);
            entity.Property(e => e.TotalMinutes)
                .HasDefaultValue(0)
                .HasComment("Total minutes played in the season");
            entity.Property(e => e.TotalPoints).HasDefaultValue(0);
            entity.Property(e => e.TotalScores)
                .HasDefaultValue(0)
                .HasComment("Total combined goals and points scored");
            entity.Property(e => e.TotalTackles).HasDefaultValue(0);
            entity.Property(e => e.TotalTurnoversWon).HasDefaultValue(0);

            entity.HasOne(d => d.Player).WithMany(p => p.SeasonPlayerTotals).HasConstraintName("season_player_totals_player_id_fkey");

            entity.HasOne(d => d.Season).WithMany(p => p.SeasonPlayerTotals).HasConstraintName("season_player_totals_season_id_fkey");
        });

        modelBuilder.Entity<ShotAnalysis>(entity =>
        {
            entity.HasKey(e => e.ShotAnalysisId).HasName("shot_analysis_pkey");

            entity.ToTable("shot_analysis", tb => tb.HasComment("Individual shot tracking with outcome and location data"));

            entity.Property(e => e.ShotNumber).HasComment("Sequential shot number within the match");
            entity.Property(e => e.TimePeriod).HasComment("Time period when shot was taken");

            entity.HasOne(d => d.Match).WithMany(p => p.ShotAnalyses).HasConstraintName("shot_analysis_match_id_fkey");

            entity.HasOne(d => d.Player).WithMany(p => p.ShotAnalyses).HasConstraintName("shot_analysis_player_id_fkey");

            entity.HasOne(d => d.PositionArea).WithMany(p => p.ShotAnalyses).HasConstraintName("shot_analysis_position_area_id_fkey");

            entity.HasOne(d => d.ShotOutcome).WithMany(p => p.ShotAnalyses).HasConstraintName("shot_analysis_shot_outcome_id_fkey");

            entity.HasOne(d => d.ShotType).WithMany(p => p.ShotAnalyses).HasConstraintName("shot_analysis_shot_type_id_fkey");
        });

        modelBuilder.Entity<ShotOutcome>(entity =>
        {
            entity.HasKey(e => e.ShotOutcomeId).HasName("shot_outcomes_pkey");

            entity.ToTable("shot_outcomes", tb => tb.HasComment("Shot outcome types (Goal, Point, Wide, Save, etc.)"));

            entity.Property(e => e.IsScore).HasDefaultValue(false);
        });

        modelBuilder.Entity<ShotType>(entity =>
        {
            entity.HasKey(e => e.ShotTypeId).HasName("shot_types_pkey");

            entity.ToTable("shot_types", tb => tb.HasComment("Shot type classifications (From Play, Free Kick, Penalty)"));
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(e => e.TeamId).HasName("teams_pkey");

            entity.ToTable("teams", tb => tb.HasComment("Opposition team information"));
        });

        modelBuilder.Entity<TeamType>(entity =>
        {
            entity.HasKey(e => e.TeamTypeId).HasName("team_types_pkey");

            entity.ToTable("team_types", tb => tb.HasComment("Team type designations (Drum, Opposition)"));
        });

        modelBuilder.Entity<TimePeriod>(entity =>
        {
            entity.HasKey(e => e.TimePeriodId).HasName("time_periods_pkey");

            entity.ToTable("time_periods", tb => tb.HasComment("Game period classifications (First Half, Second Half, Full Game)"));
        });

        modelBuilder.Entity<Venue>(entity =>
        {
            entity.HasKey(e => e.VenueId).HasName("venues_pkey");

            entity.ToTable("venues", tb => tb.HasComment("Match venue designations (Home/Away)"));
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
