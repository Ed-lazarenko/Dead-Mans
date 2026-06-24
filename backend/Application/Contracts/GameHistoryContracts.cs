namespace backend.Application.Contracts;

public sealed record UserGameModifierActivationHistoryItem(
    Guid ModifierId,
    DateTime ActivatedAtUtc
);

public sealed record UserGameQuestionAnswerHistoryItem(
    Guid RoundId,
    Guid QuestionId,
    string QuestionText,
    string CategoryName,
    DateTime AnsweredAtUtc,
    bool IsCorrect,
    int AwardedPoints,
    string? SubmittedAnswer,
    Guid? AnsweredByUserId
);

public sealed record UserGameHistoryItem(
    Guid GameId,
    string GameTitle,
    string GameStatus,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? FinishedAtUtc,
    IReadOnlyList<UserGameModifierActivationHistoryItem> ModifierActivations,
    IReadOnlyList<UserGameQuestionAnswerHistoryItem> QuestionAnswers
);
