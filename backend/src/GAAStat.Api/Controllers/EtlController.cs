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
    private readonly IMatchStatisticsEtlService _matchEtlService;
    private readonly IPlayerStatisticsEtlService _playerEtlService;
    private readonly IKpiDefinitionsEtlService _kpiEtlService;
    private readonly ILogger<EtlController> _logger;
    private readonly IConfiguration _configuration;

    // File upload constraints
    private const long MaxFileSizeBytes = 104857600; // 100MB
    private static readonly string[] AllowedExtensions = { ".xlsx", ".xls" };

    public EtlController(
        IMatchStatisticsEtlService matchEtlService,
        IPlayerStatisticsEtlService playerEtlService,
        IKpiDefinitionsEtlService kpiEtlService,
        ILogger<EtlController> logger,
        IConfiguration configuration)
    {
        _matchEtlService = matchEtlService;
        _playerEtlService = playerEtlService;
        _kpiEtlService = kpiEtlService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Upload and process a GAA statistics Excel file (match statistics, player statistics, and KPI definitions)
    /// </summary>
    /// <param name="file">Excel file containing GAA statistics (match data, player data, and KPI definitions)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ETL operation result with comprehensive statistics including KPI definitions</returns>
    [HttpPost("gaa-statistics/upload")]
    [ProducesResponseType(typeof(EtlUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(EtlUploadResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(EtlUploadResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EtlUploadResponse>> UploadGAAStatistics(
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

            // Execute Match ETL pipeline
            _logger.LogInformation("Starting Match ETL pipeline for file: {FileName}", file.FileName);
            var matchResult = await _matchEtlService.ProcessMatchStatisticsAsync(
                tempFilePath,
                cancellationToken);

            // Execute Player ETL pipeline
            var playerResult = await _playerEtlService.ProcessPlayerStatisticsAsync(
                tempFilePath,
                cancellationToken);

            // Execute KPI Definitions ETL pipeline
            _logger.LogInformation("Starting KPI Definitions ETL pipeline for file: {FileName}", file.FileName);
            var kpiResult = await _kpiEtlService.ProcessKpiDefinitionsAsync(
                tempFilePath,
                cancellationToken);

            // Calculate combined duration
            var totalDuration = matchResult.Duration + playerResult.Duration + kpiResult.Duration;

            // Map to API response - combine results from all ETL operations
            var response = new EtlUploadResponse
            {
                Success = matchResult.Success && playerResult.Success && kpiResult.Success,

                // Match statistics
                MatchesProcessed = matchResult.MatchesProcessed,
                TeamStatisticsCreated = matchResult.TeamStatisticsCreated,

                // Player statistics
                PlayerSheetsProcessed = playerResult.PlayerSheetsProcessed,
                PlayersCreated = playerResult.PlayersCreated,
                PlayersUpdated = playerResult.PlayersUpdated,
                PlayerStatisticsCreated = playerResult.PlayerStatisticsCreated,
                PlayersSkipped = playerResult.PlayersSkipped,
                ValidationErrorsTotal = playerResult.ValidationErrorsTotal,
                ValidationWarningsTotal = playerResult.ValidationWarningsTotal,

                // KPI definitions
                KpiDefinitionsCreated = kpiResult.KpiDefinitionsCreated,
                KpiDefinitionsUpdated = kpiResult.KpiDefinitionsUpdated,
                KpiDefinitionsSkipped = kpiResult.KpiDefinitionsSkipped,

                // Combined metrics
                DurationSeconds = totalDuration.TotalSeconds,

                // Combine warnings from all operations
                Warnings = matchResult.Warnings.Select(w => new EtlWarningDto
                {
                    Code = w.Code,
                    Message = w.Message,
                    SheetName = w.SheetName
                }).Concat(playerResult.Warnings.Select(w => new EtlWarningDto
                {
                    Code = w.Code,
                    Message = w.Message,
                    SheetName = w.SheetName
                })).Concat(kpiResult.Warnings.Select(w => new EtlWarningDto
                {
                    Code = w.Code,
                    Message = w.Message,
                    SheetName = w.SheetName
                })).ToList(),

                // Combine errors from all operations
                Errors = matchResult.Errors.Select(e => new EtlErrorDto
                {
                    Code = e.Code,
                    Message = e.Message,
                    SheetName = e.SheetName
                }).Concat(playerResult.Errors.Select(e => new EtlErrorDto
                {
                    Code = e.Code,
                    Message = e.Message,
                    SheetName = e.SheetName
                })).Concat(kpiResult.Errors.Select(e => new EtlErrorDto
                {
                    Code = e.Code,
                    Message = e.Message,
                    SheetName = e.SheetName
                })).ToList()
            };

            if (response.Success)
            {
                _logger.LogInformation(
                    "GAA Statistics ETL completed successfully. " +
                    "Matches: {Matches}, Team Stats: {TeamStats}, Player Stats: {PlayerStats}, Players Created: {PlayersCreated}, " +
                    "KPIs Created: {KpisCreated}, KPIs Updated: {KpisUpdated}, Duration: {Duration}s",
                    response.MatchesProcessed,
                    response.TeamStatisticsCreated,
                    response.PlayerStatisticsCreated,
                    response.PlayersCreated,
                    response.KpiDefinitionsCreated,
                    response.KpiDefinitionsUpdated,
                    response.DurationSeconds);
            }
            else
            {
                _logger.LogError(
                    "GAA Statistics ETL completed with errors. " +
                    "Matches: {Matches}, Player Stats: {PlayerStats}, KPIs Processed: {KpisProcessed}, Errors: {ErrorCount}",
                    response.MatchesProcessed,
                    response.PlayerStatisticsCreated,
                    response.KpiDefinitionsCreated + response.KpiDefinitionsUpdated + response.KpiDefinitionsSkipped,
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
