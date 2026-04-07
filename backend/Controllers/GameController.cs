using backend.Application.Abstractions;
using backend.Application.Abstractions.Auth;
using backend.Api.Contracts;
using backend.Api.Mapping;
using backend.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

/// <summary>Read-only current game board snapshot from the database.</summary>
[ApiController]
[Route("api/game")]
[Authorize]
public sealed class GameController : ControllerBase
{
    private readonly IGameBoardService _gameBoardService;
    private readonly ILogger<GameController> _logger;

    public GameController(IGameBoardService gameBoardService, ILogger<GameController> logger)
    {
        _gameBoardService = gameBoardService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(GameBoardSnapshotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        try
        {
            var board = await _gameBoardService.GetCurrentBoardAsync(cancellationToken);
            if (board is null)
            {
                _logger.LogInformation(AppMessages.Logs.GameNoBoardForGet);
                return NotFound(new ErrorResponse(AppMessages.Client.NoActiveOrFinishedGame));
            }

            return Ok(board.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AppMessages.Logs.GameBoardLoadFailed);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ErrorResponse(AppMessages.Client.UnableToLoadCurrentGame)
            );
        }
    }

    [HttpPost("cells/{cellId:guid}/open")]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> OpenCell(Guid cellId, CancellationToken cancellationToken)
    {
        try
        {
            var openResult = await _gameBoardService.TryOpenCellAsync(cellId, cancellationToken);
            if (openResult is null)
            {
                return NotFound(new ErrorResponse(AppMessages.Client.GameCellNotFound));
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AppMessages.Logs.GameCellOpenFailed, cellId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ErrorResponse(AppMessages.Client.UnableToOpenGameCell)
            );
        }
    }
}
