using SRMApp.Services;
using SRMShared.DTOs.Auth;

namespace SRMUnitTests.Services;

public class UserListViewHelperTests
{
    [Test]
    public void FilterAndSort_AppliesSearchRoleActivityAndCustomerFilters()
    {
        var selectedCustomerId = Guid.NewGuid();
        var users = new[]
        {
            User("zeta", "Zoe", "Zimmer", "Customer", true, selectedCustomerId, new DateTime(2026, 8, 2)),
            User("alpha", "Alex", "Anders", "CustomerAdmin", true, selectedCustomerId, new DateTime(2026, 8, 3)),
            User("beta", "Bea", "Berg", "Customer", false, null, null)
        };

        var result = UserListViewHelper.FilterAndSort(
            users,
            "zoe",
            "Customer",
            "active",
            selectedCustomerId.ToString(),
            UserSortOption.UsernameAscending);

        Assert.That(result.Select(x => x.Username), Is.EqualTo(new[] { "zeta" }));
    }

    [Test]
    public void FilterAndSort_SupportsUnassignedCustomersAndKeepsNeverLoggedInLast()
    {
        var users = new[]
        {
            User("never", "Never", "Logged", "Employee", true, null, null),
            User("old", "Older", "Login", "Employee", true, null, new DateTime(2026, 8, 1)),
            User("new", "Newer", "Login", "Employee", true, null, new DateTime(2026, 8, 3))
        };

        var newest = UserListViewHelper.FilterAndSort(
            users, string.Empty, string.Empty, string.Empty,
            UserListViewHelper.UnassignedCustomerFilter,
            UserSortOption.LastLoginNewest);
        var oldest = UserListViewHelper.FilterAndSort(
            users, string.Empty, string.Empty, string.Empty,
            UserListViewHelper.UnassignedCustomerFilter,
            UserSortOption.LastLoginOldest);

        Assert.Multiple(() =>
        {
            Assert.That(newest.Select(x => x.Username), Is.EqualTo(new[] { "new", "old", "never" }));
            Assert.That(oldest.Select(x => x.Username), Is.EqualTo(new[] { "old", "new", "never" }));
        });
    }

    private static UserManagementDto User(
        string username,
        string firstName,
        string lastName,
        string role,
        bool active,
        Guid? customerId,
        DateTime? lastLogin)
        => new()
        {
            Username = username,
            Email = $"{username}@example.com",
            FirstName = firstName,
            LastName = lastName,
            Roles = [role],
            IsActive = active,
            CustomerId = customerId,
            LastLoginAtUtc = lastLogin
        };
}
