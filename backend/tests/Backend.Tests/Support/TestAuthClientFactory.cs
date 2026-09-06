using System.Security.Claims;
using System.Text.Encodings.Web;
using backend.Application.Abstractions.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Backend.Tests.Support;

public static class TestAuthClientFactory
{
    public static HttpClient CreateClient(
        WebApplicationFactory<Program> factory,
        IEnumerable<string> roles,
        Guid? userId = null,
        Action<IServiceCollection>? configureServices = null
    )
    {
        return CreateFactory(factory, roles, userId, configureServices).CreateClient();
    }

    public static WebApplicationFactory<Program> CreateFactory(
        WebApplicationFactory<Program> factory,
        IEnumerable<string> roles,
        Guid? userId = null,
        Action<IServiceCollection>? configureServices = null
    )
    {
        var roleCodes = roles.ToArray();
        return factory.WithWebHostBuilder(
            builder =>
                builder.ConfigureServices(
                    services =>
                    {
                        services.RemoveAll<IClaimsTransformation>();
                        services.AddSingleton<IClaimsTransformation, PassthroughClaimsTransformation>();
                        configureServices?.Invoke(services);
                        services
                            .AddAuthentication(options =>
                            {
                                options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                                options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                                options.DefaultScheme = TestAuthenticationHandler.SchemeName;
                            })
                            .AddScheme<TestAuthSchemeOptions, TestAuthenticationHandler>(
                                TestAuthenticationHandler.SchemeName,
                                options =>
                                {
                                    options.ClaimsIssuer = string.Join(',', roleCodes);
                                    options.UserId = userId;
                                }
                            );
                    }
                )
        );
    }
}

public sealed class TestAuthSchemeOptions : AuthenticationSchemeOptions
{
    public Guid? UserId { get; set; }
}

public sealed class TestAuthenticationHandler : AuthenticationHandler<TestAuthSchemeOptions>
{
    public const string SchemeName = "TestAuth";

    public TestAuthenticationHandler(
        IOptionsMonitor<TestAuthSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder
    )
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var roles = (Options.ClaimsIssuer ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var userId = Options.UserId ?? Guid.NewGuid();
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public sealed class PassthroughClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal) =>
        Task.FromResult(principal);
}
