using SRMCore.Security;

namespace SRMIntegrationTests.TestHelpers;

internal static class CoreCurrentUserContextFactory
{
    public static ICurrentUserContext Create()
    {
        return new FakeCoreCurrentUserContext();
    }
}

internal class FakeCoreCurrentUserContext : ICurrentUserContext
{
    public bool IsAuthenticated { get; set; } = true;
    public Guid? UserId { get; set; }
    public Guid? AgentId { get; set; }
    public Guid? CustomerId { get; set; }
    public bool IsSystemAdmin { get; set; } = true;
    public bool IsEmployee { get; set; }
    public bool IsCustomerAdmin { get; set; }
    public bool IsCustomer { get; set; }
    public bool IsAgent { get; set; }
    public bool IsCustomerScopedUser { get; set; }
}
