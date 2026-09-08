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
    [HttpGet("catalog")]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(typeof(IReadOnlyList<GameQuestionCatalogItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCatalog(
        [FromQuery] Guid? categoryId,
        [FromQuery] string? search,
        [FromQuery] bool includeDisabled = true,
        CancellationToken cancellationToken = default
    )
    {
        var catalog = await _gameQuestionService.GetCatalogAsync(
            categoryId,
            search,
            includeDisabled,
            cancellationToken
        );
        return Ok(catalog.Select(x => x.ToDto()).ToArray());
    }

    [HttpGet("categories")]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(typeof(IReadOnlyList<GameQuestionCategoryItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken = default)
    {
        var categories = await _gameQuestionService.GetCategoriesAsync(cancellationToken);
        return Ok(categories.Select(x => x.ToDto()).ToArray());
    }

    [HttpPost("categories")]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(typeof(GameQuestionCategoryItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(GameQuestionCategoryItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateCategory(
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

        var result = await _gameQuestionService.CreateCategoryAsync(request.Name, cancellationToken);
        return result.Outcome switch
        {
            CreateGameQuestionCategoryOutcome.Created when result.Category is not null =>
                CreatedAtAction(nameof(GetCategories), null, result.Category.ToDto()),
            CreateGameQuestionCategoryOutcome.Existing when result.Category is not null =>
                Ok(result.Category.ToDto()),
            _ => this.BadRequestError(
                AppMessages.Client.GameQuestionInvalidRequest,
                AppMessages.ErrorCodes.GameQuestionInvalidRequest
            )
        };
    }
}
