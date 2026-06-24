using backend.Domain.Persistence;

namespace backend.Application.Contracts;

public sealed record GameQuestionCatalogItem(
    Guid QuestionId,
    string QuestionCode,
    Guid CategoryId,
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

public sealed record GameQuestionCategoryItem(
    Guid Id,
    string Name,
    int QuestionCount,
    bool IsProtected
);

public sealed record CreateGameQuestionInput(
    string? ExternalCode,
    Guid CategoryId,
    string Text,
    string Answer,
    int Reward,
    bool IsEnabled,
    int Priority
);

public sealed record ImportGameQuestionInput(
    int RowNumber,
    Guid CategoryId,
    string? Text,
    string? Answer,
    int? Reward,
    string? ExternalCode,
    bool? IsEnabled,
    int? Priority
);

public sealed record ImportGameQuestionCandidate(
    int RowNumber,
    string QuestionText,
    CreateGameQuestionInput Question
);

public sealed record ImportGameQuestionSkippedItem(
    int RowNumber,
    string? QuestionText,
    string Reason
);

public sealed record UpdateGameQuestionInput(
    Guid CategoryId,
    string Text,
    string Answer,
    int Reward,
    bool IsEnabled,
    int Priority
);

public sealed record AskedGameQuestion(
    Guid RoundId,
    Guid GameId,
    int AskOrder,
    Guid QuestionId,
    string QuestionCode,
    string CategoryName,
    string Text,
    int Reward,
    DateTime AskedAtUtc
);

public sealed record GameQuestionRoundSummary(
    Guid RoundId,
    Guid GameId,
    int AskOrder,
    Guid QuestionId,
    string QuestionText,
    string CategoryName,
    int Reward,
    string Status,
    DateTime AskedAtUtc,
    DateTime? AnsweredAtUtc,
    string? AnsweredByDisplayName,
    Guid? AnsweredByUserId,
    Guid? AnsweredForUserId,
    string? SubmittedAnswer,
    bool? IsCorrect,
    int? AwardedPoints
);

public static class GameQuestionRoundSummaryFactory
{
    public static GameQuestionRoundSummary Create(
        Guid roundId,
        Guid gameId,
        int askOrder,
        Guid questionId,
        string questionText,
        string categoryName,
        int reward,
        string status,
        DateTime askedAtUtc,
        DateTime? answeredAtUtc,
        string? answeredByDisplayName,
        Guid? answeredByUserId,
        Guid? answeredForUserId,
        string? submittedAnswer,
        bool? isCorrect,
        int? awardedPoints
    )
    {
        var normalizedStatus = string.IsNullOrWhiteSpace(status)
            ? GameQuestionRoundStatusValue.Asked
            : status;

        return new GameQuestionRoundSummary(
            roundId,
            gameId,
            askOrder,
            questionId,
            questionText,
            categoryName,
            reward,
            normalizedStatus,
            askedAtUtc,
            answeredAtUtc,
            answeredByDisplayName,
            answeredByUserId,
            answeredForUserId,
            submittedAnswer,
            isCorrect,
            awardedPoints
        );
    }
}
