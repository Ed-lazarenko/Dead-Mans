using backend.Application.Contracts;

namespace backend.Application.Abstractions.Realtime;

public interface IGameBoardEventsPublisher
{
    Task PublishCellOpenedAsync(GameCellOpenedEvent @event, CancellationToken cancellationToken = default);

    Task PublishModifierActivatedAsync(
        GameModifierActivatedEvent @event,
        CancellationToken cancellationToken = default
    );

    Task PublishModifierActivationCancelledAsync(
        GameModifierActivationCancelledEvent @event,
        CancellationToken cancellationToken = default
    );
}
