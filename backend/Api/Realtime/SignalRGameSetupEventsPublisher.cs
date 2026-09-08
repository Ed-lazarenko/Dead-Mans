using backend.Application.Abstractions.Realtime;
using backend.Api.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace backend.Api.Realtime;

public sealed class SignalRGameSetupEventsPublisher : IGameSetupEventsPublisher
{
    public const string DraftChangedEventName = RealtimeHubContracts.GameSetup.DraftChangedEvent;

    private readonly IHubContext<GameSetupHub> _hubContext;

    public SignalRGameSetupEventsPublisher(IHubContext<GameSetupHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PublishDraftChangedAsync(CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients
            .Group(RealtimeGroupNames.GameSetupAudience)
            .SendAsync(DraftChangedEventName, cancellationToken: cancellationToken);
    }
}
