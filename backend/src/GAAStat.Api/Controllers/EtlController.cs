using GAAStat.Api.Models;
using GAAStat.Services.ETL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GAAStat.Api.Controllers;

/// <summary>
/// API controller for ETL (Extract, Transform, Load) operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EtlController : ControllerBase
{
    private readonly IMatchStatisticsEtlService _etlService;
    private readonly ILogger<EtlController> _logger;
    private readonly IConfiguration _configuration;

    // File upload constraints
    private const long MaxFileSizeBytes = 104857600; // 100MB
    private static readonly string[] AllowedExtensions = { ".xlsx", ".xls" };

    public EtlController(
        IMatchStatisticsEtlService etlService,
        ILogger<EtlController> logger,
        IConfiguration configuration)
    {
        _etlService = etlService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Upload and process a match statistics Excel file
    /// </summary>
    /// <param name="file">Excel file containing match statistics</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ETL operation result with statistics</returns>
    [HttpPost("match-statistics/upload")]
    [ProducesResponseType(typeof(EtlUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(EtlUploadResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(EtlUploadResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EtlUploadResponse>> UploadMatchStatistics(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        string? tempFilePath = null;

        try
        {
            // Validate file upload
            var validationError = ValidateFile(file);
            if (validationError != null)
            {
                _logger.LogWarning("File validation failed: {Error}", validationError.Message);
                return BadRequest(new EtlUploadResponse
                {
                    Success = false,
                    Errors = new List<EtlErrorDto> { validationError }
                });
            }

            _logger.LogInformation(
                "Received file upload: {FileName} ({FileSize} bytes)",
                file.FileName,
                file.Length);

            // Save file to temp location
            tempFilePath = await SaveFileToTempAsync(file, cancellationToken);
            _logger.LogInformation("File saved to temp location: {TempPath}", tempFilePath);

            // Execute ETL pipeline
            _logger.LogInformation("Starting ETL pipeline for file: {FileName}", file.FileName);
            var etlResult = await _etlService.ProcessMatchStatisticsAsync(
                tempFilePath,
                cancellationToken);

            // Map to API response
            var response = new EtlUploadResponse
            {
                Success = etlResult.Success,
                MatchesProcessed = etlResult.MatchesProcessed,
                TeamStatisticsCreated = etlResult.TeamStatisticsCreated,
                DurationSeconds = etlResult.Duration.TotalSeconds,
                Warnings = etlResult.Warnings.Select(w => new EtlWarningDto
                {
                    Code = w.Code,
                    Message = w.Message,
                    SheetName = w.SheetName
                }).ToList(),
                Errors = etlResult.Errors.Select(e => new EtlErrorDto
                {
                    Code = e.Code,
                    Message = e.Message,
                    SheetName = e.SheetName
                }).ToList()
            };

            if (response.Success)
            {
                _logger.LogInformation(
                    "ETL completed successfully. Matches: {Matches}, Team Stats: {TeamStats}, Duration: {Duration}s",
                    response.MatchesProcessed,
                    response.TeamStatisticsCreated,
                    response.DurationSeconds);
            }
            else
            {
                _logger.LogError(
                    "ETL completed with errors. Matches: {Matches}, Errors: {ErrorCount}",
                    response.MatchesProcessed,
                    response.Errors.Count);
            }

            return Ok(response);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("ETL operation was cancelled");
            return Ok(new EtlUploadResponse
            {
                Success = false,
                Errors = new List<EtlErrorDto>
                {
                    new EtlErrorDto
                    {
                        Code = "OPERATION_CANCELLED",
                        Message = "ETL operation was cancelled by user"
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during ETL operation");
            return StatusCode(500, new EtlUploadResponse
            {
                Success = false,
                Errors = new List<EtlErrorDto>
                {
                    new EtlErrorDto
                    {
                        Code = "UNEXPECTED_ERROR",
                        Message = $"Unexpected error: {ex.Message}"
                    }
                }
            });
        }
        finally
        {
            // Always cleanup temp file
            if (!string.IsNullOrEmpty(tempFilePath) && System.IO.File.Exists(tempFilePath))
            {
                try
                {
                    System.IO.File.Delete(tempFilePath);
                    _logger.LogInformation("Temp file deleted: {TempPath}", tempFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temp file: {TempPath}", tempFilePath);
                }
            }
        }
    }

    /// <summary>
    /// Validate uploaded file
    /// </summary>
    private EtlErrorDto? ValidateFile(IFormFile file)
    {
        // Check file provided
        if (file == null || file.Length == 0)
        {
            return new EtlErrorDto
            {
                Code = "NO_FILE",
                Message = "No file was uploaded"
            };
        }

        // Check file size
        if (file.Length > MaxFileSizeBytes)
        {
            return new EtlErrorDto
            {
                Code = "FILE_TOO_LARGE",
                Message = $"File size ({file.Length} bytes) exceeds maximum allowed size ({MaxFileSizeBytes} bytes / 100MB)"
            };
        }

        // Check file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return new EtlErrorDto
            {
                Code = "INVALID_FILE_TYPE",
                Message = $"Only {string.Join(", ", AllowedExtensions)} files are allowed. Received: {extension}"
            };
        }

        return null; // Validation passed
    }

    /// <summary>
    /// Save uploaded file to temporary location
    /// </summary>
    private async Task<string> SaveFileToTempAsync(IFormFile file, CancellationToken cancellationToken)
    {
        // Get temp directory from configuration or use default
        var tempDir = _configuration["FileUpload:TempDirectory"] ?? Path.GetTempPath();
        var uploadsDir = Path.Combine(tempDir, "gaastat-uploads");

        // Create directory if it doesn't exist
        Directory.CreateDirectory(uploadsDir);

        // Generate unique filename
        var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var tempFilePath = Path.Combine(uploadsDir, uniqueFileName);

        // Save file
        using var stream = new FileStream(tempFilePath, FileMode.Create);
        await file.CopyToAsync(stream, cancellationToken);

        return tempFilePath;
    }
}
