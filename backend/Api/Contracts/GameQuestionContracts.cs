namespace backend.Api.Contracts;

public sealed record GameQuestionCatalogItemDto(
    string QuestionId,
    string QuestionCode,
    string CategoryId,
    string CategoryName,
    string Text,
    string Answer,
    int Reward,
    bool IsEnabled,
    int AskedTotalCount,
    int CorrectTotalCount,
    DateTime? LastAskedAtUtc
);

public sealed record GameQuestionCategoryItemDto(string Id, string Name, int QuestionCount);

public sealed record SetGameQuestionEnabledRequestDto(bool IsEnabled);

public sealed record SetGameQuestionCategoryEnabledRequestDto(bool IsEnabled);

public sealed record CreateGameQuestionRequestDto(
    string CategoryId,
    string Text,
    string Answer,
    int Reward,
    string? ExternalCode = null,
    bool IsEnabled = true,
    int SortOrder = 0
);

public sealed record CreateGameQuestionCategoryRequestDto(string Name);

public sealed record UpdateGameQuestionRequestDto(
    string CategoryId,
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
    string QuestionCode,
    string CategoryName,
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
    string CategoryName,
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
