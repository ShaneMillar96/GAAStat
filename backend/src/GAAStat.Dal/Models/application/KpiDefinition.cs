using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace GAAStat.Dal.Models.Application;

[Table("kpi_definitions")]
public partial class KpiDefinition
{
    [Key]
    [Column("kpi_id")]
    public int KpiId { get; set; }

    [Required]
    [Column("kpi_code")]
    [StringLength(20)]
    public string KpiCode { get; set; } = string.Empty;

    [Required]
    [Column("kpi_name")]
    [StringLength(100)]
    public string KpiName { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("calculation_formula")]
    public string? CalculationFormula { get; set; }

    [Column("benchmark_values", TypeName = "json")]
    public JsonDocument? BenchmarkValues { get; set; }

    [Column("position_relevance")]
    [StringLength(255)]
    public string? PositionRelevance { get; set; }
}