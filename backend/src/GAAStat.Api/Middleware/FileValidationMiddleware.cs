using System.Net;
using System.Text.Json;
using GAAStat.Api.Controllers;

namespace GAAStat.Api.Middleware;

/// <summary>
/// Middleware for validating file uploads and enforcing security policies
/// </summary>
public class FileValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<FileValidationMiddleware> _logger;
    private readonly FileValidationOptions _options;

    public FileValidationMiddleware(
        RequestDelegate next, 
        ILogger<FileValidationMiddleware> logger,
        FileValidationOptions options)
    {
        _next = next;
        _logger = logger;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only validate file uploads for specific endpoints
        if (ShouldValidateFiles(context))
        {
            var validationResult = await ValidateFileUploadAsync(context);
            if (!validationResult.IsValid)
            {
                await WriteErrorResponseAsync(context, validationResult.ErrorMessage, validationResult.Errors);
                return;
            }
        }

        await _next(context);
    }

    private static bool ShouldValidateFiles(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        var method = context.Request.Method.ToUpperInvariant();

        // Validate file uploads for specific endpoints
        var fileUploadEndpoints = new[]
        {
            "/api/matches/upload",
            "/api/matches/validate",
            "/api/import/upload"
        };

        return method == "POST" && fileUploadEndpoints.Any(endpoint => path.Contains(endpoint));
    }

    private async Task<FileValidationResult> ValidateFileUploadAsync(HttpContext context)
    {
        var request = context.Request;

        // Check if request has files
        if (!request.HasFormContentType || request.Form.Files.Count == 0)
        {
            return FileValidationResult.Invalid("No files found in request");
        }

        var errors = new List<string>();

        foreach (var file in request.Form.Files)
        {
            // Validate file exists and has content
            if (file == null || file.Length == 0)
            {
                errors.Add("Empty file detected");
                continue;
            }

            // Validate file name
            if (!IsValidFileName(file.FileName))
            {
                errors.Add($"Invalid file name: {file.FileName}");
                continue;
            }

            // Validate file extension
            if (!IsValidFileExtension(file.FileName))
            {
                errors.Add($"Invalid file extension for file: {file.FileName}. Only {string.Join(", ", _options.AllowedExtensions)} are allowed");
                continue;
            }

            // Validate file size
            if (file.Length > _options.MaxFileSizeBytes)
            {
                errors.Add($"File {file.FileName} exceeds maximum size of {_options.MaxFileSizeBytes / (1024 * 1024)}MB");
                continue;
            }

            // Validate MIME type
            if (!IsValidMimeType(file.ContentType))
            {
                errors.Add($"Invalid MIME type for file: {file.FileName}. Content type: {file.ContentType}");
                continue;
            }

            // Validate file signature (magic bytes) for Excel files
            if (!await IsValidFileSignatureAsync(file))
            {
                errors.Add($"Invalid file signature for file: {file.FileName}. File may be corrupted or not a valid Excel file");
                continue;
            }

            _logger.LogDebug("File {FileName} passed validation checks", file.FileName);
        }

        // Check total number of files
        if (request.Form.Files.Count > _options.MaxFilesPerRequest)
        {
            errors.Add($"Too many files. Maximum {_options.MaxFilesPerRequest} files allowed per request");
        }

        // Check total upload size
        var totalSize = request.Form.Files.Sum(f => f.Length);
        if (totalSize > _options.MaxTotalSizeBytes)
        {
            errors.Add($"Total upload size exceeds maximum limit of {_options.MaxTotalSizeBytes / (1024 * 1024)}MB");
        }

        return errors.Any() 
            ? FileValidationResult.Invalid("File validation failed", errors)
            : FileValidationResult.Valid();
    }

    private bool IsValidFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        // Check for dangerous characters
        var invalidChars = Path.GetInvalidFileNameChars();
        if (fileName.Any(c => invalidChars.Contains(c)))
            return false;

        // Check for path traversal attempts
        if (fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
            return false;

        // Check for hidden files or system files
        if (fileName.StartsWith(".") || fileName.StartsWith("~"))
            return false;

        // Check file name length
        if (fileName.Length > _options.MaxFileNameLength)
            return false;

        return true;
    }

    private bool IsValidFileExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return _options.AllowedExtensions.Contains(extension);
    }

    private bool IsValidMimeType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        return _options.AllowedMimeTypes.Contains(contentType.ToLowerInvariant());
    }

    private static async Task<bool> IsValidFileSignatureAsync(IFormFile file)
    {
        try
        {
            // Read first few bytes to check file signature
            using var stream = file.OpenReadStream();
            var buffer = new byte[8];
            await stream.ReadAsync(buffer);
            stream.Position = 0; // Reset stream position

            // Check for Excel file signatures
            // XLSX files start with PK (ZIP signature)
            if (buffer[0] == 0x50 && buffer[1] == 0x4B)
                return true;

            // XLS files have a different signature
            if (buffer[0] == 0xD0 && buffer[1] == 0xCF && buffer[2] == 0x11 && buffer[3] == 0xE0)
                return true;

            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task WriteErrorResponseAsync(HttpContext context, string message, IEnumerable<string> errors)
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        context.Response.ContentType = "application/json";

        var errorResponse = ApiResponse.Error(message, errors);
        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        _logger.LogWarning("File validation failed: {Message}. Errors: {Errors}", message, string.Join(", ", errors));

        await context.Response.WriteAsync(jsonResponse);
    }
}

#region Configuration and Helper Classes

/// <summary>
/// Configuration options for file validation middleware
/// </summary>
public class FileValidationOptions
{
    /// <summary>
    /// Maximum file size in bytes (default: 50MB)
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024; // 50MB

    /// <summary>
    /// Maximum total upload size in bytes (default: 100MB)
    /// </summary>
    public long MaxTotalSizeBytes { get; set; } = 100 * 1024 * 1024; // 100MB

    /// <summary>
    /// Maximum number of files per request (default: 1)
    /// </summary>
    public int MaxFilesPerRequest { get; set; } = 1;

    /// <summary>
    /// Maximum file name length (default: 255)
    /// </summary>
    public int MaxFileNameLength { get; set; } = 255;

    /// <summary>
    /// Allowed file extensions
    /// </summary>
    public HashSet<string> AllowedExtensions { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ".xlsx", ".xls"
    };

    /// <summary>
    /// Allowed MIME types
    /// </summary>
    public HashSet<string> AllowedMimeTypes { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", // .xlsx
        "application/vnd.ms-excel" // .xls
    };

    /// <summary>
    /// Creates default options from environment variables
    /// </summary>
    public static FileValidationOptions FromEnvironment()
    {
        var options = new FileValidationOptions();

        // Override with environment variables if available
        if (long.TryParse(Environment.GetEnvironmentVariable("MAX_FILE_SIZE_BYTES"), out var maxFileSize))
        {
            options.MaxFileSizeBytes = maxFileSize;
        }

        if (int.TryParse(Environment.GetEnvironmentVariable("MAX_FILES_PER_REQUEST"), out var maxFiles))
        {
            options.MaxFilesPerRequest = maxFiles;
        }

        return options;
    }
}

/// <summary>
/// Result of file validation
/// </summary>
public class FileValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public IEnumerable<string> Errors { get; set; } = new List<string>();

    public static FileValidationResult Valid() => new() { IsValid = true };

    public static FileValidationResult Invalid(string message) => new()
    {
        IsValid = false,
        ErrorMessage = message,
        Errors = new[] { message }
    };

    public static FileValidationResult Invalid(string message, IEnumerable<string> errors) => new()
    {
        IsValid = false,
        ErrorMessage = message,
        Errors = errors
    };
}

#endregion