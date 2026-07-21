using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed class DbGameNotificationRepository : IGameNotificationRepository
{
    private readonly ApplicationDbContext _dbContext;

    public DbGameNotificationRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<GameUserNotification>> GetUnreadForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbContext.GameUserNotifications
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.ReadAtUtc == null)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(
                x =>
                    new GameUserNotification(
                        x.Id,
                        x.Type,
                        x.CreatedAtUtc,
                        x.ModifierName,
                        x.ActorDisplayName,
                        x.QuizPointsDelta
                    )
            )
            .ToListAsync(cancellationToken);
    }

    public Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (_dbContext.Database.IsRelational())
        {
            return _dbContext.GameUserNotifications
                .Where(x => x.UserId == userId && x.ReadAtUtc == null)
                .ExecuteUpdateAsync(
                    updates => updates.SetProperty(x => x.ReadAtUtc, _ => DateTime.UtcNow),
                    cancellationToken
                );
        }

        return MarkAllReadInMemoryAsync(userId, cancellationToken);
    }

    public async Task<GameUserNotification> CreateModifierCancelledNotificationAsync(
        Guid userId,
        string modifierName,
        string cancelledByDisplayName,
        int refundedQuizPoints,
        CancellationToken cancellationToken = default
    )
    {
        var entity = new backend.Data.Entities.GameUserNotification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = GameNotificationTypes.ModifierCancelled,
            ModifierName = modifierName,
            ActorDisplayName = cancelledByDisplayName,
            QuizPointsDelta = refundedQuizPoints,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.GameUserNotifications.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new GameUserNotification(
            entity.Id,
            entity.Type,
            entity.CreatedAtUtc,
            entity.ModifierName,
            entity.ActorDisplayName,
            entity.QuizPointsDelta
        );
    }

    private async Task MarkAllReadInMemoryAsync(
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        var now = DateTime.UtcNow;
        var notifications = await _dbContext.GameUserNotifications
            .Where(x => x.UserId == userId && x.ReadAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var notification in notifications)
        {
            notification.ReadAtUtc = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
