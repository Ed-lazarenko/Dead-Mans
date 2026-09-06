using backend.Application.Abstractions;
using backend.Application.Abstractions.Realtime;
using backend.Application.Abstractions.Repositories;
using backend.Application.Realtime;
using backend.Messaging;

namespace backend.Application.Features.GameNotifications;

public sealed class GameNotificationService : IGameNotificationService
{
    private readonly IGameNotificationRepository _repository;
    private readonly IGameBoardEventsPublisher _eventsPublisher;
    private readonly ILogger<GameNotificationService> _logger;

    public GameNotificationService(
        IGameNotificationRepository repository,
        IGameBoardEventsPublisher eventsPublisher,
        ILogger<GameNotificationService> logger
    )
    {
        _repository = repository;
        _eventsPublisher = eventsPublisher;
        _logger = logger;
    }

    public Task<IReadOnlyList<Contracts.GameUserNotification>> GetUnreadAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        return _repository.GetUnreadForUserAsync(userId, cancellationToken);
    }

    public Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _repository.MarkAllReadAsync(userId, cancellationToken);
    }

    public async Task NotifyModifierCancelledAsync(
        Guid userId,
        string modifierName,
        string cancelledByDisplayName,
        int refundedQuizPoints,
        CancellationToken cancellationToken = default
    )
    {
        var notification = await _repository.CreateModifierCancelledNotificationAsync(
            userId,
            modifierName,
            cancelledByDisplayName,
            refundedQuizPoints,
            cancellationToken
        );

        await RealtimePublishGuard.TryPublishAsync(
            publishToken => _eventsPublisher.PublishUserNotificationCreatedAsync(
                new Contracts.GameUserNotificationCreatedEvent(userId, notification),
                publishToken
            ),
            _logger,
            AppMessages.Logs.RealtimeGameNotificationPublishFailed,
            userId,
            notification.NotificationId
        );
    }
}
