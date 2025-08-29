using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.Models.application;

/// <summary>
/// Match records cleared and reloaded on each Excel import
/// </summary>
[Table("matches")]
[Index("CompetitionId", Name = "idx_matches_competition")]
[Index("MatchDate", Name = "idx_matches_date")]
[Index("CompetitionId", "MatchDate", Name = "idx_matches_season")]
[Index("HomeTeamId", "AwayTeamId", Name = "idx_matches_teams")]
[Index("MatchDate", "HomeTeamId", "AwayTeamId", Name = "unique_match_teams_date", IsUnique = true)]
public partial class Match
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("competition_id")]
    public int CompetitionId { get; set; }

    [Column("match_number")]
    public int? MatchNumber { get; set; }

    [Column("match_date")]
    public DateOnly MatchDate { get; set; }

    [Column("home_team_id")]
    public int HomeTeamId { get; set; }

    [Column("away_team_id")]
    public int AwayTeamId { get; set; }

    [Column("venue")]
    [StringLength(200)]
    public string? Venue { get; set; }

    [Column("home_score_goals")]
    public int? HomeScoreGoals { get; set; }

    [Column("home_score_points")]
    public int? HomeScorePoints { get; set; }

    [Column("away_score_goals")]
    public int? AwayScoreGoals { get; set; }

    [Column("away_score_points")]
    public int? AwayScorePoints { get; set; }

    [Column("weather_conditions")]
    [StringLength(100)]
    public string? WeatherConditions { get; set; }

    [Column("attendance")]
    public int? Attendance { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    /// <summary>
    /// Source Excel sheet name for import traceability
    /// </summary>
    [Column("excel_sheet_name")]
    [StringLength(255)]
    public string? ExcelSheetName { get; set; }

    [Column("imported_at", TypeName = "timestamp without time zone")]
    public DateTime ImportedAt { get; set; }

    /// <summary>
    /// DateTime conversion property for service compatibility
    /// </summary>
    [NotMapped]
    public DateTime MatchDateTime => MatchDate.ToDateTime(TimeOnly.MinValue);

    [ForeignKey("AwayTeamId")]
    [InverseProperty("MatchAwayTeams")]
    public virtual Team AwayTeam { get; set; } = null!;

    [ForeignKey("CompetitionId")]
    [InverseProperty("Matches")]
    public virtual Competition Competition { get; set; } = null!;

    [ForeignKey("HomeTeamId")]
    [InverseProperty("MatchHomeTeams")]
    public virtual Team HomeTeam { get; set; } = null!;

    [InverseProperty("Match")]
    public virtual ICollection<MatchPlayerStat> MatchPlayerStats { get; set; } = new List<MatchPlayerStat>();

    [InverseProperty("Match")]
    public virtual ICollection<MatchSourceAnalysis> MatchSourceAnalyses { get; set; } = new List<MatchSourceAnalysis>();
}
