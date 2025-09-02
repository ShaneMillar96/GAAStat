
using Microsoft.EntityFrameworkCore;
using GAAStat.Dal.Models.Application;

namespace GAAStat.Dal.Interfaces;

public interface IGAAStatDbContext
{
    // ETL Tracking Tables
    DbSet<EtlJob> EtlJobs { get; set; }
    DbSet<EtlJobProgress> EtlJobProgresses { get; set; }
    DbSet<EtlValidationError> EtlValidationErrors { get; set; }

    // Standard DbContext methods
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    int SaveChanges();
}