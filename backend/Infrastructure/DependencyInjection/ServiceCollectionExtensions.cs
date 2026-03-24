using backend.Application.Abstractions;
using backend.Application.Abstractions.Auth;
using backend.Application.Abstractions.Repositories;
using backend.Application.Features.Auth;
using backend.Application.Features.GameControl;
using backend.Application.Features.Leaderboard;
using backend.Application.Features.Loadout;
using backend.Application.Features.Modifiers;
using backend.Data;
using backend.Infrastructure.Auth;
using backend.Infrastructure.InMemory;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDeadMansInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
        }

        services.AddSingleton<ILeaderboardRepository, InMemoryLeaderboardRepository>();
        services.AddSingleton<ILoadoutRepository, InMemoryLoadoutRepository>();
        services.AddSingleton<IModifiersRepository, InMemoryModifiersRepository>();
        services.AddSingleton<IGameControlRepository, InMemoryGameControlRepository>();

        services.AddScoped<ILeaderboardService, LeaderboardService>();
        services.AddScoped<ILoadoutService, LoadoutService>();
        services.AddScoped<IModifiersService, ModifiersService>();
        services.AddScoped<IGameControlService, GameControlService>();
        services.AddScoped<IAuthSessionService, AuthSessionService>();
        services.AddScoped<IAuthUserReader, DbAuthUserReader>();
        services.AddScoped<IUserRoleService, UserRoleService>();
        services.AddScoped<IClaimsTransformation, CurrentUserRoleClaimsTransformation>();
        services.AddHttpClient<ITwitchLoginService, TwitchLoginService>();
        services.AddHostedService<AuthPersistenceStartupValidator>();

        return services;
    }

    public static IServiceCollection AddDeadMansCors(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>()
            ?? ["http://localhost:5180", "https://localhost:5180"];

        services.AddCors(options =>
        {
            options.AddPolicy("Default", policy =>
            {
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }
}
