using Microsoft.EntityFrameworkCore;
using GAAStat.Dal.Interfaces;

namespace GAAStat.Dal.Contexts;

public partial class GAAStatDbContext : DbContext, IGAAStatDbContext
{
    public GAAStatDbContext(DbContextOptions<GAAStatDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

    }

}