using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.src.GAAStat.Dal.Models.application;

/// <summary>
/// KPI definitions with formulas and benchmark values
/// </summary>
[Table("kpi_definitions")]
[Index("KpiCode", Name = "kpi_definitions_kpi_code_key", IsUnique = true)]
public partial class KpiDefinition
{
    [Key]
    [Column("kpi_id")]
    public int KpiId { get; set; }

    [Column("kpi_code")]
    [StringLength(20)]
    public string KpiCode { get; set; } = null!;

    [Column("kpi_name")]
    [StringLength(100)]
    public string KpiName { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("calculation_formula")]
    public string? CalculationFormula { get; set; }

    [Column("benchmark_values", TypeName = "jsonb")]
    public string? BenchmarkValues { get; set; }

    [Column("position_relevance")]
    [StringLength(255)]
    public string? PositionRelevance { get; set; }
}
