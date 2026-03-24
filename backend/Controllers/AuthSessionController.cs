using System.Security.Claims;
using backend.Api.Contracts;
using backend.Application.Features.Auth;
using backend.Api.Mapping;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthSessionController : ControllerBase
{
    private readonly IAuthSessionService _authSessionService;

    public AuthSessionController(IAuthSessionService authSessionService)
    {
        _authSessionService = authSessionService;
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(AuthSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var parsedUserId))
        {
            return Unauthorized(new ErrorResponse("Auth cookie is missing required user claims."));
        }

        var session = await _authSessionService.GetSessionAsync(parsedUserId, HttpContext.RequestAborted);
        if (session is null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Unauthorized(new ErrorResponse("User no longer exists or is inactive."));
        }

        return Ok(session.ToDto());
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}
