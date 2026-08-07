using backend.Application.Contracts;
using backend.Application.Abstractions;

namespace backend.Application.Abstractions.Repositories;

public interface IGameQuestionRepository
{
    Task<IReadOnlyList<GameQuestionCatalogItem>> GetCatalogAsync(
        Guid? categoryId,
        string? search,
        bool includeDisabled,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<GameQuestionCategoryItem>> GetCategoriesAsync(
        CancellationToken cancellationToken = default
    );

    Task<GameQuestionCategoryItem> EnsureFallbackCategoryAsync(
        CancellationToken cancellationToken = default
    );

    Task<GameQuestionCategoryItem?> GetCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default
    );

    Task<GameQuestionCategoryItem?> GetCategoryAsync(
        string categoryName,
        CancellationToken cancellationToken = default
    );

    Task<GameQuestionCategoryItem> CreateCategoryAsync(
        string categoryName,
        CancellationToken cancellationToken = default
    );

    Task<DeleteGameQuestionCategoryOutcome> DeleteCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default
    );

    Task<GameQuestionCategoryItem?> UpdateCategoryAsync(
        Guid categoryId,
        string categoryName,
        CancellationToken cancellationToken = default
    );

    Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken = default);

    Task<GameQuestionCatalogItem?> CreateQuestionAsync(
        CreateGameQuestionInput input,
        CancellationToken cancellationToken = default
    );

    Task<GameQuestionCatalogItem?> UpdateQuestionAsync(
        Guid questionId,
        UpdateGameQuestionInput input,
        CancellationToken cancellationToken = default
    );

    Task<ImportGameQuestionsResult> ImportQuestionsAsync(
        IReadOnlyList<ImportGameQuestionCandidate> inputs,
        CancellationToken cancellationToken = default
    );

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

    Task<bool> SetCategoryEnabledAsync(
        Guid categoryId,
        bool isEnabled,
        CancellationToken cancellationToken = default
    );

}
