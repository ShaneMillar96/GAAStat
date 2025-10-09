using GAAStat.Dal.Contexts;
using Microsoft.EntityFrameworkCore;

namespace GAAStat.Tests.Integration;

public static class DbContextTestHelper
{
    /// <summary>
    /// Creates an in-memory database context for testing
    /// </summary>
    public static GAAStatDbContext CreateInMemoryContext(string databaseName = "TestDatabase")
    {
        var options = new DbContextOptionsBuilder<GAAStatDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        return new GAAStatDbContext(options);
    }

    /// <summary>
    /// Creates a unique in-memory database context for each test
    /// </summary>
    public static GAAStatDbContext CreateUniqueInMemoryContext()
    {
        return CreateInMemoryContext($"TestDatabase_{Guid.NewGuid()}");
    }
}
