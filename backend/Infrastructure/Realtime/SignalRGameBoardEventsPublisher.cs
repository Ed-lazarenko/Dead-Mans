using backend.Api.Contracts;
using backend.Api.Mapping;
using backend.Application.Abstractions.Realtime;
using backend.Application.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace backend.Infrastructure.Realtime;

public sealed class SignalRGameBoardEventsPublisher : IGameBoardEventsPublisher
{
    public const string CellOpenedEventName = RealtimeHubContracts.GameBoard.CellOpenedEvent;
    public const string ModifierActivatedEventName = RealtimeHubContracts.GameBoard.ModifierActivatedEvent;

    private readonly IHubContext<GameBoardHub> _hubContext;

    public SignalRGameBoardEventsPublisher(IHubContext<GameBoardHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PublishCellOpenedAsync(GameCellOpenedEvent @event, CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients
            .Group(RealtimeGroupNames.GameBoardAudience)
            .SendAsync(CellOpenedEventName, @event.ToDto(), cancellationToken);
    }

    public Task PublishModifierActivatedAsync(
        GameModifierActivatedEvent @event,
        CancellationToken cancellationToken = default
    )
    {
        return _hubContext.Clients.Group(RealtimeGroupNames.GameBoardAudience).SendAsync(
            ModifierActivatedEventName,
            @event.ToDto(),
            cancellationToken
        );
    }
}
