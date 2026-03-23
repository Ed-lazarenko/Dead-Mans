using System.Security.Claims;
using backend.Api.Auth;
using backend.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthSessionController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IUserRoleService _userRoleService;

    public AuthSessionController(ApplicationDbContext dbContext, IUserRoleService userRoleService)
    {
        _dbContext = dbContext;
        _userRoleService = userRoleService;
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var parsedUserId))
        {
            return Unauthorized(new { error = "Auth cookie is missing required user claims." });
        }

        var user = await _dbContext.Users
            .Where(x => x.Id == parsedUserId && x.IsActive)
            .Select(x => new { x.Id, x.DisplayName })
            .FirstOrDefaultAsync();

        if (user is null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Unauthorized(new { error = "User no longer exists or is inactive." });
        }

        var roles = await _userRoleService.EnsureEffectiveRolesAsync(parsedUserId, HttpContext.RequestAborted);

        return Ok(
            new
            {
                userId = user.Id,
                displayName = user.DisplayName,
                roles
            }
        );
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}
