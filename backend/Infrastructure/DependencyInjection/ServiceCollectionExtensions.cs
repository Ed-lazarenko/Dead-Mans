using backend.Application.Abstractions;
using backend.Application.Abstractions.Auth;
using backend.Application.Abstractions.Repositories;
using backend.Application.Features.Auth;
using backend.Application.Features.GameBoard;
using backend.Application.Features.GameControl;
using backend.Application.Features.Leaderboard;
using backend.Application.Features.Loadout;
using backend.Application.Features.Modifiers;
using backend.Data;
using backend.Infrastructure.Auth;
using backend.Infrastructure.Configuration;
using backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace backend.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDeadMansInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                static o => Uri.TryCreate(o.PublicBaseUrl, UriKind.Absolute, out _),
                $"{StorageOptions.SectionName}:{nameof(StorageOptions.PublicBaseUrl)} must be an absolute URL."
            )
            .ValidateOnStart();

        var connectionString = ResolveConnectionString(configuration);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            dataSourceBuilder.EnableDynamicJson();
            var dataSource = dataSourceBuilder.Build();
            services.AddSingleton(dataSource);
            services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(dataSource));
        }

        services.AddScoped<ILeaderboardRepository, UnavailableLeaderboardRepository>();
        services.AddScoped<ILoadoutRepository, DbLoadoutRepository>();
        services.AddScoped<IModifiersRepository, UnavailableModifiersRepository>();
        services.AddScoped<IGameControlRepository, UnavailableGameControlRepository>();
        services.AddScoped<IGameBoardRepository, DbGameBoardRepository>();

        services.AddScoped<ILeaderboardService, LeaderboardService>();
        services.AddScoped<ILoadoutService, LoadoutService>();
        services.AddScoped<IModifiersService, ModifiersService>();
        services.AddScoped<IGameControlService, GameControlService>();
        services.AddScoped<IGameBoardService, GameBoardService>();
        services.AddScoped<IAuthSessionService, AuthSessionService>();
        services.AddScoped<IAuthUserReader, DbAuthUserReader>();
        services.AddScoped<IUserRoleService, UserRoleService>();
        services.AddScoped<IClaimsTransformation, CurrentUserRoleClaimsTransformation>();
        services.AddHttpClient<ITwitchLoginService, TwitchLoginService>();
        services.AddHostedService<AuthPersistenceStartupValidator>();

        return services;
    }

    private static string? ResolveConnectionString(IConfiguration configuration)
    {
        var databaseUrl = configuration[ConfigurationKeys.DatabaseUrlEnvironmentVariable];
        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            return BuildConnectionStringFromDatabaseUrl(databaseUrl);
        }

        return configuration.GetConnectionString(ConnectionStringNames.Default);
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

    public static IServiceCollection AddDeadMansCors(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddOptions<CorsOptions>()
            .Bind(configuration.GetSection(CorsOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<
            IConfigureOptions<Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions>,
            ConfigureDeadMansCorsPolicy
        >();

        services.AddCors();

        return services;
    }
}
