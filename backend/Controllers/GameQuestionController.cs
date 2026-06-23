using backend.Api.Contracts;
using backend.Api.Http;
using backend.Api.Mapping;
using backend.Application.Abstractions;
using backend.Application.Abstractions.Auth;
using backend.Application.Contracts;
using backend.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace backend.Controllers;

[ApiController]
[Route("api/game/questions")]
[Authorize]
public sealed class GameQuestionController : ControllerBase
{
    private static readonly JsonSerializerOptions ImportJsonOptions = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

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

    [HttpGet("import-template")]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DownloadImportTemplate(CancellationToken cancellationToken)
    {
        var categories = await _gameQuestionService.GetCategoriesAsync(cancellationToken);
        var content = BuildImportTemplate(categories);
        return File(
            Encoding.UTF8.GetBytes(content),
            "text/plain; charset=utf-8",
            "question-import-template.jsonc"
        );
    }

    [HttpPost("import")]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(typeof(ImportGameQuestionsResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ImportQuestions(
        IFormFile? file,
        CancellationToken cancellationToken
    )
    {
        if (file is null || file.Length == 0)
        {
            return this.BadRequestError(
                AppMessages.Client.GameQuestionInvalidRequest,
                AppMessages.ErrorCodes.GameQuestionInvalidRequest
            );
        }

        ImportGameQuestionsDocumentDto? document;
        try
        {
            await using var stream = file.OpenReadStream();
            document = await JsonSerializer.DeserializeAsync<ImportGameQuestionsDocumentDto>(
                stream,
                ImportJsonOptions,
                cancellationToken
            );
        }
        catch (JsonException)
        {
            return this.BadRequestError(
                "The import file is not valid JSON/JSONC.",
                AppMessages.ErrorCodes.GameQuestionInvalidRequest
            );
        }

        if (document?.Questions is null)
        {
            return this.BadRequestError(
                "The import file must contain a 'questions' array.",
                AppMessages.ErrorCodes.GameQuestionInvalidRequest
            );
        }

        var inputs = new List<CreateGameQuestionInput>(document.Questions.Count);
        for (var index = 0; index < document.Questions.Count; index++)
        {
            var question = document.Questions[index];
            if (question is null || !Guid.TryParse(question.CategoryId, out var categoryId))
            {
                return this.BadRequestError(
                    $"Question #{index + 1} contains an invalid categoryId.",
                    AppMessages.ErrorCodes.GameQuestionInvalidRequest
                );
            }

            inputs.Add(question.ToInput(categoryId));
        }

        var result = await _gameQuestionService.ImportQuestionsAsync(inputs, cancellationToken);
        return result.Outcome switch
        {
            ImportGameQuestionsOutcome.Imported => Ok(
                new ImportGameQuestionsResultDto(result.ImportedCount)
            ),
            ImportGameQuestionsOutcome.CategoryNotFound => this.NotFoundError(
                result.ErrorMessage ?? AppMessages.Client.GameQuestionCategoryNotFound,
                AppMessages.ErrorCodes.GameQuestionCategoryNotFound
            ),
            ImportGameQuestionsOutcome.DuplicateCode => this.ConflictError(
                result.ErrorMessage ?? AppMessages.Client.GameQuestionDuplicateCode,
                AppMessages.ErrorCodes.GameQuestionDuplicateCode
            ),
            _ => this.BadRequestError(
                result.ErrorMessage ?? AppMessages.Client.GameQuestionInvalidRequest,
                AppMessages.ErrorCodes.GameQuestionInvalidRequest
            )
        };
    }

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

    private static string BuildImportTemplate(
        IReadOnlyList<backend.Application.Contracts.GameQuestionCategoryItem> categories
    )
    {
        var lines = new List<string>
        {
            "{",
            "  // Bulk import template for question catalog.",
            "  // Use categoryId values, not category names.",
            "  // priority controls question preference: lower value means higher priority.",
            "  // Available categories:",
        };

        if (categories.Count == 0)
        {
            lines.Add("  // - No categories exist yet. Create at least one category before importing.");
        }
        else
        {
            lines.AddRange(categories.Select(category => $"  // - {category.Id} ({category.Name})"));
        }

        var sampleCategoryId = categories.FirstOrDefault()?.Id.ToString()
            ?? "00000000-0000-0000-0000-000000000000";
        lines.Add("  \"questions\": [");
        lines.Add("    // Example:");
        lines.Add("    // {");
        lines.Add($"    //   \"categoryId\": \"{sampleCategoryId}\",");
        lines.Add("    //   \"text\": \"Example question text\",");
        lines.Add("    //   \"answer\": \"Example answer\",");
        lines.Add("    //   \"reward\": 100,");
        lines.Add("    //   \"isEnabled\": true,");
        lines.Add("    //   \"priority\": 0");
        lines.Add("    // }");
        lines.Add("  ]");
        lines.Add("}");
        return string.Join(Environment.NewLine, lines);
    }

    private sealed record ImportGameQuestionsDocumentDto(
        IReadOnlyList<CreateGameQuestionRequestDto>? Questions
    );
}
