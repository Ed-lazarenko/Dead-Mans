using backend.Api.Contracts;
using backend.Api.Http;
using backend.Api.Mapping;
using backend.Application.Abstractions;
using backend.Application.Abstractions.Auth;
using backend.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/game/card-runs")]
[Authorize]
public sealed class GameCardRunController : ControllerBase
{
    private readonly IGameCardRunService _service;

    public GameCardRunController(IGameCardRunService service)
    {
        _service = service;
    }

    [HttpGet("teams")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(typeof(IReadOnlyList<GameCardRunTeamOptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEligibleTeams(CancellationToken cancellationToken)
    {
        var teams = await _service.GetEligibleTeamsAsync(cancellationToken);
        return Ok(teams.Select(x => x.ToDto()).ToArray());
    }

    [HttpGet("active")]
    [ProducesResponseType(typeof(GameCardRunDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        var activeRun = await _service.GetActiveAsync(cancellationToken);
        if (activeRun is null)
        {
            return NoContent();
        }

        return Ok(activeRun.ToDto());
    }

    [HttpPost]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(typeof(GameCardRunDetailsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Start(
        [FromBody] StartGameCardRunRequestDto request,
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
                AppMessages.Client.GameCardRunInvalidRequest,
                AppMessages.ErrorCodes.GameCardRunInvalidRequest
            );
        }

        var result = await _service.StartAsync(
            request.ToInput(cellId, teamId),
            currentUserId.Value,
            cancellationToken
        );

        return result.Outcome switch
        {
            Application.Contracts.StartGameCardRunOutcome.Started when result.Run is not null =>
                StatusCode(StatusCodes.Status201Created, result.Run.ToDto()),
            Application.Contracts.StartGameCardRunOutcome.NoActiveGame => this.NotFoundError(
                AppMessages.Client.GameCardRunNoActiveGame,
                AppMessages.ErrorCodes.GameCardRunNoActiveGame
            ),
            Application.Contracts.StartGameCardRunOutcome.CellNotFound => this.NotFoundError(
                AppMessages.Client.GameCardRunCellNotFound,
                AppMessages.ErrorCodes.GameCardRunCellNotFound
            ),
            Application.Contracts.StartGameCardRunOutcome.TeamNotFound => this.NotFoundError(
                AppMessages.Client.GameCardRunTeamNotFound,
                AppMessages.ErrorCodes.GameCardRunTeamNotFound
            ),
            Application.Contracts.StartGameCardRunOutcome.CellNotOpen => this.ConflictError(
                AppMessages.Client.GameCardRunCellNotOpen,
                AppMessages.ErrorCodes.GameCardRunCellNotOpen
            ),
            Application.Contracts.StartGameCardRunOutcome.TeamNotConfirmed => this.ConflictError(
                AppMessages.Client.GameCardRunTeamNotConfirmed,
                AppMessages.ErrorCodes.GameCardRunTeamNotConfirmed
            ),
            Application.Contracts.StartGameCardRunOutcome.TeamHasNoActiveMembers => this.ConflictError(
                AppMessages.Client.GameCardRunTeamHasNoActiveMembers,
                AppMessages.ErrorCodes.GameCardRunTeamHasNoActiveMembers
            ),
            Application.Contracts.StartGameCardRunOutcome.AwaitingModifiersRequired => this.ConflictError(
                AppMessages.Client.GameCardRunAwaitingModifiersRequired,
                AppMessages.ErrorCodes.GameCardRunAwaitingModifiersRequired
            ),
            Application.Contracts.StartGameCardRunOutcome.RunAlreadyInProgress => this.ConflictError(
                AppMessages.Client.GameCardRunAlreadyInProgress,
                AppMessages.ErrorCodes.GameCardRunAlreadyInProgress
            ),
            _ => this.BadRequestError(
                AppMessages.Client.GameCardRunInvalidRequest,
                AppMessages.ErrorCodes.GameCardRunInvalidRequest
            )
        };
    }

    [HttpPost("{cardRunId:guid}/review")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(typeof(GameCardRunDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Review(Guid cardRunId, CancellationToken cancellationToken)
    {
        var currentUserId = HttpContext.TryGetUserId();
        if (!currentUserId.HasValue)
        {
            return this.BadRequestError(AppMessages.Client.AuthCookieMissingClaims);
        }

        var result = await _service.ReviewAsync(
            cardRunId,
            currentUserId.Value,
            cancellationToken
        );

        return result.Outcome switch
        {
            Application.Contracts.ReviewGameCardRunOutcome.Reviewed when result.Run is not null =>
                Ok(result.Run.ToDto()),
            Application.Contracts.ReviewGameCardRunOutcome.NotFound => this.NotFoundError(
                AppMessages.Client.GameCardRunNotFound,
                AppMessages.ErrorCodes.GameCardRunNotFound
            ),
            Application.Contracts.ReviewGameCardRunOutcome.NotInProgress => this.ConflictError(
                AppMessages.Client.GameCardRunNotInProgress,
                AppMessages.ErrorCodes.GameCardRunNotInProgress
            ),
            _ => this.BadRequestError(
                AppMessages.Client.GameCardRunInvalidRequest,
                AppMessages.ErrorCodes.GameCardRunInvalidRequest
            )
        };
    }

    [HttpPost("{cardRunId:guid}/finalize")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(typeof(GameCardRunDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Finalize(
        Guid cardRunId,
        [FromBody] FinalizeGameCardRunRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var currentUserId = HttpContext.TryGetUserId();
        if (!currentUserId.HasValue)
        {
            return this.BadRequestError(AppMessages.Client.AuthCookieMissingClaims);
        }

        if (string.IsNullOrWhiteSpace(request.Status))
        {
            return this.BadRequestError(
                AppMessages.Client.GameCardRunInvalidRequest,
                AppMessages.ErrorCodes.GameCardRunInvalidRequest
            );
        }

        if (request.KillsCount < 0 || request.BountyCount < 0)
        {
            return this.BadRequestError(
                AppMessages.Client.GameCardRunInvalidRequest,
                AppMessages.ErrorCodes.GameCardRunInvalidRequest
            );
        }

        var modifierInputs = new List<Application.Contracts.FinalizeGameCardRunModifierInput>();
        foreach (var modifier in request.ModifierResults ?? Array.Empty<FinalizeGameCardRunModifierRequestDto>())
        {
            if (!Guid.TryParse(modifier.ModifierResultId, out var modifierResultId))
            {
                return this.BadRequestError(
                    AppMessages.Client.GameCardRunInvalidRequest,
                    AppMessages.ErrorCodes.GameCardRunInvalidRequest
                );
            }

            modifierInputs.Add(modifier.ToInput(modifierResultId));
        }

        var result = await _service.FinalizeAsync(
            cardRunId,
            request.ToInput(modifierInputs),
            currentUserId.Value,
            cancellationToken
        );

        return result.Outcome switch
        {
            Application.Contracts.FinalizeGameCardRunOutcome.Completed when result.Run is not null =>
                Ok(result.Run.ToDto()),
            Application.Contracts.FinalizeGameCardRunOutcome.NotFound => this.NotFoundError(
                AppMessages.Client.GameCardRunNotFound,
                AppMessages.ErrorCodes.GameCardRunNotFound
            ),
            Application.Contracts.FinalizeGameCardRunOutcome.NotInProgress => this.ConflictError(
                AppMessages.Client.GameCardRunNotInProgress,
                AppMessages.ErrorCodes.GameCardRunNotInProgress
            ),
            Application.Contracts.FinalizeGameCardRunOutcome.ModifierResultNotFound => this.NotFoundError(
                AppMessages.Client.GameCardRunModifierResultNotFound,
                AppMessages.ErrorCodes.GameCardRunModifierResultNotFound
            ),
            _ => this.BadRequestError(
                AppMessages.Client.GameCardRunInvalidRequest,
                AppMessages.ErrorCodes.GameCardRunInvalidRequest
            )
        };
    }
}
