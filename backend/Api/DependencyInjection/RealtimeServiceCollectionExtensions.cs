using System.Text.Json;
using System.Text.Json.Serialization;
using backend.Api.Realtime;
using backend.Application.Abstractions.Realtime;

namespace backend.Api.DependencyInjection;

public static class RealtimeServiceCollectionExtensions
{
    public static IServiceCollection AddDeadMansRealtime(this IServiceCollection services)
    {
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

        return services;
    }
}
