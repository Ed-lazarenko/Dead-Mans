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

    [HttpPost]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(typeof(GameModifierDefinitionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
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
            CreateGameModifierOutcome.DuplicateCode => this.ConflictError(
                AppMessages.Client.GameModifierDuplicateCode,
                AppMessages.ErrorCodes.GameModifierDuplicateCode
            ),
            _ => this.BadRequestError(
                AppMessages.Client.GameModifierInvalidRequest,
                AppMessages.ErrorCodes.GameModifierInvalidRequest
            )
        };
    }

    [HttpPut("{modifierCode}")]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(typeof(GameModifierDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        string modifierCode,
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
            modifierCode,
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

    [HttpDelete("{modifierCode}")]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string modifierCode, CancellationToken cancellationToken)
    {
        var result = await _gameModifierService.ArchiveAsync(modifierCode, cancellationToken);
        return result.Outcome switch
        {
            DeleteGameModifierOutcome.Deleted => NoContent(),
            _ => this.NotFoundError(
                AppMessages.Client.GameModifierNotFound,
                AppMessages.ErrorCodes.GameModifierNotFound
            )
        };
    }

    [HttpPost("{modifierCode}/activate")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Activate(string modifierCode, CancellationToken cancellationToken)
    {
        var result = await _gameModifierService.ActivateAsync(
            modifierCode,
            HttpContext.TryGetUserId(),
            cancellationToken
        );

        return result.Outcome switch
        {
            ActivateGameModifierOutcome.Activated => NoContent(),
            ActivateGameModifierOutcome.UnknownModifierCode => this.NotFoundError(
                AppMessages.Client.GameModifierUnknownCode,
                AppMessages.ErrorCodes.GameModifierUnknownCode
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
}
