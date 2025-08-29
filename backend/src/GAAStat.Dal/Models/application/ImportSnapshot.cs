using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.Models.application;

/// <summary>
/// Compressed data snapshots for rollback capability
/// </summary>
[Table("import_snapshots")]
[Index("CreatedAt", Name = "idx_import_snapshots_created", AllDescending = true)]
public partial class ImportSnapshot
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [Column("matches_data", TypeName = "jsonb")]
    public string? MatchesData { get; set; }

    [Column("player_stats_data", TypeName = "jsonb")]
    public string? PlayerStatsData { get; set; }

    [Column("kickout_stats_data", TypeName = "jsonb")]
    public string? KickoutStatsData { get; set; }

    [Column("source_analysis_data", TypeName = "jsonb")]
    public string? SourceAnalysisData { get; set; }

    [Column("total_matches")]
    public int? TotalMatches { get; set; }

    [Column("total_player_records")]
    public int? TotalPlayerRecords { get; set; }

    [Column("snapshot_size_mb")]
    [Precision(10, 2)]
    public decimal? SnapshotSizeMb { get; set; }

    [Column("compression_ratio")]
    [Precision(8, 4)]
    public decimal? CompressionRatio { get; set; }

    [Column("is_compressed")]
    public bool? IsCompressed { get; set; }

    [Column("description")]
    [StringLength(500)]
    public string? Description { get; set; }

    [Column("import_type")]
    [StringLength(50)]
    public string? ImportType { get; set; }

    [Column("associated_file_name")]
    [StringLength(255)]
    public string? AssociatedFileName { get; set; }

    [Column("creation_duration_ms")]
    public int? CreationDurationMs { get; set; }

    [Column("last_validated", TypeName = "timestamp without time zone")]
    public DateTime? LastValidated { get; set; }
}
