using backend.Application.Abstractions.Auth;
using backend.Messaging;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("auth/twitch")]
public sealed class AuthController : ControllerBase
{
    private const string TwitchOAuthStateCookieName = "dm_twitch_oauth_state";

    private readonly ITwitchAuthFlowService _twitchAuthFlowService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ITwitchAuthFlowService twitchAuthFlowService,
        IWebHostEnvironment environment,
        ILogger<AuthController> logger
    )
    {
        _twitchAuthFlowService = twitchAuthFlowService;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet("login")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult Login()
    {
        var challenge = _twitchAuthFlowService.BeginLogin();
        Response.Cookies.Delete(TwitchOAuthStateCookieName, BuildOAuthStateCookieOptions());
        Response.Cookies.Append(
            TwitchOAuthStateCookieName,
            challenge.State,
            BuildOAuthStateCookieOptions()
        );

        return Redirect(challenge.AuthorizeUrl);
    }

    [HttpGet("callback")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken cancellationToken
    )
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            var normalizedReason = NormalizeFrontendReason(error);
            _logger.LogWarning(AppMessages.Logs.TwitchOAuthErrorQuery, normalizedReason);
            return Redirect(_twitchAuthFlowService.BuildFrontendRedirect("error", normalizedReason));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            _logger.LogWarning(AppMessages.Logs.TwitchOAuthMissingCode);
            return Redirect(_twitchAuthFlowService.BuildFrontendRedirect("error", "missing_code"));
        }

        if (string.IsNullOrWhiteSpace(state))
        {
            _logger.LogWarning(AppMessages.Logs.TwitchOAuthMissingState);
            return Redirect(_twitchAuthFlowService.BuildFrontendRedirect("error", "missing_state"));
        }

        if (!Request.Cookies.TryGetValue(TwitchOAuthStateCookieName, out var stateCookie))
        {
            _logger.LogWarning(AppMessages.Logs.TwitchOAuthStateCookieMissing);
            return Redirect(_twitchAuthFlowService.BuildFrontendRedirect("error", "state_cookie_missing"));
        }

        Response.Cookies.Delete(TwitchOAuthStateCookieName, BuildOAuthStateCookieOptions());

        if (!string.Equals(state, stateCookie, StringComparison.Ordinal))
        {
            _logger.LogWarning(AppMessages.Logs.TwitchOAuthStateMismatch);
            return Redirect(_twitchAuthFlowService.BuildFrontendRedirect("error", "state_mismatch"));
        }

        var completion = await _twitchAuthFlowService.CompleteLoginAsync(code, cancellationToken);
        if (completion.AuthenticatedUser is null)
        {
            return Redirect(completion.FrontendRedirectUrl);
        }

        var authUser = completion.AuthenticatedUser;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, authUser.UserId.ToString()),
            new("twitch_user_id", authUser.TwitchUserId),
            new(ClaimTypes.Name, authUser.DisplayName)
        };

        var claimsIdentity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme
        );
        var principal = new ClaimsPrincipal(claimsIdentity);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            }
        );

        _logger.LogInformation(
            AppMessages.Logs.TwitchUserSignedIn,
            authUser.UserId,
            authUser.IsNewUser
        );

        return Redirect(completion.FrontendRedirectUrl);
    }

    private CookieOptions BuildOAuthStateCookieOptions()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = !_environment.IsDevelopment() || Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddMinutes(10),
            Path = "/"
        };
    }

    private static string NormalizeFrontendReason(string reason)
    {
        return reason switch
        {
            "access_denied" => reason,
            _ => "unknown"
        };
    }
}
