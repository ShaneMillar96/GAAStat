using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GAAStat.Dal.Models.Application;

[Table("metric_categories")]
public partial class MetricCategory
{
    [Key]
    [Column("metric_category_id")]
    public int MetricCategoryId { get; set; }

    [Required]
    [Column("category_name")]
    [StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    public virtual ICollection<MetricDefinition> MetricDefinitions { get; set; } = new List<MetricDefinition>();
}