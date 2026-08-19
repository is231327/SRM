namespace SRMShared.Auth;

public static class AuthRoles
{
    public static readonly IReadOnlyCollection<AuthRoleType> All = new[]
    {
        AuthRoleType.SystemAdmin,
        AuthRoleType.Employee,
        AuthRoleType.CustomerAdmin,
        AuthRoleType.Customer,
        AuthRoleType.Agent
    };

    public static readonly IReadOnlyCollection<AuthRoleType> HumanRoles = new[]
    {
        AuthRoleType.SystemAdmin,
        AuthRoleType.Employee,
        AuthRoleType.CustomerAdmin,
        AuthRoleType.Customer
    };

    public static string ToName(AuthRoleType role) => role.ToString();

    public static string ToCsv(IEnumerable<AuthRoleType> roles) =>
        string.Join(",", roles.Select(ToName));

    public static string GetDescription(AuthRoleType role) => role switch
    {
        AuthRoleType.SystemAdmin => "Platform-wide administrator with full access.",
        AuthRoleType.Employee => "Internal operational user with cross-customer monitoring access.",
        AuthRoleType.CustomerAdmin => "Customer-scoped administrator with customer user management rights.",
        AuthRoleType.Customer => "Customer-scoped user for monitoring and configuration tasks.",
        AuthRoleType.Agent => "Machine principal used by deployed agents.",
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown auth role.")
    };
}
