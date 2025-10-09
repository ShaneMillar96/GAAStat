using System.Collections.Generic;
using System.Linq;

namespace GAAStat.Services.ETL.Models;

/// <summary>
/// Result of validation operation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Whether validation passed
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// List of validation warnings
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static ValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with errors
    /// </summary>
    public static ValidationResult Failure(params string[] errors)
    {
        return new ValidationResult
        {
            IsValid = false,
            Errors = errors.ToList()
        };
    }

    /// <summary>
    /// Adds an error to the result
    /// </summary>
    public void AddError(string error)
    {
        Errors.Add(error);
        IsValid = false;
    }

    /// <summary>
    /// Adds a warning to the result
    /// </summary>
    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }
}
