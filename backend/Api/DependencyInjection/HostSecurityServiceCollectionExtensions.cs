using backend.Api.Configuration;
using backend.Application.Abstractions.Auth;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Options;

namespace backend.Api.DependencyInjection;

public static class HostSecurityServiceCollectionExtensions
{
    private const string DataProtectionApplicationName = "DeadMans";

    public static IServiceCollection AddDeadMansHostSecurity(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment
    )
    {
        var requiresPersistentKeys = environment.IsProduction();
        services
            .AddOptions<DataProtectionSecurityOptions>()
            .Bind(configuration.GetSection(DataProtectionSecurityOptions.SectionName))
            .Validate(
                options =>
                    DataProtectionSecurityOptions.IsValidKeysDirectory(
                        options.KeysDirectory,
                        requiresPersistentKeys
                    ),
                "DataProtection:KeysDirectory must be an absolute, non-root directory and is required in Production."
            )
            .ValidateOnStart();
        services
            .AddDataProtection()
            .SetApplicationName(DataProtectionApplicationName);
        services.AddSingleton<
            IConfigureOptions<KeyManagementOptions>,
            ConfigureDataProtectionKeyManagementOptions
        >();
        services.AddHostedService<ProductionHostConfigurationStartupValidator>();
        var requiresHttpsExternalUrls = !environment.IsDevelopment()
            && !environment.IsEnvironment("Testing");
        services
            .AddOptions<TwitchAuthOptions>()
            .Bind(configuration.GetSection(TwitchAuthOptions.SectionName))
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

        return services;
    }
}
