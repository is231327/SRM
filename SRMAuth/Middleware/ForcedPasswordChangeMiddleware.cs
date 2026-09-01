using Microsoft.AspNetCore.Mvc;
using SRMShared.Auth;

namespace SRMAuth.Middleware;

public class ForcedPasswordChangeMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && TokenSessionSecurity.MustChangePassword(context.User)
            && !IsAllowedDuringForcedPasswordChange(context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = "Password change required",
                Detail = "The password must be changed before accessing this resource.",
                Status = StatusCodes.Status403Forbidden
            });
            return;
        }

        await next(context);
    }

    private static bool IsAllowedDuringForcedPasswordChange(HttpRequest request)
    {
        if (request.Path.Equals("/api/auth/change-password", StringComparison.OrdinalIgnoreCase)
            || request.Path.Equals("/api/auth/logout", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return HttpMethods.IsGet(request.Method)
            && request.Path.Equals("/api/auth/me", StringComparison.OrdinalIgnoreCase);
    }
}
