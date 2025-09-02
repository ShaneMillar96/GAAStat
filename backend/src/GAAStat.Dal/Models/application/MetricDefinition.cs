using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GAAStat.Dal.Models.Application;

[Table("metric_definitions")]
public partial class MetricDefinition
{
    [Key]
    [Column("metric_id")]
    public int MetricId { get; set; }

    [Required]
    [Column("metric_name")]
    [StringLength(100)]
    public string MetricName { get; set; } = string.Empty;

    [Column("metric_description")]
    public string? MetricDescription { get; set; }

    [Required]
    [Column("data_type")]
    [StringLength(20)]
    public string DataType { get; set; } = string.Empty;

    [Column("calculation_method")]
    public string? CalculationMethod { get; set; }

    [Required]
    [Column("metric_category_id")]
    public int MetricCategoryId { get; set; }

    [ForeignKey("MetricCategoryId")]
    public virtual MetricCategory MetricCategory { get; set; } = null!;

    public virtual ICollection<MatchTeamStatistics> MatchTeamStatistics { get; set; } = new List<MatchTeamStatistics>();
}