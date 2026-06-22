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
[Route("api/game/questions")]
[Authorize]
public sealed class GameQuestionController : ControllerBase
{
    private readonly IGameQuestionService _gameQuestionService;

    public GameQuestionController(IGameQuestionService gameQuestionService)
    {
        _gameQuestionService = gameQuestionService;
    }

    [HttpGet("catalog")]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(typeof(IReadOnlyList<GameQuestionCatalogItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCatalog(
        [FromQuery] string? category,
        [FromQuery] string? search,
        [FromQuery] bool includeDisabled = true,
        CancellationToken cancellationToken = default
    )
    {
        var catalog = await _gameQuestionService.GetCatalogAsync(
            category,
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

    [HttpPost]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(typeof(GameQuestionCatalogItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateGameQuestionRequestDto? request,
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

        var result = await _gameQuestionService.CreateQuestionAsync(request.ToInput(), cancellationToken);
        return result.Outcome switch
        {
            CreateGameQuestionOutcome.Created when result.Question is not null =>
                CreatedAtAction(nameof(GetCatalog), null, result.Question.ToDto()),
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
        if (request is null)
        {
            return this.BadRequestError(
                AppMessages.Client.GameQuestionInvalidRequest,
                AppMessages.ErrorCodes.GameQuestionInvalidRequest
            );
        }

        var result = await _gameQuestionService.UpdateQuestionAsync(
            questionId,
            request.ToInput(),
            cancellationToken
        );
        return result.Outcome switch
        {
            UpdateGameQuestionOutcome.Updated when result.Question is not null =>
                Ok(result.Question.ToDto()),
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

    [HttpPatch("categories/{category}/enabled")]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetCategoryEnabled(
        string category,
        [FromBody] SetGameQuestionCategoryEnabledRequestDto? request,
        CancellationToken cancellationToken
    )
    {
        if (request is null || string.IsNullOrWhiteSpace(category))
        {
            return this.BadRequestError(
                AppMessages.Client.GameQuestionInvalidRequest,
                AppMessages.ErrorCodes.GameQuestionInvalidRequest
            );
        }

        var updated = await _gameQuestionService.SetCategoryEnabledAsync(
            category,
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

    [HttpPost("ask-next")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(typeof(AskedGameQuestionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AskNext(CancellationToken cancellationToken)
    {
        var result = await _gameQuestionService.AskNextAsync(
            HttpContext.TryGetUserId(),
            cancellationToken
        );
        return result.Outcome switch
        {
            AskNextGameQuestionOutcome.Asked when result.AskedQuestion is not null =>
                Ok(result.AskedQuestion.ToDto()),
            AskNextGameQuestionOutcome.NoActiveGame => this.NotFoundError(
                AppMessages.Client.GameQuestionNoActiveGame,
                AppMessages.ErrorCodes.GameQuestionNoActiveGame
            ),
            AskNextGameQuestionOutcome.NoAvailableQuestions => this.NotFoundError(
                AppMessages.Client.GameQuestionNoAvailableQuestions,
                AppMessages.ErrorCodes.GameQuestionNoAvailableQuestions
            ),
            _ => this.StatusError(
                StatusCodes.Status500InternalServerError,
                AppMessages.Client.UnexpectedServerError
            )
        };
    }

    [HttpPost("rounds/{roundId:guid}/answer")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(typeof(GameQuestionRoundSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AnswerRound(
        Guid roundId,
        [FromBody] AnswerGameQuestionRequestDto? request,
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

        Guid? answeredForUserId = null;
        if (!string.IsNullOrWhiteSpace(request.AnsweredForUserId))
        {
            if (!Guid.TryParse(request.AnsweredForUserId, out var parsedAnsweredForUserId))
            {
                return this.BadRequestError(
                    AppMessages.Client.GameQuestionInvalidRequest,
                    AppMessages.ErrorCodes.GameQuestionInvalidRequest
                );
            }

            answeredForUserId = parsedAnsweredForUserId;
        }

        var result = await _gameQuestionService.AnswerRoundAsync(
            roundId,
            request.Answer,
            HttpContext.TryGetUserId(),
            answeredForUserId,
            request.AnsweredByDisplayName,
            cancellationToken
        );

        return result.Outcome switch
        {
            AnswerGameQuestionOutcome.Answered when result.Round is not null => Ok(result.Round.ToDto()),
            AnswerGameQuestionOutcome.InvalidAnswer => this.BadRequestError(
                AppMessages.Client.GameQuestionInvalidRequest,
                AppMessages.ErrorCodes.GameQuestionInvalidRequest
            ),
            AnswerGameQuestionOutcome.RoundNotFound => this.NotFoundError(
                AppMessages.Client.GameQuestionRoundNotFound,
                AppMessages.ErrorCodes.GameQuestionRoundNotFound
            ),
            AnswerGameQuestionOutcome.RoundNotPending => this.ConflictError(
                AppMessages.Client.GameQuestionRoundNotPending,
                AppMessages.ErrorCodes.GameQuestionRoundNotPending
            ),
            _ => this.StatusError(
                StatusCodes.Status500InternalServerError,
                AppMessages.Client.UnexpectedServerError
            )
        };
    }

    [HttpGet("games/{gameId:guid}/history")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(typeof(IReadOnlyList<GameQuestionRoundSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetGameHistory(Guid gameId, CancellationToken cancellationToken)
    {
        var history = await _gameQuestionService.GetGameHistoryAsync(gameId, cancellationToken);
        return Ok(history.Select(x => x.ToDto()).ToArray());
    }
}
