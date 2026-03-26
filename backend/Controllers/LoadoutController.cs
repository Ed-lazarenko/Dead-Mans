using backend.Application.Abstractions;
using backend.Api.Contracts;
using backend.Api.Mapping;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/loadout")]
public sealed class LoadoutController : ControllerBase
{
    private readonly ILoadoutService _loadoutService;

    public LoadoutController(ILoadoutService loadoutService)
    {
        _loadoutService = loadoutService;
    }

    [HttpGet]
    public async Task<ActionResult<LoadoutBoardDto>> Get(CancellationToken cancellationToken)
    {
        var board = await _loadoutService.GetBoardAsync(cancellationToken);
        return Ok(board.ToDto());
    }

    [HttpPost("{cellId}/toggle")]
    public async Task<ActionResult<LoadoutBoardDto>> ToggleCellState(
        string cellId,
        CancellationToken cancellationToken
    )
    {
        var board = await _loadoutService.ToggleCellStateAsync(cellId, cancellationToken);
        return Ok(board.ToDto());
    }
}
