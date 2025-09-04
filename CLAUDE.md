# GAAStat Development Guide

This document outlines the .NET development patterns, conventions, and best practices for the GAAStat application. The codebase follows clean architecture principles with a database-first approach optimized for GAA statistics processing and analytics.

## Architecture Overview

The application uses a layered architecture with clear separation of concerns:

- **GAAStat.Api**: Web API layer handling HTTP requests and responses
- **GAAStat.Services**: Business logic and file processing services
- **GAAStat.Dal**: Data Access Layer with Entity Framework Core
- **Database**: PostgreSQL with Flyway migrations

## Core Development Patterns

### Database-First Approach

The project uses a database-first strategy where the schema is defined in SQL migrations and EF Core models are scaffolded from the database.

```bash
# Scaffold models after schema changes
make scaffold-models
```

**Key Principles:**
- Database schema drives model generation
- All schema changes go through Flyway migrations
- Models are regenerated, not manually edited
- Use partial classes for extending generated models

### Clean Code Philosophy

Code should be self-documenting with minimal comments. Focus on:

- Descriptive method and variable names
- Single responsibility principle
- Short, focused methods
- Clear control flow

**Example:**
```csharp
public async Task<ProcessingResult> ProcessMatchStatisticsAsync(Stream csvStream, string fileName)
{
    var matchData = await ParseMatchDataAsync(csvStream);
    var validationResult = ValidateMatchData(matchData);
    
    if (!validationResult.IsValid)
        return ProcessingResult.Failed(validationResult.Errors);
    
    var match = await CreateMatchRecordAsync(matchData);
    await SavePlayerStatisticsAsync(match.Id, matchData.PlayerStats);
    
    return ProcessingResult.Success(match.Id);
}
```

### Configuration Management

Use strongly-typed configuration with environment variables:

```csharp
public static class ConfigurationKeys
{
    public const string DATABASE_CONNECTION_STRING = "DATABASE_CONNECTION_STRING";
    public const string MAX_FILE_SIZE_MB = "MAX_FILE_SIZE_MB";
    public const string FILE_UPLOAD_PATH = "FILE_UPLOAD_PATH";
}

public static class EnvironmentVariables
{
    public static string DatabaseConnectionString =>
        Environment.GetEnvironmentVariable(ConfigurationKeys.DATABASE_CONNECTION_STRING) ??
        "Host=localhost;Database=gaastat-dev;Username=gaastat;Password=password1;";
        
    public static int MaxFileSizeMb =>
        int.TryParse(Environment.GetEnvironmentVariable(ConfigurationKeys.MAX_FILE_SIZE_MB), out var size) ? size : 50;
}
```

### Constants and Enums

Replace magic strings and numbers with constants and enums:

```csharp
public static class MatchStatus
{
    public const string SCHEDULED = "scheduled";
    public const string IN_PROGRESS = "in_progress";
    public const string COMPLETED = "completed";
    public const string CANCELLED = "cancelled";
}

public static class GaaConstants
{
    public const int MAX_JERSEY_NUMBER = 99;
    public const int MIN_JERSEY_NUMBER = 1;
    public const int POINTS_PER_GOAL = 3;
    public const string DEFAULT_COMPETITION = "Club Championship";
}

public enum SportType
{
    Hurling,
    Football
}

public enum FileProcessingStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}
```

## Service Layer Patterns

### Service Interface Design

Define clear contracts with specific return types:

```csharp
public interface IMatchStatisticsService
{
    Task<ServiceResult<MatchSummary>> ProcessCsvFileAsync(Stream csvStream, string fileName);
    Task<ServiceResult<IEnumerable<TopScorer>>> GetTopScorersAsync(int matchId);
    Task<ServiceResult<TeamStatistics>> GetTeamStatisticsAsync(int teamId, int matchId);
}

public class ServiceResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public IEnumerable<string> ValidationErrors { get; set; } = [];

    public static ServiceResult<T> Success(T data) => new() { IsSuccess = true, Data = data };
    public static ServiceResult<T> Failed(string error) => new() { IsSuccess = false, ErrorMessage = error };
    public static ServiceResult<T> ValidationFailed(IEnumerable<string> errors) => 
        new() { IsSuccess = false, ValidationErrors = errors };
}
```

### Service Implementation

```csharp
public class MatchStatisticsService : IMatchStatisticsService
{
    private readonly IGAAStatDbContext _context;
    private readonly ICsvParsingService _csvParser;
    private readonly ILogger<MatchStatisticsService> _logger;

    public MatchStatisticsService(
        IGAAStatDbContext context,
        ICsvParsingService csvParser,
        ILogger<MatchStatisticsService> logger)
    {
        _context = context;
        _csvParser = csvParser;
        _logger = logger;
    }

    public async Task<ServiceResult<MatchSummary>> ProcessCsvFileAsync(Stream csvStream, string fileName)
    {
        try
        {
            var parseResult = await _csvParser.ParseMatchDataAsync(csvStream);
            if (!parseResult.IsValid)
                return ServiceResult<MatchSummary>.ValidationFailed(parseResult.Errors);

            var match = CreateMatchFromParsedData(parseResult.Data, fileName);
            await _context.Matches.AddAsync(match);
            await _context.SaveChangesAsync();

            return ServiceResult<MatchSummary>.Success(CreateMatchSummary(match));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process CSV file {FileName}", fileName);
            return ServiceResult<MatchSummary>.Failed("File processing failed");
        }
    }
}
```

## Controller Patterns

### API Controller Structure

```csharp
[ApiController]
[Route("api/[controller]")]
public class MatchesController : ControllerBase
{
    private readonly IMatchStatisticsService _matchService;

    public MatchesController(IMatchStatisticsService matchService)
    {
        _matchService = matchService;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadMatchFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse.Error("No file provided"));

        if (!IsValidFileType(file.ContentType))
            return BadRequest(ApiResponse.Error("Invalid file type. Only CSV and Excel files are supported"));

        if (file.Length > EnvironmentVariables.MaxFileSizeMb * 1024 * 1024)
            return BadRequest(ApiResponse.Error($"File size exceeds maximum limit of {EnvironmentVariables.MaxFileSizeMb}MB"));

        using var stream = file.OpenReadStream();
        var result = await _matchService.ProcessCsvFileAsync(stream, file.FileName);

        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : BadRequest(ApiResponse.Error(result.ErrorMessage, result.ValidationErrors));
    }

    [HttpGet("{id}/statistics")]
    public async Task<IActionResult> GetMatchStatistics(int id)
    {
        var result = await _matchService.GetMatchStatisticsAsync(id);
        return result.IsSuccess 
            ? Ok(ApiResponse.Success(result.Data))
            : NotFound(ApiResponse.Error("Match not found"));
    }
}
```

### API Response Pattern

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public IEnumerable<string> Errors { get; set; } = [];
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public static class ApiResponse
{
    public static ApiResponse<T> Success<T>(T data, string? message = null) => 
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<object> Success(string message) => 
        new() { Success = true, Message = message };

    public static ApiResponse<object> Error(string message, IEnumerable<string>? errors = null) => 
        new() { Success = false, Message = message, Errors = errors ?? [] };
}
```

## Data Validation

### Model Validation

Use Data Annotations for basic validation, custom validators for complex rules:

```csharp
public class CreateMatchRequest
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string HomeTeamName { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string AwayTeamName { get; set; } = string.Empty;

    [Required]
    public DateTime MatchDate { get; set; }

    [StringLength(100)]
    public string? Venue { get; set; }

    [EnumDataType(typeof(SportType))]
    public SportType Sport { get; set; }
}

public class MatchRequestValidator
{
    public ValidationResult ValidateMatchRequest(CreateMatchRequest request)
    {
        var errors = new List<string>();

        if (request.MatchDate > DateTime.UtcNow.AddYears(1))
            errors.Add("Match date cannot be more than one year in the future");

        if (request.HomeTeamName.Equals(request.AwayTeamName, StringComparison.OrdinalIgnoreCase))
            errors.Add("Home team and away team cannot be the same");

        return errors.Any() 
            ? ValidationResult.Failed(errors)
            : ValidationResult.Success();
    }
}
```

## CSV Data Processing Patterns

*Note: Final patterns will be determined after analyzing the CSV data structure*

### Flexible CSV Processing Framework

```csharp
public interface ICsvParsingService
{
    Task<ParseResult<MatchData>> ParseMatchDataAsync(Stream csvStream);
    Task<ParseResult<IEnumerable<PlayerStatistic>>> ParsePlayerStatisticsAsync(Stream csvStream);
}

public class CsvParsingService : ICsvParsingService
{
    public async Task<ParseResult<MatchData>> ParseMatchDataAsync(Stream csvStream)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null
        };

        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, config);

        try
        {
            var records = await Task.Run(() => csv.GetRecords<CsvMatchRecord>().ToList());
            var matchData = TransformCsvToMatchData(records);
            var validation = ValidateMatchData(matchData);

            return validation.IsValid 
                ? ParseResult<MatchData>.Success(matchData)
                : ParseResult<MatchData>.Failed(validation.Errors);
        }
        catch (CsvHelperException ex)
        {
            return ParseResult<MatchData>.Failed([$"CSV parsing error: {ex.Message}"]);
        }
    }
}
```

## Security Best Practices

### Input Validation

```csharp
public static class SecurityValidation
{
    public static bool IsValidFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return !string.IsNullOrEmpty(fileName) && 
               !fileName.Any(c => invalidChars.Contains(c)) &&
               !fileName.StartsWith(".") &&
               fileName.Length <= FileConstants.MAX_FILENAME_LENGTH;
    }

    public static bool IsSafeFileExtension(string extension)
    {
        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".csv", ".xlsx", ".xls"
        };
        return allowedExtensions.Contains(extension);
    }

    public static string SanitizeInput(string input, int maxLength = 1000)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return input.Length > maxLength 
            ? input[..maxLength].Trim()
            : input.Trim();
    }
}
```

### Configuration Security

Never hardcode sensitive values:

```csharp
public static class DatabaseConfiguration
{
    public static string GetConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "DATABASE_CONNECTION_STRING environment variable is required");
        }

        return connectionString;
    }
}
```

## Error Handling

### Global Exception Handling

```csharp
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await HandleValidationExceptionAsync(context, ex);
        }
        catch (FileProcessingException ex)
        {
            await HandleFileProcessingExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            await HandleGenericExceptionAsync(context, ex);
        }
    }

    private async Task HandleValidationExceptionAsync(HttpContext context, ValidationException ex)
    {
        _logger.LogWarning(ex, "Validation error occurred");
        
        var response = ApiResponse.Error("Validation failed", ex.Errors);
        await WriteResponseAsync(context, HttpStatusCode.BadRequest, response);
    }
}
```

## Dependency Injection Configuration

### Service Registration

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGAAStatServices(this IServiceCollection services)
    {
        services.AddScoped<IMatchStatisticsService, MatchStatisticsService>();
        services.AddScoped<ICsvParsingService, CsvParsingService>();
        services.AddScoped<IFileValidationService, FileValidationService>();
        services.AddScoped<IPlayerStatisticsService, PlayerStatisticsService>();
        
        return services;
    }

    public static IServiceCollection AddGAAStatDatabase(this IServiceCollection services)
    {
        services.AddDbContext<GAAStatDbContext>(options =>
            options.UseNpgsql(EnvironmentVariables.DatabaseConnectionString));

        services.AddScoped<IGAAStatDbContext>(provider =>
            provider.GetRequiredService<GAAStatDbContext>());

        return services;
    }
}
```

## Testing Patterns

### Unit Test Structure

```csharp
[TestFixture]
public class MatchStatisticsServiceTests
{
    private Mock<IGAAStatDbContext> _mockContext;
    private Mock<ICsvParsingService> _mockCsvParser;
    private Mock<ILogger<MatchStatisticsService>> _mockLogger;
    private MatchStatisticsService _service;

    [SetUp]
    public void SetUp()
    {
        _mockContext = new Mock<IGAAStatDbContext>();
        _mockCsvParser = new Mock<ICsvParsingService>();
        _mockLogger = new Mock<ILogger<MatchStatisticsService>>();
        _service = new MatchStatisticsService(_mockContext.Object, _mockCsvParser.Object, _mockLogger.Object);
    }

    [Test]
    public async Task ProcessCsvFileAsync_ValidFile_ReturnsSuccess()
    {
        // Arrange
        var csvStream = CreateTestCsvStream();
        var expectedMatchData = CreateTestMatchData();
        
        _mockCsvParser.Setup(x => x.ParseMatchDataAsync(It.IsAny<Stream>()))
                     .ReturnsAsync(ParseResult<MatchData>.Success(expectedMatchData));

        // Act
        var result = await _service.ProcessCsvFileAsync(csvStream, "test.csv");

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        _mockContext.Verify(x => x.SaveChangesAsync(), Times.Once);
    }
}
```

### Integration Test Patterns

```csharp
[TestFixture]
public class MatchesControllerIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task UploadMatchFile_ValidCsvFile_ReturnsCreated()
    {
        // Arrange
        var fileContent = CreateValidCsvContent();
        var formFile = CreateFormFile("test-match.csv", fileContent);

        // Act
        var response = await PostFileAsync("/api/matches/upload", formFile);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<MatchSummary>>(responseContent);
        
        Assert.That(apiResponse.Success, Is.True);
        Assert.That(apiResponse.Data.Id, Is.GreaterThan(0));
    }
}
```

## Performance Considerations

### Database Query Optimization

```csharp
public async Task<IEnumerable<TopScorer>> GetTopScorersAsync(int matchId, int count = 10)
{
    return await _context.PlayerStats
        .Where(ps => ps.MatchId == matchId)
        .OrderByDescending(ps => ps.PointsScored + (ps.GoalsScored * GaaConstants.POINTS_PER_GOAL))
        .Take(count)
        .Select(ps => new TopScorer
        {
            PlayerId = ps.PlayerId,
            PlayerName = $"{ps.Player.FirstName} {ps.Player.LastName}",
            TotalScore = ps.PointsScored + (ps.GoalsScored * GaaConstants.POINTS_PER_GOAL),
            Goals = ps.GoalsScored,
            Points = ps.PointsScored
        })
        .ToListAsync();
}
```

### Async/Await Best Practices

```csharp
public async Task<ProcessingResult> ProcessLargeFileAsync(Stream fileStream)
{
    const int batchSize = 1000;
    var allRecords = await ReadAllRecordsAsync(fileStream);
    
    for (int i = 0; i < allRecords.Count; i += batchSize)
    {
        var batch = allRecords.Skip(i).Take(batchSize);
        await ProcessBatchAsync(batch);
        
        // Allow other tasks to run
        await Task.Yield();
    }
    
    return ProcessingResult.Success();
}
```

## Logging Patterns

### Structured Logging

```csharp
public class MatchStatisticsService
{
    private static readonly Action<ILogger, string, int, Exception?> LogFileProcessingStarted =
        LoggerMessage.Define<string, int>(
            LogLevel.Information,
            new EventId(1001, nameof(LogFileProcessingStarted)),
            "Started processing file {FileName} with size {FileSizeBytes}");

    private static readonly Action<ILogger, int, TimeSpan, Exception?> LogFileProcessingCompleted =
        LoggerMessage.Define<int, TimeSpan>(
            LogLevel.Information,
            new EventId(1002, nameof(LogFileProcessingCompleted)),
            "Completed processing file. Created match {MatchId} in {Duration}");

    public async Task<ServiceResult<MatchSummary>> ProcessCsvFileAsync(Stream csvStream, string fileName)
    {
        LogFileProcessingStarted(_logger, fileName, (int)csvStream.Length, null);
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await InternalProcessFileAsync(csvStream, fileName);
            LogFileProcessingCompleted(_logger, result.Data.Id, stopwatch.Elapsed, null);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File processing failed for {FileName}", fileName);
            throw;
        }
    }
}
```

## Development Workflow

### Code Review Checklist

- [ ] No hardcoded values (use constants/enums)
- [ ] Proper error handling and logging
- [ ] Input validation and security checks
- [ ] Unit tests for new functionality
- [ ] Database operations are async
- [ ] Configuration uses environment variables
- [ ] Code follows established naming conventions
- [ ] Comments only where absolutely necessary
- [ ] Performance considerations addressed

### Pre-commit Checklist

- [ ] All tests pass
- [ ] No compiler warnings
- [ ] Database migrations applied
- [ ] Models regenerated if schema changed
- [ ] Security scan completed
- [ ] Performance benchmarks acceptable

## ETL Excel Processing Patterns

### Excel File Structure Requirements

The GAAStat ETL system processes Excel files containing GAA match statistics with specific structural requirements:

**File Structure:**
- Match sheets: Named with pattern `##. Competition vs Team` (e.g., `07. Drum vs Lissan 03.08.25`)
- Player stats sheets: Named with pattern `##. Player Stats vs Team` (e.g., `07. Player Stats vs Lissan 03.0`)
- Sheet numbers (01-99) are used to associate player stats sheets with their corresponding match sheets

**Critical Limitations:**
- Excel sheet names are truncated to 31 characters, which can cause date information loss
- Sheet detection relies on numeric prefixes for reliable matching
- Player stats sheets must have corresponding match sheets with the same number

### Critical Known Issues and Solutions

#### 1. Stream Exhaustion Issue
**Problem:** Creating multiple `ExcelPackage` instances from the same stream exhausts it, causing subsequent sheets to fail processing.

**Solution:** Load `ExcelPackage` once before the processing loop:
```csharp
// CRITICAL FIX: Load ExcelPackage ONCE to prevent stream exhaustion
_logger.LogInformation("🔧 Loading ExcelPackage once for all sheet processing to prevent stream issues");
fileStream.Position = 0;

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
using var package = new ExcelPackage(fileStream);

// Pass ExcelWorksheet objects instead of recreating from stream
var worksheet = package.Workbook.Worksheets[sheet.Name];
```

#### 2. Entity Framework Context State Corruption
**Problem:** Reusing the same `DbContext` instance across multiple sheet processing iterations causes context state corruption and connection pool exhaustion, leading to hanging save operations.

**Solution:** Create scoped `DbContext` for each sheet processing iteration:
```csharp
// CRITICAL FIX: Create new DbContext scope for each sheet
using var scope = _serviceProvider.CreateScope();
var scopedContext = scope.ServiceProvider.GetRequiredService<GAAStatDbContext>();

var statsCreated = await SavePlayerStatisticsWithScopedContextAsync(
    scopedContext, jobId, matchId.Value, playerStatistics, cancellationToken);
```

#### 3. Sheet Processing Timeout Handling
**Problem:** Using `return` in timeout catch blocks exits the entire processing function, preventing remaining sheets from being processed.

**Solution:** Use `continue` to process remaining sheets:
```csharp
catch (OperationCanceledException ex) when (sheetCancellationTokenSource.Token.IsCancellationRequested)
{
    _logger.LogError("Sheet processing timed out after 2 minutes for sheet '{SheetName}' - continuing with remaining sheets", sheet.Name);
    
    // CRITICAL FIX: Continue with remaining sheets instead of exiting entire function
    continue;
}
```

### EPPlus Best Practices

**Stream Management:**
- Always load `ExcelPackage` once when processing multiple sheets
- Reset stream position to 0 before creating the package
- Pass `ExcelWorksheet` objects instead of recreating from stream

**Performance Optimization:**
- Use `ExcelPackage.LicenseContext = LicenseContext.NonCommercial` for licensing
- Process worksheets directly instead of recreating package instances
- Implement proper disposal patterns with `using` statements

### Entity Framework Core ETL Patterns

**DbContext Scoping:**
```csharp
public async Task ProcessWithScopedContext()
{
    // Create new scope for isolated context state
    using var scope = _serviceProvider.CreateScope();
    var scopedContext = scope.ServiceProvider.GetRequiredService<GAAStatDbContext>();
    
    // Process data with fresh context
    await ProcessDataAsync(scopedContext);
}
```

**Batch Processing:**
- Use configurable batch sizes (default: 100 records)
- Process players and statistics in batches to avoid memory issues
- Implement bulk operations for better performance

**Async Best Practices:**
```csharp
// Always use ConfigureAwait(false) to prevent deadlocks
await scopedContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

// Use proper cancellation token handling
using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));
```

### Sheet-to-Match Mapping Pattern

**Reliable Sheet Association:**
```csharp
private string? ExtractSheetNumber(string sheetName)
{
    if (string.IsNullOrEmpty(sheetName))
        return null;
        
    var match = System.Text.RegularExpressions.Regex.Match(sheetName, @"^(\d{2})\.");
    return match.Success ? match.Groups[1].Value : null;
}

private int? FindMatchIdForPlayerStatsSheet(string playerStatsSheetName, Dictionary<string, int> matchIdMap)
{
    var playerStatsNumber = ExtractSheetNumber(playerStatsSheetName);
    if (playerStatsNumber == null) return null;

    // Find corresponding match sheet with same number
    foreach (var kvp in matchIdMap)
    {
        var matchSheetNumber = ExtractSheetNumber(kvp.Key);
        if (matchSheetNumber == playerStatsNumber)
        {
            return kvp.Value;
        }
    }
    
    return null;
}
```

### Error Handling Strategies

**Timeout Management:**
- Implement per-sheet timeouts (default: 2 minutes)
- Use overall processing timeout (default: 10 minutes)
- Continue processing remaining sheets on individual timeouts

**Validation Error Collection:**
```csharp
private async Task RecordValidationErrorAsync(int jobId, string sheetName, 
    string errorType, string errorMessage, string suggestedFix)
{
    var error = new EtlValidationError
    {
        JobId = jobId,
        SheetName = sheetName,
        ErrorType = errorType,
        ErrorMessage = errorMessage,
        SuggestedFix = suggestedFix,
        CreatedAt = DateTime.UtcNow
    };
    
    await _progressService.RecordValidationErrorAsync(error);
}
```

**Transaction Boundaries:**
- Use scoped contexts for transaction isolation
- Implement proper rollback strategies for failed operations
- Maintain data consistency across batch operations

### Performance Considerations

**Memory Management:**
- Process large files in configurable batches
- Dispose of resources properly with `using` statements
- Avoid keeping large objects in memory longer than necessary

**Connection Pool Management:**
- Create new DbContext instances for isolated operations
- Use scoped service provider for dependency injection
- Implement proper connection lifetime management

**Concurrent Processing:**
- Use `Task.Yield()` to allow other tasks to run during long operations
- Implement cancellation token support throughout the pipeline
- Monitor and log processing durations for performance tuning

This guide will be updated as additional ETL patterns and optimizations are identified.