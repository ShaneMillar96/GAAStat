using Microsoft.AspNetCore.Mvc;

namespace GAAStat.Api.Controllers;

/// <summary>
/// Simple upload controller for testing CSV/Excel file uploads
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SimpleUploadController : ControllerBase
{
    private readonly ILogger<SimpleUploadController> _logger;

    public SimpleUploadController(ILogger<SimpleUploadController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Test endpoint for uploading CSV/Excel files
    /// </summary>
    /// <param name="file">The CSV or Excel file to upload</param>
    /// <returns>Upload result</returns>
    [HttpPost("csv")]
    public async Task<IActionResult> UploadCsv(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file provided", success = false });
        }

        _logger.LogInformation("Received file upload: {FileName}, Size: {Size} bytes, ContentType: {ContentType}",
            file.FileName, file.Length, file.ContentType);

        // Validate file type
        var allowedTypes = new[] { "text/csv", "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" };
        var allowedExtensions = new[] { ".csv", ".xls", ".xlsx" };
        
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!allowedTypes.Contains(file.ContentType) && !allowedExtensions.Contains(extension))
        {
            return BadRequest(new 
            { 
                error = "Invalid file type. Only CSV and Excel files are supported",
                success = false,
                receivedType = file.ContentType,
                receivedExtension = extension
            });
        }

        // Check file size (50MB limit)
        const long maxSize = 50 * 1024 * 1024; // 50MB
        if (file.Length > maxSize)
        {
            return BadRequest(new 
            { 
                error = $"File size exceeds maximum limit of 50MB",
                success = false,
                size = file.Length
            });
        }

        try
        {
            // Read file contents to verify it's readable
            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);
            
            // Read first few lines to verify file structure
            var firstLine = await reader.ReadLineAsync();
            var secondLine = await reader.ReadLineAsync();
            var thirdLine = await reader.ReadLineAsync();

            var response = new
            {
                success = true,
                message = "File uploaded and processed successfully",
                fileInfo = new
                {
                    fileName = file.FileName,
                    size = file.Length,
                    contentType = file.ContentType,
                    extension = extension
                },
                preview = new
                {
                    firstLine,
                    secondLine,
                    thirdLine,
                    estimatedRows = file.Length / (firstLine?.Length ?? 100) // Rough estimate
                },
                timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("Successfully processed file: {FileName}", file.FileName);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file: {FileName}", file.FileName);
            
            return StatusCode(500, new 
            { 
                error = "Failed to process file",
                success = false,
                details = ex.Message
            });
        }
    }

    /// <summary>
    /// Get upload status/info
    /// </summary>
    [HttpGet("info")]
    public IActionResult GetUploadInfo()
    {
        return Ok(new
        {
            endpoint = "/api/simpleupload/csv",
            maxFileSize = "50MB",
            supportedTypes = new[] { "CSV", "XLS", "XLSX" },
            supportedMimeTypes = new[] 
            { 
                "text/csv", 
                "application/vnd.ms-excel", 
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" 
            }
        });
    }
}