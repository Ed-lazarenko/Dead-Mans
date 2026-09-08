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
    [HttpGet("catalog")]
    [ProducesResponseType(typeof(IReadOnlyList<GameModifierDefinitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCatalog(CancellationToken cancellationToken)
    {
        var catalog = await _gameModifierService.GetCatalogAsync(cancellationToken);
        return Ok(catalog.Select(x => x.ToDto()).ToArray());
    }

    [HttpGet("history")]
    [ProducesResponseType(typeof(ModifierHistoryPageDto<ModifierHistorySummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] string? search,
        [FromQuery] string status = "all",
        [FromQuery] string? cursor = null,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidHistoryPageRequest(cursor, limit))
        {
            return this.BadRequestError(AppMessages.Client.GameModifierInvalidRequest,
                AppMessages.ErrorCodes.GameModifierInvalidRequest);
        }
        var normalizedStatus = (status ?? "all").Trim().ToLowerInvariant();
        var result = await _gameModifierService.GetHistoryAsync(
            new ModifierHistoryQuery(search?.Trim(), normalizedStatus, cursor, limit),
            cancellationToken);
        return result is null
            ? this.BadRequestError(AppMessages.Client.GameModifierInvalidRequest, AppMessages.ErrorCodes.GameModifierInvalidRequest)
            : Ok(new ModifierHistoryPageDto<ModifierHistorySummaryDto>(
                result.Items.Select(x => x.ToDto()).ToArray(), result.NextCursor));
    }

    [HttpGet("{modifierId:guid}/versions")]
    [ProducesResponseType(typeof(ModifierHistoryPageDto<ModifierVersionSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVersions(
        Guid modifierId, [FromQuery] string? cursor = null, [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidHistoryPageRequest(cursor, limit))
        {
            return this.BadRequestError(AppMessages.Client.GameModifierInvalidRequest,
                AppMessages.ErrorCodes.GameModifierInvalidRequest);
        }
        var result = await _gameModifierService.GetVersionsAsync(
            modifierId, new ModifierVersionQuery(cursor, limit), cancellationToken);
        return result is null
            ? this.NotFoundError(AppMessages.Client.GameModifierNotFound, AppMessages.ErrorCodes.GameModifierNotFound)
            : Ok(new ModifierHistoryPageDto<ModifierVersionSummaryDto>(
                result.Items.Select(x => x.ToDto()).ToArray(), result.NextCursor));
    }

    [HttpGet("{modifierId:guid}/versions/{revision:int}")]
    [ProducesResponseType(typeof(ModifierVersionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVersion(
        Guid modifierId, int revision, CancellationToken cancellationToken)
    {
        var result = await _gameModifierService.GetVersionAsync(modifierId, revision, cancellationToken);
        return result is null
            ? this.NotFoundError(AppMessages.Client.GameModifierNotFound, AppMessages.ErrorCodes.GameModifierNotFound)
            : Ok(result.ToDto());
    }

    [HttpGet("{modifierId:guid}/versions/{revision:int}/games")]
    [ProducesResponseType(typeof(ModifierHistoryPageDto<ModifierVersionGameSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVersionGames(
        Guid modifierId, int revision, [FromQuery] string? cursor = null, [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidHistoryPageRequest(cursor, limit))
        {
            return this.BadRequestError(AppMessages.Client.GameModifierInvalidRequest,
                AppMessages.ErrorCodes.GameModifierInvalidRequest);
        }
        var result = await _gameModifierService.GetVersionGamesAsync(
            modifierId, revision, new ModifierVersionQuery(cursor, limit), cancellationToken);
        return result is null
            ? this.NotFoundError(AppMessages.Client.GameModifierNotFound, AppMessages.ErrorCodes.GameModifierNotFound)
            : Ok(new ModifierHistoryPageDto<ModifierVersionGameSummaryDto>(
                result.Items.Select(x => x.ToDto()).ToArray(), result.NextCursor));
    }

    private static bool IsValidHistoryPageRequest(string? cursor, int limit) =>
        limit is >= 1 and <= 100 && (cursor?.Length ?? 0) <= 512;
}
