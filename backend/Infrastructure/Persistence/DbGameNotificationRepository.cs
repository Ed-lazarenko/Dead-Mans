using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace backend.Infrastructure.Persistence;

public sealed class DbGameNotificationRepository : IGameNotificationRepository
{
    private static readonly JsonSerializerOptions NotificationJsonOptions =
        new(JsonSerializerDefaults.Web);

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
        var rows = await _dbContext.GameUserNotifications
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.ReadAtUtc == null)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new NotificationRow(
                x.Id,
                x.Type,
                x.SchemaVersion,
                x.PayloadJson,
                x.CreatedAtUtc
            ))
            .ToListAsync(cancellationToken);

        return rows.Select(ToContract).ToArray();
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
        Guid gameId,
        Guid modifierActivationId,
        string modifierName,
        string cancelledByDisplayName,
        int refundedQuizPoints,
        CancellationToken cancellationToken = default
    )
    {
        var payload = new ModifierCancelledNotificationPayload(
            modifierActivationId,
            modifierName,
            cancelledByDisplayName,
            refundedQuizPoints
        );
        var entity = new backend.Data.Entities.GameUserNotification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GameId = gameId,
            Type = GameNotificationTypes.ModifierCancelled,
            SchemaVersion = 1,
            PayloadJson = JsonSerializer.Serialize(payload, NotificationJsonOptions),
            DeduplicationKey = $"modifier_cancelled:{modifierActivationId:N}",
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.GameUserNotifications.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToContract(new NotificationRow(
            entity.Id,
            entity.Type,
            entity.SchemaVersion,
            entity.PayloadJson,
            entity.CreatedAtUtc
        ));
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

    private static GameUserNotification ToContract(NotificationRow row)
    {
        if (row.Type == GameNotificationTypes.ModifierCancelled && row.SchemaVersion == 1)
        {
            var payload = JsonSerializer.Deserialize<ModifierCancelledNotificationPayload>(
                row.PayloadJson,
                NotificationJsonOptions
            );
            if (payload is not null)
            {
                return new GameUserNotification(
                    row.Id,
                    row.Type,
                    row.CreatedAtUtc,
                    payload.ModifierName,
                    payload.ActorDisplayName,
                    payload.QuizPointsDelta
                );
            }
        }

        return new GameUserNotification(row.Id, row.Type, row.CreatedAtUtc, null, null, null);
    }

    private sealed record NotificationRow(
        Guid Id,
        string Type,
        int SchemaVersion,
        string PayloadJson,
        DateTime CreatedAtUtc
    );

    private sealed record ModifierCancelledNotificationPayload(
        Guid ModifierActivationId,
        string ModifierName,
        string ActorDisplayName,
        int QuizPointsDelta
    );
}
