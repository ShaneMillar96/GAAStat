using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.Models.application;

/// <summary>
/// KPI event type definitions with default PSR values
/// </summary>
[Table("event_types")]
[Index("Code", Name = "event_types_code_key", IsUnique = true)]
[Index("Code", Name = "idx_event_types_code")]
public partial class EventType
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("code")]
    [StringLength(10)]
    public string Code { get; set; } = null!;

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Column("category")]
    [StringLength(50)]
    public string? Category { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("default_psr_value")]
    public int? DefaultPsrValue { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }

    [InverseProperty("EventType")]
    public virtual ICollection<EventOutcome> EventOutcomes { get; set; } = new List<EventOutcome>();
}
