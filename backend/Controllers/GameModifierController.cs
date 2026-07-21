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
[Route("api/game/modifiers")]
[Authorize]
public sealed class GameModifierController : ControllerBase
{
    private readonly IGameModifierService _gameModifierService;

    public GameModifierController(IGameModifierService gameModifierService)
    {
        _gameModifierService = gameModifierService;
    }

    [HttpGet("catalog")]
    [ProducesResponseType(typeof(IReadOnlyList<GameModifierDefinitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCatalog(CancellationToken cancellationToken)
    {
        var catalog = await _gameModifierService.GetCatalogAsync(cancellationToken);
        return Ok(catalog.Select(x => x.ToDto()).ToArray());
    }

    [HttpGet("state")]
    [ProducesResponseType(typeof(GameModifierStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetState(CancellationToken cancellationToken)
    {
        var currentUserId = HttpContext.TryGetUserId();
        if (!currentUserId.HasValue)
        {
            return this.BadRequestError(AppMessages.Client.AuthCookieMissingClaims);
        }

        var state = await _gameModifierService.GetStateAsync(currentUserId.Value, cancellationToken);
        if (state is null)
        {
            return this.NotFoundError(
                AppMessages.Client.GameModifierGameNotActive,
                AppMessages.ErrorCodes.GameModifierGameNotActive
            );
        }

        return Ok(state.ToDto());
    }

    [HttpGet("admin/players")]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(typeof(IReadOnlyList<GameModifierAdminPlayerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAdminPlayers(CancellationToken cancellationToken)
    {
        var players = await _gameModifierService.GetAdminPlayersAsync(cancellationToken);
        return Ok(players.Select(x => x.ToDto()).ToArray());
    }

    [HttpGet("admin/state/{userId:guid}")]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(typeof(GameModifierStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
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

    [HttpPost]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(typeof(GameModifierDefinitionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateGameModifierRequestDto? request,
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

        var result = await _gameModifierService.CreateAsync(request.ToInput(), cancellationToken);
        return result.Outcome switch
        {
            CreateGameModifierOutcome.Created when result.Modifier is not null =>
                CreatedAtAction(nameof(GetCatalog), null, result.Modifier.ToDto()),
            _ => this.BadRequestError(
                AppMessages.Client.GameModifierInvalidRequest,
                AppMessages.ErrorCodes.GameModifierInvalidRequest
            )
        };
    }

    [HttpPut("{modifierId:guid}")]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(typeof(GameModifierDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid modifierId,
        [FromBody] UpdateGameModifierRequestDto? request,
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

        var result = await _gameModifierService.UpdateAsync(
            modifierId,
            request.ToInput(),
            cancellationToken
        );
        return result.Outcome switch
        {
            UpdateGameModifierOutcome.Updated when result.Modifier is not null =>
                Ok(result.Modifier.ToDto()),
            UpdateGameModifierOutcome.NotFound => this.NotFoundError(
                AppMessages.Client.GameModifierNotFound,
                AppMessages.ErrorCodes.GameModifierNotFound
            ),
            _ => this.BadRequestError(
                AppMessages.Client.GameModifierInvalidRequest,
                AppMessages.ErrorCodes.GameModifierInvalidRequest
            )
        };
    }

    [HttpDelete("{modifierId:guid}")]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid modifierId, CancellationToken cancellationToken)
    {
        var result = await _gameModifierService.ArchiveAsync(modifierId, cancellationToken);
        return result.Outcome switch
        {
            DeleteGameModifierOutcome.Deleted => NoContent(),
            _ => this.NotFoundError(
                AppMessages.Client.GameModifierNotFound,
                AppMessages.ErrorCodes.GameModifierNotFound
            )
        };
    }

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
        var result = await _gameModifierService.ActivateAsync(
            modifierId,
            HttpContext.TryGetUserId(),
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
            ActivateGameModifierOutcome.InsufficientQuizPoints => this.ConflictError(
                AppMessages.Client.GameModifierInsufficientQuizPoints,
                AppMessages.ErrorCodes.GameModifierInsufficientQuizPoints
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

        var result = await _gameModifierService.ActivateAsync(
            modifierId,
            targetUserId,
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
            ActivateGameModifierOutcome.InsufficientQuizPoints => this.ConflictError(
                AppMessages.Client.GameModifierInsufficientQuizPoints,
                AppMessages.ErrorCodes.GameModifierInsufficientQuizPoints
            ),
            _ => this.StatusError(
                StatusCodes.Status500InternalServerError,
                AppMessages.Client.UnexpectedServerError
            )
        };
    }

    [HttpDelete("admin/activations/{activationId:guid}")]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CancelActivation(
        Guid activationId,
        CancellationToken cancellationToken
    )
    {
        var result = await _gameModifierService.CancelActivationAsync(
            activationId,
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
            CancelGameModifierActivationOutcome.AlreadyAppliedInRound => this.ConflictError(
                AppMessages.Client.GameModifierAlreadyAppliedInRound,
                AppMessages.ErrorCodes.GameModifierAlreadyAppliedInRound
            ),
            _ => this.StatusError(
                StatusCodes.Status500InternalServerError,
                AppMessages.Client.UnexpectedServerError
            )
        };
    }
}
