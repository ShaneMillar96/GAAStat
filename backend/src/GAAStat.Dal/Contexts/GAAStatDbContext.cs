using Microsoft.EntityFrameworkCore;
using GAAStat.Dal.Interfaces;
using GAAStat.Dal.Models.Application;

namespace GAAStat.Dal.Contexts;

public partial class GAAStatDbContext : DbContext, IGAAStatDbContext
{
    public GAAStatDbContext(DbContextOptions<GAAStatDbContext> options) : base(options)
    {
    }

    // Reference Tables
    public virtual DbSet<CompetitionType> CompetitionTypes { get; set; }
    public virtual DbSet<Venue> Venues { get; set; }
    public virtual DbSet<MatchResult> MatchResults { get; set; }
    public virtual DbSet<Season> Seasons { get; set; }
    public virtual DbSet<Position> Positions { get; set; }
    public virtual DbSet<MetricCategory> MetricCategories { get; set; }
    public virtual DbSet<MetricDefinition> MetricDefinitions { get; set; }
    public virtual DbSet<KpiDefinition> KpiDefinitions { get; set; }
    public virtual DbSet<TimePeriod> TimePeriods { get; set; }
    public virtual DbSet<KickoutType> KickoutTypes { get; set; }
    public virtual DbSet<TeamType> TeamTypes { get; set; }
    public virtual DbSet<ShotType> ShotTypes { get; set; }
    public virtual DbSet<ShotOutcome> ShotOutcomes { get; set; }
    public virtual DbSet<PositionArea> PositionAreas { get; set; }
    public virtual DbSet<FreeType> FreeTypes { get; set; }
    public virtual DbSet<Competition> Competitions { get; set; }
    public virtual DbSet<Team> Teams { get; set; }

    // Core Entity Tables
    public virtual DbSet<Player> Players { get; set; }
    public virtual DbSet<Match> Matches { get; set; }

    // Statistics Tables
    public virtual DbSet<MatchTeamStatistics> MatchTeamStatistics { get; set; }
    public virtual DbSet<MatchPlayerStatistics> MatchPlayerStatistics { get; set; }
    public virtual DbSet<KickoutAnalysis> KickoutAnalyses { get; set; }
    public virtual DbSet<ShotAnalysis> ShotAnalyses { get; set; }
    public virtual DbSet<ScoreableFreeAnalysis> ScoreableFreeAnalyses { get; set; }
    public virtual DbSet<PositionalAnalysis> PositionalAnalyses { get; set; }

    // Aggregation Tables
    public virtual DbSet<SeasonPlayerTotal> SeasonPlayerTotals { get; set; }
    public virtual DbSet<PositionAverage> PositionAverages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure entity relationships and constraints
        ConfigureReferenceEntities(modelBuilder);
        ConfigureCoreEntities(modelBuilder);
        ConfigureStatisticsEntities(modelBuilder);
        ConfigureAggregationEntities(modelBuilder);
    }

    private static void ConfigureReferenceEntities(ModelBuilder modelBuilder)
    {
        // Competition Types
        modelBuilder.Entity<CompetitionType>(entity =>
        {
            entity.HasIndex(e => e.TypeName).IsUnique();
        });

        // Venues
        modelBuilder.Entity<Venue>(entity =>
        {
            entity.HasIndex(e => e.VenueCode).IsUnique();
        });

        // Match Results
        modelBuilder.Entity<MatchResult>(entity =>
        {
            entity.HasIndex(e => e.ResultCode).IsUnique();
        });

        // Seasons
        modelBuilder.Entity<Season>(entity =>
        {
            entity.HasIndex(e => e.SeasonName).IsUnique();
            entity.HasCheckConstraint("CK_Season_DateRange", "end_date > start_date");
        });

        // Positions
        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasIndex(e => e.PositionName).IsUnique();
        });

        // Metric Categories
        modelBuilder.Entity<MetricCategory>(entity =>
        {
            entity.HasIndex(e => e.CategoryName).IsUnique();
        });

        // Metric Definitions
        modelBuilder.Entity<MetricDefinition>(entity =>
        {
            entity.HasIndex(e => e.MetricName).IsUnique();
        });

        // KPI Definitions
        modelBuilder.Entity<KpiDefinition>(entity =>
        {
            entity.HasIndex(e => e.KpiCode).IsUnique();
        });

        // Teams
        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasIndex(e => e.TeamName).IsUnique();
        });
    }

    private static void ConfigureCoreEntities(ModelBuilder modelBuilder)
    {
        // Players
        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasCheckConstraint("CK_Player_JerseyNumber", "jersey_number BETWEEN 1 AND 99");
        });

        // Matches
        modelBuilder.Entity<Match>(entity =>
        {
            entity.HasCheckConstraint("CK_Match_Scores", 
                "drum_goals >= 0 AND drum_points >= 0 AND opposition_goals >= 0 AND opposition_points >= 0");
            entity.HasCheckConstraint("CK_Match_PointDifference", 
                "point_difference = (drum_goals * 3 + drum_points) - (opposition_goals * 3 + opposition_points)");
        });
    }

    private static void ConfigureStatisticsEntities(ModelBuilder modelBuilder)
    {
        // Match Player Statistics
        modelBuilder.Entity<MatchPlayerStatistics>(entity =>
        {
            entity.HasCheckConstraint("CK_MatchPlayerStats_Minutes", "minutes_played >= 0 AND minutes_played <= 120");
            entity.HasCheckConstraint("CK_MatchPlayerStats_Percentages", 
                "(engagement_efficiency IS NULL OR (engagement_efficiency >= 0 AND engagement_efficiency <= 1)) AND " +
                "(possession_success_rate IS NULL OR (possession_success_rate >= 0 AND possession_success_rate <= 1)) AND " +
                "(conversion_rate IS NULL OR (conversion_rate >= 0 AND conversion_rate <= 1)) AND " +
                "(tackle_percentage IS NULL OR (tackle_percentage >= 0 AND tackle_percentage <= 1)) AND " +
                "(kickout_percentage IS NULL OR (kickout_percentage >= 0 AND kickout_percentage <= 1))");
            entity.HasCheckConstraint("CK_MatchPlayerStats_Cards", "yellow_cards >= 0 AND black_cards >= 0 AND red_cards >= 0");
            entity.HasCheckConstraint("CK_MatchPlayerStats_Scores", "goals >= 0 AND points >= 0 AND wides >= 0 AND shots_total >= 0");
        });

        // Kickout Analysis
        modelBuilder.Entity<KickoutAnalysis>(entity =>
        {
            entity.HasCheckConstraint("CK_KickoutAnalysis_SuccessRate", "success_rate IS NULL OR (success_rate >= 0 AND success_rate <= 1)");
            entity.HasCheckConstraint("CK_KickoutAnalysis_Attempts", "successful <= total_attempts");
        });

        // Shot Analysis
        modelBuilder.Entity<ShotAnalysis>(entity =>
        {
            entity.HasCheckConstraint("CK_ShotAnalysis_ShotNumber", "shot_number > 0");
        });

        // Scoreable Free Analysis
        modelBuilder.Entity<ScoreableFreeAnalysis>(entity =>
        {
            entity.HasCheckConstraint("CK_ScoreableFree_FreeNumber", "free_number > 0");
        });

        // Positional Analysis
        modelBuilder.Entity<PositionalAnalysis>(entity =>
        {
            entity.HasCheckConstraint("CK_PositionalAnalysis_Percentages", 
                "(avg_engagement_efficiency IS NULL OR (avg_engagement_efficiency >= 0 AND avg_engagement_efficiency <= 1)) AND " +
                "(avg_possession_success_rate IS NULL OR (avg_possession_success_rate >= 0 AND avg_possession_success_rate <= 1)) AND " +
                "(avg_conversion_rate IS NULL OR (avg_conversion_rate >= 0 AND avg_conversion_rate <= 1)) AND " +
                "(avg_tackle_success_rate IS NULL OR (avg_tackle_success_rate >= 0 AND avg_tackle_success_rate <= 1))");
        });
    }

    private static void ConfigureAggregationEntities(ModelBuilder modelBuilder)
    {
        // Season Player Totals
        modelBuilder.Entity<SeasonPlayerTotal>(entity =>
        {
            entity.HasIndex(e => new { e.PlayerId, e.SeasonId }).IsUnique();
            entity.HasCheckConstraint("CK_SeasonPlayerTotal_Games", "games_played >= 0");
            entity.HasCheckConstraint("CK_SeasonPlayerTotal_Minutes", "total_minutes >= 0");
            entity.HasCheckConstraint("CK_SeasonPlayerTotal_Percentages", 
                "(avg_engagement_efficiency IS NULL OR (avg_engagement_efficiency >= 0 AND avg_engagement_efficiency <= 1)) AND " +
                "(avg_possession_success_rate IS NULL OR (avg_possession_success_rate >= 0 AND avg_possession_success_rate <= 1)) AND " +
                "(avg_conversion_rate IS NULL OR (avg_conversion_rate >= 0 AND avg_conversion_rate <= 1)) AND " +
                "(avg_tackle_success_rate IS NULL OR (avg_tackle_success_rate >= 0 AND avg_tackle_success_rate <= 1))");
        });

        // Position Averages
        modelBuilder.Entity<PositionAverage>(entity =>
        {
            entity.HasIndex(e => new { e.PositionId, e.SeasonId }).IsUnique();
            entity.HasCheckConstraint("CK_PositionAverage_Percentages", 
                "(avg_engagement_efficiency IS NULL OR (avg_engagement_efficiency >= 0 AND avg_engagement_efficiency <= 1)) AND " +
                "(avg_possession_success_rate IS NULL OR (avg_possession_success_rate >= 0 AND avg_possession_success_rate <= 1)) AND " +
                "(avg_conversion_rate IS NULL OR (avg_conversion_rate >= 0 AND avg_conversion_rate <= 1)) AND " +
                "(avg_tackle_success_rate IS NULL OR (avg_tackle_success_rate >= 0 AND avg_tackle_success_rate <= 1))");
            entity.HasCheckConstraint("CK_PositionAverage_Games", 
                "avg_scores_per_game >= 0 AND avg_possessions_per_game >= 0 AND avg_tackles_per_game >= 0");
        });
    }
}