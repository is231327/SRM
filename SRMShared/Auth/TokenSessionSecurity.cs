using System.Security.Claims;

namespace SRMShared.Auth;

public static class TokenSessionSecurity
{
    public static async Task<bool> IsCurrentAsync(
        ClaimsPrincipal principal,
        ITokenStateStore tokenStateStore,
        CancellationToken cancellationToken = default)
    {
        var subject = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var sessionVersion = principal.FindFirst(AuthClaimTypes.SessionVersion)?.Value;
        if (!Guid.TryParse(subject, out var principalId) || string.IsNullOrWhiteSpace(sessionVersion))
        {
            return false;
        }

        var principalKey = principal.IsInRole(AuthRoles.ToName(AuthRoleType.Agent))
            ? RedisTokenStateStore.BuildAgentPrincipalKey(principalId)
            : RedisTokenStateStore.BuildUserPrincipalKey(principalId);

        return await tokenStateStore.IsSessionVersionCurrentAsync(principalKey, sessionVersion, cancellationToken);
    }

    public static bool MustChangePassword(ClaimsPrincipal principal)
        => string.Equals(
            principal.FindFirst(AuthClaimTypes.MustChangePassword)?.Value,
            "true",
            StringComparison.OrdinalIgnoreCase);
}
