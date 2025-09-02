using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.src.GAAStat.Dal.Models.application;

/// <summary>
/// Detailed error tracking for data validation issues during ETL processing
/// </summary>
[Table("etl_validation_errors")]
[Index("ErrorType", Name = "idx_etl_validation_errors_error_type")]
[Index("JobId", Name = "idx_etl_validation_errors_job_id")]
public partial class EtlValidationError
{
    /// <summary>
    /// Unique identifier for validation error
    /// </summary>
    [Key]
    [Column("error_id")]
    public int ErrorId { get; set; }

    /// <summary>
    /// Reference to ETL job that encountered this error
    /// </summary>
    [Column("job_id")]
    public int JobId { get; set; }

    /// <summary>
    /// Excel sheet name where error occurred
    /// </summary>
    [Column("sheet_name")]
    [StringLength(100)]
    public string? SheetName { get; set; }

    /// <summary>
    /// Row number in sheet where error was found
    /// </summary>
    [Column("row_number")]
    public int? RowNumber { get; set; }

    /// <summary>
    /// Column name where error occurred
    /// </summary>
    [Column("column_name")]
    [StringLength(100)]
    public string? ColumnName { get; set; }

    /// <summary>
    /// Category of validation error
    /// </summary>
    [Column("error_type")]
    [StringLength(50)]
    public string? ErrorType { get; set; }

    /// <summary>
    /// Detailed error description
    /// </summary>
    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Recommended action to resolve the error
    /// </summary>
    [Column("suggested_fix")]
    public string? SuggestedFix { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("JobId")]
    [InverseProperty("EtlValidationErrors")]
    public virtual EtlJob Job { get; set; } = null!;
}
