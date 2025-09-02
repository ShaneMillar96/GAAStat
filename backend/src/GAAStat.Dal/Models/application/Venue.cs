using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GAAStat.Dal.Models.Application;

[Table("venues")]
public partial class Venue
{
    [Key]
    [Column("venue_id")]
    public int VenueId { get; set; }

    [Required]
    [Column("venue_code")]
    [StringLength(20)]
    public string VenueCode { get; set; } = string.Empty;

    [Required]
    [Column("venue_description")]
    [StringLength(100)]
    public string VenueDescription { get; set; } = string.Empty;

    public virtual ICollection<Match> Matches { get; set; } = new List<Match>();
}