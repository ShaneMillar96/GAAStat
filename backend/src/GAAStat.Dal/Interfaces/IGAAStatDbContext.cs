
namespace GAAStat.Dal.Interfaces;

public interface IGAAStatDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    int SaveChanges();
}