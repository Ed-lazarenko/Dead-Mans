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
    [HttpPost]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(typeof(GameQuestionCatalogItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateGameQuestionRequestDto? request,
        CancellationToken cancellationToken
    )
    {
        if (request is null || !Guid.TryParse(request.CategoryId, out var categoryId))
        {
            return this.BadRequestError(
                AppMessages.Client.GameQuestionInvalidRequest,
                AppMessages.ErrorCodes.GameQuestionInvalidRequest
            );
        }

        var result = await _gameQuestionService.CreateQuestionAsync(
            request.ToInput(categoryId),
            cancellationToken
        );
        return result.Outcome switch
        {
            CreateGameQuestionOutcome.Created when result.Question is not null =>
                CreatedAtAction(nameof(GetCatalog), null, result.Question.ToDto()),
            CreateGameQuestionOutcome.CategoryNotFound => this.NotFoundError(
                AppMessages.Client.GameQuestionCategoryNotFound,
                AppMessages.ErrorCodes.GameQuestionCategoryNotFound
            ),
            CreateGameQuestionOutcome.DuplicateCode => this.ConflictError(
                AppMessages.Client.GameQuestionDuplicateCode,
                AppMessages.ErrorCodes.GameQuestionDuplicateCode
            ),
            _ => this.BadRequestError(
                AppMessages.Client.GameQuestionInvalidRequest,
                AppMessages.ErrorCodes.GameQuestionInvalidRequest
            )
        };
    }

    [HttpPut("{questionId:guid}")]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(typeof(GameQuestionCatalogItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid questionId,
        [FromBody] UpdateGameQuestionRequestDto? request,
        CancellationToken cancellationToken
    )
    {
        if (request is null || !Guid.TryParse(request.CategoryId, out var categoryId))
        {
            return this.BadRequestError(
                AppMessages.Client.GameQuestionInvalidRequest,
                AppMessages.ErrorCodes.GameQuestionInvalidRequest
            );
        }

        var result = await _gameQuestionService.UpdateQuestionAsync(
            questionId,
            request.ToInput(categoryId),
            cancellationToken
        );
        return result.Outcome switch
        {
            UpdateGameQuestionOutcome.Updated when result.Question is not null =>
                Ok(result.Question.ToDto()),
            UpdateGameQuestionOutcome.CategoryNotFound => this.NotFoundError(
                AppMessages.Client.GameQuestionCategoryNotFound,
                AppMessages.ErrorCodes.GameQuestionCategoryNotFound
            ),
            UpdateGameQuestionOutcome.NotFound => this.NotFoundError(
                AppMessages.Client.GameQuestionNotFound,
                AppMessages.ErrorCodes.GameQuestionNotFound
            ),
            _ => this.BadRequestError(
                AppMessages.Client.GameQuestionInvalidRequest,
                AppMessages.ErrorCodes.GameQuestionInvalidRequest
            )
        };
    }

    [HttpPatch("{questionId:guid}/enabled")]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetQuestionEnabled(
        Guid questionId,
        [FromBody] SetGameQuestionEnabledRequestDto? request,
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

        var updated = await _gameQuestionService.SetQuestionEnabledAsync(
            questionId,
            request.IsEnabled,
            cancellationToken
        );
        if (!updated)
        {
            return this.NotFoundError(
                AppMessages.Client.GameQuestionNotFound,
                AppMessages.ErrorCodes.GameQuestionNotFound
            );
        }

        return NoContent();
    }

    [HttpDelete("{questionId:guid}")]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteQuestion(Guid questionId, CancellationToken cancellationToken)
    {
        var deleted = await _gameQuestionService.SoftDeleteQuestionAsync(questionId, cancellationToken);
        if (!deleted)
        {
            return this.NotFoundError(
                AppMessages.Client.GameQuestionNotFound,
                AppMessages.ErrorCodes.GameQuestionNotFound
            );
        }

        return NoContent();
    }
}
