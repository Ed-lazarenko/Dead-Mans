using System.Security.Claims;
using backend.Api.Contracts;
using backend.Application.Abstractions.Auth;
using backend.Messaging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace backend.Api.Auth;

public sealed class ActiveUserMiddleware
{
    private readonly RequestDelegate _next;

    public ActiveUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAuthUserReader authUserReader)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (
                string.IsNullOrWhiteSpace(userIdValue)
                || !Guid.TryParse(userIdValue, out var userId)
            )
            {
                await RejectAsync(context);
                return;
            }

            var user = await authUserReader.FindByIdAsync(userId, context.RequestAborted);
            if (user is null || !user.IsActive)
            {
                await RejectAsync(context);
                return;
            }
        }

        await _next(context);
    }

    private static async Task RejectAsync(HttpContext context)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (IsApiOrAuthPath(context.Request.Path))
        {
            await ErrorResponseFactory.WriteAsync(
                context.Response,
                StatusCodes.Status401Unauthorized,
                AppMessages.Client.UserMissingOrInactive
            );
            return;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
    }

    private static bool IsApiOrAuthPath(PathString path)
    {
        return path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/auth", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase);
    }
}
