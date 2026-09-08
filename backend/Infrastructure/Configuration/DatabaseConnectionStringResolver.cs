using Npgsql;

namespace backend.Infrastructure.Configuration;

internal static class DatabaseConnectionStringResolver
{
    public static string? ResolveAndValidate(
        IConfiguration configuration,
        IHostEnvironment environment
    )
    {
        var connectionString = Resolve(configuration);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            if (environment.IsEnvironment("Testing"))
            {
                return null;
            }

            throw new InvalidOperationException(
                $"Database connection is required. Configure ConnectionStrings:{ConnectionStringNames.Default} or {ConfigurationKeys.DatabaseUrlEnvironmentVariable}."
            );
        }

        return Validate(connectionString, environment);
    }

    public static string? Resolve(IConfiguration configuration)
    {
        var databaseUrl = configuration[ConfigurationKeys.DatabaseUrlEnvironmentVariable];
        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            return BuildConnectionStringFromDatabaseUrl(databaseUrl);
        }

        return configuration.GetConnectionString(ConnectionStringNames.Default);
    }

    public static string Validate(string connectionString, IHostEnvironment environment)
    {
        NpgsqlConnectionStringBuilder builder;
        try
        {
            builder = new NpgsqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException(
                "Database connection configuration is invalid.",
                exception
            );
        }

        ValidateRequiredFields(builder);
        if (environment.IsProduction())
        {
            ValidateProductionTransport(builder);
        }

        return builder.ConnectionString;
    }

    private static void ValidateRequiredFields(NpgsqlConnectionStringBuilder builder)
    {
        if (
            string.IsNullOrWhiteSpace(builder.Host)
            || string.IsNullOrWhiteSpace(builder.Database)
            || string.IsNullOrWhiteSpace(builder.Username)
        )
        {
            throw new InvalidOperationException(
                "Database connection must specify host, database, and username."
            );
        }
    }

    private static void ValidateProductionTransport(NpgsqlConnectionStringBuilder builder)
    {
        if (builder.SslMode != SslMode.VerifyFull)
        {
            throw new InvalidOperationException(
                "Production database connections must use SSL Mode=VerifyFull to encrypt traffic and verify the server certificate and host name."
            );
        }
    }

    private static string BuildConnectionStringFromDatabaseUrl(string databaseUrl)
    {
        if (
            !Uri.TryCreate(databaseUrl, UriKind.Absolute, out var databaseUri)
            || (databaseUri.Scheme != "postgres" && databaseUri.Scheme != "postgresql")
        )
        {
            return databaseUrl;
        }

        var credentials = databaseUri.UserInfo.Split(':', 2, StringSplitOptions.TrimEntries);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = databaseUri.Host,
            Port = databaseUri.IsDefaultPort ? 5432 : databaseUri.Port,
            Database = databaseUri.AbsolutePath.Trim('/'),
            Username = credentials.Length > 0 ? Uri.UnescapeDataString(credentials[0]) : string.Empty,
            Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : string.Empty
        };

        ApplyDatabaseUrlQueryParameters(builder, databaseUri.Query);
        return builder.ConnectionString;
    }

    private static void ApplyDatabaseUrlQueryParameters(
        NpgsqlConnectionStringBuilder builder,
        string queryString
    )
    {
        var trimmedQuery = queryString.TrimStart('?');
        if (string.IsNullOrWhiteSpace(trimmedQuery))
        {
            return;
        }

        foreach (var pair in trimmedQuery.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;

            switch (key.ToLowerInvariant())
            {
                case "sslmode":
                case "ssl mode":
                    if (Enum.TryParse<SslMode>(value, true, out var sslMode))
                    {
                        builder.SslMode = sslMode;
                    }
                    break;
                case "pooling":
                    if (bool.TryParse(value, out var pooling))
                    {
                        builder.Pooling = pooling;
                    }
                    break;
                case "maximum pool size":
                case "max pool size":
                    if (int.TryParse(value, out var maxPoolSize))
                    {
                        builder.MaxPoolSize = maxPoolSize;
                    }
                    break;
                case "minimum pool size":
                case "min pool size":
                    if (int.TryParse(value, out var minPoolSize))
                    {
                        builder.MinPoolSize = minPoolSize;
                    }
                    break;
            }
        }
    }
}
