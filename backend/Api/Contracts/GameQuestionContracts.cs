namespace backend.Api.Contracts;

public sealed record GameQuestionCatalogItemDto(
    string QuestionId,
    string VectorCode,
    string QuestionCode,
    string Category,
    string Text,
    string Answer,
    int Reward,
    bool IsEnabled,
    int AskedTotalCount,
    int CorrectTotalCount,
    DateTime? LastAskedAtUtc
);

public sealed record SetGameQuestionEnabledRequestDto(bool IsEnabled);

public sealed record SetGameQuestionCategoryEnabledRequestDto(bool IsEnabled);

public sealed record CreateGameQuestionRequestDto(
    string VectorCode,
    string Category,
    string Text,
    string Answer,
    int Reward,
    string? ExternalCode = null,
    bool IsEnabled = true,
    int SortOrder = 0
);

public sealed record UpdateGameQuestionRequestDto(
    string VectorCode,
    string Category,
    string Text,
    string Answer,
    int Reward,
    bool IsEnabled = true,
    int SortOrder = 0
);

public sealed record AskedGameQuestionDto(
    string RoundId,
    string GameId,
    int AskOrder,
    string QuestionId,
    string VectorCode,
    string QuestionCode,
    string Category,
    string Text,
    int Reward,
    DateTime AskedAtUtc
);

public sealed record AnswerGameQuestionRequestDto(
    string Answer,
    string? AnsweredByDisplayName,
    string? AnsweredForUserId
);

public sealed record GameQuestionRoundSummaryDto(
    string RoundId,
    string GameId,
    int AskOrder,
    string QuestionId,
    string QuestionText,
    string Category,
    int Reward,
    string Status,
    DateTime AskedAtUtc,
    DateTime? AnsweredAtUtc,
    string? AnsweredByDisplayName,
    string? AnsweredByUserId,
    string? AnsweredForUserId,
    string? SubmittedAnswer,
    bool? IsCorrect,
    int? AwardedPoints
);
