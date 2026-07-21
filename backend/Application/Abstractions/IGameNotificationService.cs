using backend.Application.Contracts;

namespace backend.Application.Abstractions;

public interface IGameNotificationService
{
    Task<IReadOnlyList<GameUserNotification>> GetUnreadAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default);

    Task NotifyModifierCancelledAsync(
        Guid userId,
        string modifierName,
        string cancelledByDisplayName,
        int refundedQuizPoints,
        CancellationToken cancellationToken = default
    );
}
