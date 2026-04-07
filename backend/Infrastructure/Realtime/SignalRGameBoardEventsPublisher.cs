using backend.Application.Abstractions.Realtime;
using backend.Application.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace backend.Infrastructure.Realtime;

public sealed class SignalRGameBoardEventsPublisher : IGameBoardEventsPublisher
{
    public const string CellOpenedEventName = "cellOpened";

    private readonly IHubContext<GameBoardHub> _hubContext;

    public SignalRGameBoardEventsPublisher(IHubContext<GameBoardHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PublishCellOpenedAsync(GameCellOpenedEvent @event, CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients.All.SendAsync(CellOpenedEventName, @event, cancellationToken);
    }
}
