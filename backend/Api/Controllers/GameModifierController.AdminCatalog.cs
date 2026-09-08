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

        var actor = GetActor();
        if (actor is null)
        {
            return this.BadRequestError(AppMessages.Client.AuthCookieMissingClaims);
        }
        var result = await _gameModifierService.CreateAsync(request.ToInput(), actor, cancellationToken);
        return result.Outcome switch
        {
            CreateGameModifierOutcome.Created when result.Modifier is not null =>
                CreatedAtAction(nameof(GetCatalog), null, result.Modifier.ToDto()),
            CreateGameModifierOutcome.CompatibilityLocked => this.ConflictError(
                AppMessages.Client.GameModifierContentLocked,
                AppMessages.ErrorCodes.GameModifierCompatibilityLocked
            ),
            _ => this.BadRequestError(
                AppMessages.Client.GameModifierInvalidRequest,
                AppMessages.ErrorCodes.GameModifierInvalidRequest
            )
        };
    }

    [HttpPost("preview")]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(typeof(GameModifierDraftPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Preview(
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

        var result = await _gameModifierService.PreviewCreateAsync(
            request.ToInput(),
            cancellationToken
        );
        return result.Outcome switch
        {
            PreviewGameModifierOutcome.Previewed when result.Preview is not null =>
                Ok(result.Preview.ToDto()),
            PreviewGameModifierOutcome.CalculationFailed => this.StatusError(
                StatusCodes.Status422UnprocessableEntity,
                AppMessages.Client.GameModifierPreviewCalculationFailed,
                result.ErrorCode ?? AppMessages.ErrorCodes.ModifierCalculationFailed
            ),
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

        var actor = GetActor();
        if (actor is null)
        {
            return this.BadRequestError(AppMessages.Client.AuthCookieMissingClaims);
        }
        var result = await _gameModifierService.UpdateAsync(
            modifierId,
            request.ToInput(),
            actor,
            cancellationToken
        );
        return result.Outcome switch
        {
            UpdateGameModifierOutcome.Updated or UpdateGameModifierOutcome.Unchanged when result.Modifier is not null =>
                Ok(result.Modifier.ToDto()),
            UpdateGameModifierOutcome.NotFound => this.NotFoundError(
                AppMessages.Client.GameModifierNotFound,
                AppMessages.ErrorCodes.GameModifierNotFound
            ),
            UpdateGameModifierOutcome.ContentLocked => this.ConflictError(
                AppMessages.Client.GameModifierContentLocked,
                AppMessages.ErrorCodes.GameModifierContentLocked
            ),
            UpdateGameModifierOutcome.CompatibilityLocked => this.ConflictError(
                AppMessages.Client.GameModifierContentLocked, AppMessages.ErrorCodes.GameModifierCompatibilityLocked),
            UpdateGameModifierOutcome.Stale => this.ConflictError(
                "The modifier revision is stale.", AppMessages.ErrorCodes.GameModifierRevisionStale),
            UpdateGameModifierOutcome.Archived => this.ConflictError(
                "The modifier is archived.", AppMessages.ErrorCodes.GameModifierArchived),
            UpdateGameModifierOutcome.VersionBindingMissing => this.ConflictError(
                "The modifier has no current immutable version.",
                AppMessages.ErrorCodes.GameModifierVersionBindingMissing),
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
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        Guid modifierId, [FromQuery] int expectedRevision, CancellationToken cancellationToken)
    {
        var actor = GetActor();
        if (actor is null)
        {
            return this.BadRequestError(AppMessages.Client.AuthCookieMissingClaims);
        }
        var result = await _gameModifierService.ArchiveAsync(
            modifierId, expectedRevision, actor, cancellationToken);
        return result.Outcome switch
        {
            DeleteGameModifierOutcome.Deleted => NoContent(),
            DeleteGameModifierOutcome.ContentLocked => this.ConflictError(
                AppMessages.Client.GameModifierContentLocked,
                AppMessages.ErrorCodes.GameModifierContentLocked
            ),
            DeleteGameModifierOutcome.Stale => this.ConflictError(
                "The modifier revision is stale.", AppMessages.ErrorCodes.GameModifierRevisionStale),
            DeleteGameModifierOutcome.VersionBindingMissing => this.ConflictError(
                "The modifier has no current immutable version.",
                AppMessages.ErrorCodes.GameModifierVersionBindingMissing),
            _ => this.NotFoundError(
                AppMessages.Client.GameModifierNotFound,
                AppMessages.ErrorCodes.GameModifierNotFound
            )
        };
    }

    private ModifierChangeActor? GetActor()
    {
        var userId = HttpContext.TryGetUserId();
        return userId.HasValue
            ? new ModifierChangeActor(userId.Value, User.Identity?.Name ?? userId.Value.ToString())
            : null;
    }
}
