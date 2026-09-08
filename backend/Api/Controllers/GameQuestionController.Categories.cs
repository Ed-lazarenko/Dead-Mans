using System.Text;
using System.Text.Json;
using backend.Api.Contracts;
using backend.Api.Http;
using backend.Api.Mapping;
using backend.Application.Abstractions;
using backend.Application.Abstractions.Auth;
using backend.Application.Contracts;
using backend.Application.Features.GameQuestions;
using backend.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

public sealed partial class GameQuestionController
{
    [HttpDelete("categories/{categoryId:guid}")]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(Guid categoryId, CancellationToken cancellationToken)
    {
        var result = await _gameQuestionService.DeleteCategoryAsync(categoryId, cancellationToken);
        return result.Outcome switch
        {
            DeleteGameQuestionCategoryOutcome.Deleted => NoContent(),
            DeleteGameQuestionCategoryOutcome.NotFound => this.NotFoundError(
                AppMessages.Client.GameQuestionCategoryNotFound,
                AppMessages.ErrorCodes.GameQuestionCategoryNotFound
            ),
            DeleteGameQuestionCategoryOutcome.NotEmpty => this.ConflictError(
                AppMessages.Client.GameQuestionCategoryNotEmpty,
                AppMessages.ErrorCodes.GameQuestionCategoryNotEmpty
            ),
            DeleteGameQuestionCategoryOutcome.Protected => this.ConflictError(
                AppMessages.Client.GameQuestionCategoryProtected,
                AppMessages.ErrorCodes.GameQuestionCategoryProtected
            ),
            _ => this.BadRequestError(
                AppMessages.Client.GameQuestionInvalidRequest,
                AppMessages.ErrorCodes.GameQuestionInvalidRequest
            )
        };
    }

    [HttpPut("categories/{categoryId:guid}")]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(typeof(GameQuestionCategoryItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCategory(
        Guid categoryId,
        [FromBody] CreateGameQuestionCategoryRequestDto? request,
        CancellationToken cancellationToken
    )
    {
        if (request is null)
        {
            return this.BadRequestError(
                AppMessages.Client.GameQuestionInvalidRequest,
                AppMessages.ErrorCodes.GameQuestionInvalidRequest
            );
        }

        var result = await _gameQuestionService.UpdateCategoryAsync(
            categoryId,
            request.Name,
            cancellationToken
        );
        return result.Outcome switch
        {
            UpdateGameQuestionCategoryOutcome.Updated when result.Category is not null =>
                Ok(result.Category.ToDto()),
            UpdateGameQuestionCategoryOutcome.NotFound => this.NotFoundError(
                AppMessages.Client.GameQuestionCategoryNotFound,
                AppMessages.ErrorCodes.GameQuestionCategoryNotFound
            ),
            UpdateGameQuestionCategoryOutcome.Protected => this.ConflictError(
                AppMessages.Client.GameQuestionCategoryProtected,
                AppMessages.ErrorCodes.GameQuestionCategoryProtected
            ),
            _ => this.BadRequestError(
                AppMessages.Client.GameQuestionInvalidRequest,
                AppMessages.ErrorCodes.GameQuestionInvalidRequest
            )
        };
    }

    [HttpPatch("categories/{categoryId:guid}/enabled")]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetCategoryEnabled(
        Guid categoryId,
        [FromBody] SetGameQuestionCategoryEnabledRequestDto? request,
        CancellationToken cancellationToken
    )
    {
        if (request is null)
        {
            return this.BadRequestError(
                AppMessages.Client.GameQuestionInvalidRequest,
                AppMessages.ErrorCodes.GameQuestionInvalidRequest
            );
        }

        var updated = await _gameQuestionService.SetCategoryEnabledAsync(
            categoryId,
            request.IsEnabled,
            cancellationToken
        );
        if (!updated)
        {
            return this.NotFoundError(
                AppMessages.Client.GameQuestionCategoryNotFound,
                AppMessages.ErrorCodes.GameQuestionCategoryNotFound
            );
        }

        return NoContent();
    }
}
