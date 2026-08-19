using Microsoft.EntityFrameworkCore;
using SRMAuth.Data;

namespace SRMUnitTests.TestHelpers;

internal static class AuthDbContextFactory
{
    public static SrmAuthDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SrmAuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new SrmAuthDbContext(options);
    }
}
