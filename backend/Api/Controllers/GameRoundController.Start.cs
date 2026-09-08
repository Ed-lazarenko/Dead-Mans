using backend.Api.Contracts;
using backend.Api.Http;
using backend.Api.Mapping;
using backend.Application.Abstractions;
using backend.Application.Abstractions.Auth;
using backend.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

public sealed partial class GameRoundController
{
    [HttpPost]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(typeof(GameRoundDetailsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Start(
        [FromBody] StartGameRoundRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var currentUserId = HttpContext.TryGetUserId();
        if (!currentUserId.HasValue)
        {
            return this.BadRequestError(AppMessages.Client.AuthCookieMissingClaims);
        }

        if (!Guid.TryParse(request.CellId, out var cellId) || !Guid.TryParse(request.TeamId, out var teamId))
        {
            return this.BadRequestError(
                AppMessages.Client.GameRoundInvalidRequest,
                AppMessages.ErrorCodes.GameRoundInvalidRequest
            );
        }

        var result = await _service.StartAsync(
            request.ToInput(cellId, teamId),
            currentUserId.Value,
            cancellationToken
        );

        return result.Outcome switch
        {
            Application.Contracts.StartGameRoundOutcome.Started when result.Round is not null =>
                StatusCode(StatusCodes.Status201Created, result.Round.ToDto()),
            Application.Contracts.StartGameRoundOutcome.NoActiveGame => this.NotFoundError(
                AppMessages.Client.GameRoundNoActiveGame,
                AppMessages.ErrorCodes.GameRoundNoActiveGame
            ),
            Application.Contracts.StartGameRoundOutcome.CellNotFound => this.NotFoundError(
                AppMessages.Client.GameRoundCellNotFound,
                AppMessages.ErrorCodes.GameRoundCellNotFound
            ),
            Application.Contracts.StartGameRoundOutcome.TeamNotFound => this.NotFoundError(
                AppMessages.Client.GameRoundTeamNotFound,
                AppMessages.ErrorCodes.GameRoundTeamNotFound
            ),
            Application.Contracts.StartGameRoundOutcome.CellNotOpen => this.ConflictError(
                AppMessages.Client.GameRoundCellNotOpen,
                AppMessages.ErrorCodes.GameRoundCellNotOpen
            ),
            Application.Contracts.StartGameRoundOutcome.TeamNotConfirmed => this.ConflictError(
                AppMessages.Client.GameRoundTeamNotConfirmed,
                AppMessages.ErrorCodes.GameRoundTeamNotConfirmed
            ),
            Application.Contracts.StartGameRoundOutcome.TeamHasNoActiveMembers => this.ConflictError(
                AppMessages.Client.GameRoundTeamHasNoActiveMembers,
                AppMessages.ErrorCodes.GameRoundTeamHasNoActiveMembers
            ),
            Application.Contracts.StartGameRoundOutcome.AwaitingModifiersRequired => this.ConflictError(
                AppMessages.Client.GameRoundAwaitingModifiersRequired,
                AppMessages.ErrorCodes.GameRoundAwaitingModifiersRequired
            ),
            Application.Contracts.StartGameRoundOutcome.RoundAlreadyInProgress => this.ConflictError(
                AppMessages.Client.GameRoundAlreadyInProgress,
                AppMessages.ErrorCodes.GameRoundAlreadyInProgress
            ),
            _ => this.BadRequestError(
                AppMessages.Client.GameRoundInvalidRequest,
                AppMessages.ErrorCodes.GameRoundInvalidRequest
            )
        };
    }
}
