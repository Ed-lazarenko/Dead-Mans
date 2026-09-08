using backend.Application.Abstractions;
using backend.Application.Abstractions.Auth;
using backend.Application.Abstractions.Repositories;
using backend.Data;
using backend.Infrastructure.Auth;
using backend.Infrastructure.Configuration;
using backend.Application.Configuration;
using backend.Infrastructure.Persistence;
using backend.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Amazon.S3;
using NpgsqlTypes;

namespace backend.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDeadMansInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment
    )
    {
        var usesInMemoryStorage = environment.IsEnvironment("Testing");
        var requiresHttpsExternalUrls = !environment.IsDevelopment() && !usesInMemoryStorage;

        services
            .AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                static o => CorsOptions.IsValidAllowedOrigin(o.PublicBaseUrl),
                $"{StorageOptions.SectionName}:{nameof(StorageOptions.PublicBaseUrl)} must be an absolute http/https origin without user info, query, or fragment."
            )
            .Validate(
                o => !requiresHttpsExternalUrls || CorsOptions.IsHttpsOrigin(o.PublicBaseUrl),
                $"{StorageOptions.SectionName}:{nameof(StorageOptions.PublicBaseUrl)} must use HTTPS outside Development and Testing."
            )
            .Validate(
                static o =>
                    string.IsNullOrWhiteSpace(o.ServiceUrl)
                    || CorsOptions.IsValidAllowedOrigin(o.ServiceUrl),
                $"{StorageOptions.SectionName}:{nameof(StorageOptions.ServiceUrl)} must be an absolute http/https origin when configured."
            )
            .Validate(
                o =>
                    !requiresHttpsExternalUrls
                    || string.IsNullOrWhiteSpace(o.ServiceUrl)
                    || CorsOptions.IsHttpsOrigin(o.ServiceUrl),
                $"{StorageOptions.SectionName}:{nameof(StorageOptions.ServiceUrl)} must use HTTPS outside Development and Testing."
            )
            .Validate(
                static o => StorageOptions.IsValidBucketName(o.BucketName),
                $"{StorageOptions.SectionName}:{nameof(StorageOptions.BucketName)} must be a valid lowercase S3 bucket name."
            )
            .Validate(
                o => usesInMemoryStorage || o.HasCompleteCredentials(),
                $"{StorageOptions.SectionName} access and secret keys are required outside Testing."
            )
            .ValidateOnStart();
        services
            .AddOptions<MediaStorageSettings>()
            .Bind(configuration.GetSection(MediaStorageSettings.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                static o => MediaStorageSettings.IsValidObjectKeyPrefix(o.GamesPrefix),
                $"{MediaStorageSettings.SectionName}:{nameof(MediaStorageSettings.GamesPrefix)} must be a safe object-key prefix."
            )
            .Validate(
                static o => MediaStorageSettings.IsValidObjectKeyPrefix(o.CardsGroup),
                $"{MediaStorageSettings.SectionName}:{nameof(MediaStorageSettings.CardsGroup)} must be a safe object-key prefix."
            )
            .ValidateOnStart();

        if (usesInMemoryStorage)
        {
            services.AddSingleton<IObjectStorage, InMemoryObjectStorage>();
        }
        else
        {
            services.AddSingleton<IAmazonS3>(serviceProvider =>
                S3ObjectStorage.CreateClient(
                    serviceProvider.GetRequiredService<IOptions<StorageOptions>>().Value
                )
            );
            services.AddSingleton<IObjectStorage, S3ObjectStorage>();
        }

        var connectionString = DatabaseConnectionStringResolver.Resolve(configuration);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = DatabaseConnectionStringResolver.Validate(
                connectionString,
                environment
            );
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

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IGameBoardRepository, DbGameBoardRepository>();
        services.AddScoped<IGameRoundRepository, DbGameRoundRepository>();
        services.AddScoped<IGameHistoryRepository, DbGameHistoryRepository>();
        services.AddScoped<IGameSetupRepository, DbGameSetupRepository>();
        services.AddScoped<IGameSetupCellMediaRepository, DbGameSetupCellMediaRepository>();
        services.AddScoped<IGameModifierRepository, DbGameModifierRepository>();
        services.AddScoped<IGameNotificationRepository, DbGameNotificationRepository>();
        services.AddScoped<IGameQuestionRepository, DbGameQuestionRepository>();
        services.AddScoped<IGameQuizRepository, DbGameQuizRepository>();
        services.AddScoped<IGameRegistrationReadStore, GameRegistrationReadStore>();
        services.AddScoped<IGameRegistrationPersistence, DbGameRegistrationPersistence>();
        services.AddScoped<IGameLifecycleReadStore, GameLifecycleReadStore>();
        services.AddScoped<IGameLifecyclePersistence, DbGameLifecyclePersistence>();
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
        services.AddHttpClient<ITwitchLoginService, TwitchLoginService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
            client.MaxResponseContentBufferSize = 1024 * 1024;
        });
        services.AddHostedService<DatabaseConfigurationStartupValidator>();
        services.AddHostedService<AuthPersistenceStartupValidator>();

        return services;
    }

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
