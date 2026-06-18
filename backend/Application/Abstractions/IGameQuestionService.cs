using backend.Application.Contracts;

namespace backend.Application.Abstractions;

public enum AskNextGameQuestionOutcome
{
    Asked,
    NoActiveGame,
    NoAvailableQuestions
}

public sealed record AskNextGameQuestionResult(
    AskNextGameQuestionOutcome Outcome,
    AskedGameQuestion? AskedQuestion = null
);

public enum AnswerGameQuestionOutcome
{
    Answered,
    RoundNotFound,
    RoundNotPending,
    InvalidAnswer
}

public sealed record AnswerGameQuestionResult(
    AnswerGameQuestionOutcome Outcome,
    GameQuestionRoundSummary? Round = null
);

public enum CreateGameQuestionOutcome
{
    Created,
    InvalidRequest,
    DuplicateCode
}

public sealed record CreateGameQuestionResult(
    CreateGameQuestionOutcome Outcome,
    GameQuestionCatalogItem? Question = null
);

public enum UpdateGameQuestionOutcome
{
    Updated,
    NotFound,
    InvalidRequest
}

public sealed record UpdateGameQuestionResult(
    UpdateGameQuestionOutcome Outcome,
    GameQuestionCatalogItem? Question = null
);

public interface IGameQuestionService
{
    Task<IReadOnlyList<GameQuestionCatalogItem>> GetCatalogAsync(
        string? vectorCode,
        string? category,
        string? search,
        bool includeDisabled,
        CancellationToken cancellationToken = default
    );

    Task<CreateGameQuestionResult> CreateQuestionAsync(
        CreateGameQuestionInput input,
        CancellationToken cancellationToken = default
    );

    Task<UpdateGameQuestionResult> UpdateQuestionAsync(
        Guid questionId,
        UpdateGameQuestionInput input,
        CancellationToken cancellationToken = default
    );

    Task<bool> SetQuestionEnabledAsync(
        Guid questionId,
        bool isEnabled,
        CancellationToken cancellationToken = default
    );

    Task<bool> SoftDeleteQuestionAsync(Guid questionId, CancellationToken cancellationToken = default);

    Task<int> SetCategoryEnabledAsync(
        string? vectorCode,
        string category,
        bool isEnabled,
        CancellationToken cancellationToken = default
    );

    Task<AskNextGameQuestionResult> AskNextAsync(
        Guid? askedByUserId,
        CancellationToken cancellationToken = default
    );

    Task<AnswerGameQuestionResult> AnswerRoundAsync(
        Guid roundId,
        string submittedAnswer,
        Guid? answeredByUserId,
        Guid? answeredForUserId,
        string? answeredByDisplayName,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<GameQuestionRoundSummary>> GetGameHistoryAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    );
}
