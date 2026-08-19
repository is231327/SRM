using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SRMShared.Auth;

namespace SRMCore.Security;

public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    Guid? AgentId { get; }
    Guid? CustomerId { get; }
    bool IsSystemAdmin { get; }
    bool IsEmployee { get; }
    bool IsCustomerAdmin { get; }
    bool IsCustomer { get; }
    bool IsAgent { get; }
    bool IsCustomerScopedUser { get; }
}

public class CurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            var value = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public Guid? CustomerId
    {
        get
        {
            var value = User?.FindFirstValue(AuthClaimTypes.CustomerId);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public Guid? AgentId
    {
        get
        {
            var value = User?.FindFirstValue(AuthClaimTypes.AgentId);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public bool IsSystemAdmin => User?.IsInRole(AuthRoles.ToName(AuthRoleType.SystemAdmin)) == true;
    public bool IsEmployee => User?.IsInRole(AuthRoles.ToName(AuthRoleType.Employee)) == true;
    public bool IsCustomerAdmin => User?.IsInRole(AuthRoles.ToName(AuthRoleType.CustomerAdmin)) == true;
    public bool IsCustomer => User?.IsInRole(AuthRoles.ToName(AuthRoleType.Customer)) == true;
    public bool IsAgent => User?.IsInRole(AuthRoles.ToName(AuthRoleType.Agent)) == true;
    public bool IsCustomerScopedUser => IsCustomerAdmin || IsCustomer;
}
