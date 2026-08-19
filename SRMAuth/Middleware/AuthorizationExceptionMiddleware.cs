using System.Text.Json;

namespace SRMAuth.Middleware;

public class AuthorizationExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (UnauthorizedAccessException exception)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";

            var payload = new
            {
                Title = "Forbidden",
                Status = StatusCodes.Status403Forbidden,
                Detail = exception.Message
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
        catch (InvalidOperationException exception)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";

            var payload = new
            {
                Title = "Bad Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = exception.Message
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}
