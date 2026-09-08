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
    public async Task<IActionResult> Review(
        Guid roundId,
        [FromBody] GameRoundVersionCommandRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var currentUserId = HttpContext.TryGetUserId();
        if (!currentUserId.HasValue)
        {
            return this.BadRequestError(AppMessages.Client.AuthCookieMissingClaims);
        }

        var result = await _service.ReviewAsync(
            roundId,
            new Application.Contracts.GameRoundVersionCommandInput(request.ExpectedRoundVersion),
            currentUserId.Value,
            cancellationToken
        );

        return MapTransitionResult(result);
    }

    [HttpPost("{roundId:guid}/prepare")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(typeof(GameRoundDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Prepare(
        Guid roundId,
        [FromBody] GameRoundVersionCommandRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var currentUserId = HttpContext.TryGetUserId();
        if (!currentUserId.HasValue)
        {
            return this.BadRequestError(AppMessages.Client.AuthCookieMissingClaims);
        }

        var result = await _service.PrepareAsync(
            roundId,
            new Application.Contracts.GameRoundVersionCommandInput(request.ExpectedRoundVersion),
            currentUserId.Value,
            cancellationToken
        );
        return MapTransitionResult(result);
    }

    [HttpPost("{roundId:guid}/rebuild")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(typeof(GameRoundDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Rebuild(
        Guid roundId,
        [FromBody] GameRoundVersionCommandRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var currentUserId = HttpContext.TryGetUserId();
        if (!currentUserId.HasValue)
        {
            return this.BadRequestError(AppMessages.Client.AuthCookieMissingClaims);
        }

        var result = await _service.RebuildAsync(
            roundId,
            new Application.Contracts.GameRoundVersionCommandInput(request.ExpectedRoundVersion),
            currentUserId.Value,
            cancellationToken
        );
        return MapTransitionResult(result);
    }

    [HttpPost("{roundId:guid}/begin-gameplay")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(typeof(GameRoundDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> BeginGameplay(
        Guid roundId,
        [FromBody] GameRoundVersionCommandRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var currentUserId = HttpContext.TryGetUserId();
        if (!currentUserId.HasValue)
        {
            return this.BadRequestError(AppMessages.Client.AuthCookieMissingClaims);
        }

        var result = await _service.BeginGameplayAsync(
            roundId,
            new Application.Contracts.GameRoundVersionCommandInput(request.ExpectedRoundVersion),
            currentUserId.Value,
            cancellationToken
        );
        return MapTransitionResult(result);
    }

    [HttpPost("{roundId:guid}/resume-gameplay")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(typeof(GameRoundDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResumeGameplay(
        Guid roundId,
        [FromBody] GameRoundVersionCommandRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var currentUserId = HttpContext.TryGetUserId();
        if (!currentUserId.HasValue)
        {
            return this.BadRequestError(AppMessages.Client.AuthCookieMissingClaims);
        }

        var result = await _service.ResumeGameplayAsync(
            roundId,
            new Application.Contracts.GameRoundVersionCommandInput(request.ExpectedRoundVersion),
            currentUserId.Value,
            cancellationToken
        );
        return MapTransitionResult(result);
    }

    [HttpPost("{roundId:guid}/technical-cancel")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(typeof(GameRoundDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> TechnicalCancel(
        Guid roundId,
        [FromBody] TechnicalCancelGameRoundRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var currentUserId = HttpContext.TryGetUserId();
        if (!currentUserId.HasValue)
        {
            return this.BadRequestError(AppMessages.Client.AuthCookieMissingClaims);
        }

        var result = await _service.TechnicalCancelAsync(
            roundId,
            new Application.Contracts.TechnicalCancelGameRoundInput(
                request.ExpectedRoundVersion,
                request.ReasonCode,
                request.PublicSummary,
                request.InternalDetail
            ),
            currentUserId.Value,
            cancellationToken
        );
        return MapTransitionResult(result);
    }

    [HttpPost("{roundId:guid}/finalize")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(typeof(GameRoundDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
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

        if (!TryMapRuleGroups(request.RuleGroups, out var ruleGroupInputs))
        {
            return this.BadRequestError(
                AppMessages.Client.GameRoundInvalidRequest,
                AppMessages.ErrorCodes.GameRoundInvalidRequest
            );
        }

        var result = await _service.FinalizeAsync(
            roundId,
            request.ToInput(modifierInputs, ruleGroupInputs),
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
            Application.Contracts.FinalizeGameRoundOutcome.StaleVersion => this.ConflictError(
                AppMessages.Client.GameRoundStaleVersion,
                AppMessages.ErrorCodes.GameRoundStaleVersion
            ),
            Application.Contracts.FinalizeGameRoundOutcome.ModifierResultNotFound => this.NotFoundError(
                AppMessages.Client.GameRoundModifierResultNotFound,
                AppMessages.ErrorCodes.GameRoundModifierResultNotFound
            ),
            Application.Contracts.FinalizeGameRoundOutcome.CalculationFailed => this.StatusError(
                StatusCodes.Status422UnprocessableEntity,
                AppMessages.Client.GameRoundModifierCalculationFailed,
                result.ErrorCode ?? AppMessages.ErrorCodes.ModifierCalculationFailed
            ),
            _ => this.BadRequestError(
                AppMessages.Client.GameRoundInvalidRequest,
                result.ErrorCode ?? AppMessages.ErrorCodes.GameRoundInvalidRequest
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
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
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

        if (!TryMapRuleGroups(request.RuleGroups, out var ruleGroupInputs))
        {
            return this.BadRequestError(
                AppMessages.Client.GameRoundInvalidRequest,
                AppMessages.ErrorCodes.GameRoundInvalidRequest
            );
        }

        var result = await _service.PreviewScoreAsync(
            roundId,
            request.ToInput(modifierInputs, ruleGroupInputs),
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
            Application.Contracts.FinalizeGameRoundOutcome.StaleVersion => this.ConflictError(
                AppMessages.Client.GameRoundStaleVersion,
                AppMessages.ErrorCodes.GameRoundStaleVersion
            ),
            Application.Contracts.FinalizeGameRoundOutcome.ModifierResultNotFound => this.NotFoundError(
                AppMessages.Client.GameRoundModifierResultNotFound,
                AppMessages.ErrorCodes.GameRoundModifierResultNotFound
            ),
            Application.Contracts.FinalizeGameRoundOutcome.CalculationFailed => this.StatusError(
                StatusCodes.Status422UnprocessableEntity,
                AppMessages.Client.GameRoundModifierCalculationFailed,
                result.ErrorCode ?? AppMessages.ErrorCodes.ModifierCalculationFailed
            ),
            _ => this.BadRequestError(
                AppMessages.Client.GameRoundInvalidRequest,
                result.ErrorCode ?? AppMessages.ErrorCodes.GameRoundInvalidRequest
            )
        };
    }

    private static bool TryMapRuleGroups(
        IReadOnlyList<FinalizeGameRoundRuleGroupRequestDto>? requests,
        out IReadOnlyList<Application.Contracts.FinalizeGameRoundRuleGroupInput> inputs
    )
    {
        var mapped = new List<Application.Contracts.FinalizeGameRoundRuleGroupInput>();
        foreach (var request in requests ?? [])
        {
            if (!Guid.TryParse(request.ResolutionGroupId, out var groupId)
                || string.IsNullOrWhiteSpace(request.OutcomeStatus))
            {
                inputs = [];
                return false;
            }

            var memberIds = new List<Guid>();
            foreach (var value in request.MemberResultIds ?? [])
            {
                if (!Guid.TryParse(value, out var memberId))
                {
                    inputs = [];
                    return false;
                }
                memberIds.Add(memberId);
            }
            mapped.Add(request.ToInput(groupId, memberIds));
        }

        inputs = mapped;
        return true;
    }

    private IActionResult MapTransitionResult(
        Application.Contracts.TransitionGameRoundResult result
    )
    {
        return result.Outcome switch
        {
            Application.Contracts.TransitionGameRoundOutcome.Transitioned
                when result.Round is not null => Ok(result.Round.ToDto()),
            Application.Contracts.TransitionGameRoundOutcome.NotFound => this.NotFoundError(
                AppMessages.Client.GameRoundNotFound,
                AppMessages.ErrorCodes.GameRoundNotFound
            ),
            Application.Contracts.TransitionGameRoundOutcome.InvalidState => this.ConflictError(
                AppMessages.Client.GameRoundNotInProgress,
                AppMessages.ErrorCodes.GameRoundNotInProgress
            ),
            Application.Contracts.TransitionGameRoundOutcome.StaleVersion => this.ConflictError(
                AppMessages.Client.GameRoundStaleVersion,
                AppMessages.ErrorCodes.GameRoundStaleVersion
            ),
            Application.Contracts.TransitionGameRoundOutcome.InvalidRequest => this.BadRequestError(
                AppMessages.Client.GameRoundInvalidRequest,
                AppMessages.ErrorCodes.GameRoundInvalidRequest
            ),
            _ => this.BadRequestError(
                AppMessages.Client.GameRoundInvalidRequest,
                AppMessages.ErrorCodes.GameRoundInvalidRequest
            )
        };
    }
}
