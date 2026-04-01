using backend.Api.Contracts;
using backend.Infrastructure.Auth;
using backend.Messaging;
using backend.Infrastructure.Configuration;
using backend.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
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
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
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

    app.UseForwardedHeaders();
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

public partial class Program;
