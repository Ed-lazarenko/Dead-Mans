using backend.Application.Contracts;

namespace backend.Application.Abstractions.Realtime;

public interface IGameBoardEventsPublisher
{
    Task PublishCellOpenedAsync(GameCellOpenedEvent payload, CancellationToken cancellationToken = default);

    Task PublishModifierActivatedAsync(
        GameModifierActivatedEvent payload,
        CancellationToken cancellationToken = default
    );

    Task PublishModifierActivationCancelledAsync(
        GameModifierActivationCancelledEvent payload,
        CancellationToken cancellationToken = default
    );

    Task PublishModifierAvailabilityChangedAsync(
        GameModifierAvailabilityChangedEvent payload,
        CancellationToken cancellationToken = default
    );

    Task PublishRoundStateChangedAsync(
        GameRoundStateChangedEvent payload,
        CancellationToken cancellationToken = default
    );

    Task PublishQuizStateChangedAsync(
        GameQuizStateChangedEvent payload,
        CancellationToken cancellationToken = default
    );

    Task PublishUserNotificationCreatedAsync(
        GameUserNotificationCreatedEvent payload,
        CancellationToken cancellationToken = default
    );

    Task PublishGameLifecycleChangedAsync(
        GameLifecycleChangedEvent payload,
        CancellationToken cancellationToken = default
    );

    Task PublishModifierCatalogChangedAsync(
        ModifierCatalogChangedEvent payload,
        CancellationToken cancellationToken = default
    ) => Task.CompletedTask;
}
