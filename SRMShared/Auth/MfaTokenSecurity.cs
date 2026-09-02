using System.Security.Claims;

namespace SRMShared.Auth;

public static class MfaTokenSecurity
{
    public static bool HasRequiredMfa(ClaimsPrincipal principal)
        => principal.IsInRole(AuthRoles.ToName(AuthRoleType.Agent))
            || string.Equals(principal.FindFirst(AuthClaimTypes.MfaAuthenticated)?.Value, "true", StringComparison.OrdinalIgnoreCase);
}
