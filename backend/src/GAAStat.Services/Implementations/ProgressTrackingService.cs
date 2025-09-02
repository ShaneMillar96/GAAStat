using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GAAStat.Dal.Contexts;
using GAAStat.Dal.Models.Application;
using GAAStat.Services.Interfaces;
using GAAStat.Services.Models;

namespace GAAStat.Services.Implementations;

/// <summary>
/// Service for tracking ETL job progress and updating database records
/// Manages EtlJobProgress and EtlValidationError entities
/// </summary>
public class ProgressTrackingService : IProgressTrackingService
{
    private readonly GAAStatDbContext _context;
    private readonly ILogger<ProgressTrackingService> _logger;

    public ProgressTrackingService(GAAStatDbContext context, ILogger<ProgressTrackingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Creates initial ETL job record in database
    /// </summary>
    public async Task<ServiceResult<int>> CreateEtlJobAsync(string fileName, long fileSizeBytes, string? createdBy = null)
    {
        try
        {
            var job = new EtlJob
            {
                FileName = fileName,
                FileSizeBytes = fileSizeBytes,
                Status = EtlJobStatus.PENDING,
                CreatedBy = createdBy,
                CreatedAt = DateTime.Now, // Use local time for PostgreSQL timestamp without time zone
                UpdatedAt = DateTime.Now  // Use local time for PostgreSQL timestamp without time zone
            };

            await _context.EtlJobs.AddAsync(job);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created ETL job {JobId} for file {FileName}", job.JobId, fileName);
            return ServiceResult<int>.Success(job.JobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create ETL job for file {FileName}", fileName);
            return ServiceResult<int>.Failed("Failed to create ETL job");
        }
    }

    /// <summary>
    /// Updates ETL job status (pending, processing, completed, failed)
    /// </summary>
    public async Task<ServiceResult> UpdateEtlJobStatusAsync(int jobId, string status, string? errorSummary = null)
    {
        try
        {
            var job = await _context.EtlJobs.FindAsync(jobId);
            if (job == null)
            {
                return ServiceResult.Failed($"ETL job {jobId} not found");
            }

            job.Status = status;
            job.ErrorSummary = errorSummary;
            job.UpdatedAt = DateTime.Now;

            if (status == EtlJobStatus.PROCESSING && job.StartedAt == null)
            {
                job.StartedAt = DateTime.Now;
            }
            else if (status is EtlJobStatus.COMPLETED or EtlJobStatus.FAILED)
            {
                job.CompletedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated ETL job {JobId} status to {Status}", jobId, status);
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update ETL job {JobId} status to {Status}", jobId, status);
            return ServiceResult.Failed("Failed to update ETL job status");
        }
    }

    /// <summary>
    /// Records detailed progress update for current processing stage
    /// </summary>
    public async Task<ServiceResult> RecordProgressUpdateAsync(EtlProgressUpdate progressUpdate)
    {
        try
        {
            var progress = new EtlJobProgress
            {
                JobId = progressUpdate.JobId,
                Stage = progressUpdate.Stage,
                TotalSteps = progressUpdate.TotalSteps,
                CompletedSteps = progressUpdate.CompletedSteps,
                Status = progressUpdate.Status,
                Message = progressUpdate.Message,
                UpdatedAt = DateTime.Now
            };

            await _context.EtlJobProgresses.AddAsync(progress);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Recorded progress for ETL job {JobId}: {Stage} - {Message}", 
                progressUpdate.JobId, progressUpdate.Stage, progressUpdate.Message);
            
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record progress for ETL job {JobId}", progressUpdate.JobId);
            return ServiceResult.Failed("Failed to record progress update");
        }
    }

    /// <summary>
    /// Records validation error encountered during ETL processing
    /// </summary>
    public async Task<ServiceResult> RecordValidationErrorAsync(
        int jobId,
        string? sheetName,
        int? rowNumber,
        string? columnName,
        string errorType,
        string errorMessage,
        string? suggestedFix = null)
    {
        try
        {
            var error = new EtlValidationError
            {
                JobId = jobId,
                SheetName = sheetName,
                RowNumber = rowNumber,
                ColumnName = columnName,
                ErrorType = errorType,
                ErrorMessage = errorMessage,
                SuggestedFix = suggestedFix,
                CreatedAt = DateTime.Now
            };

            await _context.EtlValidationErrors.AddAsync(error);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Recorded validation error for ETL job {JobId}: {ErrorType} - {ErrorMessage}", 
                jobId, errorType, errorMessage);
            
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record validation error for ETL job {JobId}", jobId);
            return ServiceResult.Failed("Failed to record validation error");
        }
    }

    /// <summary>
    /// Records multiple validation errors in a single database operation
    /// </summary>
    public async Task<ServiceResult> RecordValidationErrorsAsync(int jobId, IEnumerable<ValidationError> validationErrors)
    {
        try
        {
            var errors = validationErrors.Select(ve => new EtlValidationError
            {
                JobId = jobId,
                SheetName = ve.SheetName,
                RowNumber = ve.RowNumber,
                ColumnName = ve.ColumnName,
                ErrorType = ve.ErrorType,
                ErrorMessage = ve.ErrorMessage,
                SuggestedFix = ve.SuggestedFix,
                CreatedAt = DateTime.Now
            });

            await _context.EtlValidationErrors.AddRangeAsync(errors);
            await _context.SaveChangesAsync();

            var errorCount = errors.Count();
            _logger.LogWarning("Recorded {ErrorCount} validation errors for ETL job {JobId}", errorCount, jobId);
            
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record validation errors for ETL job {JobId}", jobId);
            return ServiceResult.Failed("Failed to record validation errors");
        }
    }

    /// <summary>
    /// Retrieves current progress status for an ETL job
    /// </summary>
    public async Task<ServiceResult<EtlProgressUpdate?>> GetCurrentProgressAsync(int jobId)
    {
        try
        {
            var latestProgress = await _context.EtlJobProgresses
                .Where(p => p.JobId == jobId)
                .OrderByDescending(p => p.UpdatedAt)
                .FirstOrDefaultAsync();

            if (latestProgress == null)
            {
                return ServiceResult<EtlProgressUpdate?>.Success(null);
            }

            var progressUpdate = new EtlProgressUpdate
            {
                JobId = latestProgress.JobId,
                Stage = latestProgress.Stage,
                TotalSteps = latestProgress.TotalSteps,
                CompletedSteps = latestProgress.CompletedSteps,
                Status = latestProgress.Status ?? string.Empty,
                Message = latestProgress.Message ?? string.Empty
            };

            return ServiceResult<EtlProgressUpdate?>.Success(progressUpdate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current progress for ETL job {JobId}", jobId);
            return ServiceResult<EtlProgressUpdate?>.Failed("Failed to retrieve progress information");
        }
    }

    /// <summary>
    /// Marks ETL job as started and records start timestamp
    /// </summary>
    public async Task<ServiceResult> MarkJobStartedAsync(int jobId)
    {
        return await UpdateEtlJobStatusAsync(jobId, EtlJobStatus.PROCESSING);
    }

    /// <summary>
    /// Marks ETL job as completed and records completion timestamp
    /// </summary>
    public async Task<ServiceResult> MarkJobCompletedAsync(int jobId)
    {
        return await UpdateEtlJobStatusAsync(jobId, EtlJobStatus.COMPLETED);
    }
}