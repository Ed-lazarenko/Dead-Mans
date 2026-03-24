using backend.Application.Abstractions;
using backend.Application.Abstractions.Auth;
using backend.Api.Contracts;
using backend.Api.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/game-state")]
public sealed class GameStateController : ControllerBase
{
    private readonly IGameControlService _gameControlService;

    public GameStateController(IGameControlService gameControlService)
    {
        _gameControlService = gameControlService;
    }

    [HttpGet]
    public async Task<ActionResult<GameControlStateDto>> Get(CancellationToken cancellationToken)
    {
        var state = await _gameControlService.GetStateAsync(cancellationToken);
        return Ok(state.ToDto());
    }

    [HttpPost("start")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    public async Task<ActionResult<GameControlStateDto>> Start(CancellationToken cancellationToken)
    {
        var state = await _gameControlService.StartAsync(cancellationToken);
        return Ok(state.ToDto());
    }

    [HttpPost("pause")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    public async Task<ActionResult<GameControlStateDto>> Pause(CancellationToken cancellationToken)
    {
        var state = await _gameControlService.PauseAsync(cancellationToken);
        return Ok(state.ToDto());
    }

    [HttpPost("resume")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    public async Task<ActionResult<GameControlStateDto>> Resume(CancellationToken cancellationToken)
    {
        var state = await _gameControlService.ResumeAsync(cancellationToken);
        return Ok(state.ToDto());
    }

    [HttpPost("next-round")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    public async Task<ActionResult<GameControlStateDto>> NextRound(CancellationToken cancellationToken)
    {
        var state = await _gameControlService.NextRoundAsync(cancellationToken);
        return Ok(state.ToDto());
    }

    [HttpPost("reset")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    public async Task<ActionResult<GameControlStateDto>> Reset(CancellationToken cancellationToken)
    {
        var state = await _gameControlService.ResetAsync(cancellationToken);
        return Ok(state.ToDto());
    }
}
