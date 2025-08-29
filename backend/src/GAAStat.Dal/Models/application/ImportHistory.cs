using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.Models.application;

/// <summary>
/// Audit trail of all Excel import operations
/// </summary>
[Table("import_history")]
[Index("ImportStatus", "ImportStartedAt", Name = "idx_import_history_status")]
public partial class ImportHistory
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("import_type")]
    [StringLength(50)]
    public string ImportType { get; set; } = null!;

    [Column("file_name")]
    [StringLength(500)]
    public string? FileName { get; set; }

    [Column("file_size")]
    public long? FileSize { get; set; }

    [Column("matches_imported")]
    public int? MatchesImported { get; set; }

    [Column("players_processed")]
    public int? PlayersProcessed { get; set; }

    [Column("events_created")]
    public int? EventsCreated { get; set; }

    [Column("import_started_at", TypeName = "timestamp without time zone")]
    public DateTime ImportStartedAt { get; set; }

    [Column("import_completed_at", TypeName = "timestamp without time zone")]
    public DateTime? ImportCompletedAt { get; set; }

    [Column("import_status")]
    [StringLength(20)]
    public string ImportStatus { get; set; } = null!;

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("snapshot_id")]
    public int? SnapshotId { get; set; }

    [Column("processing_duration_seconds")]
    public int? ProcessingDurationSeconds { get; set; }
}
