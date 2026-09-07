using backend.Api.Contracts;
using backend.Api.Mapping;
using backend.Application.Abstractions.Realtime;
using backend.Application.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace backend.Infrastructure.Realtime;

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

    public Task PublishModifierActivationCancelledAsync(
        GameModifierActivationCancelledEvent @event,
        CancellationToken cancellationToken = default
    )
    {
        return _hubContext.Clients.Group(RealtimeGroupNames.GameBoardAudience).SendAsync(
            ModifierActivationCancelledEventName,
            @event.ToDto(),
            cancellationToken
        );
    }

    public Task PublishModifierAvailabilityChangedAsync(
        GameModifierAvailabilityChangedEvent @event,
        CancellationToken cancellationToken = default
    )
    {
        return _hubContext.Clients.Group(RealtimeGroupNames.GameBoardAudience).SendAsync(
            ModifierAvailabilityChangedEventName,
            @event.ToDto(),
            cancellationToken
        );
    }

    public Task PublishRoundStateChangedAsync(
        GameRoundStateChangedEvent @event,
        CancellationToken cancellationToken = default
    )
    {
        return _hubContext.Clients.Group(RealtimeGroupNames.GameBoardAudience).SendAsync(
            RoundStateChangedEventName,
            @event.ToDto(),
            cancellationToken
        );
    }

    public Task PublishQuizStateChangedAsync(
        GameQuizStateChangedEvent @event,
        CancellationToken cancellationToken = default
    )
    {
        return _hubContext.Clients.Group(RealtimeGroupNames.GameBoardAudience).SendAsync(
            QuizStateChangedEventName,
            @event.ToDto(),
            cancellationToken
        );
    }

    public Task PublishUserNotificationCreatedAsync(
        GameUserNotificationCreatedEvent @event,
        CancellationToken cancellationToken = default
    )
    {
        return _hubContext.Clients.Group(RealtimeGroupNames.GameBoardUserAudience(@event.UserId)).SendAsync(
            UserNotificationCreatedEventName,
            @event.ToDto(),
            cancellationToken
        );
    }

    public Task PublishGameLifecycleChangedAsync(
        GameLifecycleChangedEvent @event,
        CancellationToken cancellationToken = default
    )
    {
        return _hubContext.Clients.Group(RealtimeGroupNames.GameBoardAudience).SendAsync(
            GameLifecycleChangedEventName,
            @event.ToDto(),
            cancellationToken
        );
    }

    public Task PublishModifierCatalogChangedAsync(
        ModifierCatalogChangedEvent @event,
        CancellationToken cancellationToken = default
    )
    {
        var dto = new ModifierCatalogChangedEventDto(
            @event.Modifiers.Select(x => new ModifierCatalogChangedItemDto(
                x.ModifierId.ToString(), x.Revision, x.IsArchived)).ToArray(),
            @event.OccurredAtUtc);
        return _hubContext.Clients.Group(RealtimeGroupNames.GameBoardAudience).SendAsync(
            ModifierCatalogChangedEventName, dto, cancellationToken);
    }
}
