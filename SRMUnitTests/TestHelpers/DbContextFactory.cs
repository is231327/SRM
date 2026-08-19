using Microsoft.EntityFrameworkCore;
using SRMCore.Data;

namespace SRMUnitTests.TestHelpers;

internal static class DbContextFactory
{
    public static SrmCoreDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SrmCoreDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new SrmCoreDbContext(options);
    }
}
