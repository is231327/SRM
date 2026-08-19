using SRMAuth.Security;

namespace SRMUnitTests.TestHelpers;

internal class FakeCurrentUserContext : ICurrentUserContext
{
    public bool IsAuthenticated { get; set; } = true;
    public Guid? UserId { get; set; }
    public Guid? CustomerId { get; set; }
    public bool IsSystemAdmin { get; set; }
    public bool IsEmployee { get; set; }
    public bool IsCustomerAdmin { get; set; }
    public bool IsCustomer { get; set; }
    public bool CanManageUsers { get; set; }
}
