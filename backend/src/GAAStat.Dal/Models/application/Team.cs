using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GAAStat.Dal.Models.application;

public partial class Team
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [StringLength(50)]
    public string? County { get; set; }

    [Required]
    [StringLength(20)]
    public string Sport { get; set; } = null!;

    [StringLength(50)]
    public string? Division { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Player> Players { get; set; } = new List<Player>();
}