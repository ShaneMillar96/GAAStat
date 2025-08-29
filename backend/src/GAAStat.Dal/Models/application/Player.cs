using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GAAStat.Dal.Models.application;

public partial class Player
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int TeamId { get; set; }

    public int? JerseyNumber { get; set; }

    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = null!;

    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = null!;

    [StringLength(30)]
    public string? Position { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public int? HeightCm { get; set; }

    public int? WeightKg { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [ForeignKey("TeamId")]
    public virtual Team Team { get; set; } = null!;

    public virtual ICollection<PlayerStat> PlayerStats { get; set; } = new List<PlayerStat>();
}