using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.src.GAAStat.Dal.Models.application;

/// <summary>
/// Statistical metric explanations and calculation methods
/// </summary>
[Table("metric_definitions")]
[Index("MetricName", Name = "metric_definitions_metric_name_key", IsUnique = true)]
public partial class MetricDefinition
{
    [Key]
    [Column("metric_id")]
    public int MetricId { get; set; }

    [Column("metric_name")]
    [StringLength(100)]
    public string MetricName { get; set; } = null!;

    [Column("metric_description")]
    public string? MetricDescription { get; set; }

    [Column("data_type")]
    [StringLength(20)]
    public string DataType { get; set; } = null!;

    [Column("calculation_method")]
    public string? CalculationMethod { get; set; }

    [Column("metric_category_id")]
    public int MetricCategoryId { get; set; }

    [InverseProperty("MetricDefinition")]
    public virtual ICollection<MatchTeamStatistic> MatchTeamStatistics { get; set; } = new List<MatchTeamStatistic>();

    [ForeignKey("MetricCategoryId")]
    [InverseProperty("MetricDefinitions")]
    public virtual MetricCategory MetricCategory { get; set; } = null!;
}
