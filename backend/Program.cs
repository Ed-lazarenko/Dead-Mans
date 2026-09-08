using backend.Api.Auth;
using backend.Api.Contracts;
using backend.Api.DependencyInjection;
using backend.Api.Http;
using backend.Api.Realtime;
using backend.Application.Abstractions.Auth;
using backend.Data;
using backend.Messaging;
using backend.Infrastructure.Configuration;
using backend.Infrastructure.DependencyInjection;
using backend.Infrastructure.Health;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Server.Kestrel.Core;

try
{
    var builder = WebApplication.CreateBuilder(args);
    var isDevelopment = builder.Environment.IsDevelopment();
    var isTesting = builder.Environment.IsEnvironment("Testing");
    var requiresHttpsExternalUrls = !isDevelopment && !isTesting;
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.AddServerHeader = false;
        options.Limits.MaxRequestBodySize = 6 * 1024 * 1024;
    });
    builder.Services.AddHsts(options =>
    {
        options.MaxAge = TimeSpan.FromDays(180);
    });
    builder.Host.UseSerilog(
        (context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext();
        }
    );
    if (isDevelopment)
    {
        builder.Configuration.AddJsonFile(
            "appsettings.Local.json",
            optional: true,
            reloadOnChange: true
        );
    }

    builder.Configuration.AddEnvironmentVariables();
    builder.Services.AddDeadMansHostSecurity(builder.Configuration, builder.Environment);
    builder.Services
        .AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            );
        });
    builder.Services
        .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.Cookie.Name = AuthCookieNames.Authentication;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = isDevelopment
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
            options.ExpireTimeSpan = TimeSpan.FromDays(7);
            options.SlidingExpiration = true;
            options.Events = new CookieAuthenticationEvents
            {
                OnRedirectToLogin = async context =>
                {
                    if (IsApplicationEndpoint(context.Request.Path))
                    {
                        await WriteErrorResponseAsync(
                            context.Response,
                            StatusCodes.Status401Unauthorized,
                            AppMessages.Client.AuthenticationRequired
                        );
                        return;
                    }

                    context.Response.Redirect(context.RedirectUri);
                },
                OnRedirectToAccessDenied = async context =>
                {
                    if (IsApplicationEndpoint(context.Request.Path))
                    {
                        await WriteErrorResponseAsync(
                            context.Response,
                            StatusCodes.Status403Forbidden,
                            AppMessages.Client.AccessDenied
                        );
                        return;
                    }

                    context.Response.Redirect(context.RedirectUri);
                }
            };
        });
    builder.Services.AddAuthorization();
    builder.Services.AddDeadMansInfrastructure(builder.Configuration, builder.Environment);
    builder.Services.AddDeadMansRealtime();
    var healthChecks = builder.Services
        .AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>(
            name: HealthCheckContracts.Names.Database,
            tags: [HealthCheckContracts.Tags.Ready]
        );
    if (!isTesting)
    {
        healthChecks.AddCheck<ObjectStorageHealthCheck>(
            name: HealthCheckContracts.Names.ObjectStorage,
            tags: [HealthCheckContracts.Tags.Ready]
        );
    }
    builder.Services.AddDeadMansRateLimiting(builder.Configuration, builder.Environment);
    builder.Services.AddDeadMansCors(builder.Configuration, builder.Environment);
    builder.Services
        .AddOptions<ForwardedHeadersSecurityOptions>()
        .Bind(builder.Configuration.GetSection(ForwardedHeadersSecurityOptions.SectionName))
        .ValidateDataAnnotations()
        .Validate(
            options =>
                !options.Enabled
                || options.TrustedProxies.All(proxy => IPAddress.TryParse(proxy, out _)),
            "ForwardedHeaders:TrustedProxies must contain valid IP addresses."
        )
        .Validate(
            options =>
                !options.Enabled
                || options.TrustedNetworks.All(network => TryParseCidrNetwork(network, out _)),
            "ForwardedHeaders:TrustedNetworks must contain valid CIDR values."
        )
        .Validate(
            options =>
                !options.Enabled
                || isDevelopment
                || isTesting
                || options.TrustedProxies.Length > 0
                || options.TrustedNetworks.Length > 0,
            "ForwardedHeaders requires at least one trusted proxy or network outside Development and Testing."
        )
        .ValidateOnStart();
    var forwardedHeadersSecurityOptions = builder.Configuration
        .GetSection(ForwardedHeadersSecurityOptions.SectionName)
        .Get<ForwardedHeadersSecurityOptions>()
        ?? new ForwardedHeadersSecurityOptions();
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        if (!forwardedHeadersSecurityOptions.Enabled)
        {
            return;
        }

        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        if (isDevelopment && forwardedHeadersSecurityOptions.TrustAllProxiesInDevelopment)
        {
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
            return;
        }

        if (
            forwardedHeadersSecurityOptions.TrustedProxies.Length == 0
            && forwardedHeadersSecurityOptions.TrustedNetworks.Length == 0
        )
        {
            return;
        }

        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();

        foreach (var trustedProxy in forwardedHeadersSecurityOptions.TrustedProxies)
        {
            if (!IPAddress.TryParse(trustedProxy, out var parsedIp))
            {
                throw new InvalidOperationException(
                    $"ForwardedHeaders:TrustedProxies contains invalid IP address '{trustedProxy}'."
                );
            }

            options.KnownProxies.Add(parsedIp);
        }

        foreach (var trustedNetwork in forwardedHeadersSecurityOptions.TrustedNetworks)
        {
            if (!TryParseCidrNetwork(trustedNetwork, out var network))
            {
                throw new InvalidOperationException(
                    $"ForwardedHeaders:TrustedNetworks contains invalid CIDR '{trustedNetwork}'."
                );
            }

            options.KnownNetworks.Add(network);
        }
    });
    builder.Services
        .AddOptions<TwitchAuthOptions>()
        .Bind(builder.Configuration.GetSection(TwitchAuthOptions.SectionName))
        .ValidateDataAnnotations()
        .Validate(
            options => TwitchAuthOptions.HasValidScopes(options.Scopes),
            "TwitchAuth:Scopes must contain unique, non-empty scopes."
        )
        .Validate(
            options =>
                TwitchAuthOptions.IsValidRedirectUri(
                    options.RedirectUri,
                    requiresHttpsExternalUrls
                ),
            "TwitchAuth:RedirectUri must be an absolute http/https URL without user info or fragment and must use HTTPS outside Development and Testing."
        )
        .Validate(
            options =>
                TwitchAuthOptions.IsValidRedirectUri(
                    options.FrontendRedirectUri,
                    requiresHttpsExternalUrls
                ),
            "TwitchAuth:FrontendRedirectUri must be an absolute http/https URL without user info or fragment and must use HTTPS outside Development and Testing."
        )
        .ValidateOnStart();

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseMiddleware<ApiExceptionHandlingMiddleware>();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    if (app.Environment.IsDevelopment())
    {
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/openapi/deadmans.v1.yaml", "Dead-Mans API v1");
            c.RoutePrefix = "swagger";
        });
    }

    if (forwardedHeadersSecurityOptions.Enabled)
    {
        app.UseForwardedHeaders();
    }
    app.UseCors(CorsPolicyNames.Default);
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    app.UseAuthentication();
    app.UseMiddleware<ActiveUserMiddleware>();
    app.UseMiddleware<ApiClientRequestValidationMiddleware>();
    app.UseAuthorization();
    app.UseRateLimiter();

    app.MapGet(
        "/openapi/deadmans.v1.yaml",
        () => Results.File(
            Path.Combine(app.Environment.ContentRootPath, "openapi", "deadmans.v1.yaml"),
            "application/yaml"
        )
    );
    app.MapHealthChecks(
        HealthCheckContracts.LivenessPath,
        new HealthCheckOptions { Predicate = _ => false }
    );
    app.MapHealthChecks(
        HealthCheckContracts.ReadinessPath,
        new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(HealthCheckContracts.Tags.Ready)
        }
    );
    app.MapHub<GameBoardHub>(RealtimeHubContracts.GameBoard.HubPath);
    app.MapHub<GameSetupHub>(RealtimeHubContracts.GameSetup.HubPath);
    app.MapControllers();

    app.Run();
}
catch (Microsoft.Extensions.Hosting.HostAbortedException)
{
    throw;
}
catch (Exception ex)
{
    if (Log.Logger != null)
    {
        Log.Fatal(ex, AppMessages.Logs.ApplicationTerminatedUnexpectedly);
    }
    else
    {
        Console.Error.WriteLine(ex);
    }

    throw;
}
finally
{
    Log.CloseAndFlush();
}

static Task WriteErrorResponseAsync(HttpResponse response, int statusCode, string message)
{
    ApiErrorMetrics.Record(statusCode, null, "auth");
    return ErrorResponseFactory.WriteAsync(response, statusCode, message);
}

static bool IsApplicationEndpoint(PathString path)
{
    return path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/auth", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase);
}

static bool TryParseCidrNetwork(
    string cidr,
    out Microsoft.AspNetCore.HttpOverrides.IPNetwork network
)
{
    network = default!;
    var parts = cidr.Split('/', 2, StringSplitOptions.TrimEntries);
    if (parts.Length != 2)
    {
        return false;
    }

    if (!IPAddress.TryParse(parts[0], out var address))
    {
        return false;
    }

    if (!int.TryParse(parts[1], out var prefixLength))
    {
        return false;
    }

    var maxPrefixLength = address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128;
    if (prefixLength < 0 || prefixLength > maxPrefixLength)
    {
        return false;
    }

    network = new Microsoft.AspNetCore.HttpOverrides.IPNetwork(address, prefixLength);
    return true;
}

public partial class Program;
