namespace backend.Api.Contracts;

public sealed record GameUserNotificationDto(
    string NotificationId,
    string Type,
    DateTime CreatedAtUtc,
    string? ModifierName,
    string? ActorDisplayName,
    int? QuizPointsDelta
);

public sealed record GameUserNotificationCreatedEventDto(GameUserNotificationDto Notification);
