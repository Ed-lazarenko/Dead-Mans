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
    [HttpPost("{modifierId:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Activate(Guid modifierId, CancellationToken cancellationToken)
    {
        var currentUserId = HttpContext.TryGetUserId();
        var result = await _gameModifierService.ActivateAsync(
            modifierId,
            currentUserId,
            currentUserId,
            cancellationToken
        );

        return result.Outcome switch
        {
            ActivateGameModifierOutcome.Activated => NoContent(),
            ActivateGameModifierOutcome.NotFound => this.NotFoundError(
                AppMessages.Client.GameModifierNotFound,
                AppMessages.ErrorCodes.GameModifierNotFound
            ),
            ActivateGameModifierOutcome.GameNotActive => this.NotFoundError(
                AppMessages.Client.GameModifierGameNotActive,
                AppMessages.ErrorCodes.GameModifierGameNotActive
            ),
            ActivateGameModifierOutcome.ModifierNotEnabled => this.ConflictError(
                AppMessages.Client.GameModifierNotEnabled,
                AppMessages.ErrorCodes.GameModifierNotEnabled
            ),
            ActivateGameModifierOutcome.ModifierConflictActive => this.ConflictError(
                AppMessages.Client.GameModifierConflictActive,
                AppMessages.ErrorCodes.GameModifierConflictActive
            ),
            ActivateGameModifierOutcome.ModifierLimitReached => this.ConflictError(
                AppMessages.Client.GameModifierLimitReached,
                AppMessages.ErrorCodes.GameModifierLimitReached
            ),
            ActivateGameModifierOutcome.ModifierOrderingClosed => this.ConflictError(
                AppMessages.Client.GameModifierOrderingClosed,
                AppMessages.ErrorCodes.GameModifierOrderingClosed
            ),
            ActivateGameModifierOutcome.ActiveTeamMember => this.ConflictError(
                AppMessages.Client.GameModifierActiveTeamMember,
                AppMessages.ErrorCodes.GameModifierActiveTeamMember
            ),
            ActivateGameModifierOutcome.InsufficientQuizPoints => this.ConflictError(
                AppMessages.Client.GameModifierInsufficientQuizPoints,
                AppMessages.ErrorCodes.GameModifierInsufficientQuizPoints
            ),
            ActivateGameModifierOutcome.EmergencyDisabled => this.ConflictError(
                AppMessages.Client.GameModifierEmergencyDisabled,
                AppMessages.ErrorCodes.GameModifierEmergencyDisabled
            ),
            ActivateGameModifierOutcome.VersionBindingMissing => this.ConflictError(
                "The game modifier revision binding is missing.",
                AppMessages.ErrorCodes.GameModifierVersionBindingMissing
            ),
            ActivateGameModifierOutcome.UserNotResolved => this.BadRequestError(
                AppMessages.Client.AuthCookieMissingClaims,
                AppMessages.ErrorCodes.GameModifierUserNotResolved
            ),
            _ => this.StatusError(
                StatusCodes.Status500InternalServerError,
                AppMessages.Client.UnexpectedServerError
            )
        };
    }

    [HttpPost("{modifierId:guid}/emergency-disable")]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EmergencyDisable(
        Guid modifierId,
        [FromBody] EmergencyDisableGameModifierRequestDto? request,
        CancellationToken cancellationToken
    )
    {
        if (request is null)
        {
            return this.BadRequestError(
                AppMessages.Client.GameModifierInvalidRequest,
                AppMessages.ErrorCodes.GameModifierInvalidRequest
            );
        }

        var result = await _gameModifierService.EmergencyDisableAsync(
            modifierId,
            HttpContext.TryGetUserId(),
            request.Reason,
            cancellationToken
        );
        return result.Outcome switch
        {
            EmergencyDisableGameModifierOutcome.Disabled or
            EmergencyDisableGameModifierOutcome.AlreadyDisabled => NoContent(),
            EmergencyDisableGameModifierOutcome.GameNotActive => this.NotFoundError(
                AppMessages.Client.GameModifierGameNotActive,
                AppMessages.ErrorCodes.GameModifierGameNotActive
            ),
            EmergencyDisableGameModifierOutcome.ModifierNotEnabled => this.ConflictError(
                AppMessages.Client.GameModifierNotEnabled,
                AppMessages.ErrorCodes.GameModifierNotEnabled
            ),
            EmergencyDisableGameModifierOutcome.UserNotResolved => this.BadRequestError(
                AppMessages.Client.AuthCookieMissingClaims,
                AppMessages.ErrorCodes.GameModifierUserNotResolved
            ),
            _ => this.BadRequestError(
                AppMessages.Client.GameModifierInvalidRequest,
                AppMessages.ErrorCodes.GameModifierInvalidRequest
            )
        };
    }

    [HttpPost("admin/activate")]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AdminActivate(
        [FromBody] AdminActivateGameModifierRequestDto? request,
        CancellationToken cancellationToken
    )
    {
        if (request is null
            || !Guid.TryParse(request.ModifierId, out var modifierId)
            || !Guid.TryParse(request.TargetUserId, out var targetUserId))
        {
            return this.BadRequestError(
                AppMessages.Client.GameModifierInvalidRequest,
                AppMessages.ErrorCodes.GameModifierInvalidRequest
            );
        }

        var stateResult = await _gameModifierService.GetAdminStateAsync(targetUserId, cancellationToken);
        if (stateResult.Outcome == GetAdminGameModifierStateOutcome.PlayerNotFound)
        {
            return this.NotFoundError(
                AppMessages.Client.GameModifierPlayerNotFound,
                AppMessages.ErrorCodes.GameModifierPlayerNotFound
            );
        }

        if (stateResult.Outcome == GetAdminGameModifierStateOutcome.GameNotActive)
        {
            return this.NotFoundError(
                AppMessages.Client.GameModifierGameNotActive,
                AppMessages.ErrorCodes.GameModifierGameNotActive
            );
        }

        var currentUserId = HttpContext.TryGetUserId();
        var result = await _gameModifierService.ActivateAsync(
            modifierId,
            targetUserId,
            currentUserId,
            cancellationToken
        );

        return result.Outcome switch
        {
            ActivateGameModifierOutcome.Activated => NoContent(),
            ActivateGameModifierOutcome.NotFound => this.NotFoundError(
                AppMessages.Client.GameModifierNotFound,
                AppMessages.ErrorCodes.GameModifierNotFound
            ),
            ActivateGameModifierOutcome.GameNotActive => this.NotFoundError(
                AppMessages.Client.GameModifierGameNotActive,
                AppMessages.ErrorCodes.GameModifierGameNotActive
            ),
            ActivateGameModifierOutcome.ModifierNotEnabled => this.ConflictError(
                AppMessages.Client.GameModifierNotEnabled,
                AppMessages.ErrorCodes.GameModifierNotEnabled
            ),
            ActivateGameModifierOutcome.ModifierConflictActive => this.ConflictError(
                AppMessages.Client.GameModifierConflictActive,
                AppMessages.ErrorCodes.GameModifierConflictActive
            ),
            ActivateGameModifierOutcome.ModifierLimitReached => this.ConflictError(
                AppMessages.Client.GameModifierLimitReached,
                AppMessages.ErrorCodes.GameModifierLimitReached
            ),
            ActivateGameModifierOutcome.ModifierOrderingClosed => this.ConflictError(
                AppMessages.Client.GameModifierOrderingClosed,
                AppMessages.ErrorCodes.GameModifierOrderingClosed
            ),
            ActivateGameModifierOutcome.ActiveTeamMember => this.ConflictError(
                AppMessages.Client.GameModifierActiveTeamMember,
                AppMessages.ErrorCodes.GameModifierActiveTeamMember
            ),
            ActivateGameModifierOutcome.InsufficientQuizPoints => this.ConflictError(
                AppMessages.Client.GameModifierInsufficientQuizPoints,
                AppMessages.ErrorCodes.GameModifierInsufficientQuizPoints
            ),
            ActivateGameModifierOutcome.VersionBindingMissing => this.ConflictError(
                "The game modifier revision binding is missing.",
                AppMessages.ErrorCodes.GameModifierVersionBindingMissing
            ),
            ActivateGameModifierOutcome.UserNotResolved => this.BadRequestError(
                AppMessages.Client.AuthCookieMissingClaims,
                AppMessages.ErrorCodes.GameModifierUserNotResolved
            ),
            _ => this.StatusError(
                StatusCodes.Status500InternalServerError,
                AppMessages.Client.UnexpectedServerError
            )
        };
    }

    [HttpPost("activations/{activationId:guid}/self-cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> SelfCancelActivation(
        Guid activationId,
        [FromBody] CancelGameModifierActivationRequestDto? request,
        CancellationToken cancellationToken
    )
    {
        return CancelActivationCoreAsync(
            activationId,
            request,
            isAdmin: false,
            cancellationToken
        );
    }

    [HttpPost("admin/activations/{activationId:guid}/cancel")]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CancelActivation(
        Guid activationId,
        [FromBody] CancelGameModifierActivationRequestDto? request,
        CancellationToken cancellationToken
    )
    {
        return await CancelActivationCoreAsync(
            activationId,
            request,
            isAdmin: true,
            cancellationToken
        );
    }

    private async Task<IActionResult> CancelActivationCoreAsync(
        Guid activationId,
        CancelGameModifierActivationRequestDto? request,
        bool isAdmin,
        CancellationToken cancellationToken
    )
    {
        if (request is null || request.ExpectedRoundVersion <= 0)
        {
            return this.BadRequestError(
                AppMessages.Client.GameModifierInvalidRequest,
                AppMessages.ErrorCodes.GameModifierInvalidRequest
            );
        }

        var result = await _gameModifierService.CancelActivationAsync(
            activationId,
            HttpContext.TryGetUserId(),
            request.ExpectedRoundVersion,
            isAdmin,
            request.Reason,
            User.Identity?.Name,
            cancellationToken
        );

        return result.Outcome switch
        {
            CancelGameModifierActivationOutcome.Cancelled => NoContent(),
            CancelGameModifierActivationOutcome.GameNotActive => this.NotFoundError(
                AppMessages.Client.GameModifierGameNotActive,
                AppMessages.ErrorCodes.GameModifierGameNotActive
            ),
            CancelGameModifierActivationOutcome.ActivationNotFound => this.NotFoundError(
                AppMessages.Client.GameModifierActivationNotFound,
                AppMessages.ErrorCodes.GameModifierActivationNotFound
            ),
            CancelGameModifierActivationOutcome.Forbidden => this.StatusError(
                StatusCodes.Status403Forbidden,
                AppMessages.Client.GameModifierActivationCancelForbidden,
                AppMessages.ErrorCodes.GameModifierActivationCancelForbidden
            ),
            CancelGameModifierActivationOutcome.InvalidRoundState => this.ConflictError(
                AppMessages.Client.GameModifierActivationCancelInvalidState,
                AppMessages.ErrorCodes.GameModifierActivationCancelInvalidState
            ),
            CancelGameModifierActivationOutcome.StaleVersion => this.ConflictError(
                AppMessages.Client.GameRoundStaleVersion,
                AppMessages.ErrorCodes.GameRoundStaleVersion
            ),
            CancelGameModifierActivationOutcome.ReasonRequired => this.BadRequestError(
                AppMessages.Client.GameModifierActivationCancelReasonRequired,
                AppMessages.ErrorCodes.GameModifierActivationCancelReasonRequired
            ),
            CancelGameModifierActivationOutcome.UserNotResolved => this.BadRequestError(
                AppMessages.Client.AuthCookieMissingClaims,
                AppMessages.ErrorCodes.GameModifierUserNotResolved
            ),
            _ => this.StatusError(
                StatusCodes.Status500InternalServerError,
                AppMessages.Client.UnexpectedServerError
            )
        };
    }
}
