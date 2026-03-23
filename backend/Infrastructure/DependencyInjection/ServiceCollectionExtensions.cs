using backend.Application.Abstractions;
using backend.Data;
using backend.Infrastructure.InMemory;
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

        services.AddSingleton<ILeaderboardService, InMemoryLeaderboardService>();
        services.AddSingleton<ILoadoutService, InMemoryLoadoutService>();
        services.AddSingleton<IModifiersService, InMemoryModifiersService>();
        services.AddSingleton<IGameControlService, InMemoryGameControlService>();

        return services;
    }

    public static IServiceCollection AddDeadMansCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("Default", policy =>
            {
                policy
                    .WithOrigins(
                        "http://localhost:5173",
                        "https://localhost:5173"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }
}
