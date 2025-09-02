using System.ComponentModel.DataAnnotations;

namespace GAAStat.Api.Models.Etl;

/// <summary>
/// Request for file upload with metadata
/// </summary>
public class FileUploadRequest
{
    [Required]
    public IFormFile File { get; set; } = null!;

    [StringLength(100)]
    public string? CreatedBy { get; set; }
}

/// <summary>
/// ETL job progress request parameters
/// </summary>
public class EtlProgressRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int JobId { get; set; }
}

/// <summary>
/// Request parameters for listing ETL jobs
/// </summary>
public class EtlJobListRequest
{
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    public string? Status { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }
}