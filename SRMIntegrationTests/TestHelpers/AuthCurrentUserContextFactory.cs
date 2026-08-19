using SRMAuth.Security;

namespace SRMIntegrationTests.TestHelpers;

internal static class AuthCurrentUserContextFactory
{
    public static ICurrentUserContext Create()
    {
        return new FakeAuthCurrentUserContext();
    }
}

internal class FakeAuthCurrentUserContext : ICurrentUserContext
{
    public bool IsAuthenticated { get; set; } = true;
    public Guid? UserId { get; set; }
    public Guid? CustomerId { get; set; }
    public bool IsSystemAdmin { get; set; } = true;
    public bool IsEmployee { get; set; }
    public bool IsCustomerAdmin { get; set; }
    public bool IsCustomer { get; set; }
    public bool CanManageUsers { get; set; } = true;
}
