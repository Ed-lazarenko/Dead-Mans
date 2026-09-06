using backend.Application.Abstractions;
using backend.Application.Abstractions.Realtime;
using backend.Application.Abstractions.Repositories;
using backend.Application.Configuration;
using backend.Application.Contracts;
using backend.Application.Realtime;
using backend.Messaging;
using Microsoft.Extensions.Options;

namespace backend.Application.Features.GameSetup;

public sealed class GameSetupService : IGameSetupService
{
    private readonly IGameSetupRepository _repository;
    private readonly IGameModifierRepository _gameModifierRepository;
    private readonly IGameQuestionRepository _gameQuestionRepository;
    private readonly IObjectStorage _objectStorage;
    private readonly IGameSetupEventsPublisher _eventsPublisher;
    private readonly MediaStorageSettings _storageSettings;
    private readonly ILogger<GameSetupService> _logger;

    public GameSetupService(
        IGameSetupRepository repository,
        IGameModifierRepository gameModifierRepository,
        IGameQuestionRepository gameQuestionRepository,
        IObjectStorage objectStorage,
        IGameSetupEventsPublisher eventsPublisher,
        IOptions<MediaStorageSettings> storageSettings,
        ILogger<GameSetupService> logger
    )
    {
        _repository = repository;
        _gameModifierRepository = gameModifierRepository;
        _gameQuestionRepository = gameQuestionRepository;
        _objectStorage = objectStorage;
        _eventsPublisher = eventsPublisher;
        _storageSettings = storageSettings.Value;
        _logger = logger;
    }

    public Task<GameBoardSnapshot?> GetDraftSetupAsync(CancellationToken cancellationToken = default)
    {
        return _repository.GetLatestDraftSetupSnapshotAsync(cancellationToken);
    }

    public async Task<CreateDraftGameSetupResult> CreateDraftSetupAsync(
        string title,
        CancellationToken cancellationToken = default
    )
    {
        if (!GameSetupDraftValidator.TryNormalizeTitle(title, out var normalizedTitle))
        {
            return new CreateDraftGameSetupResult(CreateDraftGameSetupOutcome.InvalidTitle);
        }

        if (await _repository.DraftGameExistsAsync(cancellationToken))
        {
            return new CreateDraftGameSetupResult(CreateDraftGameSetupOutcome.DraftAlreadyExists);
        }

        var snapshot = await _repository.CreateDraftSetupAsync(normalizedTitle, cancellationToken);
        if (snapshot is null)
        {
            return new CreateDraftGameSetupResult(CreateDraftGameSetupOutcome.DraftAlreadyExists);
        }

        await PublishDraftChangedBestEffortAsync();
        return new CreateDraftGameSetupResult(CreateDraftGameSetupOutcome.Created, snapshot);
    }

    public async Task<UpdateDraftGameSetupResult> UpdateDraftSetupAsync(
        GameSetupDraftUpdate update,
        CancellationToken cancellationToken = default
    )
    {
        if (!GameSetupDraftValidator.TryNormalizeTitle(update.Title, out var normalizedTitle))
        {
            return new UpdateDraftGameSetupResult(UpdateDraftGameSetupOutcome.InvalidTitle);
        }

        if (!GameSetupDraftValidator.TryNormalizeRowLabels(update.RowLabels, out var normalizedRowLabels))
        {
            return new UpdateDraftGameSetupResult(UpdateDraftGameSetupOutcome.InvalidRowLabels);
        }

        if (!GameSetupDraftValidator.TryNormalizeColumnLabels(update.ColLabels, out var normalizedColumnLabels))
        {
            return new UpdateDraftGameSetupResult(UpdateDraftGameSetupOutcome.InvalidColumnLabels);
        }

        if (!GameSetupDraftValidator.TryNormalizeCellUpdates(
                normalizedRowLabels.Length,
                normalizedColumnLabels.Length,
                update.Cells,
                out var normalizedCells
            ))
        {
            return new UpdateDraftGameSetupResult(UpdateDraftGameSetupOutcome.InvalidCells);
        }

        if (!GameSetupDraftValidator.TryNormalizeEnabledModifierIds(
                update.EnabledModifierIds,
                out var normalizedEnabledModifierIds
            ))
        {
            return new UpdateDraftGameSetupResult(UpdateDraftGameSetupOutcome.InvalidEnabledModifiers);
        }

        if (!await _gameModifierRepository.ModifierIdsExistAsync(
                normalizedEnabledModifierIds,
                cancellationToken
            ))
        {
            return new UpdateDraftGameSetupResult(UpdateDraftGameSetupOutcome.InvalidEnabledModifiers);
        }

        if (!GameSetupDraftValidator.TryNormalizeEnabledQuestionIds(
                update.EnabledQuestionIds,
                out var normalizedEnabledQuestionIds
            ))
        {
            return new UpdateDraftGameSetupResult(UpdateDraftGameSetupOutcome.InvalidEnabledQuestions);
        }

        if (!await _gameQuestionRepository.QuestionIdsExistAsync(
                normalizedEnabledQuestionIds,
                cancellationToken
            ))
        {
            return new UpdateDraftGameSetupResult(UpdateDraftGameSetupOutcome.InvalidEnabledQuestions);
        }

        var normalizedUpdate = new GameSetupDraftUpdate(
            update.ExpectedVersion,
            normalizedTitle,
            normalizedRowLabels,
            normalizedColumnLabels,
            normalizedCells,
            normalizedEnabledModifierIds,
            normalizedEnabledQuestionIds
        );

        var saveResult = await _repository.UpdateDraftSetupAsync(normalizedUpdate, cancellationToken);
        var result = saveResult.Status switch
        {
            UpdateDraftSetupRepositoryStatus.Updated when saveResult.Snapshot is not null =>
                new UpdateDraftGameSetupResult(UpdateDraftGameSetupOutcome.Updated, saveResult.Snapshot),
            UpdateDraftSetupRepositoryStatus.StaleVersion =>
                new UpdateDraftGameSetupResult(UpdateDraftGameSetupOutcome.StaleVersion),
            _ => new UpdateDraftGameSetupResult(UpdateDraftGameSetupOutcome.NoDraftFound),
        };

        if (result.Outcome == UpdateDraftGameSetupOutcome.Updated)
        {
            await PublishDraftChangedBestEffortAsync();
        }

        return result;
    }

    public async Task<DeleteDraftGameSetupResult> DeleteDraftSetupAsync(
        CancellationToken cancellationToken = default
    )
    {
        var deletedGameId = await _repository.DeleteDraftSetupAsync(cancellationToken);
        if (deletedGameId is null)
        {
            return new DeleteDraftGameSetupResult(DeleteDraftGameSetupOutcome.NoDraftFound);
        }

        await GameSetupMediaStorageDeletion.TryDeleteGameMediaPrefixAsync(
            _objectStorage,
            _storageSettings,
            _logger,
            deletedGameId.Value,
            cancellationToken
        );

        await PublishDraftChangedBestEffortAsync();
        return new DeleteDraftGameSetupResult(DeleteDraftGameSetupOutcome.Deleted);
    }

    private Task PublishDraftChangedBestEffortAsync()
    {
        return RealtimePublishGuard.TryPublishAsync(
            publishToken => _eventsPublisher.PublishDraftChangedAsync(publishToken),
            _logger,
            AppMessages.Logs.RealtimeGameSetupDraftChangedPublishFailed
        );
    }
}
