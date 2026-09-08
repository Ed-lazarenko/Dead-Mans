using backend.Api.Configuration;
using Microsoft.Extensions.Options;

namespace backend.Api.DependencyInjection;

public static class CorsServiceCollectionExtensions
{
    public static IServiceCollection AddDeadMansCors(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment
    )
    {
        var requiresHttpsOrigins =
            !environment.IsDevelopment() && !environment.IsEnvironment("Testing");
        services
            .AddOptions<CorsOptions>()
            .Bind(configuration.GetSection(CorsOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                static options => options.GetNormalizedAllowedOrigins().Length > 0,
                $"{CorsOptions.SectionName}:{nameof(CorsOptions.AllowedOrigins)} must contain at least one non-empty origin."
            )
            .Validate(
                static options => options.AllowedOrigins.All(CorsOptions.IsValidAllowedOrigin),
                $"{CorsOptions.SectionName}:{nameof(CorsOptions.AllowedOrigins)} must contain absolute http/https origins without paths, query strings, fragments, or user info."
            )
            .Validate(
                options =>
                    !requiresHttpsOrigins
                    || options.AllowedOrigins.All(CorsOptions.IsHttpsOrigin),
                $"{CorsOptions.SectionName}:{nameof(CorsOptions.AllowedOrigins)} must use HTTPS outside Development and Testing."
            )
            .ValidateOnStart();

        services.AddSingleton<
            IConfigureOptions<Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions>,
            ConfigureDeadMansCorsPolicy
        >();
        services.AddCors();

        return services;
    }
}
