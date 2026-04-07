using backend.Application.Contracts;

namespace backend.Application.Abstractions.Realtime;

public interface IGameBoardEventsPublisher
{
    Task PublishCellOpenedAsync(GameCellOpenedEvent @event, CancellationToken cancellationToken = default);
}
