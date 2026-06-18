using backend.Application.Abstractions;
using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Domain.Persistence;

namespace backend.Application.Features.GameQuestions;

public sealed class GameQuestionService : IGameQuestionService
{
    private readonly IGameQuestionRepository _repository;

    public GameQuestionService(IGameQuestionRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<GameQuestionCatalogItem>> GetCatalogAsync(
        string? vectorCode,
        string? category,
        string? search,
        bool includeDisabled,
        CancellationToken cancellationToken = default
    )
    {
        return _repository.GetCatalogAsync(
            vectorCode,
            category,
            search,
            includeDisabled,
            cancellationToken
        );
    }

    public async Task<CreateGameQuestionResult> CreateQuestionAsync(
        CreateGameQuestionInput input,
        CancellationToken cancellationToken = default
    )
    {
        if (!GameQuestionValidator.TryNormalizeCreate(input, out var normalized))
        {
            return new CreateGameQuestionResult(CreateGameQuestionOutcome.InvalidRequest);
        }

        var created = await _repository.CreateQuestionAsync(normalized, cancellationToken);
        return created is null
            ? new CreateGameQuestionResult(CreateGameQuestionOutcome.DuplicateCode)
            : new CreateGameQuestionResult(CreateGameQuestionOutcome.Created, created);
    }

    public async Task<UpdateGameQuestionResult> UpdateQuestionAsync(
        Guid questionId,
        UpdateGameQuestionInput input,
        CancellationToken cancellationToken = default
    )
    {
        if (!GameQuestionValidator.TryNormalizeUpdate(input, out var normalized))
        {
            return new UpdateGameQuestionResult(UpdateGameQuestionOutcome.InvalidRequest);
        }

        var updated = await _repository.UpdateQuestionAsync(questionId, normalized, cancellationToken);
        return updated is null
            ? new UpdateGameQuestionResult(UpdateGameQuestionOutcome.NotFound)
            : new UpdateGameQuestionResult(UpdateGameQuestionOutcome.Updated, updated);
    }

    public Task<bool> SetQuestionEnabledAsync(
        Guid questionId,
        bool isEnabled,
        CancellationToken cancellationToken = default
    )
    {
        return _repository.SetQuestionEnabledAsync(questionId, isEnabled, cancellationToken);
    }

    public Task<bool> SoftDeleteQuestionAsync(
        Guid questionId,
        CancellationToken cancellationToken = default
    )
    {
        return _repository.SoftDeleteQuestionAsync(questionId, cancellationToken);
    }

    public Task<int> SetCategoryEnabledAsync(
        string? vectorCode,
        string category,
        bool isEnabled,
        CancellationToken cancellationToken = default
    )
    {
        return _repository.SetCategoryEnabledAsync(vectorCode, category, isEnabled, cancellationToken);
    }

    public async Task<AskNextGameQuestionResult> AskNextAsync(
        Guid? askedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        var activeGameId = await _repository.GetActiveGameIdAsync(cancellationToken);
        if (!activeGameId.HasValue)
        {
            return new AskNextGameQuestionResult(AskNextGameQuestionOutcome.NoActiveGame);
        }

        var askedQuestion = await _repository.AskNextQuestionAsync(
            activeGameId.Value,
            askedByUserId,
            cancellationToken
        );
        if (askedQuestion is null)
        {
            return new AskNextGameQuestionResult(AskNextGameQuestionOutcome.NoAvailableQuestions);
        }

        return new AskNextGameQuestionResult(AskNextGameQuestionOutcome.Asked, askedQuestion);
    }

    public async Task<AnswerGameQuestionResult> AnswerRoundAsync(
        Guid roundId,
        string submittedAnswer,
        Guid? answeredByUserId,
        Guid? answeredForUserId,
        string? answeredByDisplayName,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(submittedAnswer))
        {
            return new AnswerGameQuestionResult(AnswerGameQuestionOutcome.InvalidAnswer);
        }

        var round = await _repository.GetRoundAsync(roundId, cancellationToken);
        if (round is null)
        {
            return new AnswerGameQuestionResult(AnswerGameQuestionOutcome.RoundNotFound);
        }

        if (round.Status != GameQuestionRoundStatusValue.Asked)
        {
            return new AnswerGameQuestionResult(AnswerGameQuestionOutcome.RoundNotPending, round);
        }

        var updatedRound = await _repository.AnswerRoundAsync(
            roundId,
            answeredByUserId,
            answeredForUserId,
            answeredByDisplayName,
            submittedAnswer,
            cancellationToken
        );
        if (updatedRound is null)
        {
            return new AnswerGameQuestionResult(AnswerGameQuestionOutcome.RoundNotPending, round);
        }

        return new AnswerGameQuestionResult(AnswerGameQuestionOutcome.Answered, updatedRound);
    }

    public Task<IReadOnlyList<GameQuestionRoundSummary>> GetGameHistoryAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    )
    {
        return _repository.GetGameHistoryAsync(gameId, cancellationToken);
    }
}
