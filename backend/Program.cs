using backend.Api.Contracts;
using backend.Infrastructure.Auth;
using backend.Messaging;
using backend.Infrastructure.Configuration;
using backend.Infrastructure.DependencyInjection;
using backend.Infrastructure.Realtime;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

try
{
    var builder = WebApplication.CreateBuilder(args);
    var isDevelopment = builder.Environment.IsDevelopment();
    builder.Host.UseSerilog(
        (context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext();
        }
    );
    builder.Configuration
        .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables();

    // Add services to the container.
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
            options.SlidingExpiration = true;
            options.Events = new CookieAuthenticationEvents
            {
                OnRedirectToLogin = async context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api")
                        || context.Request.Path.StartsWithSegments("/auth"))
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
                    if (context.Request.Path.StartsWithSegments("/api")
                        || context.Request.Path.StartsWithSegments("/auth"))
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
    builder.Services.AddDeadMansInfrastructure(builder.Configuration);
    builder.Services.AddDeadMansCors(builder.Configuration);
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
            options => options.Scopes.Length > 0,
            "TwitchAuth:Scopes must contain at least one scope."
        )
        .ValidateOnStart();

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    // Configure the HTTP request pipeline.
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
    // CORS before HTTPS redirect so 307 responses include Access-Control-Allow-Origin for the SPA.
    app.UseCors(CorsPolicyNames.Default);
    // In Development, skip HTTP→HTTPS redirect: the SPA is usually on http://localhost:5180 while the
    // API is on http://localhost:5285. Redirecting to https://localhost:7007 makes the request
    // cross-site (different scheme), so SameSite=Lax auth cookies are not sent with fetch → 401.
    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapGet(
        "/openapi/deadmans.v1.yaml",
        () => Results.File(
            Path.Combine(app.Environment.ContentRootPath, "openapi", "deadmans.v1.yaml"),
            "application/yaml"
        )
    );
    app.MapHub<GameBoardHub>("/hubs/game-board");
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
    response.StatusCode = statusCode;
    return response.WriteAsJsonAsync(new ErrorResponse(message));
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
