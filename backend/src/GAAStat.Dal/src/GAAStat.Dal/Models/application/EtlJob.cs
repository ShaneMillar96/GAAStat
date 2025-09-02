using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.src.GAAStat.Dal.Models.application;

/// <summary>
/// Master table tracking Excel ETL job execution status and metadata
/// </summary>
[Table("etl_jobs")]
[Index("CreatedAt", Name = "idx_etl_jobs_created_at")]
[Index("CreatedBy", Name = "idx_etl_jobs_created_by")]
[Index("Status", Name = "idx_etl_jobs_status")]
public partial class EtlJob
{
    /// <summary>
    /// Unique identifier for ETL job
    /// </summary>
    [Key]
    [Column("job_id")]
    public int JobId { get; set; }

    /// <summary>
    /// Original name of uploaded Excel file
    /// </summary>
    [Column("file_name")]
    [StringLength(255)]
    public string FileName { get; set; } = null!;

    /// <summary>
    /// Size of uploaded file in bytes
    /// </summary>
    [Column("file_size_bytes")]
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Current job status: pending, processing, completed, failed
    /// </summary>
    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = null!;

    /// <summary>
    /// Timestamp when job processing began
    /// </summary>
    [Column("started_at", TypeName = "timestamp without time zone")]
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Timestamp when job finished (success or failure)
    /// </summary>
    [Column("completed_at", TypeName = "timestamp without time zone")]
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// High-level summary of errors if job failed
    /// </summary>
    [Column("error_summary")]
    public string? ErrorSummary { get; set; }

    /// <summary>
    /// User or system that initiated the ETL job
    /// </summary>
    [Column("created_by")]
    [StringLength(100)]
    public string? CreatedBy { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Job")]
    public virtual ICollection<EtlJobProgress> EtlJobProgresses { get; set; } = new List<EtlJobProgress>();

    [InverseProperty("Job")]
    public virtual ICollection<EtlValidationError> EtlValidationErrors { get; set; } = new List<EtlValidationError>();
}
