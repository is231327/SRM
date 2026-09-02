using SRMShared.DTOs.Auth;

namespace SRMApp.Services;

public static class UserListViewHelper
{
    public const string UnassignedCustomerFilter = "unassigned";

    public static IReadOnlyList<UserManagementDto> FilterAndSort(
        IEnumerable<UserManagementDto> source,
        string searchText,
        string role,
        string activity,
        string customer,
        UserSortOption sort)
    {
        var query = source
            .Where(x => MatchesSearch(x, searchText))
            .Where(x => string.IsNullOrWhiteSpace(role)
                || x.Roles.Contains(role, StringComparer.OrdinalIgnoreCase))
            .Where(x => activity switch
            {
                "active" => x.IsActive,
                "inactive" => !x.IsActive,
                _ => true
            })
            .Where(x => MatchesCustomer(x, customer));

        return sort switch
        {
            UserSortOption.UsernameDescending => query.OrderByDescending(x => x.Username, StringComparer.OrdinalIgnoreCase).ToList(),
            UserSortOption.NameAscending => query.OrderBy(x => x.LastName, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.FirstName, StringComparer.OrdinalIgnoreCase).ToList(),
            UserSortOption.NameDescending => query.OrderByDescending(x => x.LastName, StringComparer.OrdinalIgnoreCase).ThenByDescending(x => x.FirstName, StringComparer.OrdinalIgnoreCase).ToList(),
            UserSortOption.LastLoginNewest => query.OrderByDescending(x => x.LastLoginAtUtc.HasValue).ThenByDescending(x => x.LastLoginAtUtc).ToList(),
            UserSortOption.LastLoginOldest => query.OrderByDescending(x => x.LastLoginAtUtc.HasValue).ThenBy(x => x.LastLoginAtUtc).ToList(),
            _ => query.OrderBy(x => x.Username, StringComparer.OrdinalIgnoreCase).ToList()
        };
    }

    private static bool MatchesSearch(UserManagementDto user, string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        var value = searchText.Trim();
        return Contains(user.Username, value)
            || Contains(user.Email, value)
            || Contains(user.FirstName, value)
            || Contains(user.LastName, value);
    }

    private static bool MatchesCustomer(UserManagementDto user, string customer)
    {
        if (string.IsNullOrWhiteSpace(customer))
        {
            return true;
        }

        if (string.Equals(customer, UnassignedCustomerFilter, StringComparison.Ordinal))
        {
            return !user.CustomerId.HasValue;
        }

        return Guid.TryParse(customer, out var customerId) && user.CustomerId == customerId;
    }

    private static bool Contains(string value, string searchText)
        => value.Contains(searchText, StringComparison.OrdinalIgnoreCase);
}

public enum UserSortOption
{
    UsernameAscending,
    UsernameDescending,
    NameAscending,
    NameDescending,
    LastLoginNewest,
    LastLoginOldest
}
