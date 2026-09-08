using backend.Application.Abstractions;
using backend.Application.Abstractions.Realtime;
using backend.Application.Abstractions.Repositories;
using backend.Application.Configuration;
using backend.Application.Contracts;
using backend.Application.Realtime;
using backend.Messaging;
using Microsoft.Extensions.Options;

namespace backend.Application.Features.GameSetup;

public sealed class GameSetupCellMediaService : IGameSetupCellMediaService
{
    private readonly IGameSetupRepository _gameSetupRepository;
    private readonly IGameSetupCellMediaRepository _cellMediaRepository;
    private readonly IObjectStorage _objectStorage;
    private readonly IGameSetupEventsPublisher _eventsPublisher;
    private readonly MediaStorageSettings _storageSettings;
    private readonly ILogger<GameSetupCellMediaService> _logger;

    public GameSetupCellMediaService(
        IGameSetupRepository gameSetupRepository,
        IGameSetupCellMediaRepository cellMediaRepository,
        IObjectStorage objectStorage,
        IGameSetupEventsPublisher eventsPublisher,
        IOptions<MediaStorageSettings> storageSettings,
        ILogger<GameSetupCellMediaService> logger
    )
    {
        _gameSetupRepository = gameSetupRepository;
        _cellMediaRepository = cellMediaRepository;
        _objectStorage = objectStorage;
        _eventsPublisher = eventsPublisher;
        _storageSettings = storageSettings.Value;
        _logger = logger;
    }

    public async Task<UploadDraftGameSetupCellMediaResult> UploadAsync(
        Guid cellId,
        Stream content,
        string contentType,
        long contentLength,
        CancellationToken cancellationToken = default
    )
    {
        if (!GameSetupCellMediaValidator.IsAllowedUpload(contentType, contentLength, out var normalizedMimeType))
        {
            return new UploadDraftGameSetupCellMediaResult(UploadDraftGameSetupCellMediaOutcome.InvalidFile);
        }

        if (!GameSetupCellMediaValidator.HasMatchingFileSignature(content, normalizedMimeType))
        {
            return new UploadDraftGameSetupCellMediaResult(UploadDraftGameSetupCellMediaOutcome.InvalidFile);
        }

        if (!await _gameSetupRepository.DraftGameExistsAsync(cancellationToken))
        {
            return new UploadDraftGameSetupCellMediaResult(UploadDraftGameSetupCellMediaOutcome.NoDraft);
        }

        var draftCell = await _cellMediaRepository.FindDraftCellAsync(cellId, cancellationToken);
        if (draftCell is null)
        {
            return new UploadDraftGameSetupCellMediaResult(UploadDraftGameSetupCellMediaOutcome.CellNotFound);
        }

        var extension = GameSetupCellMediaValidator.ResolveExtension(normalizedMimeType);
        if (extension.Length == 0)
        {
            return new UploadDraftGameSetupCellMediaResult(UploadDraftGameSetupCellMediaOutcome.InvalidFile);
        }

        var existingMedia = await _cellMediaRepository.GetCellMediaAsync(cellId, cancellationToken);
        var mediaAssetId = Guid.NewGuid();
        var objectKey = GameSetupCellMediaValidator.BuildObjectKey(
            _storageSettings,
            draftCell,
            mediaAssetId,
            extension
        );
        var bucket = _storageSettings.BucketName;

        try
        {
            if (content.CanSeek)
            {
                content.Position = 0;
            }

            await _objectStorage.PutObjectAsync(
                bucket,
                objectKey,
                content,
                normalizedMimeType,
                cancellationToken
            );

            GameBoardCellMedia media;
            try
            {
                media = await _cellMediaRepository.AttachMediaAsync(
                    cellId,
                    mediaAssetId,
                    bucket,
                    objectKey,
                    normalizedMimeType,
                    contentLength,
                    _storageSettings.PublicBaseUrl,
                    cancellationToken
                );
            }
            catch (Exception attachEx)
            {
                await TryDeleteStorageObjectAsync(bucket, objectKey, cellId, attachEx, cancellationToken);
                throw;
            }

            if (existingMedia is not null)
            {
                await TryDeleteDetachedObjectAsync(existingMedia, cellId, cancellationToken);
            }

            await PublishDraftChangedBestEffortAsync();
            return new UploadDraftGameSetupCellMediaResult(UploadDraftGameSetupCellMediaOutcome.Uploaded, media);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                AppMessages.Logs.GameSetupCellMediaStorageUploadFailed,
                cellId,
                draftCell.GameId
            );
            return new UploadDraftGameSetupCellMediaResult(UploadDraftGameSetupCellMediaOutcome.StorageFailed);
        }
    }

    public async Task<DeleteDraftGameSetupCellMediaResult> DeleteAsync(
        Guid cellId,
        CancellationToken cancellationToken = default
    )
    {
        if (!await _gameSetupRepository.DraftGameExistsAsync(cancellationToken))
        {
            return new DeleteDraftGameSetupCellMediaResult(DeleteDraftGameSetupCellMediaOutcome.NoDraft);
        }

        var draftCell = await _cellMediaRepository.FindDraftCellAsync(cellId, cancellationToken);
        if (draftCell is null)
        {
            return new DeleteDraftGameSetupCellMediaResult(DeleteDraftGameSetupCellMediaOutcome.CellNotFound);
        }

        var detachedMedia = await _cellMediaRepository.DetachMediaAsync(cellId, cancellationToken);
        if (detachedMedia is null)
        {
            return new DeleteDraftGameSetupCellMediaResult(DeleteDraftGameSetupCellMediaOutcome.MediaNotFound);
        }

        await TryDeleteDetachedObjectAsync(detachedMedia, cellId, cancellationToken);
        await PublishDraftChangedBestEffortAsync();
        return new DeleteDraftGameSetupCellMediaResult(DeleteDraftGameSetupCellMediaOutcome.Deleted);
    }

    private Task PublishDraftChangedBestEffortAsync()
    {
        return RealtimePublishGuard.TryPublishAsync(
            publishToken => _eventsPublisher.PublishDraftChangedAsync(publishToken),
            _logger,
            AppMessages.Logs.RealtimeGameSetupDraftChangedPublishFailed
        );
    }

    private Task TryDeleteDetachedObjectAsync(
        StoredCellMedia media,
        Guid cellId,
        CancellationToken cancellationToken
    )
    {
        return GameSetupMediaStorageDeletion.TryDeleteObjectAsync(
            _objectStorage,
            _logger,
            cellId,
            media.Bucket,
            media.ObjectKey,
            failureCause: null,
            cancellationToken
        );
    }

    private Task TryDeleteStorageObjectAsync(
        string bucket,
        string objectKey,
        Guid cellId,
        Exception failureCause,
        CancellationToken cancellationToken
    )
    {
        return GameSetupMediaStorageDeletion.TryDeleteObjectAsync(
            _objectStorage,
            _logger,
            cellId,
            bucket,
            objectKey,
            failureCause,
            cancellationToken
        );
    }
}
