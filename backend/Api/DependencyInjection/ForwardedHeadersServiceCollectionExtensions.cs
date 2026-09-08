using System.Net;
using backend.Api.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using ForwardedIpNetwork = System.Net.IPNetwork;

namespace backend.Api.DependencyInjection;

public static class ForwardedHeadersServiceCollectionExtensions
{
    public static IServiceCollection AddDeadMansForwardedHeaders(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment
    )
    {
        services
            .AddOptions<ForwardedHeadersSecurityOptions>()
            .Bind(configuration.GetSection(ForwardedHeadersSecurityOptions.SectionName))
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
                    || environment.IsDevelopment()
                    || environment.IsEnvironment("Testing")
                    || options.TrustedProxies.Length > 0
                    || options.TrustedNetworks.Length > 0,
                "ForwardedHeaders requires at least one trusted proxy or network outside Development and Testing."
            )
            .ValidateOnStart();

        var securityOptions =
            configuration
                .GetSection(ForwardedHeadersSecurityOptions.SectionName)
                .Get<ForwardedHeadersSecurityOptions>() ?? new ForwardedHeadersSecurityOptions();
        services.Configure<ForwardedHeadersOptions>(options =>
            ConfigureForwardedHeaders(options, securityOptions, environment.IsDevelopment())
        );

        return services;
    }

    private static void ConfigureForwardedHeaders(
        ForwardedHeadersOptions options,
        ForwardedHeadersSecurityOptions securityOptions,
        bool isDevelopment
    )
    {
        if (!securityOptions.Enabled)
        {
            return;
        }

        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        if (isDevelopment && securityOptions.TrustAllProxiesInDevelopment)
        {
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            return;
        }

        if (
            securityOptions.TrustedProxies.Length == 0
            && securityOptions.TrustedNetworks.Length == 0
        )
        {
            return;
        }

        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();

        foreach (var trustedProxy in securityOptions.TrustedProxies)
        {
            options.KnownProxies.Add(IPAddress.Parse(trustedProxy));
        }

        foreach (var trustedNetwork in securityOptions.TrustedNetworks)
        {
            _ = TryParseCidrNetwork(trustedNetwork, out var network);
            options.KnownIPNetworks.Add(network);
        }
    }

    private static bool TryParseCidrNetwork(string cidr, out ForwardedIpNetwork network)
    {
        network = default!;
        var parts = cidr.Split('/', 2, StringSplitOptions.TrimEntries);
        if (
            parts.Length != 2
            || !IPAddress.TryParse(parts[0], out var address)
            || !int.TryParse(parts[1], out var prefixLength)
        )
        {
            return false;
        }

        var maxPrefixLength = address.AddressFamily
            == System.Net.Sockets.AddressFamily.InterNetwork
            ? 32
            : 128;
        if (prefixLength < 0 || prefixLength > maxPrefixLength)
        {
            return false;
        }

        network = new ForwardedIpNetwork(address, prefixLength);
        return true;
    }
}
