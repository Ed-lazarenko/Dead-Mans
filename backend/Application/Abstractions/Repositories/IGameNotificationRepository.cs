using backend.Application.Contracts;

namespace backend.Application.Abstractions.Repositories;

public interface IGameNotificationRepository
{
    Task<IReadOnlyList<GameUserNotification>> GetUnreadForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<GameUserNotification> CreateModifierCancelledNotificationAsync(
        Guid userId,
        Guid gameId,
        Guid modifierActivationId,
        string modifierName,
        string cancelledByDisplayName,
        int refundedQuizPoints,
        CancellationToken cancellationToken = default
    );
}
