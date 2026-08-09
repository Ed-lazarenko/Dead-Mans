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
[Route("api/game/rounds")]
[Authorize]
public sealed class GameRoundController : ControllerBase
{
    private readonly IGameRoundService _service;

    public GameRoundController(IGameRoundService service)
    {
        _service = service;
    }

    [HttpGet("teams")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(typeof(IReadOnlyList<GameRoundTeamOptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEligibleTeams(CancellationToken cancellationToken)
    {
        var teams = await _service.GetEligibleTeamsAsync(cancellationToken);
        return Ok(teams.Select(x => x.ToDto()).ToArray());
    }

    [HttpGet("active")]
    [ProducesResponseType(typeof(GameRoundDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        var activeRound = await _service.GetActiveAsync(cancellationToken);
        if (activeRound is null)
        {
            return NoContent();
        }

        return Ok(activeRound.ToDto());
    }

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

    [HttpPost("{roundId:guid}/review")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(typeof(GameRoundDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Review(Guid roundId, CancellationToken cancellationToken)
    {
        var currentUserId = HttpContext.TryGetUserId();
        if (!currentUserId.HasValue)
        {
            return this.BadRequestError(AppMessages.Client.AuthCookieMissingClaims);
        }

        var result = await _service.ReviewAsync(
            roundId,
            currentUserId.Value,
            cancellationToken
        );

        return result.Outcome switch
        {
            Application.Contracts.ReviewGameRoundOutcome.Reviewed when result.Round is not null =>
                Ok(result.Round.ToDto()),
            Application.Contracts.ReviewGameRoundOutcome.NotFound => this.NotFoundError(
                AppMessages.Client.GameRoundNotFound,
                AppMessages.ErrorCodes.GameRoundNotFound
            ),
            Application.Contracts.ReviewGameRoundOutcome.NotInProgress => this.ConflictError(
                AppMessages.Client.GameRoundNotInProgress,
                AppMessages.ErrorCodes.GameRoundNotInProgress
            ),
            _ => this.BadRequestError(
                AppMessages.Client.GameRoundInvalidRequest,
                AppMessages.ErrorCodes.GameRoundInvalidRequest
            )
        };
    }

    [HttpPost("{roundId:guid}/finalize")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(typeof(GameRoundDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Finalize(
        Guid roundId,
        [FromBody] FinalizeGameRoundRequestDto request,
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
                AppMessages.Client.GameRoundInvalidRequest,
                AppMessages.ErrorCodes.GameRoundInvalidRequest
            );
        }

        if (request.KillsCount < 0 || request.BountyCount < 0)
        {
            return this.BadRequestError(
                AppMessages.Client.GameRoundInvalidRequest,
                AppMessages.ErrorCodes.GameRoundInvalidRequest
            );
        }

        var modifierInputs = new List<Application.Contracts.FinalizeGameRoundModifierInput>();
        foreach (var modifier in request.ModifierResults ?? Array.Empty<FinalizeGameRoundModifierRequestDto>())
        {
            if (!Guid.TryParse(modifier.ModifierResultId, out var modifierResultId))
            {
                return this.BadRequestError(
                    AppMessages.Client.GameRoundInvalidRequest,
                    AppMessages.ErrorCodes.GameRoundInvalidRequest
                );
            }

            modifierInputs.Add(modifier.ToInput(modifierResultId));
        }

        var result = await _service.FinalizeAsync(
            roundId,
            request.ToInput(modifierInputs),
            currentUserId.Value,
            cancellationToken
        );

        return result.Outcome switch
        {
            Application.Contracts.FinalizeGameRoundOutcome.Completed when result.Round is not null =>
                Ok(result.Round.ToDto()),
            Application.Contracts.FinalizeGameRoundOutcome.NotFound => this.NotFoundError(
                AppMessages.Client.GameRoundNotFound,
                AppMessages.ErrorCodes.GameRoundNotFound
            ),
            Application.Contracts.FinalizeGameRoundOutcome.NotInProgress => this.ConflictError(
                AppMessages.Client.GameRoundNotInProgress,
                AppMessages.ErrorCodes.GameRoundNotInProgress
            ),
            Application.Contracts.FinalizeGameRoundOutcome.ModifierResultNotFound => this.NotFoundError(
                AppMessages.Client.GameRoundModifierResultNotFound,
                AppMessages.ErrorCodes.GameRoundModifierResultNotFound
            ),
            _ => this.BadRequestError(
                AppMessages.Client.GameRoundInvalidRequest,
                AppMessages.ErrorCodes.GameRoundInvalidRequest
            )
        };
    }

    [HttpPost("{roundId:guid}/score-preview")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(typeof(GameRoundScorePreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PreviewScore(
        Guid roundId,
        [FromBody] FinalizeGameRoundRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var currentUserId = HttpContext.TryGetUserId();
        if (!currentUserId.HasValue)
        {
            return this.BadRequestError(AppMessages.Client.AuthCookieMissingClaims);
        }

        if (string.IsNullOrWhiteSpace(request.Status)
            || request.KillsCount < 0
            || request.BountyCount < 0)
        {
            return this.BadRequestError(
                AppMessages.Client.GameRoundInvalidRequest,
                AppMessages.ErrorCodes.GameRoundInvalidRequest
            );
        }

        var modifierInputs = new List<Application.Contracts.FinalizeGameRoundModifierInput>();
        foreach (var modifier in request.ModifierResults ?? Array.Empty<FinalizeGameRoundModifierRequestDto>())
        {
            if (!Guid.TryParse(modifier.ModifierResultId, out var modifierResultId))
            {
                return this.BadRequestError(
                    AppMessages.Client.GameRoundInvalidRequest,
                    AppMessages.ErrorCodes.GameRoundInvalidRequest
                );
            }

            modifierInputs.Add(modifier.ToInput(modifierResultId));
        }

        var result = await _service.PreviewScoreAsync(
            roundId,
            request.ToInput(modifierInputs),
            currentUserId.Value,
            cancellationToken
        );

        return result.Outcome switch
        {
            Application.Contracts.FinalizeGameRoundOutcome.Completed
                when result.ScoreDetails is not null =>
                Ok(result.ToDto()),
            Application.Contracts.FinalizeGameRoundOutcome.NotFound => this.NotFoundError(
                AppMessages.Client.GameRoundNotFound,
                AppMessages.ErrorCodes.GameRoundNotFound
            ),
            Application.Contracts.FinalizeGameRoundOutcome.NotInProgress => this.ConflictError(
                AppMessages.Client.GameRoundNotInProgress,
                AppMessages.ErrorCodes.GameRoundNotInProgress
            ),
            Application.Contracts.FinalizeGameRoundOutcome.ModifierResultNotFound => this.NotFoundError(
                AppMessages.Client.GameRoundModifierResultNotFound,
                AppMessages.ErrorCodes.GameRoundModifierResultNotFound
            ),
            _ => this.BadRequestError(
                AppMessages.Client.GameRoundInvalidRequest,
                AppMessages.ErrorCodes.GameRoundInvalidRequest
            )
        };
    }
}
