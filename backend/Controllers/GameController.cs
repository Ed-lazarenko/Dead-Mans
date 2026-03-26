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

    public GameController(IGameBoardService gameBoardService)
    {
        _gameBoardService = gameBoardService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(GameBoardSnapshotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var board = await _gameBoardService.GetCurrentBoardAsync(cancellationToken);
        if (board is null)
        {
            return NotFound(new ErrorResponse("No active or finished game was found."));
        }

        return Ok(board.ToDto());
    }
}
