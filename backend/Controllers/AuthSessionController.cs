using System.Security.Claims;
using backend.Api.Auth;
using backend.Api.Contracts;
using backend.Api.Http;
using backend.Application.Abstractions.Auth;
using backend.Api.Mapping;
using backend.Messaging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthSessionController : ControllerBase
{
    private readonly IAuthSessionService _authSessionService;
    private readonly ILogger<AuthSessionController> _logger;

    public AuthSessionController(
        IAuthSessionService authSessionService,
        ILogger<AuthSessionController> logger
    )
    {
        _authSessionService = authSessionService;
        _logger = logger;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(AuthSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Me()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return NoContent();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var parsedUserId))
        {
            _logger.LogWarning(AppMessages.Logs.AuthSessionMissingClaim);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return NoContent();
        }

        var session = await _authSessionService.GetSessionAsync(parsedUserId, HttpContext.RequestAborted);
        if (session is null)
        {
            _logger.LogWarning(AppMessages.Logs.AuthSessionUserGone, parsedUserId);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return NoContent();
        }

        return Ok(session.ToDto());
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Logout()
    {
        if (!IsApiClientRequest())
        {
            return this.StatusError(
                StatusCodes.Status403Forbidden,
                AppMessages.Client.LogoutRequiresApiClientHeader
            );
        }

        _logger.LogInformation(AppMessages.Logs.UserSignedOut);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    private bool IsApiClientRequest()
    {
        return Request.Headers.TryGetValue(AuthRequestHeaders.ApiClient, out var values)
            && values.Count == 1
            && string.Equals(
                values[0],
                AuthRequestHeaders.ApiClientValue,
                StringComparison.Ordinal
            );
    }
}
