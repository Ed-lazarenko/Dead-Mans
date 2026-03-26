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
    private readonly ILogger<GameStateController> _logger;

    public GameStateController(IGameControlService gameControlService, ILogger<GameStateController> logger)
    {
        _gameControlService = gameControlService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<GameControlStateDto>> Get(CancellationToken cancellationToken)
    {
        try
        {
            var state = await _gameControlService.GetStateAsync(cancellationToken);
            return Ok(state.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Game state read failed (configuration or domain rule).");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading game state.");
            throw;
        }
    }

    [HttpPost("start")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    public async Task<ActionResult<GameControlStateDto>> Start(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Game control: start requested.");
            var state = await _gameControlService.StartAsync(cancellationToken);
            return Ok(state.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Game state start failed.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error starting game.");
            throw;
        }
    }

    [HttpPost("pause")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    public async Task<ActionResult<GameControlStateDto>> Pause(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Game control: pause requested.");
            var state = await _gameControlService.PauseAsync(cancellationToken);
            return Ok(state.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Game state pause failed.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error pausing game.");
            throw;
        }
    }

    [HttpPost("resume")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    public async Task<ActionResult<GameControlStateDto>> Resume(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Game control: resume requested.");
            var state = await _gameControlService.ResumeAsync(cancellationToken);
            return Ok(state.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Game state resume failed.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error resuming game.");
            throw;
        }
    }

    [HttpPost("next-round")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    public async Task<ActionResult<GameControlStateDto>> NextRound(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Game control: next round requested.");
            var state = await _gameControlService.NextRoundAsync(cancellationToken);
            return Ok(state.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Game state next-round failed.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error advancing round.");
            throw;
        }
    }

    [HttpPost("reset")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    public async Task<ActionResult<GameControlStateDto>> Reset(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Game control: reset requested.");
            var state = await _gameControlService.ResetAsync(cancellationToken);
            return Ok(state.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Game state reset failed.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error resetting game.");
            throw;
        }
    }
}
