using SRMCore.Security;

namespace SRMCore.Middleware;

public class AuthorizationExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ForbiddenAccessException exception)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                title = "Forbidden",
                status = StatusCodes.Status403Forbidden,
                detail = exception.Message
            });
        }
    }
}
