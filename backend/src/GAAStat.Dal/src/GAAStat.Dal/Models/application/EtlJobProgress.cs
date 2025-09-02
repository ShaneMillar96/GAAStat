using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.src.GAAStat.Dal.Models.application;

/// <summary>
/// Stage-by-stage progress tracking for ETL jobs with detailed status updates
/// </summary>
[Table("etl_job_progress")]
[Index("JobId", Name = "idx_etl_job_progress_job_id")]
[Index("UpdatedAt", Name = "idx_etl_job_progress_updated_at")]
public partial class EtlJobProgress
{
    /// <summary>
    /// Unique identifier for progress entry
    /// </summary>
    [Key]
    [Column("progress_id")]
    public int ProgressId { get; set; }

    /// <summary>
    /// Reference to parent ETL job
    /// </summary>
    [Column("job_id")]
    public int JobId { get; set; }

    /// <summary>
    /// Current processing stage (e.g., &quot;Parsing Excel&quot;, &quot;Validating Data&quot;)
    /// </summary>
    [Column("stage")]
    [StringLength(100)]
    public string Stage { get; set; } = null!;

    /// <summary>
    /// Total number of steps for this stage
    /// </summary>
    [Column("total_steps")]
    public int? TotalSteps { get; set; }

    /// <summary>
    /// Number of steps completed in this stage
    /// </summary>
    [Column("completed_steps")]
    public int? CompletedSteps { get; set; }

    /// <summary>
    /// Status of current stage
    /// </summary>
    [Column("status")]
    [StringLength(50)]
    public string? Status { get; set; }

    /// <summary>
    /// Detailed progress message for user feedback
    /// </summary>
    [Column("message")]
    public string? Message { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("JobId")]
    [InverseProperty("EtlJobProgresses")]
    public virtual EtlJob Job { get; set; } = null!;
}
