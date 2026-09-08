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
