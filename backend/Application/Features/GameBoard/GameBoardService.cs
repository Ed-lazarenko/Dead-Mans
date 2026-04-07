using backend.Application.Abstractions;
using backend.Application.Abstractions.Repositories;
using backend.Application.Abstractions.Realtime;
using backend.Application.Contracts;

namespace backend.Application.Features.GameBoard;

public sealed class GameBoardService : IGameBoardService
{
    private readonly IGameBoardRepository _repository;
    private readonly IGameBoardEventsPublisher _eventsPublisher;
    private readonly ILogger<GameBoardService> _logger;

    public GameBoardService(
        IGameBoardRepository repository,
        IGameBoardEventsPublisher eventsPublisher,
        ILogger<GameBoardService> logger
    )
    {
        _repository = repository;
        _eventsPublisher = eventsPublisher;
        _logger = logger;
    }

    public Task<GameBoardSnapshot?> GetCurrentBoardAsync(CancellationToken cancellationToken = default)
    {
        return _repository.GetCurrentBoardAsync(cancellationToken);
    }

    public async Task<OpenGameCellResult?> TryOpenCellAsync(
        Guid cellId,
        CancellationToken cancellationToken = default
    )
    {
        var result = await _repository.TryOpenCellAsync(cellId, cancellationToken);
        if (result is null || !result.StateChanged)
        {
            return result;
        }

        try
        {
            await _eventsPublisher.PublishCellOpenedAsync(
                new GameCellOpenedEvent(result.GameId, result.Version, result.Cell),
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish game cell opened event. CellId: {CellId}.", cellId);
        }

        return result;
    }
}
