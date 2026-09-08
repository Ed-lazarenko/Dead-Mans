using backend.Application.Abstractions;
using backend.Application.Abstractions.Auth;
using backend.Application.Features.GameSetup;
using backend.Api.Contracts;
using backend.Api.Http;
using backend.Api.Mapping;
using backend.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/game/setup/cells/{cellId:guid}/media")]
[Authorize(Roles = AuthRoleCodes.Admin)]
public sealed class GameSetupCellMediaController : ControllerBase
{
    private readonly IGameSetupCellMediaService _cellMediaService;

    public GameSetupCellMediaController(IGameSetupCellMediaService cellMediaService)
    {
        _cellMediaService = cellMediaService;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestFormLimits(MultipartBodyLengthLimit = GameSetupCellMediaLimits.MaxUploadBytes)]
    [RequestSizeLimit(GameSetupCellMediaLimits.MaxUploadBytes)]
    [ProducesResponseType(typeof(GameBoardCellMediaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Upload(
        Guid cellId,
        IFormFile? file,
        CancellationToken cancellationToken
    )
    {
        if (file is null || file.Length == 0 || file.Length > GameSetupCellMediaLimits.MaxUploadBytes)
        {
            return this.BadRequestError(
                AppMessages.Client.InvalidGameSetupCellMediaUpload,
                AppMessages.ErrorCodes.GameSetupInvalidCellMediaUpload
            );
        }

        await using var stream = file.OpenReadStream();
        var result = await _cellMediaService.UploadAsync(
            cellId,
            stream,
            file.ContentType,
            file.Length,
            cancellationToken
        );

        return result.Outcome switch
        {
            UploadDraftGameSetupCellMediaOutcome.Uploaded when result.Media is not null =>
                Ok(result.Media.ToDto()),
            UploadDraftGameSetupCellMediaOutcome.NoDraft => this.NotFoundError(
                AppMessages.Client.NoDraftGameForSetup,
                AppMessages.ErrorCodes.GameSetupNoDraft
            ),
            UploadDraftGameSetupCellMediaOutcome.CellNotFound => this.NotFoundError(
                AppMessages.Client.GameSetupCellNotFound,
                AppMessages.ErrorCodes.GameSetupCellNotFound
            ),
            UploadDraftGameSetupCellMediaOutcome.InvalidFile => this.BadRequestError(
                AppMessages.Client.InvalidGameSetupCellMediaUpload,
                AppMessages.ErrorCodes.GameSetupInvalidCellMediaUpload
            ),
            _ => this.StatusError(
                StatusCodes.Status500InternalServerError,
                AppMessages.Client.UnableToUploadGameSetupCellMedia
            ),
        };
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid cellId, CancellationToken cancellationToken)
    {
        var result = await _cellMediaService.DeleteAsync(cellId, cancellationToken);
        return result.Outcome switch
        {
            DeleteDraftGameSetupCellMediaOutcome.Deleted => NoContent(),
            DeleteDraftGameSetupCellMediaOutcome.NoDraft => this.NotFoundError(
                AppMessages.Client.NoDraftGameForSetup,
                AppMessages.ErrorCodes.GameSetupNoDraft
            ),
            DeleteDraftGameSetupCellMediaOutcome.CellNotFound => this.NotFoundError(
                AppMessages.Client.GameSetupCellNotFound,
                AppMessages.ErrorCodes.GameSetupCellNotFound
            ),
            DeleteDraftGameSetupCellMediaOutcome.MediaNotFound => this.NotFoundError(
                AppMessages.Client.GameSetupCellMediaNotFound,
                AppMessages.ErrorCodes.GameSetupCellMediaNotFound
            ),
            _ => this.StatusError(
                StatusCodes.Status500InternalServerError,
                AppMessages.Client.UnableToDeleteGameSetupCellMedia
            ),
        };
    }
}
