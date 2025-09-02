using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.src.GAAStat.Dal.Models.application;

/// <summary>
/// Statistical metric category groupings
/// </summary>
[Table("metric_categories")]
[Index("CategoryName", Name = "metric_categories_category_name_key", IsUnique = true)]
public partial class MetricCategory
{
    [Key]
    [Column("metric_category_id")]
    public int MetricCategoryId { get; set; }

    [Column("category_name")]
    [StringLength(100)]
    public string CategoryName { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [InverseProperty("MetricCategory")]
    public virtual ICollection<MetricDefinition> MetricDefinitions { get; set; } = new List<MetricDefinition>();
}
