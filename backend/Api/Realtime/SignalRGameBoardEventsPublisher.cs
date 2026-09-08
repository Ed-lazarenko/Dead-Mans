using backend.Api.Contracts;
using backend.Api.Mapping;
using backend.Application.Abstractions.Realtime;
using backend.Application.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace backend.Api.Realtime;

public sealed class SignalRGameBoardEventsPublisher : IGameBoardEventsPublisher
{
    public const string CellOpenedEventName = RealtimeHubContracts.GameBoard.CellOpenedEvent;
    public const string RoundStateChangedEventName =
        RealtimeHubContracts.GameBoard.RoundStateChangedEvent;
    public const string ModifierActivatedEventName = RealtimeHubContracts.GameBoard.ModifierActivatedEvent;
    public const string ModifierActivationCancelledEventName =
        RealtimeHubContracts.GameBoard.ModifierActivationCancelledEvent;
    public const string ModifierAvailabilityChangedEventName =
        RealtimeHubContracts.GameBoard.ModifierAvailabilityChangedEvent;
    public const string QuizStateChangedEventName = RealtimeHubContracts.GameBoard.QuizStateChangedEvent;
    public const string UserNotificationCreatedEventName =
        RealtimeHubContracts.GameBoard.UserNotificationCreatedEvent;
    public const string GameLifecycleChangedEventName =
        RealtimeHubContracts.GameBoard.GameLifecycleChangedEvent;
    public const string ModifierCatalogChangedEventName =
        RealtimeHubContracts.GameBoard.ModifierCatalogChangedEvent;

    private readonly IHubContext<GameBoardHub> _hubContext;

    public SignalRGameBoardEventsPublisher(IHubContext<GameBoardHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PublishCellOpenedAsync(GameCellOpenedEvent payload, CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients
            .Group(RealtimeGroupNames.GameBoardAudience)
            .SendAsync(CellOpenedEventName, payload.ToDto(), cancellationToken);
    }

    public Task PublishModifierActivatedAsync(
        GameModifierActivatedEvent payload,
        CancellationToken cancellationToken = default
    )
    {
        return _hubContext.Clients.Group(RealtimeGroupNames.GameBoardAudience).SendAsync(
            ModifierActivatedEventName,
            payload.ToDto(),
            cancellationToken
        );
    }

    public Task PublishModifierActivationCancelledAsync(
        GameModifierActivationCancelledEvent payload,
        CancellationToken cancellationToken = default
    )
    {
        return _hubContext.Clients.Group(RealtimeGroupNames.GameBoardAudience).SendAsync(
            ModifierActivationCancelledEventName,
            payload.ToDto(),
            cancellationToken
        );
    }

    public Task PublishModifierAvailabilityChangedAsync(
        GameModifierAvailabilityChangedEvent payload,
        CancellationToken cancellationToken = default
    )
    {
        return _hubContext.Clients.Group(RealtimeGroupNames.GameBoardAudience).SendAsync(
            ModifierAvailabilityChangedEventName,
            payload.ToDto(),
            cancellationToken
        );
    }

    public Task PublishRoundStateChangedAsync(
        GameRoundStateChangedEvent payload,
        CancellationToken cancellationToken = default
    )
    {
        return _hubContext.Clients.Group(RealtimeGroupNames.GameBoardAudience).SendAsync(
            RoundStateChangedEventName,
            payload.ToDto(),
            cancellationToken
        );
    }

    public Task PublishQuizStateChangedAsync(
        GameQuizStateChangedEvent payload,
        CancellationToken cancellationToken = default
    )
    {
        return _hubContext.Clients.Group(RealtimeGroupNames.GameBoardAudience).SendAsync(
            QuizStateChangedEventName,
            payload.ToDto(),
            cancellationToken
        );
    }

    public Task PublishUserNotificationCreatedAsync(
        GameUserNotificationCreatedEvent payload,
        CancellationToken cancellationToken = default
    )
    {
        return _hubContext.Clients.Group(RealtimeGroupNames.GameBoardUserAudience(payload.UserId)).SendAsync(
            UserNotificationCreatedEventName,
            payload.ToDto(),
            cancellationToken
        );
    }

    public Task PublishGameLifecycleChangedAsync(
        GameLifecycleChangedEvent payload,
        CancellationToken cancellationToken = default
    )
    {
        return _hubContext.Clients.Group(RealtimeGroupNames.GameBoardAudience).SendAsync(
            GameLifecycleChangedEventName,
            payload.ToDto(),
            cancellationToken
        );
    }

    public Task PublishModifierCatalogChangedAsync(
        ModifierCatalogChangedEvent payload,
        CancellationToken cancellationToken = default
    )
    {
        var dto = new ModifierCatalogChangedEventDto(
            payload.Modifiers.Select(x => new ModifierCatalogChangedItemDto(
                x.ModifierId.ToString(), x.Revision, x.IsArchived)).ToArray(),
            payload.OccurredAtUtc);
        return _hubContext.Clients.Group(RealtimeGroupNames.GameBoardAudience).SendAsync(
            ModifierCatalogChangedEventName, dto, cancellationToken);
    }
}
