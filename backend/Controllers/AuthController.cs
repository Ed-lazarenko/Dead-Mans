using backend.Application.Abstractions.Auth;
using backend.Infrastructure.Auth;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace backend.Controllers;

[ApiController]
[Route("auth/twitch")]
public sealed class AuthController : ControllerBase
{
    private const string OAuthStateCookieName = "dm_twitch_oauth_state";

    private readonly ITwitchLoginService _twitchLoginService;
    private readonly TwitchAuthOptions _twitchAuthOptions;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ITwitchLoginService twitchLoginService,
        IOptions<TwitchAuthOptions> twitchAuthOptions,
        IWebHostEnvironment environment,
        ILogger<AuthController> logger
    )
    {
        _twitchLoginService = twitchLoginService;
        _twitchAuthOptions = twitchAuthOptions.Value;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet("login")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult Login()
    {
        var state = TwitchStateGenerator.Create();
        Response.Cookies.Delete(OAuthStateCookieName, BuildOAuthStateCookieOptions());
        Response.Cookies.Append(
            OAuthStateCookieName,
            state,
            BuildOAuthStateCookieOptions()
        );

        var twitchAuthorizeUrl = _twitchLoginService.BuildAuthorizeUrl(state);
        return Redirect(twitchAuthorizeUrl);
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
            _logger.LogWarning("Twitch OAuth returned error query parameter: {OAuthError}.", error);
            return Redirect(BuildFrontendRedirect("error", NormalizeFrontendReason(error)));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            _logger.LogWarning("Twitch OAuth callback missing authorization code.");
            return Redirect(BuildFrontendRedirect("error", "missing_code"));
        }

        if (string.IsNullOrWhiteSpace(state))
        {
            _logger.LogWarning("Twitch OAuth callback missing state parameter.");
            return Redirect(BuildFrontendRedirect("error", "missing_state"));
        }

        if (!Request.Cookies.TryGetValue(OAuthStateCookieName, out var stateCookie))
        {
            _logger.LogWarning("Twitch OAuth state cookie is missing.");
            return Redirect(BuildFrontendRedirect("error", "state_cookie_missing"));
        }

        Response.Cookies.Delete(OAuthStateCookieName, BuildOAuthStateCookieOptions());

        if (!string.Equals(state, stateCookie, StringComparison.Ordinal))
        {
            _logger.LogWarning("Twitch OAuth state did not match state cookie.");
            return Redirect(BuildFrontendRedirect("error", "state_mismatch"));
        }

        try
        {
            var authUser = await _twitchLoginService.AuthenticateAsync(code, cancellationToken);

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
                "User signed in via Twitch. UserId: {UserId}, IsNewUser: {IsNewUser}.",
                authUser.UserId,
                authUser.IsNewUser
            );
        }
        catch (InactiveUserLoginException ex)
        {
            _logger.LogWarning(ex, "Inactive user attempted Twitch sign-in. UserId: {UserId}.", ex.UserId);
            return Redirect(BuildFrontendRedirect("error", "account_inactive"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Twitch authentication callback failed before redirect.");
            return Redirect(BuildFrontendRedirect("error", "authentication_failed"));
        }

        return Redirect(BuildFrontendRedirect("authenticated"));
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

    private string BuildFrontendRedirect(string status, string? reason = null)
    {
        var query = new Dictionary<string, string?> { ["status"] = status };
        if (!string.IsNullOrWhiteSpace(reason))
        {
            query["reason"] = reason;
        }

        return QueryHelpers.AddQueryString(_twitchAuthOptions.FrontendRedirectUri, query);
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
