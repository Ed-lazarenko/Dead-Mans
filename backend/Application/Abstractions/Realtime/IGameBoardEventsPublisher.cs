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

    Task PublishModifierAvailabilityChangedAsync(
        GameModifierAvailabilityChangedEvent @event,
        CancellationToken cancellationToken = default
    );

    Task PublishRoundStateChangedAsync(
        GameRoundStateChangedEvent @event,
        CancellationToken cancellationToken = default
    );

    Task PublishQuizStateChangedAsync(
        GameQuizStateChangedEvent @event,
        CancellationToken cancellationToken = default
    );

    Task PublishUserNotificationCreatedAsync(
        GameUserNotificationCreatedEvent @event,
        CancellationToken cancellationToken = default
    );
}
