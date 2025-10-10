using System.ComponentModel.DataAnnotations;
using GAAStat.Services.ETL.Models;
using Microsoft.Extensions.Logging;

namespace GAAStat.Services.ETL.Transformers;

/// <summary>
/// Transforms and validates KPI definition data extracted from Excel.
/// Performs data validation, normalization, and business rule enforcement.
/// </summary>
public class KpiDataTransformer
{
    private readonly ILogger<KpiDataTransformer> _logger;

    // Valid team assignment values
    private static readonly string[] ValidTeamAssignments = { "Home", "Opposition", "Both" };

    // PSR value constraints (allows negative values for penalties, turnovers, etc.)
    private const decimal MinPsrValue = -10.0m;
    private const decimal MaxPsrValue = 10.0m;

    public KpiDataTransformer(ILogger<KpiDataTransformer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates a list of KPI definitions
    /// </summary>
    /// <param name="definitions">List of KPI definitions to validate</param>
    /// <returns>True if all validations pass</returns>
    public bool ValidateKpiDefinitions(List<KpiDefinitionData> definitions)
    {
        bool isValid = true;

        if (definitions.Count == 0)
        {
            _logger.LogWarning("No KPI definitions found to validate");
            return false;
        }

        foreach (var definition in definitions)
        {
            try
            {
                ValidateSingleDefinition(definition);
            }
            catch (ValidationException ex)
            {
                _logger.LogError(ex, "Validation failed for row {Row}: {Message}",
                    definition.SourceRowNumber, ex.Message);
                isValid = false;
            }
        }

        // Check for duplicates
        if (!ValidateNoDuplicates(definitions))
        {
            isValid = false;
        }

        return isValid;
    }

    /// <summary>
    /// Validates a single KPI definition
    /// </summary>
    private void ValidateSingleDefinition(KpiDefinitionData definition)
    {
        // Validate Event Number
        if (definition.EventNumber <= 0)
        {
            throw new ValidationException($"Invalid Event Number: {definition.EventNumber}. Must be > 0.");
        }

        // Validate Event Name
        if (string.IsNullOrWhiteSpace(definition.EventName))
        {
            throw new ValidationException("Event Name cannot be empty");
        }

        if (definition.EventName.Length > 100)
        {
            throw new ValidationException($"Event Name too long: {definition.EventName.Length} chars (max 100)");
        }

        // Validate Outcome
        if (string.IsNullOrWhiteSpace(definition.Outcome))
        {
            throw new ValidationException("Outcome cannot be empty");
        }

        if (definition.Outcome.Length > 100)
        {
            throw new ValidationException($"Outcome too long: {definition.Outcome.Length} chars (max 100)");
        }

        // Validate Team Assignment
        if (!ValidTeamAssignments.Contains(definition.TeamAssignment, StringComparer.OrdinalIgnoreCase))
        {
            throw new ValidationException(
                $"Invalid Team Assignment: '{definition.TeamAssignment}'. " +
                $"Must be one of: {string.Join(", ", ValidTeamAssignments)}");
        }

        // Validate PSR Value
        if (definition.PsrValue < MinPsrValue || definition.PsrValue > MaxPsrValue)
        {
            throw new ValidationException(
                $"PSR Value out of range: {definition.PsrValue}. " +
                $"Must be between {MinPsrValue} and {MaxPsrValue}");
        }

        // Validate Definition (optional field, but warn if empty)
        if (string.IsNullOrWhiteSpace(definition.Definition))
        {
            _logger.LogWarning("Row {Row}: Definition field is empty", definition.SourceRowNumber);
        }

        if (definition.Definition.Length > 1000)
        {
            throw new ValidationException($"Definition too long: {definition.Definition.Length} chars (max 1000)");
        }

        _logger.LogDebug("Validation passed for row {Row}: {EventName} - {Outcome}",
            definition.SourceRowNumber, definition.EventName, definition.Outcome);
    }

    /// <summary>
    /// Validates that there are no duplicate KPI definitions
    /// Duplicate = Same Event Number + Event Name + Outcome + Team Assignment
    /// </summary>
    private bool ValidateNoDuplicates(List<KpiDefinitionData> definitions)
    {
        var seen = new HashSet<string>();
        var duplicates = new List<string>();

        foreach (var definition in definitions)
        {
            // Create unique key: EventNumber|EventName|Outcome|TeamAssignment
            var key = $"{definition.EventNumber}|{definition.EventName}|{definition.Outcome}|{definition.TeamAssignment}".ToLower();

            if (seen.Contains(key))
            {
                var duplicate = $"Row {definition.SourceRowNumber}: Event {definition.EventNumber} - {definition.EventName} - {definition.Outcome} ({definition.TeamAssignment})";
                duplicates.Add(duplicate);
                _logger.LogError("Duplicate KPI definition found: {Duplicate}", duplicate);
            }
            else
            {
                seen.Add(key);
            }
        }

        if (duplicates.Any())
        {
            _logger.LogError("Found {Count} duplicate KPI definitions", duplicates.Count);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Normalizes team assignment values and other fields to match database constraints
    /// </summary>
    public void NormalizeDefinitions(List<KpiDefinitionData> definitions)
    {
        foreach (var definition in definitions)
        {
            // Normalize Team Assignment (capitalize first letter, fix typos)
            definition.TeamAssignment = NormalizeTeamAssignment(definition.TeamAssignment);

            // Trim all string fields
            definition.EventName = definition.EventName.Trim();
            definition.Outcome = definition.Outcome.Trim();
            definition.Definition = definition.Definition.Trim();
        }

        _logger.LogDebug("Normalized {Count} KPI definitions", definitions.Count);
    }

    /// <summary>
    /// Normalizes team assignment value (fixes typos like "Oppostion" → "Opposition")
    /// </summary>
    private string NormalizeTeamAssignment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        // Fix common typo: "Oppostion" → "Opposition"
        if (value.Equals("Oppostion", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Normalized team assignment: 'Oppostion' → 'Opposition'");
            return "Opposition";
        }

        // Capitalize first letter of valid values
        return value switch
        {
            var v when v.Equals("home", StringComparison.OrdinalIgnoreCase) => "Home",
            var v when v.Equals("opposition", StringComparison.OrdinalIgnoreCase) => "Opposition",
            var v when v.Equals("both", StringComparison.OrdinalIgnoreCase) => "Both",
            _ => value // Return as-is if not recognized (will fail validation)
        };
    }
}
