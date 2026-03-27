using backend.Application.Abstractions;
using backend.Application.Abstractions.Auth;
using backend.Api.Contracts;
using backend.Api.Mapping;
using backend.Messaging;
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
            _logger.LogWarning(ex, AppMessages.Logs.GameStateReadFailed);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AppMessages.Logs.GameStateUnexpectedLoadError);
            throw;
        }
    }

    [HttpPost("start")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    public async Task<ActionResult<GameControlStateDto>> Start(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(AppMessages.Logs.GameControlStartRequested);
            var state = await _gameControlService.StartAsync(cancellationToken);
            return Ok(state.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, AppMessages.Logs.GameStateStartFailed);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AppMessages.Logs.GameStartUnexpectedError);
            throw;
        }
    }

    [HttpPost("pause")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    public async Task<ActionResult<GameControlStateDto>> Pause(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(AppMessages.Logs.GameControlPauseRequested);
            var state = await _gameControlService.PauseAsync(cancellationToken);
            return Ok(state.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, AppMessages.Logs.GameStatePauseFailed);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AppMessages.Logs.GamePauseUnexpectedError);
            throw;
        }
    }

    [HttpPost("resume")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    public async Task<ActionResult<GameControlStateDto>> Resume(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(AppMessages.Logs.GameControlResumeRequested);
            var state = await _gameControlService.ResumeAsync(cancellationToken);
            return Ok(state.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, AppMessages.Logs.GameStateResumeFailed);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AppMessages.Logs.GameResumeUnexpectedError);
            throw;
        }
    }

    [HttpPost("next-round")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    public async Task<ActionResult<GameControlStateDto>> NextRound(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(AppMessages.Logs.GameControlNextRoundRequested);
            var state = await _gameControlService.NextRoundAsync(cancellationToken);
            return Ok(state.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, AppMessages.Logs.GameStateNextRoundFailed);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AppMessages.Logs.GameNextRoundUnexpectedError);
            throw;
        }
    }

    [HttpPost("reset")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    public async Task<ActionResult<GameControlStateDto>> Reset(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(AppMessages.Logs.GameControlResetRequested);
            var state = await _gameControlService.ResetAsync(cancellationToken);
            return Ok(state.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, AppMessages.Logs.GameStateResetFailed);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AppMessages.Logs.GameResetUnexpectedError);
            throw;
        }
    }
}
