using backend.Application.Abstractions;
using backend.Application.Abstractions.Auth;
using backend.Api.Contracts;
using backend.Api.Http;
using backend.Api.Mapping;
using backend.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/game/setup")]
[Authorize(Roles = AuthRoleCodes.Admin)]
public sealed class GameSetupController : ControllerBase
{
    private readonly IGameSetupService _gameSetupService;
    private readonly ILogger<GameSetupController> _logger;

    public GameSetupController(IGameSetupService gameSetupService, ILogger<GameSetupController> logger)
    {
        _gameSetupService = gameSetupService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(GameSetupSnapshotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var snapshot = await _gameSetupService.GetDraftSetupAsync(cancellationToken);
        if (snapshot is null)
        {
            _logger.LogInformation(AppMessages.Logs.GameSetupDraftNotFound);
            return this.NotFoundError(
                AppMessages.Client.NoDraftGameForSetup,
                AppMessages.ErrorCodes.GameSetupNoDraft
            );
        }

        return Ok(snapshot.ToSetupDto());
    }

    [HttpPost]
    [ProducesResponseType(typeof(GameSetupSnapshotDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateGameSetupRequestDto? request,
        CancellationToken cancellationToken
    )
    {
        var result = await _gameSetupService.CreateDraftSetupAsync(request?.Title ?? string.Empty, cancellationToken);
        return result.Outcome switch
        {
            CreateDraftGameSetupOutcome.Created when result.Snapshot is not null =>
                CreatedAtAction(nameof(Get), null, result.Snapshot.ToSetupDto()),
            CreateDraftGameSetupOutcome.DraftAlreadyExists => this.ConflictError(
                AppMessages.Client.DraftGameAlreadyExists,
                AppMessages.ErrorCodes.GameSetupDraftExists
            ),
            CreateDraftGameSetupOutcome.InvalidTitle => this.BadRequestError(
                AppMessages.Client.InvalidGameSetupTitle,
                AppMessages.ErrorCodes.InvalidGameSetupTitle
            ),
            _ => this.StatusError(
                StatusCodes.Status500InternalServerError,
                AppMessages.Client.UnableToCreateGameSetup
            )
        };
    }

    [HttpPut]
    [ProducesResponseType(typeof(GameSetupSnapshotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        [FromBody] UpdateGameSetupRequestDto? request,
        CancellationToken cancellationToken
    )
    {
        if (request is null)
        {
            return this.BadRequestError(
                AppMessages.Client.InvalidGameSetupSaveRequest,
                AppMessages.ErrorCodes.GameSetupInvalidSaveRequest
            );
        }

        var result = await _gameSetupService.UpdateDraftSetupAsync(
            request.ToUpdateModel(),
            cancellationToken
        );

        return result.Outcome switch
        {
            UpdateDraftGameSetupOutcome.Updated when result.Snapshot is not null =>
                Ok(result.Snapshot.ToSetupDto()),
            UpdateDraftGameSetupOutcome.NoDraftFound => this.NotFoundError(
                AppMessages.Client.NoDraftGameForSetup,
                AppMessages.ErrorCodes.GameSetupNoDraft
            ),
            UpdateDraftGameSetupOutcome.StaleVersion => this.ConflictError(
                AppMessages.Client.GameSetupDraftVersionConflict,
                AppMessages.ErrorCodes.GameSetupDraftVersionConflict
            ),
            UpdateDraftGameSetupOutcome.InvalidTitle => this.BadRequestError(
                AppMessages.Client.InvalidGameSetupTitle,
                AppMessages.ErrorCodes.InvalidGameSetupTitle
            ),
            UpdateDraftGameSetupOutcome.InvalidRowLabels
            or UpdateDraftGameSetupOutcome.InvalidColumnLabels
            or UpdateDraftGameSetupOutcome.InvalidCells
            or UpdateDraftGameSetupOutcome.InvalidEnabledModifiers
            or UpdateDraftGameSetupOutcome.InvalidEnabledQuestions =>
                this.BadRequestError(
                    AppMessages.Client.InvalidGameSetupSaveRequest,
                    AppMessages.ErrorCodes.GameSetupInvalidSaveRequest
                ),
            _ => this.StatusError(
                StatusCodes.Status500InternalServerError,
                AppMessages.Client.UnableToSaveGameSetup
            )
        };
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(CancellationToken cancellationToken)
    {
        var result = await _gameSetupService.DeleteDraftSetupAsync(cancellationToken);
        return result.Outcome switch
        {
            DeleteDraftGameSetupOutcome.Deleted => NoContent(),
            DeleteDraftGameSetupOutcome.NoDraftFound => this.NotFoundError(
                AppMessages.Client.NoDraftGameForSetup,
                AppMessages.ErrorCodes.GameSetupNoDraft
            ),
            _ => this.StatusError(
                StatusCodes.Status500InternalServerError,
                AppMessages.Client.UnableToDeleteGameSetup
            )
        };
    }
}
