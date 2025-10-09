using Microsoft.EntityFrameworkCore;
using GAAStat.Dal.Interfaces;
using GAAStat.Dal.Models.Application;

namespace GAAStat.Dal.Contexts;

public class GAAStatDbContext : DbContext, IGAAStatDbContext
{
    public GAAStatDbContext(DbContextOptions<GAAStatDbContext> options) : base(options)
    {
    }

    // DbSets for all 9 tables
    public DbSet<Season> Seasons { get; set; } = null!;
    public DbSet<Position> Positions { get; set; } = null!;
    public DbSet<Team> Teams { get; set; } = null!;
    public DbSet<Competition> Competitions { get; set; } = null!;
    public DbSet<Player> Players { get; set; } = null!;
    public DbSet<Match> Matches { get; set; } = null!;
    public DbSet<MatchTeamStatistics> MatchTeamStatistics { get; set; } = null!;
    public DbSet<PlayerMatchStatistics> PlayerMatchStatistics { get; set; } = null!;
    public DbSet<KpiDefinition> KpiDefinitions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureSeasons(modelBuilder);
        ConfigurePositions(modelBuilder);
        ConfigureTeams(modelBuilder);
        ConfigureCompetitions(modelBuilder);
        ConfigurePlayers(modelBuilder);
        ConfigureMatches(modelBuilder);
        ConfigureMatchTeamStatistics(modelBuilder);
        ConfigurePlayerMatchStatistics(modelBuilder);
        ConfigureKpiDefinitions(modelBuilder);
    }

    private void ConfigureSeasons(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Season>(entity =>
        {
            entity.ToTable("seasons");
            entity.HasKey(e => e.SeasonId);
            entity.Property(e => e.SeasonId).HasColumnName("season_id");
            entity.Property(e => e.Year).HasColumnName("year").IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.IsCurrent).HasColumnName("is_current").HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.Year).HasDatabaseName("idx_seasons_year");
            entity.HasIndex(e => e.IsCurrent).HasDatabaseName("idx_seasons_current").HasFilter("is_current = TRUE");
        });
    }

    private void ConfigurePositions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Position>(entity =>
        {
            entity.ToTable("positions");
            entity.HasKey(e => e.PositionId);
            entity.Property(e => e.PositionId).HasColumnName("position_id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(10).IsRequired();
            entity.Property(e => e.DisplayOrder).HasColumnName("display_order").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.Code).HasDatabaseName("idx_positions_code");
        });
    }

    private void ConfigureTeams(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Team>(entity =>
        {
            entity.ToTable("teams");
            entity.HasKey(e => e.TeamId);
            entity.Property(e => e.TeamId).HasColumnName("team_id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Abbreviation).HasColumnName("abbreviation").HasMaxLength(10);
            entity.Property(e => e.IsDrum).HasColumnName("is_drum").HasDefaultValue(false);
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.Name).HasDatabaseName("idx_teams_name");
            entity.HasIndex(e => e.IsDrum).HasDatabaseName("idx_teams_drum").HasFilter("is_drum = TRUE");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("idx_teams_active").HasFilter("is_active = TRUE");
        });
    }

    private void ConfigureCompetitions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Competition>(entity =>
        {
            entity.ToTable("competitions");
            entity.HasKey(e => e.CompetitionId);
            entity.Property(e => e.CompetitionId).HasColumnName("competition_id");
            entity.Property(e => e.SeasonId).HasColumnName("season_id").IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Type).HasColumnName("type").HasMaxLength(50).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Season)
                .WithMany(s => s.Competitions)
                .HasForeignKey(e => e.SeasonId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.SeasonId).HasDatabaseName("idx_competitions_season");
            entity.HasIndex(e => e.Type).HasDatabaseName("idx_competitions_type");
            entity.HasIndex(e => new { e.SeasonId, e.Name }).IsUnique().HasDatabaseName("idx_competitions_season_name");
        });
    }

    private void ConfigurePlayers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>(entity =>
        {
            entity.ToTable("players");
            entity.HasKey(e => e.PlayerId);
            entity.Property(e => e.PlayerId).HasColumnName("player_id");
            entity.Property(e => e.JerseyNumber).HasColumnName("jersey_number").IsRequired();
            entity.Property(e => e.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.PositionId).HasColumnName("position_id").IsRequired();
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Position)
                .WithMany(p => p.Players)
                .HasForeignKey(e => e.PositionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.JerseyNumber).HasDatabaseName("idx_players_jersey");
            entity.HasIndex(e => e.PositionId).HasDatabaseName("idx_players_position");
            entity.HasIndex(e => e.FullName).HasDatabaseName("idx_players_full_name");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("idx_players_active").HasFilter("is_active = TRUE");
        });
    }

    private void ConfigureMatches(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Match>(entity =>
        {
            entity.ToTable("matches");
            entity.HasKey(e => e.MatchId);
            entity.Property(e => e.MatchId).HasColumnName("match_id");
            entity.Property(e => e.CompetitionId).HasColumnName("competition_id").IsRequired();
            entity.Property(e => e.MatchNumber).HasColumnName("match_number").IsRequired();
            entity.Property(e => e.HomeTeamId).HasColumnName("home_team_id").IsRequired();
            entity.Property(e => e.AwayTeamId).HasColumnName("away_team_id").IsRequired();
            entity.Property(e => e.MatchDate).HasColumnName("match_date").IsRequired();
            entity.Property(e => e.Venue).HasColumnName("venue").HasMaxLength(50).IsRequired();
            entity.Property(e => e.HomeScoreFirstHalf).HasColumnName("home_score_first_half").HasMaxLength(10);
            entity.Property(e => e.HomeScoreSecondHalf).HasColumnName("home_score_second_half").HasMaxLength(10);
            entity.Property(e => e.HomeScoreFullTime).HasColumnName("home_score_full_time").HasMaxLength(10);
            entity.Property(e => e.AwayScoreFirstHalf).HasColumnName("away_score_first_half").HasMaxLength(10);
            entity.Property(e => e.AwayScoreSecondHalf).HasColumnName("away_score_second_half").HasMaxLength(10);
            entity.Property(e => e.AwayScoreFullTime).HasColumnName("away_score_full_time").HasMaxLength(10);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Competition)
                .WithMany(c => c.Matches)
                .HasForeignKey(e => e.CompetitionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.HomeTeam)
                .WithMany(t => t.HomeMatches)
                .HasForeignKey(e => e.HomeTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AwayTeam)
                .WithMany(t => t.AwayMatches)
                .HasForeignKey(e => e.AwayTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.CompetitionId).HasDatabaseName("idx_matches_competition");
            entity.HasIndex(e => e.MatchDate).HasDatabaseName("idx_matches_date");
            entity.HasIndex(e => e.HomeTeamId).HasDatabaseName("idx_matches_home_team");
            entity.HasIndex(e => e.AwayTeamId).HasDatabaseName("idx_matches_away_team");
            entity.HasIndex(e => new { e.CompetitionId, e.MatchNumber }).IsUnique().HasDatabaseName("idx_matches_competition_number");
        });
    }

    private void ConfigureMatchTeamStatistics(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MatchTeamStatistics>(entity =>
        {
            entity.ToTable("match_team_statistics");
            entity.HasKey(e => e.MatchTeamStatId);
            entity.Property(e => e.MatchTeamStatId).HasColumnName("match_team_stat_id");
            entity.Property(e => e.MatchId).HasColumnName("match_id").IsRequired();
            entity.Property(e => e.TeamId).HasColumnName("team_id").IsRequired();
            entity.Property(e => e.Period).HasColumnName("period").HasMaxLength(10).IsRequired();
            entity.Property(e => e.Scoreline).HasColumnName("scoreline").HasMaxLength(10);
            entity.Property(e => e.TotalPossession).HasColumnName("total_possession").HasPrecision(5, 4);
            entity.Property(e => e.ScoreSourceKickoutLong).HasColumnName("score_source_kickout_long").HasDefaultValue(0);
            entity.Property(e => e.ScoreSourceKickoutShort).HasColumnName("score_source_kickout_short").HasDefaultValue(0);
            entity.Property(e => e.ScoreSourceOppKickoutLong).HasColumnName("score_source_opp_kickout_long").HasDefaultValue(0);
            entity.Property(e => e.ScoreSourceOppKickoutShort).HasColumnName("score_source_opp_kickout_short").HasDefaultValue(0);
            entity.Property(e => e.ScoreSourceTurnover).HasColumnName("score_source_turnover").HasDefaultValue(0);
            entity.Property(e => e.ScoreSourcePossessionLost).HasColumnName("score_source_possession_lost").HasDefaultValue(0);
            entity.Property(e => e.ScoreSourceShotShort).HasColumnName("score_source_shot_short").HasDefaultValue(0);
            entity.Property(e => e.ScoreSourceThrowUpIn).HasColumnName("score_source_throw_up_in").HasDefaultValue(0);
            entity.Property(e => e.ShotSourceKickoutLong).HasColumnName("shot_source_kickout_long").HasDefaultValue(0);
            entity.Property(e => e.ShotSourceKickoutShort).HasColumnName("shot_source_kickout_short").HasDefaultValue(0);
            entity.Property(e => e.ShotSourceOppKickoutLong).HasColumnName("shot_source_opp_kickout_long").HasDefaultValue(0);
            entity.Property(e => e.ShotSourceOppKickoutShort).HasColumnName("shot_source_opp_kickout_short").HasDefaultValue(0);
            entity.Property(e => e.ShotSourceTurnover).HasColumnName("shot_source_turnover").HasDefaultValue(0);
            entity.Property(e => e.ShotSourcePossessionLost).HasColumnName("shot_source_possession_lost").HasDefaultValue(0);
            entity.Property(e => e.ShotSourceShotShort).HasColumnName("shot_source_shot_short").HasDefaultValue(0);
            entity.Property(e => e.ShotSourceThrowUpIn).HasColumnName("shot_source_throw_up_in").HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Match)
                .WithMany(m => m.TeamStatistics)
                .HasForeignKey(e => e.MatchId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Team)
                .WithMany(t => t.TeamStatistics)
                .HasForeignKey(e => e.TeamId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.MatchId).HasDatabaseName("idx_mts_match");
            entity.HasIndex(e => e.TeamId).HasDatabaseName("idx_mts_team");
            entity.HasIndex(e => e.Period).HasDatabaseName("idx_mts_period");
            entity.HasIndex(e => new { e.MatchId, e.TeamId, e.Period }).IsUnique().HasDatabaseName("idx_mts_match_team_period");
        });
    }

    private void ConfigurePlayerMatchStatistics(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerMatchStatistics>(entity =>
        {
            entity.ToTable("player_match_statistics");
            entity.HasKey(e => e.PlayerMatchStatId);
            entity.Property(e => e.PlayerMatchStatId).HasColumnName("player_match_stat_id");
            entity.Property(e => e.MatchId).HasColumnName("match_id").IsRequired();
            entity.Property(e => e.PlayerId).HasColumnName("player_id").IsRequired();

            // Summary Statistics
            entity.Property(e => e.MinutesPlayed).HasColumnName("minutes_played").HasDefaultValue(0);
            entity.Property(e => e.TotalEngagements).HasColumnName("total_engagements").HasDefaultValue(0);
            entity.Property(e => e.TePerPsr).HasColumnName("te_per_psr").HasPrecision(5, 2);
            entity.Property(e => e.Scores).HasColumnName("scores").HasMaxLength(20);
            entity.Property(e => e.Psr).HasColumnName("psr").HasDefaultValue(0);
            entity.Property(e => e.PsrPerTp).HasColumnName("psr_per_tp").HasPrecision(5, 2);

            // Possession Play
            entity.Property(e => e.Tp).HasColumnName("tp").HasDefaultValue(0);
            entity.Property(e => e.Tow).HasColumnName("tow").HasDefaultValue(0);
            entity.Property(e => e.Interceptions).HasColumnName("interceptions").HasDefaultValue(0);
            entity.Property(e => e.Tpl).HasColumnName("tpl").HasDefaultValue(0);
            entity.Property(e => e.Kp).HasColumnName("kp").HasDefaultValue(0);
            entity.Property(e => e.Hp).HasColumnName("hp").HasDefaultValue(0);
            entity.Property(e => e.Ha).HasColumnName("ha").HasDefaultValue(0);
            entity.Property(e => e.Turnovers).HasColumnName("turnovers").HasDefaultValue(0);
            entity.Property(e => e.Ineffective).HasColumnName("ineffective").HasDefaultValue(0);
            entity.Property(e => e.ShotShort).HasColumnName("shot_short").HasDefaultValue(0);
            entity.Property(e => e.ShotSave).HasColumnName("shot_save").HasDefaultValue(0);
            entity.Property(e => e.Fouled).HasColumnName("fouled").HasDefaultValue(0);
            entity.Property(e => e.Woodwork).HasColumnName("woodwork").HasDefaultValue(0);

            // Kickout Analysis - Drum
            entity.Property(e => e.KoDrumKow).HasColumnName("ko_drum_kow").HasDefaultValue(0);
            entity.Property(e => e.KoDrumWc).HasColumnName("ko_drum_wc").HasDefaultValue(0);
            entity.Property(e => e.KoDrumBw).HasColumnName("ko_drum_bw").HasDefaultValue(0);
            entity.Property(e => e.KoDrumSw).HasColumnName("ko_drum_sw").HasDefaultValue(0);

            // Kickout Analysis - Opposition
            entity.Property(e => e.KoOppKow).HasColumnName("ko_opp_kow").HasDefaultValue(0);
            entity.Property(e => e.KoOppWc).HasColumnName("ko_opp_wc").HasDefaultValue(0);
            entity.Property(e => e.KoOppBw).HasColumnName("ko_opp_bw").HasDefaultValue(0);
            entity.Property(e => e.KoOppSw).HasColumnName("ko_opp_sw").HasDefaultValue(0);

            // Attacking Play
            entity.Property(e => e.Ta).HasColumnName("ta").HasDefaultValue(0);
            entity.Property(e => e.Kr).HasColumnName("kr").HasDefaultValue(0);
            entity.Property(e => e.Kl).HasColumnName("kl").HasDefaultValue(0);
            entity.Property(e => e.Cr).HasColumnName("cr").HasDefaultValue(0);
            entity.Property(e => e.Cl).HasColumnName("cl").HasDefaultValue(0);

            // Shots from Play
            entity.Property(e => e.ShotsPlayTotal).HasColumnName("shots_play_total").HasDefaultValue(0);
            entity.Property(e => e.ShotsPlayPoints).HasColumnName("shots_play_points").HasDefaultValue(0);
            entity.Property(e => e.ShotsPlay2Points).HasColumnName("shots_play_2points").HasDefaultValue(0);
            entity.Property(e => e.ShotsPlayGoals).HasColumnName("shots_play_goals").HasDefaultValue(0);
            entity.Property(e => e.ShotsPlayWide).HasColumnName("shots_play_wide").HasDefaultValue(0);
            entity.Property(e => e.ShotsPlayShort).HasColumnName("shots_play_short").HasDefaultValue(0);
            entity.Property(e => e.ShotsPlaySave).HasColumnName("shots_play_save").HasDefaultValue(0);
            entity.Property(e => e.ShotsPlayWoodwork).HasColumnName("shots_play_woodwork").HasDefaultValue(0);
            entity.Property(e => e.ShotsPlayBlocked).HasColumnName("shots_play_blocked").HasDefaultValue(0);
            entity.Property(e => e.ShotsPlay45).HasColumnName("shots_play_45").HasDefaultValue(0);
            entity.Property(e => e.ShotsPlayPercentage).HasColumnName("shots_play_percentage").HasPrecision(5, 4);

            // Scoreable Frees
            entity.Property(e => e.FreesTotal).HasColumnName("frees_total").HasDefaultValue(0);
            entity.Property(e => e.FreesPoints).HasColumnName("frees_points").HasDefaultValue(0);
            entity.Property(e => e.Frees2Points).HasColumnName("frees_2points").HasDefaultValue(0);
            entity.Property(e => e.FreesGoals).HasColumnName("frees_goals").HasDefaultValue(0);
            entity.Property(e => e.FreesWide).HasColumnName("frees_wide").HasDefaultValue(0);
            entity.Property(e => e.FreesShort).HasColumnName("frees_short").HasDefaultValue(0);
            entity.Property(e => e.FreesSave).HasColumnName("frees_save").HasDefaultValue(0);
            entity.Property(e => e.FreesWoodwork).HasColumnName("frees_woodwork").HasDefaultValue(0);
            entity.Property(e => e.Frees45).HasColumnName("frees_45").HasDefaultValue(0);
            entity.Property(e => e.FreesQf).HasColumnName("frees_qf").HasDefaultValue(0);
            entity.Property(e => e.FreesPercentage).HasColumnName("frees_percentage").HasPrecision(5, 4);

            // Total Shots
            entity.Property(e => e.TotalShots).HasColumnName("total_shots").HasDefaultValue(0);
            entity.Property(e => e.TotalShotsPercentage).HasColumnName("total_shots_percentage").HasPrecision(5, 4);

            // Assists
            entity.Property(e => e.AssistsTotal).HasColumnName("assists_total").HasDefaultValue(0);
            entity.Property(e => e.AssistsPoint).HasColumnName("assists_point").HasDefaultValue(0);
            entity.Property(e => e.AssistsGoal).HasColumnName("assists_goal").HasDefaultValue(0);

            // Tackles
            entity.Property(e => e.TacklesTotal).HasColumnName("tackles_total").HasDefaultValue(0);
            entity.Property(e => e.TacklesContested).HasColumnName("tackles_contested").HasDefaultValue(0);
            entity.Property(e => e.TacklesMissed).HasColumnName("tackles_missed").HasDefaultValue(0);
            entity.Property(e => e.TacklesPercentage).HasColumnName("tackles_percentage").HasPrecision(5, 4);

            // Frees Conceded
            entity.Property(e => e.FreesConcededTotal).HasColumnName("frees_conceded_total").HasDefaultValue(0);
            entity.Property(e => e.FreesConcededAttack).HasColumnName("frees_conceded_attack").HasDefaultValue(0);
            entity.Property(e => e.FreesConcededMidfield).HasColumnName("frees_conceded_midfield").HasDefaultValue(0);
            entity.Property(e => e.FreesConcededDefense).HasColumnName("frees_conceded_defense").HasDefaultValue(0);
            entity.Property(e => e.FreesConcededPenalty).HasColumnName("frees_conceded_penalty").HasDefaultValue(0);

            // 50m Free Conceded
            entity.Property(e => e.Frees50MTotal).HasColumnName("frees_50m_total").HasDefaultValue(0);
            entity.Property(e => e.Frees50MDelay).HasColumnName("frees_50m_delay").HasDefaultValue(0);
            entity.Property(e => e.Frees50MDissent).HasColumnName("frees_50m_dissent").HasDefaultValue(0);
            entity.Property(e => e.Frees50M3V3).HasColumnName("frees_50m_3v3").HasDefaultValue(0);

            // Bookings
            entity.Property(e => e.YellowCards).HasColumnName("yellow_cards").HasDefaultValue(0);
            entity.Property(e => e.BlackCards).HasColumnName("black_cards").HasDefaultValue(0);
            entity.Property(e => e.RedCards).HasColumnName("red_cards").HasDefaultValue(0);

            // Throw Up
            entity.Property(e => e.ThrowUpWon).HasColumnName("throw_up_won").HasDefaultValue(0);
            entity.Property(e => e.ThrowUpLost).HasColumnName("throw_up_lost").HasDefaultValue(0);

            // Goalkeeper Stats
            entity.Property(e => e.GkTotalKickouts).HasColumnName("gk_total_kickouts").HasDefaultValue(0);
            entity.Property(e => e.GkKickoutRetained).HasColumnName("gk_kickout_retained").HasDefaultValue(0);
            entity.Property(e => e.GkKickoutLost).HasColumnName("gk_kickout_lost").HasDefaultValue(0);
            entity.Property(e => e.GkKickoutPercentage).HasColumnName("gk_kickout_percentage").HasPrecision(5, 4);
            entity.Property(e => e.GkSaves).HasColumnName("gk_saves").HasDefaultValue(0);

            // Timestamps
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Match)
                .WithMany(m => m.PlayerStatistics)
                .HasForeignKey(e => e.MatchId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Player)
                .WithMany(p => p.MatchStatistics)
                .HasForeignKey(e => e.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.MatchId).HasDatabaseName("idx_pms_match");
            entity.HasIndex(e => e.PlayerId).HasDatabaseName("idx_pms_player");
            entity.HasIndex(e => new { e.MatchId, e.PlayerId }).HasDatabaseName("idx_pms_match_player");
            entity.HasIndex(e => e.MinutesPlayed).HasDatabaseName("idx_pms_minutes").HasFilter("minutes_played > 0");
        });
    }

    private void ConfigureKpiDefinitions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KpiDefinition>(entity =>
        {
            entity.ToTable("kpi_definitions");
            entity.HasKey(e => e.KpiId);
            entity.Property(e => e.KpiId).HasColumnName("kpi_id");
            entity.Property(e => e.EventNumber).HasColumnName("event_number").IsRequired();
            entity.Property(e => e.EventName).HasColumnName("event_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Outcome).HasColumnName("outcome").HasMaxLength(100).IsRequired();
            entity.Property(e => e.TeamAssignment).HasColumnName("team_assignment").HasMaxLength(50).IsRequired();
            entity.Property(e => e.PsrValue).HasColumnName("psr_value").HasPrecision(5, 2).IsRequired();
            entity.Property(e => e.Definition).HasColumnName("definition").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => new { e.EventNumber, e.EventName }).HasDatabaseName("idx_kpi_event");
            entity.HasIndex(e => e.Outcome).HasDatabaseName("idx_kpi_outcome");
        });
    }
}