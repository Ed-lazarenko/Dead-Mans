using backend.Api.Contracts;
using backend.Messaging;

namespace backend.Api.Auth;

/// <summary>
/// Rejects state-changing browser requests authenticated by the application cookie unless they
/// carry the non-simple header added by the frontend API client. Cross-site HTML forms cannot add
/// this header, which provides an additional CSRF boundary on top of SameSite cookies and CORS.
/// </summary>
public sealed class ApiClientRequestValidationMiddleware
{
    private readonly RequestDelegate _next;

    public ApiClientRequestValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (RequiresApiClientHeader(context) && !HasValidApiClientHeader(context.Request))
        {
            await ErrorResponseFactory.WriteAsync(
                context.Response,
                StatusCodes.Status403Forbidden,
                AppMessages.Client.ApiClientHeaderRequired,
                AppMessages.ErrorCodes.ApiClientHeaderRequired
            );
            return;
        }

        await _next(context);
    }

    private static bool RequiresApiClientHeader(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true
            || !context.Request.Cookies.ContainsKey(AuthCookieNames.Authentication)
            || IsSafeMethod(context.Request.Method))
        {
            return false;
        }

        return context.Request.Path.StartsWithSegments(
                "/api",
                StringComparison.OrdinalIgnoreCase
            )
            || context.Request.Path.Equals(
                "/auth/logout",
                StringComparison.OrdinalIgnoreCase
            );
    }

    private static bool HasValidApiClientHeader(HttpRequest request)
    {
        return request.Headers.TryGetValue(AuthRequestHeaders.ApiClient, out var values)
            && values.Count == 1
            && string.Equals(
                values[0],
                AuthRequestHeaders.ApiClientValue,
                StringComparison.Ordinal
            );
    }

    private static bool IsSafeMethod(string method)
    {
        return HttpMethods.IsGet(method)
            || HttpMethods.IsHead(method)
            || HttpMethods.IsOptions(method);
    }
}
