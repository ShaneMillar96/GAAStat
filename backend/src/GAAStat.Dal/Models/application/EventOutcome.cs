using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.Models.application;

/// <summary>
/// Possible outcomes for events with assigned PSR values (-3 to +3)
/// </summary>
[Table("event_outcomes")]
[Index("EventTypeId", "Outcome", Name = "event_outcomes_event_type_id_outcome_key", IsUnique = true)]
[Index("EventTypeId", "PsrValue", Name = "idx_event_outcomes_psr")]
public partial class EventOutcome
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("event_type_id")]
    public int EventTypeId { get; set; }

    [Column("outcome")]
    [StringLength(100)]
    public string Outcome { get; set; } = null!;

    /// <summary>
    /// Performance Success Rate value assigned to this outcome
    /// </summary>
    [Column("psr_value")]
    public int PsrValue { get; set; }

    [Column("assign_to")]
    [StringLength(20)]
    public string AssignTo { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("EventTypeId")]
    [InverseProperty("EventOutcomes")]
    public virtual EventType EventType { get; set; } = null!;
}
