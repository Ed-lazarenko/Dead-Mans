using backend.Application.Contracts;

namespace backend.Application.Abstractions.Repositories;

public interface IGameQuestionRepository
{
    Task<IReadOnlyList<GameQuestionCatalogItem>> GetCatalogAsync(
        string? vectorCode,
        string? category,
        string? search,
        bool includeDisabled,
        CancellationToken cancellationToken = default
    );

    /// <summary>Persists a new question. Returns null when the (vector, code) pair already exists.</summary>
    Task<GameQuestionCatalogItem?> CreateQuestionAsync(
        CreateGameQuestionInput input,
        CancellationToken cancellationToken = default
    );

    /// <summary>Updates an existing, non-deleted question. Returns null when it does not exist.</summary>
    Task<GameQuestionCatalogItem?> UpdateQuestionAsync(
        Guid questionId,
        UpdateGameQuestionInput input,
        CancellationToken cancellationToken = default
    );

    /// <summary>Checks that every id refers to an existing, non-deleted question.</summary>
    Task<bool> QuestionIdsExistAsync(
        IReadOnlyList<Guid> questionIds,
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

    Task<Guid?> GetActiveGameIdAsync(CancellationToken cancellationToken = default);

    Task<AskedGameQuestion?> AskNextQuestionAsync(
        Guid gameId,
        Guid? askedByUserId,
        CancellationToken cancellationToken = default
    );

    Task<GameQuestionRoundSummary?> AnswerRoundAsync(
        Guid roundId,
        Guid? answeredByUserId,
        Guid? answeredForUserId,
        string? answeredByDisplayName,
        string submittedAnswer,
        CancellationToken cancellationToken = default
    );

    Task<GameQuestionRoundSummary?> GetRoundAsync(
        Guid roundId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<GameQuestionRoundSummary>> GetGameHistoryAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    );
}
