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
    private readonly ILogger<LoadoutController> _logger;

    public LoadoutController(ILoadoutService loadoutService, ILogger<LoadoutController> logger)
    {
        _loadoutService = loadoutService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<LoadoutBoardDto>> Get(CancellationToken cancellationToken)
    {
        try
        {
            var board = await _loadoutService.GetBoardAsync(cancellationToken);
            return Ok(board.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load loadout board.");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ErrorResponse("Unable to load loadout board.")
            );
        }
    }

    [HttpPost("{cellId}/toggle")]
    public async Task<ActionResult<LoadoutBoardDto>> ToggleCellState(
        string cellId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var board = await _loadoutService.ToggleCellStateAsync(cellId, cancellationToken);
            return Ok(board.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid loadout cell toggle request. CellId: {CellId}.", cellId);
            return BadRequest(new ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error toggling loadout cell. CellId: {CellId}.", cellId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ErrorResponse("Unable to update loadout cell.")
            );
        }
    }
}
