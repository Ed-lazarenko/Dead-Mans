using backend.Api.Contracts;
using backend.Api.Errors;
using backend.Api.Http;
using backend.Api.Mapping;
using backend.Application.Abstractions;
using backend.Application.Contracts;
using backend.Application.Abstractions.Auth;
using backend.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/game/lifecycle")]
[Authorize(Roles = AuthRoleCodes.Admin)]
public sealed class GameLifecycleController : ControllerBase
{
    private readonly IGameLifecycleService _lifecycleService;

    public GameLifecycleController(IGameLifecycleService lifecycleService)
    {
        _lifecycleService = lifecycleService;
    }

    [HttpPost("open-registration")]
    [ProducesResponseType(typeof(GameLifecycleStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> OpenRegistration(CancellationToken cancellationToken)
    {
        var result = await _lifecycleService.OpenRegistrationAsync(cancellationToken);
        return ToActionResult(result, GameLifecycleStatuses.Ready);
    }

    [HttpPost("start")]
    [ProducesResponseType(typeof(GameLifecycleStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Start(CancellationToken cancellationToken)
    {
        var result = await _lifecycleService.StartGameAsync(cancellationToken);
        return ToActionResult(result, GameLifecycleStatuses.Active);
    }

    [HttpGet("games/{gameId:guid}/finish-preview")]
    [ProducesResponseType(typeof(GameFinishPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFinishPreview(
        Guid gameId,
        CancellationToken cancellationToken
    )
    {
        var result = await _lifecycleService.GetFinishPreviewAsync(gameId, cancellationToken);
        return result.Success && result.Preview is not null
            ? Ok(result.Preview.ToDto())
            : this.Error(DomainErrorHttpPolicy.FromLifecycle(result.Error));
    }

    [HttpPost("games/{gameId:guid}/finish")]
    [ProducesResponseType(typeof(FinishGameResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Finish(
        Guid gameId,
        [FromBody] FinishGameRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var currentUserId = HttpContext.TryGetUserId();
        if (!currentUserId.HasValue)
        {
            return this.BadRequestError(AppMessages.Client.AuthCookieMissingClaims);
        }

        var acknowledgedWarnings = (request.AcknowledgedWarningCodes ?? Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToHashSet(StringComparer.Ordinal);
        var result = await _lifecycleService.FinishGameAsync(
            gameId,
            new FinishGameInput(
                request.ExpectedBoardVersion,
                request.RequestId,
                acknowledgedWarnings,
                request.Note
            ),
            currentUserId.Value,
            cancellationToken
        );
        return result.Success && result.Summary is not null
            ? Ok(new FinishGameResponseDto(result.Summary.ToDto(), result.AlreadyFinished))
            : this.Error(DomainErrorHttpPolicy.FromLifecycle(result.Error));
    }

    [HttpDelete("games/{gameId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ArchiveGame(Guid gameId, CancellationToken cancellationToken)
    {
        var result = await _lifecycleService.ArchiveGameAsync(gameId, cancellationToken);
        if (result.Success)
        {
            return NoContent();
        }

        return this.Error(DomainErrorHttpPolicy.FromLifecycle(result.Error));
    }

    private IActionResult ToActionResult(GameLifecycleResult result, string successStatus)
    {
        if (result.Success && result.GameId.HasValue)
        {
            return Ok(new GameLifecycleStateDto(result.GameId.Value, successStatus));
        }

        return this.Error(DomainErrorHttpPolicy.FromLifecycle(result.Error));
    }
}
