namespace backend.Api.Contracts;

public sealed record GameQuestionCatalogItemDto(
    string QuestionId,
    string QuestionCode,
    string CategoryId,
    string CategoryName,
    string Text,
    string Answer,
    int Reward,
    int Priority,
    bool IsEnabled,
    int AskedTotalCount,
    int CorrectTotalCount,
    DateTime? LastAskedAtUtc
);

public sealed record GameQuestionCategoryItemDto(
    string Id,
    string Name,
    int QuestionCount,
    bool IsProtected
);

public sealed record SetGameQuestionEnabledRequestDto(bool IsEnabled);

public sealed record SetGameQuestionCategoryEnabledRequestDto(bool IsEnabled);

public sealed record CreateGameQuestionRequestDto(
    string CategoryId,
    string Text,
    string Answer,
    int Reward,
    string? ExternalCode = null,
    bool IsEnabled = true,
    int Priority = 0
);

public sealed record CreateGameQuestionCategoryRequestDto(string Name);

public sealed record ImportGameQuestionRequestDto(
    string? Text,
    string? Answer,
    int? Reward,
    string? CategoryId = null,
    string? ExternalCode = null,
    bool? IsEnabled = null,
    int? Priority = null
);

public sealed record ImportGameQuestionSourceDto(
    string? Text,
    string? Answer,
    int? Reward,
    string? CategoryId,
    string? ExternalCode,
    bool? IsEnabled,
    int? Priority
);

public sealed record ImportGameQuestionSkippedItemDto(
    int RowNumber,
    string? QuestionText,
    string ReasonCode,
    string Reason,
    ImportGameQuestionSourceDto? SourceQuestion
);

public sealed record ImportGameQuestionsResultDto(
    int ImportedCount,
    IReadOnlyList<ImportGameQuestionSkippedItemDto> SkippedQuestions
);

public sealed record UpdateGameQuestionRequestDto(
    string CategoryId,
    string Text,
    string Answer,
    int Reward,
    bool IsEnabled = true,
    int Priority = 0
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

public sealed record ManualQuizAwardRequestDto(string AwardedToUserId, int Points);

public sealed record ManualQuizAwardPlayerDto(string UserId, string Login, string DisplayName);

public sealed record ManualQuizAwardSummaryDto(
    string AwardId,
    string GameId,
    string AwardedToUserId,
    string AwardedToDisplayName,
    string AwardedByUserId,
    string AwardedByDisplayName,
    int Points,
    DateTime AwardedAtUtc
);

public sealed record GameQuizStateChangedEventDto(
    string GameId,
    string ChangeKind,
    DateTime OccurredAtUtc
);
