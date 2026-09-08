using backend.Api.Configuration;
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

        return services;
    }
}
