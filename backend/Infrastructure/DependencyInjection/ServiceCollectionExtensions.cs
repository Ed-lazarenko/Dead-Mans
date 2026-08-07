using backend.Application.Abstractions;
using backend.Application.Abstractions.Auth;
using backend.Application.Abstractions.Realtime;
using backend.Application.Abstractions.Repositories;
using backend.Application.Features.Auth;
using backend.Application.Features.GameBoard;
using backend.Application.Features.GameRounds;
using backend.Application.Features.GameHistory;
using backend.Application.Features.GameLifecycle;
using backend.Application.Features.GameModifiers;
using backend.Application.Features.GameNotifications;
using backend.Application.Features.GameQuestions;
using backend.Application.Features.GameRegistration;
using backend.Application.Features.GameSetup;
using backend.Api.Contracts;
using backend.Data;
using backend.Infrastructure.Auth;
using backend.Infrastructure.Configuration;
using backend.Application.Configuration;
using backend.Infrastructure.Persistence;
using backend.Infrastructure.Realtime;
using backend.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace backend.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDeadMansInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment
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
        services
            .AddOptions<MediaStorageSettings>()
            .Bind(configuration.GetSection(MediaStorageSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        if (environment.IsEnvironment("Testing"))
        {
            services.AddSingleton<IObjectStorage, InMemoryObjectStorage>();
        }
        else
        {
            services.AddSingleton<IObjectStorage, S3ObjectStorage>();
        }

        var connectionString = ResolveConnectionString(configuration);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            dataSourceBuilder.EnableDynamicJson();
            var dataSource = dataSourceBuilder.Build();
            services.AddSingleton(dataSource);
            services.AddDbContext<ApplicationDbContext>(
                options =>
                    options.UseNpgsql(
                        dataSource,
                        npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history")
                    )
                    .ReplaceService<IHistoryRepository, SnakeCaseNpgsqlHistoryRepository>()
            );
        }

        services.AddScoped<IGameBoardRepository, DbGameBoardRepository>();
        services.AddScoped<IGameBoardService, GameBoardService>();
        services.AddScoped<IGameRoundRepository, DbGameRoundRepository>();
        services.AddScoped<IGameRoundService, GameRoundService>();
        services.AddScoped<IGameHistoryRepository, DbGameHistoryRepository>();
        services.AddScoped<IGameHistoryService, GameHistoryService>();
        services.AddScoped<IGameSetupRepository, DbGameSetupRepository>();
        services.AddScoped<IGameSetupService, GameSetupService>();
        services.AddScoped<IGameSetupCellMediaRepository, DbGameSetupCellMediaRepository>();
        services.AddScoped<IGameSetupCellMediaService, GameSetupCellMediaService>();
        services.AddScoped<IGameModifierRepository, DbGameModifierRepository>();
        services.AddScoped<IGameModifierService, GameModifierService>();
        services.AddScoped<IGameNotificationRepository, DbGameNotificationRepository>();
        services.AddScoped<IGameNotificationService, GameNotificationService>();
        services.AddScoped<IGameQuestionRepository, DbGameQuestionRepository>();
        services.AddScoped<IGameQuizRepository, DbGameQuizRepository>();
        services.AddScoped<IGameQuestionService, GameQuestionService>();
        services.AddScoped<IGameQuizService, GameQuizService>();
        services.AddScoped<IGameRegistrationReadStore, GameRegistrationReadStore>();
        services.AddScoped<IGameRegistrationPersistence, DbGameRegistrationPersistence>();
        services.AddScoped<IGameRegistrationService, GameRegistrationService>();
        services.AddScoped<IGameLifecycleReadStore, GameLifecycleReadStore>();
        services.AddScoped<IGameLifecyclePersistence, DbGameLifecyclePersistence>();
        services.AddScoped<IGameLifecycleService, GameLifecycleService>();
        services.AddScoped<IAuthSessionService, AuthSessionService>();
        services.AddScoped<ITwitchAuthFlowService, TwitchAuthFlowService>();
        if (environment.IsEnvironment("Testing"))
        {
            services.AddScoped<DbAuthUserReader>();
            services.AddScoped<IAuthUserReader, TestingAuthUserReader>();
        }
        else
        {
            services.AddScoped<IAuthUserReader, DbAuthUserReader>();
        }
        services.AddScoped<IUserRoleService, UserRoleService>();
        services.AddScoped<IClaimsTransformation, CurrentUserRoleClaimsTransformation>();
        services.AddHttpClient<ITwitchLoginService, TwitchLoginService>();
        services.AddSingleton<IGameBoardEventsPublisher, SignalRGameBoardEventsPublisher>();
        services.AddSingleton<IGameSetupEventsPublisher, SignalRGameSetupEventsPublisher>();
        services
            .AddSignalR()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                );
            });
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

    public static IServiceCollection AddDeadMansHealthChecks(this IServiceCollection services)
    {
        services
            .AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>(
                name: HealthCheckContracts.Names.Database,
                tags: new[] { HealthCheckContracts.Tags.Ready }
            );

        return services;
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
            .Validate(
                static options => options.GetNormalizedAllowedOrigins().Length > 0,
                $"{CorsOptions.SectionName}:{nameof(CorsOptions.AllowedOrigins)} must contain at least one non-empty origin."
            )
            .Validate(
                static options => options.AllowedOrigins.All(CorsOptions.IsValidAllowedOrigin),
                $"{CorsOptions.SectionName}:{nameof(CorsOptions.AllowedOrigins)} must contain absolute http/https origins without paths, query strings, fragments, or user info."
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
