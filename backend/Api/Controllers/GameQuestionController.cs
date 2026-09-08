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
    public async Task<IActionResult> DownloadImportTemplate(
        [FromQuery] string? locale,
        CancellationToken cancellationToken
    )
    {
        var categories = await _gameQuestionService.GetCategoriesAsync(cancellationToken);
        var content = BuildImportTemplate(categories, locale);
        return File(
            Encoding.UTF8.GetBytes(content),
            "text/plain; charset=utf-8",
            "question-import-template.jsonc"
        );
    }

    [HttpPost("import")]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [RequestFormLimits(MultipartBodyLengthLimit = GameQuestionImportLimits.MaxUploadBytes)]
    [RequestSizeLimit(GameQuestionImportLimits.MaxUploadBytes)]
    [ProducesResponseType(typeof(ImportGameQuestionsResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ImportQuestions(
        IFormFile? file,
        CancellationToken cancellationToken
    )
    {
        if (file is null || file.Length == 0 || file.Length > GameQuestionImportLimits.MaxUploadBytes)
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

        var fallbackCategory = await _gameQuestionService.EnsureFallbackCategoryAsync(
            cancellationToken
        );
        var categories = await _gameQuestionService.GetCategoriesAsync(cancellationToken);
        var knownCategoryIds = categories
            .Select(category => category.Id)
            .Append(fallbackCategory.Id)
            .ToHashSet();

        var inputs = new List<ImportGameQuestionInput>(document.Questions.Count);
        for (var index = 0; index < document.Questions.Count; index++)
        {
            var question = document.Questions[index];
            Guid categoryId;
            if (question is null || string.IsNullOrWhiteSpace(question.CategoryId))
            {
                categoryId = fallbackCategory.Id;
            }
            else if (!Guid.TryParse(question.CategoryId, out var parsedCategoryId)
                || !knownCategoryIds.Contains(parsedCategoryId))
            {
                categoryId = fallbackCategory.Id;
            }
            else
            {
                categoryId = parsedCategoryId;
            }

            inputs.Add(
                (question ?? new ImportGameQuestionRequestDto(null, null, null)).ToInput(
                    index + 1,
                    categoryId
                )
            );
        }

        var result = await _gameQuestionService.ImportQuestionsAsync(inputs, cancellationToken);
        return Ok(
            new ImportGameQuestionsResultDto(
                result.ImportedCount,
                (result.SkippedQuestions ?? Array.Empty<ImportGameQuestionSkippedItem>())
                    .Select(item => item.ToDto())
                    .ToArray()
            )
        );
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

    private static string BuildImportTemplate(
        IReadOnlyList<backend.Application.Contracts.GameQuestionCategoryItem> categories,
        string? locale
    )
    {
        var useRussian = locale?.StartsWith("ru", StringComparison.OrdinalIgnoreCase) == true;
        var lines = new List<string>();

        if (useRussian)
        {
            lines.AddRange(
                [
                    "{",
                    "  // Шаблон JSONC для массового импорта вопросов.",
                    "  // Обязательные поля у вопроса: text, answer, reward.",
                    $"  // Если categoryId не указан, вопрос попадёт в категорию \"{QuestionCatalogDefaults.UncategorizedCategoryName}\".",
                    "  // Если isEnabled не указан, вопрос будет загружен выключенным.",
                    "  // Если priority не указан, будет использовано значение 0.",
                    "  // В игре сначала выбираются вопросы с наименьшим числом показов (AskedTotalCount).",
                    "  // Если таких несколько, берутся вопросы с наибольшим priority.",
                    "  // Если и после этого кандидатов несколько, один из них выбирается случайно.",
                    "  //",
                    "  // Описание полей:",
                    "  // - categoryId: необязательный Guid категории. Список доступных Guid указан ниже.",
                    "  // - text: текст вопроса, который увидит ведущий или игрок.",
                    "  // - answer: правильный ответ на вопрос.",
                    "  // - reward: количество очков за правильный ответ.",
                    "  // - isEnabled: станет ли вопрос доступен для выбора в играх сразу после импорта.",
                    "  // - priority: относительный приоритет вопроса (0 - значение по умолчанию). Чем выше значение, тем выше шанс,",
                    "  //   что будет выбран именно этот вопрос среди одинаково редко задаваемых вопросов.",
                    "  //",
                    "  // Доступные категории:",
                ]
            );
        }
        else
        {
            lines.AddRange(
                [
                    "{",
                    "  // JSONC template for bulk question import.",
                    "  // Required fields for each question: text, answer, reward.",
                    $"  // If categoryId is omitted, the question is assigned to \"{QuestionCatalogDefaults.UncategorizedCategoryName}\".",
                    "  // If isEnabled is omitted, the question is imported as disabled.",
                    "  // If priority is omitted, the default value is 0.",
                    "  // In gameplay, the system first considers the least-used questions (AskedTotalCount).",
                    "  // If several questions tie, the ones with the highest priority are preferred.",
                    "  // If several candidates still remain, one of them is chosen at random.",
                    "  //",
                    "  // Field guide:",
                    "  // - categoryId: optional category Guid. The available Guid values are listed below.",
                    "  // - text: question text shown to the host or players.",
                    "  // - answer: the correct answer for the question.",
                    "  // - reward: points awarded for a correct answer.",
                    "  // - isEnabled: whether the question becomes selectable for games immediately after import.",
                    "  // - priority: relative question priority (0 is the default value). Higher values make the question more likely",
                    "  //   to be chosen among questions that have been asked equally often.",
                    "  //",
                    "  // Available categories:",
                ]
            );
        }

        if (categories.Count == 0)
        {
            lines.Add(
                useRussian
                    ? "  // - Категорий пока нет. Перед импортом будет создана системная категория по умолчанию."
                    : "  // - No categories exist yet. The system fallback category will be created automatically."
            );
        }
        else
        {
            lines.AddRange(categories.Select(category => $"  // - {category.Id} ({category.Name})"));
        }

        lines.Add(useRussian ? "  // Пример:" : "  // Example:");
        lines.Add("  \"questions\": [");
        lines.Add("    {");
        lines.Add(
            useRussian
                ? $"      \"categoryId\": \"{QuestionCatalogDefaults.UncategorizedCategoryId}\","
                : $"      \"categoryId\": \"{QuestionCatalogDefaults.UncategorizedCategoryId}\","
        );
        lines.Add(
            useRussian
                ? "      \"text\": \"Какой ник у стримера?\","
                : "      \"text\": \"What is the streamer's nickname?\","
        );
        lines.Add(
            useRussian
                ? "      \"answer\": \"GlobalMentor\","
                : "      \"answer\": \"GlobalMentor\","
        );
        lines.Add(
            useRussian
                ? "      \"reward\": 1,"
                : "      \"reward\": 1,"
        );
        lines.Add(
            useRussian
                ? "      \"isEnabled\": true,"
                : "      \"isEnabled\": true,"
        );
        lines.Add("      \"priority\": 0");
        lines.Add("    }");
        lines.Add("  ]");
        lines.Add("}");
        return string.Join(Environment.NewLine, lines);
    }

    private sealed record ImportGameQuestionsDocumentDto(
        IReadOnlyList<ImportGameQuestionRequestDto>? Questions
    );
}
