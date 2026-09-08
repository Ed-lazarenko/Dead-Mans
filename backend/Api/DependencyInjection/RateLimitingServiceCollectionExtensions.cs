using System.Security.Claims;
using System.Threading.RateLimiting;
using backend.Api.Contracts;
using backend.Api.Http;
using backend.Api.Configuration;
using backend.Messaging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;

namespace backend.Api.DependencyInjection;

public static class RateLimitingServiceCollectionExtensions
{
    public static IServiceCollection AddDeadMansRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment
    )
    {
        var requiresRateLimiting = !environment.IsDevelopment()
            && !environment.IsEnvironment("Testing");
        services
            .AddOptions<RateLimitingOptions>()
            .Bind(configuration.GetSection(RateLimitingOptions.SectionName))
            .Validate(
                options => !requiresRateLimiting || options.Enabled,
                $"{RateLimitingOptions.SectionName} must be enabled outside Development and Testing."
            )
            .Validate(
                static options =>
                    !options.Enabled
                    || (
                        options.Auth.PermitLimit > 0
                        && options.Auth.WindowSeconds > 0
                        && options.Reads.PermitLimit > 0
                        && options.Reads.WindowSeconds > 0
                        && options.Realtime.PermitLimit > 0
                        && options.Realtime.WindowSeconds > 0
                        && options.Mutations.PermitLimit > 0
                        && options.Mutations.WindowSeconds > 0
                    ),
                $"{RateLimitingOptions.SectionName} limits must be positive when enabled."
            )
            .ValidateOnStart();

        var options =
            configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
            ?? new RateLimitingOptions();

        var enabled = options.Enabled && !environment.IsEnvironment("Testing");

        services.AddRateLimiter(limiterOptions =>
        {
            limiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiterOptions.OnRejected = static async (context, _) =>
            {
                ApiErrorMetrics.Record(
                    StatusCodes.Status429TooManyRequests,
                    AppMessages.ErrorCodes.TooManyRequests,
                    "rate_limiter"
                );

                if (!context.HttpContext.Response.HasStarted)
                {
                    await ErrorResponseFactory.WriteAsync(
                        context.HttpContext.Response,
                        StatusCodes.Status429TooManyRequests,
                        AppMessages.Client.TooManyRequests,
                        AppMessages.ErrorCodes.TooManyRequests
                    );
                }
            };

            limiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                httpContext =>
                {
                    if (!enabled)
                    {
                        return RateLimitPartition.GetNoLimiter("disabled");
                    }

                    if (
                        httpContext.Request.Path.StartsWithSegments(
                            "/auth",
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        return CreateFixedWindow("auth", httpContext, options.Auth);
                    }

                    if (IsMutatingApiRequest(httpContext))
                    {
                        return CreateFixedWindow("mutation", httpContext, options.Mutations);
                    }

                    if (IsReadApiRequest(httpContext))
                    {
                        return CreateFixedWindow("read", httpContext, options.Reads);
                    }

                    if (
                        httpContext.Request.Path.StartsWithSegments(
                            "/hubs",
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        return CreateFixedWindow("realtime", httpContext, options.Realtime);
                    }

                    return RateLimitPartition.GetNoLimiter("unlimited");
                }
            );
        });

        return services;
    }

    private static RateLimitPartition<string> CreateFixedWindow(
        string scope,
        HttpContext httpContext,
        RateLimitRule rule
    )
    {
        var clientKey = ResolveClientKey(httpContext);
        return RateLimitPartition.GetFixedWindowLimiter(
            $"{scope}:{clientKey}",
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = rule.PermitLimit,
                Window = TimeSpan.FromSeconds(rule.WindowSeconds),
                QueueLimit = 0
            }
        );
    }

    private static bool IsMutatingApiRequest(HttpContext context)
    {
        if (
            !context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
        )
        {
            return false;
        }

        var method = context.Request.Method;
        return HttpMethods.IsPost(method)
            || HttpMethods.IsPut(method)
            || HttpMethods.IsPatch(method)
            || HttpMethods.IsDelete(method);
    }

    private static bool IsReadApiRequest(HttpContext context)
    {
        return context.Request.Path.StartsWithSegments(
                "/api",
                StringComparison.OrdinalIgnoreCase
            )
            && (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method));
    }

    private static string ResolveClientKey(HttpContext context)
    {
        var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            return $"user:{userId}";
        }

        var ip = context.Connection.RemoteIpAddress?.ToString();
        return $"ip:{ip ?? "unknown"}";
    }
}
