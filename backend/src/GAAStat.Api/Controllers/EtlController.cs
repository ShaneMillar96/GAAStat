using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using GAAStat.Api.Models;
using GAAStat.Api.Models.Etl;
using GAAStat.Services.Interfaces;
using GAAStat.Services.Models;
using GAAStat.Dal;

namespace GAAStat.Api.Controllers;

/// <summary>
/// ETL (Extract, Transform, Load) operations for GAA match data processing
/// Handles Excel file uploads and provides progress tracking for data processing jobs
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EtlController : ControllerBase
{
    private readonly IExcelProcessingService _excelProcessingService;
    private readonly IProgressTrackingService _progressTrackingService;
    private readonly ILogger<EtlController> _logger;

    public EtlController(
        IExcelProcessingService excelProcessingService,
        IProgressTrackingService progressTrackingService,
        ILogger<EtlController> logger)
    {
        _excelProcessingService = excelProcessingService;
        _progressTrackingService = progressTrackingService;
        _logger = logger;
    }

    /// <summary>
    /// Upload and process Excel file containing GAA match statistics
    /// Validates file format, creates ETL job, and begins processing
    /// </summary>
    /// <param name="file">Excel file (.xlsx) containing GAA match data</param>
    /// <param name="createdBy">Optional user identifier who initiated the upload</param>
    /// <returns>ETL job information with initial analysis results</returns>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<EtlUploadResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadMatchFile(
        [Required] IFormFile file,
        [FromForm] string? createdBy = null)
    {
        // Validate file presence
        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse.Error("No file provided or file is empty"));
        }

        // Validate file size
        var maxFileSizeBytes = EnvironmentVariables.MaxFileSizeMb * 1024 * 1024;
        if (file.Length > maxFileSizeBytes)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, 
                ApiResponse.Error($"File size exceeds maximum limit of {EnvironmentVariables.MaxFileSizeMb}MB"));
        }

        // Validate file type
        if (!IsValidExcelFile(file))
        {
            return BadRequest(ApiResponse.Error(
                "Invalid file type. Only Excel files (.xlsx) are supported",
                ["Supported formats: .xlsx"]));
        }

        // Validate file name
        if (!IsValidFileName(file.FileName))
        {
            return BadRequest(ApiResponse.Error("Invalid file name. File name contains invalid characters"));
        }

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _excelProcessingService.ProcessExcelFileAsync(stream, file.FileName, createdBy);

            if (!result.IsSuccess)
            {
                var errorMessage = result.ErrorMessage ?? "File processing failed";
                var errors = result.ValidationErrors?.Any() == true 
                    ? result.ValidationErrors 
                    : null;

                return BadRequest(ApiResponse.Error(errorMessage, errors));
            }

            var response = CreateEtlUploadResponse(result.Data!, file);
            
            _logger.LogInformation("ETL job {JobId} created successfully for file {FileName}", 
                result.Data!.JobId, file.FileName);

            return CreatedAtAction(
                nameof(GetEtlProgress), 
                new { jobId = result.Data.JobId }, 
                ApiResponse.Success(response, "File uploaded and ETL job created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing file {FileName}", file.FileName);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                ApiResponse.Error("An unexpected error occurred while processing the file"));
        }
    }

    /// <summary>
    /// Get current progress status for an ETL job
    /// Provides real-time updates on processing stages and completion percentage
    /// </summary>
    /// <param name="jobId">ETL job identifier</param>
    /// <returns>Current progress information including stage, status, and completion percentage</returns>
    [HttpGet("progress/{jobId}")]
    [ProducesResponseType(typeof(ApiResponse<EtlProgressResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetEtlProgress([Range(1, int.MaxValue)] int jobId)
    {
        try
        {
            var result = await _excelProcessingService.GetProcessingStatusAsync(jobId);

            if (!result.IsSuccess)
            {
                return NotFound(ApiResponse.Error(result.ErrorMessage ?? "ETL job not found"));
            }

            if (result.Data == null)
            {
                return NotFound(ApiResponse.Error("No progress information available for this job"));
            }

            var response = CreateEtlProgressResponse(result.Data);
            return Ok(ApiResponse.Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving progress for ETL job {JobId}", jobId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Error("Failed to retrieve job progress"));
        }
    }

    /// <summary>
    /// List recent ETL jobs with optional filtering
    /// Supports pagination and filtering by status, date range
    /// </summary>
    /// <param name="pageSize">Number of jobs per page (1-100)</param>
    /// <param name="page">Page number (starting from 1)</param>
    /// <param name="status">Optional filter by job status (pending, processing, completed, failed)</param>
    /// <param name="fromDate">Optional filter for jobs created after this date</param>
    /// <param name="toDate">Optional filter for jobs created before this date</param>
    /// <returns>Paginated list of ETL jobs with summary information</returns>
    [HttpGet("jobs")]
    [ProducesResponseType(typeof(ApiResponse<EtlJobListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetEtlJobs(
        [Range(1, 100)] int pageSize = 20,
        [Range(1, int.MaxValue)] int page = 1,
        string? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        // Validate page size limits
        if (pageSize > EnvironmentVariables.MaxJobListPageSize)
        {
            return BadRequest(ApiResponse.Error(
                $"Page size cannot exceed {EnvironmentVariables.MaxJobListPageSize}"));
        }

        // Validate date range
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
        {
            return BadRequest(ApiResponse.Error("fromDate cannot be later than toDate"));
        }

        try
        {
            // Note: This would typically call a service method like GetEtlJobsAsync
            // For now, returning a placeholder response since the service method isn't implemented
            var response = new EtlJobListResponse
            {
                Jobs = [],
                TotalCount = 0,
                PageSize = pageSize,
                Page = page,
                TotalPages = 0,
                HasNextPage = false,
                HasPreviousPage = page > 1
            };

            _logger.LogInformation("Retrieved ETL jobs list: Page {Page}, Size {PageSize}, Status {Status}", 
                page, pageSize, status ?? "All");

            return Ok(ApiResponse.Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ETL jobs list");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Error("Failed to retrieve ETL jobs"));
        }
    }

    /// <summary>
    /// Get validation errors for a specific ETL job
    /// Returns detailed error information including sheet, row, and column context
    /// </summary>
    /// <param name="jobId">ETL job identifier</param>
    /// <returns>Collection of validation errors with detailed context</returns>
    [HttpGet("jobs/{jobId}/errors")]
    [ProducesResponseType(typeof(ApiResponse<EtlJobErrorsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetEtlJobErrors([Range(1, int.MaxValue)] int jobId)
    {
        try
        {
            var result = await _excelProcessingService.GetValidationErrorsAsync(jobId);

            if (!result.IsSuccess)
            {
                return NotFound(ApiResponse.Error(result.ErrorMessage ?? "ETL job not found"));
            }

            var response = CreateEtlJobErrorsResponse(jobId, result.Data!);
            return Ok(ApiResponse.Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving errors for ETL job {JobId}", jobId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Error("Failed to retrieve job errors"));
        }
    }

    #region Private Helper Methods

    private static bool IsValidExcelFile(IFormFile file)
    {
        if (string.IsNullOrEmpty(file.FileName))
            return false;

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".xlsx" };
        
        var contentType = file.ContentType?.ToLowerInvariant();
        var allowedContentTypes = new[]
        {
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        return allowedExtensions.Contains(extension) && 
               (contentType != null && allowedContentTypes.Contains(contentType));
    }

    private static bool IsValidFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        if (fileName.Length > FileConstants.MAX_FILENAME_LENGTH)
            return false;

        var invalidChars = Path.GetInvalidFileNameChars();
        return !fileName.Any(c => invalidChars.Contains(c)) && 
               !fileName.StartsWith(".");
    }

    private static EtlUploadResponse CreateEtlUploadResponse(ExcelProcessingResult processingResult, IFormFile file)
    {
        return new EtlUploadResponse
        {
            JobId = processingResult.JobId,
            FileName = processingResult.FileName,
            FileSizeBytes = file.Length,
            Status = "Processing",
            CreatedAt = DateTime.UtcNow,
            // Note: Additional fields would be populated from database query in a full implementation
        };
    }

    private static EtlProgressResponse CreateEtlProgressResponse(EtlProgressUpdate progressUpdate)
    {
        return new EtlProgressResponse
        {
            JobId = progressUpdate.JobId,
            Stage = progressUpdate.Stage,
            Status = progressUpdate.Status,
            Message = progressUpdate.Message,
            TotalSteps = progressUpdate.TotalSteps,
            CompletedSteps = progressUpdate.CompletedSteps,
            ProgressPercentage = progressUpdate.ProgressPercentage,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static EtlJobErrorsResponse CreateEtlJobErrorsResponse(int jobId, IEnumerable<ValidationError> validationErrors)
    {
        var errorsList = validationErrors.ToList();
        
        return new EtlJobErrorsResponse
        {
            JobId = jobId,
            ValidationErrors = errorsList.Select(CreateEtlValidationErrorResponse),
            TotalErrorCount = errorsList.Count,
            ErrorTypeSummary = errorsList.GroupBy(e => e.ErrorType)
                                        .Select(g => $"{g.Key}: {g.Count()}")
        };
    }

    private static EtlValidationErrorResponse CreateEtlValidationErrorResponse(ValidationError error)
    {
        return new EtlValidationErrorResponse
        {
            SheetName = error.SheetName,
            RowNumber = error.RowNumber,
            ColumnName = error.ColumnName,
            ErrorType = error.ErrorType,
            ErrorMessage = error.ErrorMessage,
            SuggestedFix = error.SuggestedFix,
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}

/// <summary>
/// File-related constants
/// </summary>
public static class FileConstants
{
    public const int MAX_FILENAME_LENGTH = 255;
}