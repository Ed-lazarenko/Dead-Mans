using System.Net;

namespace backend.Api.Configuration;

internal static class AllowedHostsValidator
{
    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsProduction())
        {
            return;
        }

        var hosts = (configuration["AllowedHosts"] ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (hosts.Length == 0)
        {
            throw new InvalidOperationException(
                "AllowedHosts must contain at least one explicit public host in Production."
            );
        }

        foreach (var host in hosts)
        {
            if (host.Contains('*', StringComparison.Ordinal) || host.Contains('+', StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "AllowedHosts must not contain wildcard entries in Production."
                );
            }

            var unwrappedHost = host.Trim('[', ']');
            if (Uri.CheckHostName(unwrappedHost) == UriHostNameType.Unknown)
            {
                throw new InvalidOperationException(
                    "AllowedHosts contains an invalid host name in Production."
                );
            }

            if (
                string.Equals(unwrappedHost, "localhost", StringComparison.OrdinalIgnoreCase)
                || (IPAddress.TryParse(unwrappedHost, out var address) && IPAddress.IsLoopback(address))
            )
            {
                throw new InvalidOperationException(
                    "AllowedHosts must not contain localhost or loopback addresses in Production."
                );
            }
        }
    }
}
