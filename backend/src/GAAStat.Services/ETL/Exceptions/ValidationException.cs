using System;

namespace GAAStat.Services.ETL.Exceptions;

/// <summary>
/// Exception thrown when ETL validation fails.
/// </summary>
public class ValidationException : Exception
{
    /// <summary>
    /// Field name that failed validation
    /// </summary>
    public string? FieldName { get; }

    /// <summary>
    /// Field value that failed validation
    /// </summary>
    public object? FieldValue { get; }

    public ValidationException(string message) : base(message)
    {
    }

    public ValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ValidationException(string fieldName, object? fieldValue, string validationMessage)
        : base($"Validation failed for field '{fieldName}' with value '{fieldValue}': {validationMessage}")
    {
        FieldName = fieldName;
        FieldValue = fieldValue;
    }
}
