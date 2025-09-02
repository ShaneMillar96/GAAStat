using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Dal.src.GAAStat.Dal.Models.application;

/// <summary>
/// Match venue designations (Home/Away)
/// </summary>
[Table("venues")]
[Index("VenueCode", Name = "venues_venue_code_key", IsUnique = true)]
public partial class Venue
{
    [Key]
    [Column("venue_id")]
    public int VenueId { get; set; }

    [Column("venue_code")]
    [StringLength(20)]
    public string VenueCode { get; set; } = null!;

    [Column("venue_description")]
    [StringLength(100)]
    public string VenueDescription { get; set; } = null!;

    [InverseProperty("Venue")]
    public virtual ICollection<Match> Matches { get; set; } = new List<Match>();
}
