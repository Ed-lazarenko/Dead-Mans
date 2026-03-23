using backend.Application.Abstractions;
using backend.Application.Mapping;
using backend.Api.Contracts;
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
}
