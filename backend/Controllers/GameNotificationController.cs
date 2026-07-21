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
[Route("api/game/notifications")]
[Authorize]
public sealed class GameNotificationController : ControllerBase
{
    private readonly IGameNotificationService _notificationService;

    public GameNotificationController(IGameNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<GameUserNotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUnread(CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        if (userId is null)
        {
            return this.UnauthorizedError(AppMessages.Client.AuthenticationRequired);
        }

        var notifications = await _notificationService.GetUnreadAsync(userId.Value, cancellationToken);
        return Ok(notifications.Select(x => x.ToDto()).ToArray());
    }

    [HttpPost("read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        if (userId is null)
        {
            return this.UnauthorizedError(AppMessages.Client.AuthenticationRequired);
        }

        await _notificationService.MarkAllReadAsync(userId.Value, cancellationToken);
        return NoContent();
    }

    private Guid? RequireUserId() => HttpContext.TryGetUserId();
}
