namespace backend.Api.Contracts;

public sealed record UserGameModifierActivationHistoryItemDto(
    string ModifierCode,
    DateTime ActivatedAtUtc
);

public sealed record UserGameQuestionAnswerHistoryItemDto(
    string RoundId,
    string QuestionId,
    string QuestionText,
    string CategoryName,
    DateTime AnsweredAtUtc,
    bool IsCorrect,
    int AwardedPoints,
    string? SubmittedAnswer,
    string? AnsweredByUserId
);

public sealed record UserGameHistoryItemDto(
    string GameId,
    string GameTitle,
    string GameStatus,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? FinishedAtUtc,
    IReadOnlyList<UserGameModifierActivationHistoryItemDto> ModifierActivations,
    IReadOnlyList<UserGameQuestionAnswerHistoryItemDto> QuestionAnswers
);
