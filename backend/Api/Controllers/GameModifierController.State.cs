using backend.Api.Contracts;
using backend.Api.Http;
using backend.Api.Mapping;
using backend.Application.Abstractions;
using backend.Application.Abstractions.Auth;
using backend.Application.Contracts;
using backend.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

public sealed partial class GameModifierController
{
    [HttpGet("state")]
    [ProducesResponseType(typeof(GameModifierStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetState(CancellationToken cancellationToken)
    {
        var currentUserId = HttpContext.TryGetUserId();
        if (!currentUserId.HasValue)
        {
            return this.BadRequestError(AppMessages.Client.AuthCookieMissingClaims);
        }

        var result = await _gameModifierService.GetStateAsync(currentUserId.Value, cancellationToken);
        return result.Outcome switch
        {
            GetGameModifierStateOutcome.Loaded when result.State is not null => Ok(result.State.ToDto()),
            GetGameModifierStateOutcome.VersionBindingMissing => this.ConflictError(
                "The active game has an incomplete modifier version binding.",
                AppMessages.ErrorCodes.GameModifierVersionBindingMissing),
            _ => NoContent()
        };
    }

    [HttpGet("admin/players")]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(typeof(GameModifierAdminPlayersResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAdminPlayers(CancellationToken cancellationToken)
    {
        var result = await _gameModifierService.GetAdminPlayersAsync(cancellationToken);
        return Ok(result.ToDto());
    }

    [HttpGet("admin/state/{userId:guid}")]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(typeof(GameModifierStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetAdminState(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _gameModifierService.GetAdminStateAsync(userId, cancellationToken);
        return result.Outcome switch
        {
            GetAdminGameModifierStateOutcome.Loaded when result.State is not null =>
                Ok(result.State.ToDto()),
            GetAdminGameModifierStateOutcome.PlayerNotFound => this.NotFoundError(
                AppMessages.Client.GameModifierPlayerNotFound,
                AppMessages.ErrorCodes.GameModifierPlayerNotFound
            ),
            GetAdminGameModifierStateOutcome.VersionBindingMissing => this.ConflictError(
                "The active game has an incomplete modifier version binding.",
                AppMessages.ErrorCodes.GameModifierVersionBindingMissing),
            _ => this.NotFoundError(
                AppMessages.Client.GameModifierGameNotActive,
                AppMessages.ErrorCodes.GameModifierGameNotActive
            )
        };
    }

    [HttpGet("admin/activations")]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(typeof(IReadOnlyList<GameModifierActivationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAdminActiveActivations(CancellationToken cancellationToken)
    {
        var result = await _gameModifierService.GetAdminActiveActivationsAsync(cancellationToken);
        if (!result.HasActiveGame)
        {
            return this.NotFoundError(
                AppMessages.Client.GameModifierGameNotActive,
                AppMessages.ErrorCodes.GameModifierGameNotActive
            );
        }

        return Ok(result.Activations.Select(x => x.ToDto()).ToArray());
    }
}
