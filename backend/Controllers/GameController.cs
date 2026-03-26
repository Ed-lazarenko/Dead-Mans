using backend.Application.Abstractions;
using backend.Api.Contracts;
using backend.Api.Mapping;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/game")]
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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        try
        {
            var board = await _gameBoardService.GetCurrentBoardAsync(cancellationToken);
            if (board is null)
            {
                _logger.LogInformation("No active or finished game with a board was found.");
                return NotFound(new ErrorResponse("No active or finished game was found."));
            }

            return Ok(board.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load current game board.");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ErrorResponse("Unable to load the current game.")
            );
        }
    }
}
